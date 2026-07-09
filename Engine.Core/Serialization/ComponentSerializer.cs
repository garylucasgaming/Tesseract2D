using System;
using System.Collections.Generic;
using System.Reflection;
using Engine.Core.ECS;

namespace Engine.Core.Serialization
{
    public static class ComponentSerializer
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

        public static Dictionary<string, object> ExportComponent(GameComponent component)
        {
            var data = new Dictionary<string, object>();
            var type = component.GetType();

            // 1. Fields
            foreach(var field in type.GetFields(Flags))
            {
                if(field.Name == "gameObject" || field.Name == "Parent")
                    continue;

                var value = field.GetValue(component);
                if(value != null && (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType.IsEnum))
                {
                    data[field.Name] = field.FieldType.IsEnum ? value.ToString() : value;
                }
            }

            // 2. Properties
            foreach(var prop in type.GetProperties(Flags))
            {
                if(!prop.CanRead || !prop.CanWrite || prop.Name == "gameObject" || prop.Name == "Parent")
                    continue;

                var value = prop.GetValue(component);
                if(value != null && (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string) || prop.PropertyType.IsEnum))
                {
                    data[prop.Name] = prop.PropertyType.IsEnum ? value.ToString() : value;
                }
            }

            return data;
        }

        public static void ImportComponent(GameComponent component, Dictionary<string, object> state)
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
                    if(field != null)
                    {
                        if(field.FieldType.IsEnum)
                        {
                            field.SetValue(component, Enum.Parse(field.FieldType, kvp.Value.ToString()));
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
                    if(prop != null && prop.CanWrite)
                    {
                        if(prop.PropertyType.IsEnum)
                        {
                            prop.SetValue(component, Enum.Parse(prop.PropertyType, kvp.Value.ToString()), null);
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
    }
}