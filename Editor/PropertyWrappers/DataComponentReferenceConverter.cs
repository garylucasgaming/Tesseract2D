using Engine.Core.ECS.Components;
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
    public class DataComponentReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false; // Allow cell editing

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            Type targetType = context?.PropertyDescriptor?.PropertyType ?? typeof(DataComponent);
            List<DataComponent> choices = new List<DataComponent> { null };

            if(EditorContextManager.ActiveLoadedScene?.Database?.Databases != null)
            {
                foreach(var db in EditorContextManager.ActiveLoadedScene.Database.Databases)
                {
                    foreach(var component in db.ComponentDatabase.Values)
                    {
                        if(targetType.IsAssignableFrom(component.GetType()))
                        {
                            choices.Add(component);
                        }
                    }
                }
            }

            return new StandardValuesCollection(choices);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value is DataComponent comp)
                    return string.IsNullOrWhiteSpace(comp.DisplayName) ? $"Unnamed {comp.GetType().Name}" : comp.DisplayName;

                return "None (DataAsset)";
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
                    foreach(DataComponent choice in choices)
                    {
                        if(choice == null)
                            continue;
                        if(choice.DisplayName != null && choice.DisplayName.Equals(str, StringComparison.OrdinalIgnoreCase))
                            return choice;
                    }

                    // 2. Partial match
                    foreach(DataComponent choice in choices)
                    {
                        if(choice == null)
                            continue;
                        if(choice.DisplayName != null && choice.DisplayName.Contains(str, StringComparison.OrdinalIgnoreCase))
                            return choice;
                    }
                }

                // 💡 Safe Fallback: Unrecognized string safely reverts to null (None) instead of throwing an exception!
                return null;
            }
            return null;
        }
    }
}
