using Engine.Core.Runtime;
using Engine.Editor.WinFormsApp1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.ComponentModel.TypeConverter;

namespace Engine.Editor.PropertyWrappers
{
    public class GameEventMethodConverter : TypeConverter
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

            if(gameEvent?.TargetGameObject != null && !string.IsNullOrEmpty(gameEvent.TargetComponentTypeName))
            {
                // Find the component instance on the target GameObject
                var targetComp = gameEvent.TargetGameObject.Components.Values
                    .FirstOrDefault(c => c.GetType().FullName == gameEvent.TargetComponentTypeName || c.GetType().Name == gameEvent.TargetComponentTypeName);

                if(targetComp != null)
                {
                    // Find all public instance methods returning void with zero parameters
                    var validMethods = targetComp.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
                        .Select(m => m.Name);

                    foreach(var method in validMethods)
                    {
                        if(!choices.Contains(method))
                        {
                            choices.Add(method);
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
                    return str;
                }
                return "None (Method)";
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

                return str;
            }
            return string.Empty;
        }
    }


}
