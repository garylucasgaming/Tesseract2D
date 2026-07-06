using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace GISM.Core.Parser
{
    public class AssetPackage
    {
        public Dictionary<string, object> Assets { get; set; } = new Dictionary<string, object>();

        public List<T> Get<T>() where T : class => Assets.Values.OfType<T>().ToList();

        public T GetByName<T>(string name) where T : class
        {
            if(Assets.TryGetValue(name, out var asset))
                return asset as T;
            return null;
        }
    }

    public class GISMParserSettings
    {
        public Type DefaultInferredType { get; set; } = null;
        public List<Assembly> TypeAssemblies { get; set; } = new List<Assembly>();
    }

    public class GISMParser
    {
        private readonly GISMParserSettings _settings;
        private readonly Dictionary<string, object> _referenceTracker = new Dictionary<string, object>();

        public GISMParser(GISMParserSettings settings = null)
        {
            _settings = settings ?? new GISMParserSettings();
            if(!_settings.TypeAssemblies.Contains(Assembly.GetExecutingAssembly()))
            {
                _settings.TypeAssemblies.Add(Assembly.GetExecutingAssembly());
            }
        }

        public AssetPackage ParseManifest(string gismContent)
        {
            _referenceTracker.Clear();
            var package = new AssetPackage();
            var stack = new Stack<ParsingContext>();
            ParsingContext currentContext = null;

            int fileLineNumber = 0;

            using(var reader = new StringReader(gismContent))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    fileLineNumber++;

                    try
                    {
                        string cleanLine = StripComments(line);
                        if(string.IsNullOrWhiteSpace(cleanLine))
                            continue;

                        int currentIndent = line.Length - line.TrimStart().Length;
                        string trimmedLine = cleanLine.Trim();

                        // Unwind scope stack when indentation decreases
                        while(stack.Count > 0 && stack.Peek().Indent >= currentIndent)
                        {
                            stack.Pop();
                            currentContext = stack.Count > 0 ? stack.Peek() : null;
                        }

                        // Structural block closers
                        if(trimmedLine == "]" || trimmedLine == "}")
                        {
                            if(stack.Count > 0)
                            {
                                stack.Pop();
                                currentContext = stack.Count > 0 ? stack.Peek() : null;
                            }
                            continue;
                        }

                        // --- Object Declarations OR List Objects (- Something) ---
                        if(trimmedLine.StartsWith("-"))
                        {
                            string declarationText = trimmedLine.Substring(1).Trim();
                            object objInstance = null;

                            // Case A: We are in a raw primitive list element mode (numbers, strings, guids)
                            if(currentContext != null && currentContext.InCollectionMode &&
                                (declarationText.StartsWith("\"") || !declarationText.Contains("<") && FindTypeInAssemblies(declarationText) == null))
                            {
                                Type itemType = currentContext.CollectionContext.GetType().GetGenericArguments()[0];
                                objInstance = ResolveValueString(declarationText, itemType);
                                ((IList) currentContext.CollectionContext).Add(objInstance);

                                if(objInstance == null || IsPrimitiveOrSimple(objInstance.GetType()))
                                    continue; // Primitives don't hold children, we can move to the next line safely
                            }
                            else
                            {
                                // Case B: It's a formal complex object declaration (- Name <Type>)
                                Type structuralFallback = currentContext?.ActiveProperty?.PropertyType ?? _settings.DefaultInferredType;

                                // If we are inside an object list, infer the fallback type from the list's generic argument!
                                if(currentContext != null && currentContext.InCollectionMode)
                                {
                                    structuralFallback = currentContext.CollectionContext.GetType().GetGenericArguments()[0];
                                }
                                // FIX: If we are inside a dictionary declaration block, resolve the fallback type from the value argument type constraint
                                else if(currentContext != null && currentContext.InsideDictionaryBlock)
                                {
                                    structuralFallback = currentContext.CollectionContext.GetType().GetGenericArguments()[1];
                                }

                                objInstance = CreateObjectFromDeclaration(declarationText, structuralFallback);
                            }

                            if(objInstance == null)
                                throw new InvalidOperationException($"GISM Parser Error: Could not resolve type or instantiate object from declaration '{declarationText}'");

                            // --- Dynamic Destination Routing Block ---
                            if(currentContext != null && currentContext.InCollectionMode)
                            {
                                ((IList) currentContext.CollectionContext).Add(objInstance);
                                Console.WriteLine($"[GISM DEBUG] Appending element of type {objInstance.GetType().Name} into Parent List.");
                            }
                            else if(currentContext != null && currentContext.InsideDictionaryBlock && !string.IsNullOrEmpty(currentContext.PendingDictionaryKey))
                            {
                                IDictionary dict = (IDictionary) currentContext.CollectionContext;
                                Type[] genericArgs = dict.GetType().GetGenericArguments();
                                object resolvedKey = ConvertSimpleValue(currentContext.PendingDictionaryKey, genericArgs[0]);

                                Console.WriteLine($"[GISM DEBUG] Adding DICT Entry: Key={resolvedKey} onto Type={currentContext.Instance.GetType().Name}");
                                dict[resolvedKey] = objInstance;
                                currentContext.PendingDictionaryKey = null;
                            }
                            // SMART CHECK: Intercept components and route them to their parent GameObject manually if the layout spacing unrolled
                            else if(currentContext != null && objInstance.GetType().Name.EndsWith("Component") && currentContext.Instance.GetType().Name == "GameObject")
                            {
                                var componentsProp = currentContext.Instance.GetType().GetProperty("Components", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                if(componentsProp != null)
                                {
                                    var compDict = componentsProp.GetValue(currentContext.Instance) as IDictionary;
                                    if(compDict != null)
                                    {
                                        compDict[objInstance.GetType()] = objInstance;
                                        Console.WriteLine($"[GISM DEBUG] Dynamically routed Component {objInstance.GetType().Name} into GameObject component storage.");
                                    }
                                }
                            }
                            else if(currentContext != null && currentContext.ActiveProperty != null)
                            {
                                Console.WriteLine($"[GISM DEBUG] Setting Property: {currentContext.ActiveProperty.Name} on {currentContext.Instance.GetType().Name} to {objInstance.GetType().Name}");
                                currentContext.ActiveProperty.SetValue(currentContext.Instance, objInstance);
                            }
                            else // Root level assets safety tracking
                            {
                                // Generate unique tracking keys so sibling GameObjects don't overwrite each other in the Asset dictionary
                                string baseKey = GetObjectName(objInstance) ?? objInstance.GetType().Name;
                                string uniqueKey = $"{baseKey}_{Guid.NewGuid().ToString().Substring(0, 8)}";

                                Console.WriteLine($"[GISM DEBUG] Registered ROOT Asset package key: {uniqueKey} [{objInstance.GetType().Name}]");
                                package.Assets[uniqueKey] = objInstance;
                            }

                            // Push context so nested fields/properties look up against this object instance
                            var objCtx = new ParsingContext(currentIndent, objInstance);
                            stack.Push(objCtx);
                            currentContext = objCtx;
                            continue;
                        }

                        // --- Property Assignments (Key = Value) ---
                        if(trimmedLine.Contains("="))
                        {
                            int equalsIdx = trimmedLine.IndexOf('=');
                            string key = trimmedLine.Substring(0, equalsIdx).Trim();
                            string rawValue = trimmedLine.Substring(equalsIdx + 1).Trim();

                            Type explicitValueTypeHint = null;
                            if(key.Contains("<") && key.Contains(">"))
                            {
                                int open = key.IndexOf('<');
                                int close = key.IndexOf('>');
                                string typeTag = key.Substring(open + 1, close - open - 1).Trim();
                                explicitValueTypeHint = FindTypeInAssemblies(typeTag);

                                key = key.Substring(0, open).Trim();
                            }

                            if(currentContext == null || currentContext.Instance == null)
                                continue;

                            // Case A: Inside a Dictionary Block Context
                            if(currentContext.InsideDictionaryBlock)
                            {
                                IDictionary dict = (IDictionary) currentContext.CollectionContext;
                                Type[] genericArgs = dict.GetType().GetGenericArguments();

                                if(string.IsNullOrEmpty(rawValue))
                                {
                                    currentContext.PendingDictionaryKey = key;
                                }
                                else
                                {
                                    object resolvedKey = ConvertSimpleValue(key, genericArgs[0]);
                                    object resolvedVal = ResolveValueString(rawValue, explicitValueTypeHint ?? genericArgs[1]);
                                    dict[resolvedKey] = resolvedVal;
                                }
                                continue;
                            }

                            // Case B: Standard Object Properties / Fields
                            PropertyInfo prop = currentContext.Instance.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                            Type targetConversionType = explicitValueTypeHint ?? prop?.PropertyType;

                            if(prop != null)
                            {
                                currentContext.ActiveProperty = prop;

                                if(rawValue == "[") // Collections/Lists
                                {
                                    IList listInstance = (IList) prop.GetValue(currentContext.Instance);
                                    if(listInstance == null)
                                    {
                                        listInstance = (IList) Activator.CreateInstance(prop.PropertyType);
                                        prop.SetValue(currentContext.Instance, listInstance);
                                    }

                                    var listCtx = new ParsingContext(currentIndent, currentContext.Instance)
                                    {
                                        InCollectionMode = true,
                                        CollectionContext = listInstance
                                    };
                                    stack.Push(listCtx);
                                    currentContext = listCtx;
                                    continue;
                                }
                                else if(rawValue == "{") // Dictionaries
                                {
                                    IDictionary dictInstance = (IDictionary) prop.GetValue(currentContext.Instance);
                                    if(dictInstance == null)
                                    {
                                        dictInstance = (IDictionary) Activator.CreateInstance(prop.PropertyType);
                                        prop.SetValue(currentContext.Instance, dictInstance);
                                    }

                                    var dictCtx = new ParsingContext(currentIndent, currentContext.Instance)
                                    {
                                        InsideDictionaryBlock = true,
                                        CollectionContext = dictInstance
                                    };
                                    stack.Push(dictCtx);
                                    currentContext = dictCtx;
                                    continue;
                                }
                            }

                            if(targetConversionType != null && !string.IsNullOrEmpty(rawValue))
                            {
                                object resolvedVal = ResolveValueString(rawValue, targetConversionType);

                                if(prop != null && prop.CanWrite)
                                {
                                    prop.SetValue(currentContext.Instance, resolvedVal);
                                }
                                else
                                {
                                    FieldInfo field = currentContext.Instance.GetType().GetField(
                                        key,
                                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy
                                    );
                                    if(field != null)
                                    {
                                        field.SetValue(currentContext.Instance, resolvedVal);
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"GISM Parser Error on line {fileLineNumber}: Failed while processing text: '{line.Trim()}'.\n" +
                            $"Internal Details: {ex.Message}\n" +
                            $"Parser Stack Trace:\n{ex.StackTrace}"
                        );
                    }
                }
            }

            return package;
        }

        private object CreateObjectFromDeclaration(string declaration, Type fallbackType)
        {
            string remainingText = ExtractAnchorToken(declaration, out string anchorId);
            string objectName = null;
            Type resolvedType = null;

            int openBracket = remainingText.IndexOf('<');
            int closeBracket = remainingText.IndexOf('>');

            if(openBracket != -1 && closeBracket > openBracket)
            {
                string explicitTypeName = remainingText.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();
                resolvedType = FindTypeInAssemblies(explicitTypeName);

                if(resolvedType == null)
                {
                    throw new InvalidOperationException($"GISM Parser Error: Explicit type caster '<{explicitTypeName}>' could not be resolved in any loaded assemblies.");
                }

                string before = remainingText.Substring(0, openBracket).Trim();
                string after = remainingText.Substring(closeBracket + 1).Trim();
                objectName = string.IsNullOrEmpty(before) ? (string.IsNullOrEmpty(after) ? null : after) : before;
            }
            else
            {
                string shorthandText = remainingText.Trim();
                resolvedType = FindTypeInAssemblies(shorthandText);

                if(resolvedType == null)
                {
                    resolvedType = fallbackType;
                    objectName = shorthandText;
                }
            }

            if(resolvedType == null)
            {
                throw new InvalidOperationException($"GISM Parser Error: Object declaration '{remainingText}' does not correspond to a valid type name, and no fallback type was inferred.");
            }

            object instance = Activator.CreateInstance(resolvedType);

            if(!string.IsNullOrEmpty(objectName))
            {
                PropertyInfo nameProp = resolvedType.GetProperty("Name",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if(nameProp != null && nameProp.CanWrite)
                {
                    nameProp.SetValue(instance, objectName);
                }
                else
                {
                    throw new InvalidOperationException($"GISM Parser Error: Object declaration assigned name '{objectName}' to type '{resolvedType.Name}', but no writeable 'Name' or 'name' property exists on that class.");
                }
            }

            if(!string.IsNullOrEmpty(anchorId))
                _referenceTracker[anchorId] = instance;

            return instance;
        }

        private object ResolveValueString(string rawValue, Type targetType)
        {
            if(string.IsNullOrEmpty(rawValue) || rawValue == "null")
                return null;

            if(rawValue.StartsWith("REF(\"") && rawValue.EndsWith("\")"))
            {
                int start = 5;
                int end = rawValue.LastIndexOf('"');
                string refId = rawValue.Substring(start, end - start);

                if(_referenceTracker.TryGetValue(refId, out var trackedObj))
                    return trackedObj;

                throw new InvalidOperationException($"GISM Parser Error: Reference target string '{refId}' could not be resolved.");
            }

            return ConvertSimpleValue(rawValue, targetType);
        }

        private string ExtractAnchorToken(string input, out string anchorId)
        {
            anchorId = null;
            int refIdx = input.IndexOf("REF(\"");
            if(refIdx == -1)
                return input;

            int start = refIdx + 5;
            int end = input.IndexOf("\"", start);
            if(end != -1)
            {
                anchorId = input.Substring(start, end - start);

                int closingParenthesis = input.IndexOf(')', end);
                int cutEnd = (closingParenthesis != -1) ? closingParenthesis + 1 : end + 1;

                return (input.Substring(0, refIdx) + input.Substring(cutEnd)).Trim();
            }

            return input;
        }

        private object ConvertSimpleValue(string val, Type targetType)
        {
            if(targetType == null)
                return val.Trim('\"');
            string clean = val.Trim('\"', ' ');
            Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if(underlying == typeof(Guid))
                return Guid.TryParse(clean, out Guid g) ? g : Guid.Empty;
            if(underlying == typeof(string))
                return clean;
            if(underlying.IsEnum)
                return Enum.Parse(underlying, clean, true);
            if(underlying == typeof(bool))
                return bool.Parse(clean);

            if(underlying == typeof(Type))
            {
                // Prioritize checking settings assemblies explicitly configured for your engine context first
                foreach(var assembly in _settings.TypeAssemblies)
                {
                    Type t = assembly.GetType(clean) ?? assembly.GetTypes().FirstOrDefault(x => x.Name == clean);
                    if(t != null)
                        return t;
                }

                foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        Type t = assembly.GetType(clean) ?? assembly.GetTypes().FirstOrDefault(x => x.Name == clean);
                        if(t != null)
                            return t;
                    }
                    catch { continue; }
                }

                throw new TypeLoadException($"GISM Parser Error: Failed to resolve the string symbol '{clean}' into a valid System.Type.");
            }

            return Convert.ChangeType(clean, underlying, System.Globalization.CultureInfo.InvariantCulture);
        }

        private Type FindTypeInAssemblies(string typeName)
        {
            if(string.IsNullOrWhiteSpace(typeName))
                return null;

            typeName = typeName.Trim();

            // --- Robust Nullable<T> and T? handling ---
            // Handles: Nullable<Guid>, System.Nullable<Guid>, Guid?, etc.
            if(
                (typeName.EndsWith("?", StringComparison.Ordinal)) ||
                (typeName.StartsWith("Nullable<", StringComparison.OrdinalIgnoreCase) && typeName.EndsWith(">")) ||
                (typeName.StartsWith("System.Nullable<", StringComparison.OrdinalIgnoreCase) && typeName.EndsWith(">"))
            )
            {
                string underlyingTypeName;
                if(typeName.EndsWith("?"))
                {
                    underlyingTypeName = typeName.Substring(0, typeName.Length - 1).Trim();
                }
                else
                {
                    int open = typeName.IndexOf('<');
                    int close = typeName.LastIndexOf('>');
                    if(open != -1 && close > open)
                        underlyingTypeName = typeName.Substring(open + 1, close - open - 1).Trim();
                    else
                        underlyingTypeName = "object";
                }

                Type underlyingType = FindTypeInAssemblies(underlyingTypeName);
                if(underlyingType == null)
                    return null;

                // Only value types can be nullable
                if(!underlyingType.IsValueType)
                    return underlyingType;

                return typeof(Nullable<>).MakeGenericType(underlyingType);
            }

            // --- Common primitive aliases ---
            if(typeName.Equals("Guid", StringComparison.OrdinalIgnoreCase))
                return typeof(Guid);
            if(typeName.Equals("int", StringComparison.OrdinalIgnoreCase))
                return typeof(int);
            if(typeName.Equals("string", StringComparison.OrdinalIgnoreCase))
                return typeof(string);
            if(typeName.Equals("Boolean", StringComparison.OrdinalIgnoreCase) || typeName.Equals("bool", StringComparison.OrdinalIgnoreCase))
                return typeof(bool);
            if(typeName.Equals("Single", StringComparison.OrdinalIgnoreCase) || typeName.Equals("float", StringComparison.OrdinalIgnoreCase))
                return typeof(float);

            // --- Try to resolve type from configured assemblies ---
            foreach(var assembly in _settings.TypeAssemblies)
            {
                // Try full name, then System namespace, then by short name
                Type t = assembly.GetType(typeName)
                    ?? assembly.GetType("System." + typeName)
                    ?? assembly.GetTypes().FirstOrDefault(x => x.Name.Equals(typeName, StringComparison.Ordinal));
                if(t != null)
                    return t;
                try
                {
                    foreach(Type type in assembly.GetTypes())
                    {
                        if(type.Name.Equals(typeName, StringComparison.Ordinal))
                            return type;
                    }
                }
                catch(System.Reflection.ReflectionTypeLoadException ex)
                {
                    foreach(Type type in ex.Types)
                    {
                        if(type != null && type.Name.Equals(typeName, StringComparison.Ordinal))
                            return type;
                    }
                }
            }

            return null;
        }

        private string GetObjectName(object obj)
        {
            return obj?.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj)?.ToString();
        }

        private bool IsPrimitiveOrSimple(Type type) =>
            type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(decimal);

        private class ParsingContext
        {
            public int Indent
            {
                get;
            }
            public object Instance
            {
                get;
            }
            public PropertyInfo ActiveProperty
            {
                get; set;
            }
            public bool InsideDictionaryBlock
            {
                get; set;
            }
            public string PendingDictionaryKey
            {
                get; set;
            }
            public bool InCollectionMode
            {
                get; set;
            }
            public object CollectionContext
            {
                get; set;
            }

            public ParsingContext(int indent, object instance)
            {
                Indent = indent;
                Instance = instance;
            }
        }

        private string StripComments(string line)
        {
            int hash = line.IndexOf('#');
            return hash == -1 ? line : line.Substring(0, hash);
        }
    }
}