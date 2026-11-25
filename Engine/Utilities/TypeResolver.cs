using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core.Utilities
{
    public static class TypeResolver
    {
        private static readonly Dictionary<string, Type> _typeCache = new();
        private static readonly object _cacheLock = new();

        public static Type? FindType(string typeName, Type? requiredBaseType = null)
        {
            if(string.IsNullOrEmpty(typeName))
                return null;

            // Include base type in cache key to prevent collisions if same name exists under different bases
            string cacheKey = requiredBaseType != null ? $"{typeName}_{requiredBaseType.FullName}" : typeName;

            lock(_cacheLock)
            {
                if(_typeCache.TryGetValue(cacheKey, out var cachedType))
                    return cachedType;

                // SaveScene writes AssemblyQualifiedName for managers and systems; resolve it first.
                Type? type = Type.GetType(typeName, false);
                if(IsCompatible(type, requiredBaseType))
                {
                    _typeCache[cacheKey] = type!;
                    return type;
                }

                var exactMatches = new List<Type>();
                var shortNameMatches = new List<Type>();

                foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // Try matching by full name or exact name with base check.
                    type = assembly.GetType(typeName, false, true);
                    if(IsCompatible(type, requiredBaseType))
                        exactMatches.Add(type!);

                    // Fallback: search class name across all namespaces with base check.
                    try
                    {
                        shortNameMatches.AddRange(
                            assembly.GetTypes()
                                .Where(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                                            IsCompatible(t, requiredBaseType)));
                    }
                    catch
                    {
                        // Ignore dynamic assemblies that throw on GetTypes()
                    }
                }

                if(TryResolveUnique(exactMatches, cacheKey, typeName, "full name", out var resolved))
                    return resolved;

                if(exactMatches.Count > 1)
                {
                    Log.Warning($"[TypeResolver] Ambiguous full name '{typeName}'; skipping resolution.");
                    return null;
                }

                if(TryResolveUnique(shortNameMatches, cacheKey, typeName, "short name", out resolved))
                    return resolved;

                if(shortNameMatches.Count > 1)
                {
                    Log.Warning($"[TypeResolver] Ambiguous short name '{typeName}'; skipping resolution.");
                    return null;
                }

                return null;
            }
        }

        public static void ClearCache()
        {
            lock(_cacheLock)
            {
                _typeCache.Clear();
            }
        }

        private static bool TryResolveUnique(
            List<Type> matches,
            string cacheKey,
            string typeName,
            string matchKind,
            out Type? resolved)
        {
            var distinct = matches.Distinct().ToList();
            if(distinct.Count != 1)
            {
                resolved = null;
                return false;
            }

            resolved = distinct[0];
            _typeCache[cacheKey] = resolved;
            return true;
        }

        private static bool IsCompatible(Type? type, Type? requiredBaseType) =>
            type != null && (requiredBaseType == null || requiredBaseType.IsAssignableFrom(type));
    }
}
