using Engine.Core.ECS.Components.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Runtime
{
    public static class StyleManager
    {
        private static Dictionary<string, StyleTag> _registry = new Dictionary<string, StyleTag>();

        public static void RegisterStyle(StyleTag tag)
        {
            _registry[tag.Name] = tag;
        }

        public static StyleTag? GetStyle(string name)
        {
            return _registry.TryGetValue(name, out var tag) ? tag : null;
        }

        public static void Clear()
        {
            _registry.Clear();
        }

    }
}
