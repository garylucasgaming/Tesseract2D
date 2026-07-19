using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using System;
using System.Collections;
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
    public class InlineCollectionConverter : ExpandableObjectConverter
    {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            var properties = new PropertyDescriptorCollection(null);

            if(value is IList list)
            {
                // 1. Add a virtual "Size" property descriptor at the top
                properties.Add(new CollectionSizeDescriptor(context.PropertyDescriptor, list));

                // 2. Add a virtual property descriptor for every single active item slot
                for(int i = 0; i < list.Count; i++)
                {
                    properties.Add(new CollectionIndexDescriptor(context.PropertyDescriptor, list, i));
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

    // ====================================================
    // VIRTUAL PROPERTY FOR THE COLLECTION SIZE / COUNT
    // ====================================================
    public class CollectionSizeDescriptor : PropertyDescriptor
    {
        private readonly IList _list;
        private readonly PropertyDescriptor _parentProp;

        public CollectionSizeDescriptor(PropertyDescriptor parentProp, IList list)
            : base("Size", new Attribute[] { new CategoryAttribute("Collection Layout") })
        {
            _parentProp = parentProp;
            _list = list;
        }

        public override Type ComponentType => typeof(IList);
        public override bool IsReadOnly => _list.IsFixedSize && !_list.GetType().IsArray; // True for raw fixed arrays without resizing hooks
        public override Type PropertyType => typeof(int);

        public override object GetValue(object component) => _list.Count;

        public override void SetValue(object component, object value)
        {
            int newSize = (int) value;
            if(newSize < 0 || newSize == _list.Count)
                return;

            Type listType = _list.GetType();

            // Handle standard C# Arrays (T[])
            if(listType.IsArray)
            {
                Type elementType = listType.GetElementType();
                Array newArray = Array.CreateInstance(elementType, newSize);

                // Copy over old items up to the boundary limit
                int copyCount = Math.Min(_list.Count, newSize);
                Array.Copy((Array) _list, newArray, copyCount);

                // Find the parent Component/Object instance to reassign the brand new array handle via reflection
                var owner = context_Bypass_Owner(component);
                _parentProp.SetValue(owner, newArray);
            }
            // Handle Generic System.Collections.Generic.List<T>
            else
            {
                while(_list.Count > newSize)
                {
                    _list.RemoveAt(_list.Count - 1);
                }
                while(_list.Count < newSize)
                {
                    Type elementType = listType.GetGenericArguments()[0];
                    object defaultVal = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;
                    _list.Add(defaultVal);
                }
            }

            TypeDescriptor.Refresh(component);
        }

        private object context_Bypass_Owner(object comp) => comp is FilteredPropertyWrapper wrapper ? wrapper.GetPropertyOwner(null) : comp;

        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component)
        {
        }
        public override bool ShouldSerializeValue(object component) => false;
    }

    // ====================================================
    // VIRTUAL PROPERTY FOR INDIVIDUAL SLOTS (Element [0], Element [1]...)
    // ====================================================
    public class CollectionIndexDescriptor : PropertyDescriptor
    {
        private readonly IList _list;
        private readonly int _index;

        public CollectionIndexDescriptor(PropertyDescriptor parentProp, IList list, int index)
            : base($"Element [{index}]", new Attribute[] { new CategoryAttribute("Elements") })
        {
            _list = list;
            _index = index;
        }

        public override Type ComponentType => typeof(IList);
        public override bool IsReadOnly => false;
        public override Type PropertyType => _list.GetType().IsArray ? _list.GetType().GetElementType() : _list.GetType().GetGenericArguments()[0];

        public override object GetValue(object component) => _index < _list.Count ? _list[_index] : null;

        public override void SetValue(object component, object value)
        {
            if(_index < _list.Count)
            {
                _list[_index] = value;
                TypeDescriptor.Refresh(component);
            }
        }

        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component)
        {
        }
        public override bool ShouldSerializeValue(object component) => false;
    }

    public class DataComponentReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true; // Forces dropdown-only selection

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if(context?.PropertyDescriptor == null)
                return null;

            Type targetType = context.PropertyDescriptor.PropertyType; // e.g. SwordDataComponent
            List<DataComponent> choices = new List<DataComponent> { null }; // Allow assigning "None"

            // Scrape your loaded database manager for entries matching this exact derived sub-class type
            // (Assumes you have a global point of access to your loaded editor databases)
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
                // Match the picked text name back to its true memory reference instance pointer
                var values = GetStandardValues(context);
                foreach(DataComponent choice in values)
                {
                    if(choice == null && str == "None (DataAsset)")
                        return null;
                    if(choice != null && choice.DisplayName == str)
                        return choice;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

    // ==========================================
    // 2. GAME OBJECT DROPDOWN CONVERTER (Scene Picker)
    // ==========================================
    public class GameObjectReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<GameObject> sceneObjects = new List<GameObject> { null };

            // Hook this into wherever your active editing world/scene tracks its live object matrix!
            if(EditorContextManager.ActiveLoadedScene != null)
            {
                sceneObjects.AddRange(EditorContextManager.ActiveLoadedScene.Entities.GetSerializableEntities());
            }

            return new StandardValuesCollection(sceneObjects);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value is GameObject go)
                    return $"{go.Name} (ID: {go.Id.ToString().Substring(0, 5)})";
                return "None (GameObject)";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                foreach(GameObject go in GetStandardValues(context))
                {
                    if(go == null && str == "None (GameObject)")
                        return null;
                    if(go != null && $"{go.Name} (ID: {go.Id.ToString().Substring(0, 5)})" == str)
                        return go;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

    // ==========================================
    // 3. COMPONENT DROPDOWN CONVERTER (Cross-Component Links)
    // ==========================================
    public class ComponentReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if(context?.PropertyDescriptor == null)
                return null;
            Type targetComponentType = context.PropertyDescriptor.PropertyType; // e.g. SpriteRendererComponent

            List<object> validComponents = new List<object> { null };

            // Scrape the active scene to find components that inherit from or match the target field type
            if(EditorContextManager.ActiveLoadedScene != null)
            {
                foreach(var go in EditorContextManager.ActiveLoadedScene.Entities.GetSerializableEntities())
                {
                    foreach(var comp in go.Components)
                    {
                        if(targetComponentType.IsAssignableFrom(comp.GetType()))
                        {
                            validComponents.Add(comp);
                        }
                    }
                }
            }

            return new StandardValuesCollection(validComponents);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value != null)
                {
                    // If your component has an owner back-pointer, show which GameObject houses it!
                    var ownerProp = value.GetType().GetProperty("Owner");
                    string ownerName = ownerProp != null && ownerProp.GetValue(value) is GameObject go ? go.Name : "Detached";
                    return $"{ownerName} -> {value.GetType().Name}";
                }
                return "None (Component)";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                foreach(var comp in GetStandardValues(context))
                {
                    if(comp == null && str == "None (Component)")
                        return null;
                    if(comp != null)
                    {
                        var ownerProp = comp.GetType().GetProperty("Owner");
                        string ownerName = ownerProp != null && ownerProp.GetValue(comp) is GameObject go ? go.Name : "Detached";
                        if($"{ownerName} -> {comp.GetType().Name}" == str)
                            return comp;
                    }
                }
            }
            return base.ConvertFrom(context, culture, value);
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
            // 👇 NEW: Detect if this property is a valid List or Array collection container
            if(typeof(System.Collections.IList).IsAssignableFrom(propType))
            {
                _customConverter = new InlineCollectionConverter();
            }
            // Your existing engine reference detection logic continues safely below...
            else if(propType.IsClass && propType != typeof(string))
            {
                if(typeof(DataComponent).IsAssignableFrom(propType))
                {
                    _customConverter = new DataComponentReferenceConverter();
                }
                else if(typeof(GameObject).IsAssignableFrom(propType) || propType.Name == "GameObject")
                {
                    _customConverter = new GameObjectReferenceConverter();
                }
                else if(propType.Name.EndsWith("Component") || typeof(Component).IsAssignableFrom(propType))
                {
                    _customConverter = new ComponentReferenceConverter();
                }
                else
                {
                    _customConverter = new EngineObjectReferenceConverter();
                }
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