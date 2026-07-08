using Engine.Core.Utilities;
using System;
using System.ComponentModel;
using System.Linq;

namespace Engine.Editor.WinFormsApp1
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

public class FilteredPropertyWrapper : CustomTypeDescriptor, ICustomTypeDescriptor
    {
        private readonly object _target;

        public FilteredPropertyWrapper(object target)
        {
            _target = target;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var baseProperties = TypeDescriptor.GetProperties(_target, attributes, true);
            var filteredProperties = new PropertyDescriptorCollection(null);

            string[] systemBlacklist = {
         
    };

            foreach(PropertyDescriptor prop in baseProperties)
            {
                if(prop.IsReadOnly || systemBlacklist.Contains(prop.Name))
                    continue;

                if(!prop.IsBrowsable)
                    continue;

                Type propType = prop.PropertyType;

                if(propType.IsClass && propType != typeof(string))
                {
                    bool isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(propType);

                    // 👇 NEW: Detect custom engine references (GameObjects, Components, etc.)
                    bool isEngineObject = typeof(Engine.Core.ECS.GameObject).IsAssignableFrom(propType) ||
                                          propType.Name == "GameObject" ||
                                          propType.Name.EndsWith("Component");

                    if(isEngineObject)
                    {
                        // Let it through! We want to show the reference box.
                        // We will override how it displays below so it doesn't cause a recursion loop.
                    }
                    else if(!isCollection)
                    {
                        // It's an unhandled external class object, skip to avoid loops
                        continue;
                    }
                }

                // Wrap the original property in our custom router descriptor
                filteredProperties.Add(new FilteredPropertyDescriptor(prop, _target));
            }

            return filteredProperties;
        }
        public override PropertyDescriptorCollection GetProperties() => GetProperties(null);

        // 👇 FIX: Return the actual target. This tells the PropertyGrid that the 
        // underlying instance owning the properties is your real live Component.
        public override object GetPropertyOwner(PropertyDescriptor pd) => _target;
    }

    /// <summary>
    /// Custom descriptor that intercepts UI edits and pushes them straight into live engine memory.
    /// </summary>
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

            // 👇 If it's an engine reference, attach our safe converter to choke off recursion loops
            Type propType = baseDescriptor.PropertyType;
            if(propType.IsClass && propType != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(propType))
            {
                _customConverter = new EngineObjectReferenceConverter();
            }
        }

        public override TypeConverter Converter => _customConverter ?? base.Converter;

        // 👇 FIX: Route the component argument straight to the target for all structural methods
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

            // Force a direct Reflection call to bypass WinForms optimization checks
            Type targetType = _targetComponent.GetType();
            var propInfo = targetType.GetProperty(this.Name,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if(propInfo != null && propInfo.CanWrite)
            {
                object convertedValue;

                // 👇 FIX: Bypasses simple conversion for arrays/lists
                if(propInfo.PropertyType.IsClass && propInfo.PropertyType != typeof(string))
                {
                    convertedValue = value; // Keep reference intact
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, propInfo.PropertyType);
                }

                Log.Info($"[Wrapper Success] Successfully invoked setter for '{propInfo.Name}' via Reflection.");
                propInfo.SetValue(_targetComponent, convertedValue, null);
                TypeDescriptor.Refresh(_targetComponent);
            }
            else
            {
                // Fallback using the exact target component metadata mapping context
                _baseDescriptor.SetValue(_targetComponent, value);
            }
        }
    }
}