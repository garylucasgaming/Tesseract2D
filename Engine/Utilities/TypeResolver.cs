using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Utilities
{
    public static class TypeResolver
    {
        private static readonly Dictionary<string, Type> _typeCache = new();

        public static Type? FindType(string typeName, Type? requiredBaseType = null)
        {
            if(string.IsNullOrEmpty(typeName))
                return null;

            // Include base type in cache key to prevent collisions if same name exists under different bases
            string cacheKey = requiredBaseType != null ? $"{typeName}_{requiredBaseType.FullName}" : typeName;

            if(_typeCache.TryGetValue(cacheKey, out var cachedType))
                return cachedType;

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 1. Try matching by full name or exact name with base check
                var type = assembly.GetType(typeName, false, true);
                if(type != null && (requiredBaseType == null || requiredBaseType.IsAssignableFrom(type)))
                {
                    _typeCache[cacheKey] = type;
                    return type;
                }

                // 2. Fallback: Search class name across all namespaces with base check
                try
                {
                    type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                                             (requiredBaseType == null || requiredBaseType.IsAssignableFrom(t)));

                    if(type != null)
                    {
                        _typeCache[cacheKey] = type;
                        return type;
                    }
                }
                catch
                {
                    // Ignore dynamic assemblies that throw on GetTypes()
                }
            }

            return null;
        }
    }
}
