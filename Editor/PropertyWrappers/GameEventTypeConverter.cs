using Engine.Core.Runtime;
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
    public class GameEventComponentTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true; // Forces strict dropdown selection

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var choices = new List<string> { "None" };

            // Find the parent GameEvent instance from the context
            GameEvent? gameEvent = null;
            if(context?.Instance is GameEvent ge)
            {
                gameEvent = ge;
            }
            else if(context?.Instance is FilteredPropertyWrapper wrapper && wrapper.Target is GameEvent geTarget)
            {
                gameEvent = geTarget;
            }

            if(gameEvent?.TargetGameObject?.Components != null)
            {
                foreach(var comp in gameEvent.TargetGameObject.Components.Values)
                {
                    if(comp != null)
                    {
                        string fullName = comp.GetType().FullName ?? comp.GetType().Name;
                        if(!choices.Contains(fullName))
                        {
                            choices.Add(fullName);
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
                if(value is string str && !string.IsNullOrEmpty(str) && str != "None")
                {
                    // Clean up namespace/suffix for display purposes in the cell
                    string displayName = str.Contains('.') ? str.Substring(str.LastIndexOf('.') + 1) : str;
                    if(displayName.EndsWith("Component"))
                    {
                        displayName = displayName.Substring(0, displayName.Length - "Component".Length);
                    }
                    return displayName;
                }
                return "None (Component)";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                str = str.Trim();
                if(string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return string.Empty;

                // If user selected a short display name, match it back to the full type name stored in the model
                var choices = GetStandardValues(context);
                foreach(string choice in choices)
                {
                    if(choice == "None")
                        continue;
                    string shortName = choice.Contains('.') ? choice.Substring(choice.LastIndexOf('.') + 1) : choice;
                    if(shortName.Equals(str, StringComparison.OrdinalIgnoreCase) || choice.Equals(str, StringComparison.OrdinalIgnoreCase))
                    {
                        return choice;
                    }
                }
                return str;
            }
            return string.Empty;
        }
    }

}
