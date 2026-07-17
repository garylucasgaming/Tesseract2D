using System;
using System.Collections.Generic;
using System.Reflection;
using Engine.Core.ECS;
using Microsoft.Xna.Framework; // Reference MonoGame's Vector2

namespace Engine.Core.Serialization
{
    public static class ComponentSerializer
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        /// Checks if a member is decorated with [GISMIgnore] (or any standard Ignore attribute).
        /// </summary>
        private static bool ShouldIgnore(MemberInfo member)
        {
            foreach(var attr in member.GetCustomAttributes(true))
            {
                var attrName = attr.GetType().Name;
                if(attrName == "IgnoreAttribute")
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Identifies types we natively support exporting.
        /// </summary>
        private static bool IsSupportedType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type.IsEnum ||
                   type == typeof(Vector2);
        }

        public static Dictionary<string, object> ExportComponent(ECS.GameComponent component)
        {
            var data = new Dictionary<string, object>();
            var type = component.GetType();

            // 1. Fields
            foreach(var field in type.GetFields(Flags))
            {
                if(field.Name == "gameObject" || field.Name == "Parent" || ShouldIgnore(field))
                    continue;

                var value = field.GetValue(component);
                if(value != null && IsSupportedType(field.FieldType))
                {
                    if(field.FieldType.IsEnum)
                    {
                        data[field.Name] = value.ToString();
                    }
                    else if(field.FieldType == typeof(Vector2))
                    {
                        var vec = (Vector2) value;
                        data[field.Name] = new Dictionary<string, float> { { "X", vec.X }, { "Y", vec.Y } };
                    }
                    else
                    {
                        data[field.Name] = value;
                    }
                }
            }

            // 2. Properties
            foreach(var prop in type.GetProperties(Flags))
            {
                if(!prop.CanRead || !prop.CanWrite || prop.Name == "gameObject" || prop.Name == "Parent" || ShouldIgnore(prop))
                    continue;

                var value = prop.GetValue(component);
                if(value != null && IsSupportedType(prop.PropertyType))
                {
                    if(prop.PropertyType.IsEnum)
                    {
                        data[prop.Name] = value.ToString();
                    }
                    else if(prop.PropertyType == typeof(Vector2))
                    {
                        var vec = (Vector2) value;
                        data[prop.Name] = new Dictionary<string, float> { { "X", vec.X }, { "Y", vec.Y } };
                    }
                    else
                    {
                        data[prop.Name] = value;
                    }
                }
            }

            return data;
        }

        public static void ImportComponent(ECS.GameComponent component, Dictionary<string, object> state)
        {
            if(state == null)
                return;
            var type = component.GetType();

            foreach(var kvp in state)
            {
                try
                {
                    // Try Field
                    var field = type.GetField(kvp.Key, Flags);
                    if(field != null && !ShouldIgnore(field))
                    {
                        if(field.FieldType.IsEnum)
                        {
                            field.SetValue(component, Enum.Parse(field.FieldType, kvp.Value.ToString()));
                        }
                        else if(field.FieldType == typeof(Vector2))
                        {
                            field.SetValue(component, ParseVector2(kvp.Value));
                        }
                        else
                        {
                            var converted = Convert.ChangeType(kvp.Value, field.FieldType);
                            field.SetValue(component, converted);
                        }
                        continue;
                    }

                    // Try Property
                    var prop = type.GetProperty(kvp.Key, Flags);
                    if(prop != null && prop.CanWrite && !ShouldIgnore(prop))
                    {
                        if(prop.PropertyType.IsEnum)
                        {
                            prop.SetValue(component, Enum.Parse(prop.PropertyType, kvp.Value.ToString()), null);
                        }
                        else if(prop.PropertyType == typeof(Vector2))
                        {
                            prop.SetValue(component, ParseVector2(kvp.Value), null);
                        }
                        else
                        {
                            var converted = Convert.ChangeType(kvp.Value, prop.PropertyType);
                            prop.SetValue(component, converted, null);
                        }
                    }
                }
                catch
                {
                    // Suppress or log structural mismatches smoothly
                }
            }
        }

        /// <summary>
        /// Robust helper to extract a MonoGame Vector2 from serialized dictionary shapes or inline strings.
        /// </summary>
        private static Vector2 ParseVector2(object value)
        {
            if(value is Vector2 directVec)
            {
                return directVec;
            }

            // Handles standard deserialized maps: Dictionary<object, object> or Dictionary<string, object>
            if(value is System.Collections.IDictionary dict)
            {
                float x = 0f;
                float y = 0f;

                if(dict.Contains("X"))
                    x = Convert.ToSingle(dict["X"]);
                if(dict.Contains("Y"))
                    y = Convert.ToSingle(dict["Y"]);

                return new Vector2(x, y);
            }

            // Fallback parser if your YAML engine loads inline string formats like "16, 16" or "{X:16 Y:16}"
            if(value is string str)
            {
                str = str.Trim('{', '}', ' ');
                var parts = str.Split(new[] { ',', ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(parts.Length >= 2)
                {
                    float x = 0, y = 0;
                    if(parts.Length == 2)
                    {
                        float.TryParse(parts[0], out x);
                        float.TryParse(parts[1], out y);
                    }
                    else
                    {
                        for(int i = 0; i < parts.Length - 1; i++)
                        {
                            if(parts[i].Equals("X", StringComparison.OrdinalIgnoreCase))
                                float.TryParse(parts[i + 1], out x);
                            if(parts[i].Equals("Y", StringComparison.OrdinalIgnoreCase))
                                float.TryParse(parts[i + 1], out y);
                        }
                    }
                    return new Vector2(x, y);
                }
            }

            return Vector2.Zero;
        }
    }
}