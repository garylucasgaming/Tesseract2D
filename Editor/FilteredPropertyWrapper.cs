using Engine.Core.Utilities;
using System;
using System.ComponentModel;
using System.Linq;

namespace Engine.Editor.WinFormsApp1
{
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
                "Components", "Children", "Parent", "ParentId",
                "ContextScene", "ParentTransform", "GameObjects", "Entities"
            };

            foreach(PropertyDescriptor prop in baseProperties)
            {
                if(prop.IsReadOnly || systemBlacklist.Contains(prop.Name))
                    continue;

                if(!prop.IsBrowsable)
                    continue;

                if(prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    continue;

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

        public FilteredPropertyDescriptor(PropertyDescriptor baseDescriptor, object targetComponent)
            : base(baseDescriptor)
        {
            _baseDescriptor = baseDescriptor;
            _targetComponent = targetComponent;
        }

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
                object convertedValue = Convert.ChangeType(value, propInfo.PropertyType);

                Log.Info($"[Wrapper Success] Successfully invoked setter for '{propInfo.Name}' via Reflection. Value: {convertedValue}");

                propInfo.SetValue(_targetComponent, convertedValue, null);

                // Force the UI grid layout context to synchronize changes
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