using Engine.Core.Collections;
using Engine.Core.ECS.Components;
using Engine.Editor.WinFormsApp1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ComponentModel.TypeConverter;

namespace Engine.Editor.PropertyWrappers
{
    public class DataReferenceDropdownConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<DataComponent> choices = new List<DataComponent> { null };

            object realTarget = GetRealInstance(context);
            Database targetDb = null;

            if(realTarget is DataComponent dataComp)
            {
                targetDb = dataComp.DatabaseReference;
            }

            if(targetDb != null)
            {
                Type targetType = realTarget?.GetType() ?? typeof(DataComponent);

                foreach(var entry in targetDb.ComponentDatabase.Values)
                {
                    if(entry != null && targetType.IsAssignableFrom(entry.GetType()))
                    {
                        choices.Add(entry);
                    }
                }
            }

            return new StandardValuesCollection(choices);
        }

        private object GetRealInstance(ITypeDescriptorContext context)
        {
            object instance = context?.Instance;
            if(instance is FilteredPropertyWrapper wrapper)
                return wrapper.Target;
            return instance;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value is DataComponent comp)
                {
                    return string.IsNullOrWhiteSpace(comp.DisplayName)
                        ? $"Unnamed {comp.GetType().Name}"
                        : comp.DisplayName;
                }
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
                foreach(DataComponent choice in choices)
                {
                    if(choice == null)
                        continue;

                    string choiceDisplayName = ConvertTo(context, culture, choice, typeof(string)) as string;
                    if(choiceDisplayName != null && (choiceDisplayName.Equals(str, StringComparison.OrdinalIgnoreCase) || choiceDisplayName.Contains(str, StringComparison.OrdinalIgnoreCase)))
                        return choice;
                }

                return null;
            }
            return null;
        }
    }

}
