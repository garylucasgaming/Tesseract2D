using Engine.Core.Collections;
using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using static System.ComponentModel.TypeConverter;
using Engine.Editor.PropertyWrappers;

namespace Engine.Editor.WinFormsApp1
{



   
    

    public class FilteredPropertyWrapper : CustomTypeDescriptor, ICustomTypeDescriptor
    {
        private readonly object _target;

        public object Target => _target;

        public FilteredPropertyWrapper(object target)
        {
            _target = target;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var baseProperties = TypeDescriptor.GetProperties(_target, attributes, true);
            var filteredProperties = new PropertyDescriptorCollection(null);

            foreach(PropertyDescriptor prop in baseProperties)
            {
                if(prop.IsReadOnly || !prop.IsBrowsable)
                    continue;

                Type propType = prop.PropertyType;

                if(propType.IsClass && propType != typeof(string))
                {
                    bool isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(propType);

                    // allows specific engine objects through the inspector
                    bool isEngineObject = typeof(GameObject).IsAssignableFrom(propType) ||
                               typeof(Database).IsAssignableFrom(propType) ||
                               typeof(DataComponent).IsAssignableFrom(propType) ||
                               typeof(GameEvent).IsAssignableFrom(propType) || // 💡 ADDED THIS
                               propType.Name == "GameObject" ||
                               propType.Name == "GameEvent" ||                                    // 💡 ADDED THIS
                               propType.Name.EndsWith("Component") ||
                               propType.Name.EndsWith("Database") ||
                               prop.Name.Equals("DatabaseReference", StringComparison.OrdinalIgnoreCase);

                    if(!isEngineObject && !isCollection)
                        continue;
                }

                filteredProperties.Add(new FilteredPropertyDescriptor(prop, _target));
            }

            return filteredProperties;
        }

        public override PropertyDescriptorCollection GetProperties() => GetProperties(null);
        public override object GetPropertyOwner(PropertyDescriptor pd) => _target;
    }


   
   
    
   

    public class FilteredPropertyDescriptor : PropertyDescriptor
    {
        private readonly PropertyDescriptor _baseDescriptor;
        private readonly object _targetComponent;
        private readonly TypeConverter _customConverter;

        public FilteredPropertyDescriptor(PropertyDescriptor baseDescriptor, object targetComponent)
     : base(baseDescriptor)
        {
            _baseDescriptor = baseDescriptor;
            _targetComponent = targetComponent;

            Type propType = baseDescriptor.PropertyType;

            
            if(baseDescriptor.Name.Equals("FontAssetPath", StringComparison.OrdinalIgnoreCase) ||
                baseDescriptor.Name.Equals("FontPath", StringComparison.OrdinalIgnoreCase) ||
                (propType == typeof(string) && baseDescriptor.Name.EndsWith("Font", StringComparison.OrdinalIgnoreCase)))
            {
                _customConverter = new SpriteFontReferenceConverter();
            }
            
            else if(baseDescriptor.Name.Equals("DatabaseReference", StringComparison.OrdinalIgnoreCase) ||
                typeof(Database).IsAssignableFrom(propType))
            {
                _customConverter = new DatabaseReferenceConverter();
            }
            else if(baseDescriptor.Name.Equals("DataReference", StringComparison.OrdinalIgnoreCase))
            {
                _customConverter = new DataReferenceDropdownConverter();
            }
            // Fallback to default engine property converters
            else if(typeof(System.Collections.IList).IsAssignableFrom(propType))
            {
                _customConverter = new InlineCollectionConverter();
            }
            else if(propType.IsClass && propType != typeof(string))
            {
                if(typeof(DataComponent).IsAssignableFrom(propType))
                    _customConverter = new DataComponentReferenceConverter();
                else if(typeof(GameObject).IsAssignableFrom(propType) || propType.Name == "GameObject")
                    _customConverter = new GameObjectReferenceConverter();
                else if(propType.Name.EndsWith("Component") || typeof(GameComponent).IsAssignableFrom(propType))
                    _customConverter = new ComponentReferenceConverter();

                else if(propType.Name == "GameEvent" || typeof(GameEvent).IsAssignableFrom(propType))
                    _customConverter = new GameEventConverter();
                else
                    _customConverter = new EngineObjectReferenceConverter();
            }
        }

        public override TypeConverter Converter => _customConverter ?? base.Converter;

        public override bool CanResetValue(object component) => _baseDescriptor.CanResetValue(_targetComponent);
        public override object GetValue(object component) => _baseDescriptor.GetValue(_targetComponent);
        public override void ResetValue(object component) => _baseDescriptor.ResetValue(_targetComponent);
        public override bool ShouldSerializeValue(object component) => _baseDescriptor.ShouldSerializeValue(_targetComponent);

        public override Type ComponentType => _baseDescriptor.ComponentType;
        public override bool IsReadOnly => _baseDescriptor.IsReadOnly;
        public override Type PropertyType => _baseDescriptor.PropertyType;

        public override void SetValue(object component, object value)
        {
            if(_targetComponent == null)
                return;

            Type targetType = _targetComponent.GetType();
            var propInfo = targetType.GetProperty(this.Name,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if(propInfo != null && propInfo.CanWrite)
            {
                object convertedValue;

                if(value is string strValue && Converter != null && Converter.CanConvertFrom(typeof(string)))
                {
                    convertedValue = Converter.ConvertFrom(strValue);
                }
                else if(propInfo.PropertyType.IsClass && propInfo.PropertyType != typeof(string))
                {
                    convertedValue = value;
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, propInfo.PropertyType);
                }

                propInfo.SetValue(_targetComponent, convertedValue, null);

                // Clear DataReference if DatabaseReference changes
                if(this.Name.Equals("DatabaseReference", StringComparison.OrdinalIgnoreCase))
                {
                    var dataRefProp = targetType.GetProperty("DataReference");
                    dataRefProp?.SetValue(_targetComponent, null);
                }

                TypeDescriptor.Refresh(_targetComponent);
            }
            else
            {
                _baseDescriptor.SetValue(_targetComponent, value);
            }
        }
    }
}
