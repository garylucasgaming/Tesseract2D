using Engine.Editor.WinFormsApp1;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.PropertyWrappers
{
    public class InlineCollectionConverter : ExpandableObjectConverter
    {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            var properties = new PropertyDescriptorCollection(null);

            if(value is IList list)
            {
                // 1. Pass the top-level parent object context (context.Instance) down to the size field 
                // so it can trigger a full UI layout repaint when rows are added or removed.
                object owner = context?.Instance;
                properties.Add(new CollectionSizeDescriptor(context?.PropertyDescriptor, list, owner));

                // 2. Build the alphanumeric virtual index slots
                for(int i = 0; i < list.Count; i++)
                {
                    properties.Add(new CollectionIndexDescriptor(context?.PropertyDescriptor, list, i));
                }
            }

            return properties;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string) && value is IList list)
            {
                string typeName = value.GetType().IsArray ? "Array" : "List";
                return $"{typeName} [{list.Count}]";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
