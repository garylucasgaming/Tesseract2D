using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Serialization
{
    public static class ComponentTypeRegistry
    {
        private static readonly Dictionary<string, Type> _stringToType = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<Type, string> _typeToString = new();

        /// <summary>
        /// Registers a component type with a clean string alias for serialization.
        /// </summary>
        public static void Register(string alias, Type type)
        {
            _stringToType[alias] = type;
            _typeToString[type] = alias;
        }

        public static Type? GetType(string alias)
        {
            return _stringToType.TryGetValue(alias, out var type) ? type : null;
        }

        public static string GetAlias(Type type)
        {
            return _typeToString.TryGetValue(type, out var alias) ? alias : type.Name;
        }
    }
}
