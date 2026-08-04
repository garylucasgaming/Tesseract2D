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
    public class ComponentReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> validComponents = new List<object> { null };
            Type targetComponentType = context?.PropertyDescriptor?.PropertyType ?? typeof(object);

            if(EditorContextManager.ActiveLoadedScene != null)
            {
                var entities = EditorContextManager.ActiveLoadedScene.Entities.GetSerializableEntities();
                if(entities != null)
                {
                    foreach(var go in entities)
                    {
                        if(go?.Components == null)
                            continue;

                        foreach(var comp in go.Components.Values)
                        {
                            if(comp == null)
                                continue;

                            if(targetComponentType == typeof(object) || targetComponentType.IsAssignableFrom(comp.GetType()))
                            {
                                validComponents.Add(comp);
                            }
                        }
                    }
                }
            }

            return new StandardValuesCollection(validComponents);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value == null)
                    return "None (Component)";

                if(value is string str)
                    return str;

                string ownerName = "Detached";
                Type compType = value.GetType();

                var ownerField = compType.GetField("gameObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                              ?? compType.GetField("owner", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                              ?? compType.GetField("entity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

                object ownerObj = ownerField?.GetValue(value);

                if(ownerObj == null)
                {
                    var ownerProp = compType.GetProperty("gameObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                 ?? compType.GetProperty("owner", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                 ?? compType.GetProperty("entity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

                    ownerObj = ownerProp?.GetValue(value);
                }

                if(ownerObj is GameObject go)
                {
                    ownerName = string.IsNullOrWhiteSpace(go.Name) ? "Unnamed GameObject" : go.Name;
                }
                else if(ownerObj != null)
                {
                    var nameProp = ownerObj.GetType().GetProperty("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    string nameVal = nameProp?.GetValue(ownerObj)?.ToString();
                    if(!string.IsNullOrWhiteSpace(nameVal))
                    {
                        ownerName = nameVal;
                    }
                }

                string cleanCompName = compType.Name;
                if(cleanCompName.EndsWith("Component"))
                {
                    cleanCompName = cleanCompName.Substring(0, cleanCompName.Length - "Component".Length);
                }

                return $"{ownerName} -> {cleanCompName}";
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
                    foreach(object comp in choices)
                    {
                        if(comp == null)
                            continue;
                        string choiceDisplayName = ConvertTo(context, culture, comp, typeof(string)) as string;
                        if(choiceDisplayName != null && choiceDisplayName.Equals(str, StringComparison.OrdinalIgnoreCase))
                            return comp;
                    }

                    // 2. Partial match
                    foreach(object comp in choices)
                    {
                        if(comp == null)
                            continue;
                        string choiceDisplayName = ConvertTo(context, culture, comp, typeof(string)) as string;
                        if(choiceDisplayName != null && choiceDisplayName.Contains(str, StringComparison.OrdinalIgnoreCase))
                            return comp;
                    }
                }

                return null; // Fallback safely to None
            }

            return null;
        }
    }

}
