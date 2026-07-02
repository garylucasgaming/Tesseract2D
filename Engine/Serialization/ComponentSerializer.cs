using System;
using System.Reflection;
using Tommy;
using Engine.Core.ECS;

namespace Engine.Core.Serialization
{
    public static class ComponentSerializer
    {
        public static TomlTable ExportComponent(GameComponent component)
        {
            var table = new TomlTable();
            var type = component.GetType();

            // 1. Export Fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach(var field in fields)
            {
                if(field.Name == "gameObject")
                    continue;

                var value = field.GetValue(component);
                if(value != null)
                    table[field.Name] = value.ToString();
            }

            // 2. Export Properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach(var prop in properties)
            {
                if(!prop.CanRead || !prop.CanWrite || prop.Name == "gameObject")
                    continue;

                var value = prop.GetValue(component);
                if(value != null)
                    table[prop.Name] = value.ToString();
            }

            return table;
        }

        public static void ImportComponent(GameComponent component, TomlTable table)
        {
            var type = component.GetType();

            foreach(var entry in table.Keys)
            {
                var node = table[entry];
                if(node == null)
                    continue;

                object rawValue;
                if(node.IsString)
                    rawValue = node.AsString.Value;
                else if(node.IsInteger)
                    rawValue = node.AsInteger.Value;
                else if(node.IsFloat)
                    rawValue = node.AsFloat.Value;
                else if(node.IsBoolean)
                    rawValue = node.AsBoolean.Value;
                else
                    continue;

                try
                {
                    // 1. Try Field
                    var field = type.GetField(entry, BindingFlags.Public | BindingFlags.Instance);
                    if(field != null)
                    {
                        if(field.FieldType.IsPrimitive || field.FieldType == typeof(string))
                        {
                            object convertedValue = Convert.ChangeType(rawValue, field.FieldType);
                            field.SetValue(component, convertedValue);
                        }
                        continue;
                    }

                    // 2. Try Property
                    var prop = type.GetProperty(entry, BindingFlags.Public | BindingFlags.Instance);
                    if(prop != null && prop.CanWrite)
                    {
                        if(prop.PropertyType.IsEnum)
                        {
                            object enumValue = Enum.Parse(prop.PropertyType, rawValue.ToString());
                            prop.SetValue(component, enumValue, null);
                            continue;
                        }

                        if(prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
                        {
                            object convertedValue = Convert.ChangeType(rawValue, prop.PropertyType);
                            prop.SetValue(component, convertedValue, null);
                        }
                    }
                }
                catch(Exception)
                {
                    // Suppress complex layouts smoothly
                }
            }
        }
    }
}