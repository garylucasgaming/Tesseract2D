using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GISM.Core.Attributes;

namespace GISM.Core.Serializer
{
    public class GISMSerializerOptions
    {
        public bool IsExplicit { get; set; } = false;
        // REMOVED: IncludePrivateFields flag is completely gone
    }

    public class GISMSerializer
    {
        private const string IndentString = "    ";
        private readonly GISMSerializerOptions _options;
        private readonly Type _ignoreAttributeType = typeof(GISMIgnore);

        private readonly Dictionary<object, string> _objectToId = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
        private readonly HashSet<object> _fullySerializedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
        private readonly HashSet<object> _objectsThatNeedAnchors = new HashSet<object>(ReferenceEqualityComparer.Instance);
        private int _referenceCounter = 0;

        public GISMSerializer() => _options = new GISMSerializerOptions();
        public GISMSerializer(GISMSerializerOptions options) => _options = options ?? new GISMSerializerOptions();

        public string Serialize(object rootObject)
        {
            if(rootObject == null)
                return string.Empty;

            _objectToId.Clear();
            _fullySerializedObjects.Clear();
            _objectsThatNeedAnchors.Clear();
            _referenceCounter = 0;

            // First Pass: Scan graph for cross-references
            DetermineReferences(rootObject, new HashSet<object>(ReferenceEqualityComparer.Instance));

            StringBuilder sb = new StringBuilder();

            // If the root is a collection, let's unpack its elements directly at the root level
            if(typeof(IEnumerable).IsAssignableFrom(rootObject.GetType()) && rootObject.GetType() != typeof(string))
            {
                foreach(object item in (IEnumerable) rootObject)
                {
                    if(item == null)
                        continue;
                    SerializeObject(item, 0, sb);
                }
            }
            else
            {
                SerializeObject(rootObject, 0, sb);
            }

            return sb.ToString();
        }

        private void DetermineReferences(object obj, HashSet<object> visitedInBranch)
        {
            if(obj == null || IsPrimitiveOrSimple(obj.GetType()))
                return;

            if(_objectToId.ContainsKey(obj))
            {
                _objectsThatNeedAnchors.Add(obj);
                return;
            }

            _referenceCounter++;
            _objectToId[obj] = $"id_{_referenceCounter}";

            Type type = obj.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if(!typeof(IEnumerable).IsAssignableFrom(type) || type == typeof(string))
            {
                foreach(FieldInfo field in type.GetFields(flags))
                {
                    // Filter: Skip manually ignored elements
                    if(field.IsDefined(_ignoreAttributeType, inherit: true))
                        continue;

                    // FIX: If this is an automatic backing field, check if its parent property has [GISMIgnore]
                    if(field.Name.StartsWith("<") && field.Name.Contains("BackingField"))
                    {
                        string propName = field.Name.Substring(1, field.Name.IndexOf('>') - 1);
                        PropertyInfo prop = type.GetProperty(propName, flags);
                        if(prop != null && prop.IsDefined(_ignoreAttributeType, inherit: true))
                            continue;
                    }

                    if(field.Name.StartsWith("<") && !field.Name.Contains("BackingField"))
                        continue;

                    object val = field.GetValue(obj);
                    if(val != null)
                        DetermineReferences(val, visitedInBranch);
                }
            }

            // Scan actual elements
            if(typeof(IDictionary).IsAssignableFrom(type))
            {
                foreach(DictionaryEntry entry in (IDictionary) obj)
                {
                    if(entry.Value != null)
                        DetermineReferences(entry.Value, visitedInBranch);
                }
            }
            else if(typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                foreach(object item in (IEnumerable) obj)
                {
                    if(item != null)
                        DetermineReferences(item, visitedInBranch);
                }
            }
        }

        private void SerializeObject(object obj, int indentLevel, StringBuilder sb)
        {
            if(obj == null)
                return;

            string currentIndent = GetIndent(indentLevel);
            Type type = obj.GetType();

            string cleanTypeName = GetCleanTypeName(type);
            string typeDeclaration = _options.IsExplicit ? $"<{cleanTypeName}>" : cleanTypeName;

            if(_objectsThatNeedAnchors.Contains(obj) && _objectToId.TryGetValue(obj, out string refId))
            {
                sb.AppendLine($"{currentIndent}- {typeDeclaration} REF(\"{refId}\")");
            }
            else
            {
                sb.AppendLine($"{currentIndent}- {typeDeclaration}");
            }

            _fullySerializedObjects.Add(obj);

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // 1. Process Fields
            foreach(FieldInfo field in type.GetFields(bindingFlags))
            {
                // Filter: Skip fields marked with [GISMIgnore]
                if(field.IsDefined(_ignoreAttributeType, inherit: true))
                    continue;

                // FIX: If this is an automatic backing field, check if its parent property has [GISMIgnore]
                if(field.Name.StartsWith("<") && field.Name.Contains("BackingField"))
                {
                    string propName = field.Name.Substring(1, field.Name.IndexOf('>') - 1);
                    PropertyInfo prop = type.GetProperty(propName, bindingFlags);
                    if(prop != null && prop.IsDefined(_ignoreAttributeType, inherit: true))
                        continue;
                }

                if(field.FieldType.BaseType == typeof(MulticastDelegate))
                    continue;
                if(field.Name.StartsWith("<") && !field.Name.Contains("BackingField"))
                    continue;

                string cleanFieldName = field.Name;
                if(cleanFieldName.StartsWith("<") && cleanFieldName.Contains(">"))
                {
                    cleanFieldName = cleanFieldName.Substring(1, cleanFieldName.IndexOf('>') - 1);
                }

                SerializeMember(cleanFieldName, field.FieldType, field.GetValue(obj), indentLevel, sb);
            }

            // 2. Process Properties
            foreach(PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Filter: Skip properties marked with [GISMIgnore]
                if(prop.IsDefined(_ignoreAttributeType, inherit: true))
                    continue;

                if(!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;

                bool isCollection = typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string);
                if(!prop.CanWrite && !isCollection)
                    continue;

                if(type.GetField($"<{prop.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance) != null)
                    continue;

                SerializeMember(prop.Name, prop.PropertyType, prop.GetValue(obj), indentLevel, sb);
            }
        }

        private void SerializeMember(string memberName, Type memberType, object value, int indentLevel, StringBuilder sb)
        {
            string currentIndent = GetIndent(indentLevel);

            if(value == null)
            {
                sb.AppendLine($"{currentIndent}{IndentString}{memberName} = null");
                return;
            }

            Type actualType = value.GetType();

            if(!IsPrimitiveOrSimple(actualType) && _fullySerializedObjects.Contains(value))
            {
                string refId = _objectToId[value];
                sb.AppendLine($"{currentIndent}{IndentString}{memberName} = REF(\"{refId}\")");
                return;
            }

            if(IsPrimitiveOrSimple(actualType))
            {
                string castTag = _options.IsExplicit ? $" <{GetCleanTypeName(memberType)}>" : "";
                sb.AppendLine($"{currentIndent}{IndentString}{memberName}{castTag} = {FormatSimpleValue(value)}");
            }
            else if(typeof(IDictionary).IsAssignableFrom(actualType))
            {
                sb.AppendLine($"{currentIndent}{IndentString}{memberName} = {{");
                IDictionary dict = (IDictionary) value;
                foreach(DictionaryEntry entry in dict)
                {
                    string keyStr = entry.Key is Type t ? GetCleanTypeName(t) : (entry.Key is string ? $"\"{entry.Key}\"" : entry.Key.ToString());

                    if(entry.Value != null && !IsPrimitiveOrSimple(entry.Value.GetType()))
                    {
                        if(_fullySerializedObjects.Contains(entry.Value))
                        {
                            string dictRefId = _objectToId[entry.Value];
                            sb.AppendLine($"{currentIndent}{IndentString}{IndentString}{keyStr} = REF(\"{dictRefId}\")");
                        }
                        else
                        {
                            sb.AppendLine($"{currentIndent}{IndentString}{IndentString}{keyStr} = ");
                            SerializeObject(entry.Value, indentLevel + 3, sb);
                        }
                    }
                    // Fixed: Ensured bare comparison characters aren't processed in body text context outside fences
                    else
                    {
                        sb.AppendLine($"{currentIndent}{IndentString}{IndentString}{keyStr} = {FormatSimpleValue(entry.Value)}");
                    }
                }
                sb.AppendLine($"{currentIndent}{IndentString}}}");
            }
            else if(typeof(IEnumerable).IsAssignableFrom(actualType) && actualType != typeof(string))
            {
                sb.AppendLine($"{currentIndent}{IndentString}{memberName} = [");
                foreach(object item in (IEnumerable) value)
                {
                    if(item == null)
                        continue;
                    if(IsPrimitiveOrSimple(item.GetType()))
                    {
                        sb.AppendLine($"{currentIndent}{IndentString}{IndentString}- {FormatSimpleValue(item)}");
                    }
                    else
                    {
                        if(_fullySerializedObjects.Contains(item))
                        {
                            string listRefId = _objectToId[item];
                            sb.AppendLine($"{currentIndent}{IndentString}{IndentString}- REF(\"{listRefId}\")");
                        }
                        else
                        {
                            SerializeObject(item, indentLevel + 2, sb);
                        }
                    }
                }
                sb.AppendLine($"{currentIndent}{IndentString}]");
            }
            else
            {
                sb.AppendLine($"{currentIndent}{IndentString}{memberName} = ");
                SerializeObject(value, indentLevel + 2, sb);
            }
        }

        private string GetCleanTypeName(Type type)
        {
            if(!type.IsGenericType)
                return type.Name;

            string name = type.Name.Split('`')[0];
            var args = new List<string>();
            foreach(var arg in type.GetGenericArguments())
            {
                args.Add(GetCleanTypeName(arg));
            }
            return $"{name}<{string.Join(", ", args)}>";
        }

        private bool IsPrimitiveOrSimple(Type type) =>
            type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(decimal);

        private string FormatSimpleValue(object value)
        {
            if(value is bool b)
                return b ? "true" : "false";
            if(value is string s)
                return $"\"{s}\"";
            if(value is Guid g)
                return $"\"{g}\"";
            return value?.ToString() ?? "null";
        }

        private string GetIndent(int count) => new string(' ', count * 4);
    }
}