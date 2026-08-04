using Engine.Core.ECS;
using Engine.Core.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ComponentModel.TypeConverter;

namespace Engine.Editor.PropertyWrappers
{
    public class GameObjectReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> sceneObjects = new List<object> { null };

            if(EditorContextManager.ActiveLoadedScene != null)
            {
                var entities = EditorContextManager.ActiveLoadedScene.Entities.GetSerializableEntities();
                if(entities != null)
                {
                    foreach(var entity in entities)
                    {
                        if(entity != null)
                        {
                            sceneObjects.Add(entity);
                        }
                    }
                }
            }

            return new StandardValuesCollection(sceneObjects);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value == null)
                    return "None (GameObject)";

                if(value is string str)
                    return str;

                if(value is GameObject go)
                {
                    return string.IsNullOrWhiteSpace(go.Name)
                        ? $"Unnamed GameObject ({value.GetType().Name})"
                        : go.Name;
                }

                var nameProp = value.GetType().GetProperty("Name");
                if(nameProp != null)
                {
                    string nameVal = nameProp.GetValue(value)?.ToString();
                    if(!string.IsNullOrWhiteSpace(nameVal))
                        return nameVal;
                }

                return $"Unnamed {value.GetType().Name}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                str = str.Trim();
                if(string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return null;

                var choices = GetStandardValues(context);
                if(choices != null)
                {
                    // 1. Exact match
                    foreach(object choice in choices)
                    {
                        if(choice == null)
                            continue;
                        string choiceDisplayName = ConvertTo(context, culture, choice, typeof(string)) as string;
                        if(choiceDisplayName != null && choiceDisplayName.Equals(str, StringComparison.OrdinalIgnoreCase))
                            return choice;
                    }

                    // 2. Partial match
                    foreach(object choice in choices)
                    {
                        if(choice == null)
                            continue;
                        string choiceDisplayName = ConvertTo(context, culture, choice, typeof(string)) as string;
                        if(choiceDisplayName != null && choiceDisplayName.Contains(str, StringComparison.OrdinalIgnoreCase))
                            return choice;
                    }
                }

                return null; // Fallback safely to None
            }

            return null;
        }
    }

}
