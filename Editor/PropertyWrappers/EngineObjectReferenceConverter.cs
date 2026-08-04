using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.PropertyWrappers
{
    public class EngineObjectReferenceConverter : TypeConverter
    {



        // This stops the PropertyGrid from showing the "+" expand button
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => false;

        // Force it to display as a clean string representation in the grid row
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value == null)
                    return "None (Object)";

                // Use reflection to gracefully grab a Name property if it exists, or fallback to the type
                var nameProp = value.GetType().GetProperty("Name");
                if(nameProp != null)
                {
                    return $"{nameProp.GetValue(value)} ({value.GetType().Name})";
                }

                return $"({value.GetType().Name})";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
