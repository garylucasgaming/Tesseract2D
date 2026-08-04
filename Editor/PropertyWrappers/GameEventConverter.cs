using Engine.Core.ECS;
using Engine.Core.Runtime;
using Engine.Editor.WinFormsApp1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.PropertyWrappers
{
    public class GameEventConverter : ExpandableObjectConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            var properties = new List<PropertyDescriptor>();

            properties.Add(new GameEventPropertyDescriptor(nameof(GameEvent.TargetGameObject), typeof(GameObject)));
            properties.Add(new GameEventPropertyDescriptor(nameof(GameEvent.TargetComponentTypeName), typeof(string)));
            properties.Add(new GameEventPropertyDescriptor(nameof(GameEvent.MethodName), typeof(string)));

            return new PropertyDescriptorCollection(properties.ToArray());
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string) && value is GameEvent gameEvent)
            {
                string targetName = gameEvent.TargetGameObject?.Name ?? "None";
                string methodName = string.IsNullOrEmpty(gameEvent.MethodName) ? "No Event" : gameEvent.MethodName;
                return $"{targetName} -> {methodName}";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

}
