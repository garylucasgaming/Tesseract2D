using GISM.Core.Parser;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GISM.Core.Parser
{
    /// <summary>
    /// Handles the multi-pass rehydration of a compiled GISM AST back into live C# objects.
    /// </summary>
    public class GISMDeserializer
    {
        private readonly GISMParserSettings _settings;
        private readonly Dictionary<string, object> _objectTracker = new Dictionary<string, object>();
        private readonly List<object> _rootInstances = new List<object>();

        public GISMDeserializer(GISMParserSettings settings)
        {
            _settings = settings ?? new GISMParserSettings();
        }

        /// <summary>
        /// Orchestrates tokenization, parsing, and multi-pass object rehydration.
        /// </summary>
        public GISMResult Deserialize(string rawGism)
        {
            _objectTracker.Clear();
            _rootInstances.Clear();

            // Step 1: Tokenize
            var tokenizer = new Lexer(rawGism);
            List<Token> tokens = tokenizer.Tokenize();

            // Step 2: Parse to AST
            var parser = new GParser(tokens, _settings);
            FileRootNode rootAst = parser.Parse();

            // Step 3: Pass 1 - Instantiate everything to build out our reference addresses
            foreach(var objNode in rootAst.RootObjects)
            {
                var instance = InstantiateObjectTree(objNode, parser);
                if(instance != null)
                {
                    _rootInstances.Add(instance);
                }
            }

            // Step 4: Pass 2 - Populate properties and safely wire reference pointers/loops
            foreach(var objNode in rootAst.RootObjects)
            {
                PopulateObjectTree(objNode, parser);
            }

            return new GISMResult(_rootInstances, _objectTracker);
        }

        private object InstantiateObjectTree(ObjectNode node, GParser parser)
        {
            Type resolvedType = parser.ResolveType(node.TypeName);
            if(resolvedType == null)
                return null;

            object liveInstance = Activator.CreateInstance(resolvedType);

            // Ensure every object block has a tracking address key
            string trackingKey = !string.IsNullOrEmpty(node.ReferenceId) ? node.ReferenceId : Guid.NewGuid().ToString();
            node.ReferenceId = trackingKey;

            _objectTracker[trackingKey] = liveInstance;

            // Deep-scan properties for inline complex objects needing early instantiation
            foreach(var prop in node.Properties)
            {
                FindNestedObjectsToInstantiate(prop.Value, parser);
            }

            return liveInstance;
        }

        private void FindNestedObjectsToInstantiate(ASTNode valueNode, GParser parser)
        {
            if(valueNode is ObjectNode childObj)
            {
                InstantiateObjectTree(childObj, parser);
            }
            else if(valueNode is ListNode listNode)
            {
                foreach(var el in listNode.Elements)
                {
                    FindNestedObjectsToInstantiate(el, parser);
                }
            }
            else if(valueNode is DictionaryNode dictNode)
            {
                foreach(var prop in dictNode.Entries)
                {
                    FindNestedObjectsToInstantiate(prop.Value, parser);
                }
            }
        }

        private void PopulateObjectTree(ObjectNode node, GParser parser)
        {
            if(!_objectTracker.TryGetValue(node.ReferenceId, out object liveInstance))
                return;

            Type type = liveInstance.GetType();

            foreach(var propNode in node.Properties)
            {
                var propInfo = type.GetProperty(propNode.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                var fieldInfo = type.GetField(propNode.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                if(propInfo == null && fieldInfo == null)
                    continue;

                Type targetType = propInfo != null ? propInfo.PropertyType : fieldInfo.FieldType;
                object assignedValue = ResolveValueNode(propNode.Value, targetType, parser);

                if(propInfo != null && propInfo.CanWrite)
                {
                    propInfo.SetValue(liveInstance, assignedValue);
                }
                else if(fieldInfo != null)
                {
                    fieldInfo.SetValue(liveInstance, assignedValue);
                }
            }
        }

        private object ResolveValueNode(ASTNode node, Type targetType, GParser parser)
        {
            if(node == null)
                return null;

            if(node is ReferenceNode refNode)
            {
                if(_objectTracker.TryGetValue(refNode.Id, out object referencedObj))
                {
                    return referencedObj;
                }
                return null;
            }

            if(node is ObjectNode objNode)
            {
                PopulateObjectTree(objNode, parser);
                return _objectTracker[objNode.ReferenceId];
            }

            if(node is LiteralValueNode litNode)
            {
                return ConvertLiteral(litNode.RawValue, targetType);
            }

            if(node is ListNode listNode)
            {
                // Handles base lists / array assignments later
                return null;
            }

            return null;
        }

        private object ConvertLiteral(string rawValue, Type targetType)
        {
            if(targetType == typeof(string))
                return rawValue;
            if(string.IsNullOrWhiteSpace(rawValue))
                return null;

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if(underlyingType == typeof(Guid))
                return Guid.Parse(rawValue);
            if(underlyingType == typeof(int))
                return int.Parse(rawValue);
            if(underlyingType == typeof(float))
                return float.Parse(rawValue);
            if(underlyingType == typeof(bool))
                return bool.Parse(rawValue);

            try
            {
                return Convert.ChangeType(rawValue, underlyingType);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// A structured container returned by the deserializer for easy engine queries.
    /// </summary>
    public class GISMResult
    {
        public List<object> RootObjects
        {
            get;
        }
        public Dictionary<string, object> ObjectRegistry
        {
            get;
        }

        public GISMResult(List<object> rootObjects, Dictionary<string, object> objectRegistry)
        {
            RootObjects = rootObjects;
            ObjectRegistry = objectRegistry;
        }

        /// <summary>
        /// Yields every deserialized object in the file matching or inheriting from Type T.
        /// </summary>
        public List<T> GetObjectsOfType<T>()
        {
            var results = new List<T>();
            foreach(var val in ObjectRegistry.Values)
            {
                if(val is T matchingInstance)
                {
                    results.Add(matchingInstance);
                }
            }
            return results;
        }
    }
}