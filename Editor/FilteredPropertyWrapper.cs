using Engine.Core.Collections;
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

    // ====================================================
    // VIRTUAL PROPERTY FOR THE COLLECTION SIZE / COUNT
    // ====================================================
    public class CollectionSizeDescriptor : PropertyDescriptor
    {
        private readonly IList _list;
        private readonly PropertyDescriptor _parentProp;
        private readonly object _owner;

        // Added BrowsableAttribute(true) to survive PropertyGrid selection passes
        public CollectionSizeDescriptor(PropertyDescriptor parentProp, IList list, object owner)
            : base("Size", new Attribute[] { new CategoryAttribute("Collection Layout"), new BrowsableAttribute(true) })
        {
            _parentProp = parentProp;
            _list = list;
            _owner = owner;
        }

        public override Type ComponentType => typeof(IList);
        public override bool IsReadOnly => _list.IsFixedSize && !_list.GetType().IsArray;
        public override Type PropertyType => typeof(int);

        public override object GetValue(object component) => _list.Count;

        public override void SetValue(object component, object value)
        {
            int newSize = (int) value;
            if(newSize < 0 || newSize == _list.Count)
                return;

            Type listType = _list.GetType();

            if(listType.IsArray)
            {
                Type elementType = listType.GetElementType();
                Array newArray = Array.CreateInstance(elementType, newSize);
                int copyCount = Math.Min(_list.Count, newSize);
                Array.Copy((Array) _list, newArray, copyCount);

                if(_parentProp != null && _owner != null)
                {
                    _parentProp.SetValue(_owner, newArray);
                }
            }
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

            // Tell the master object inspector to completely rebuild its visual card tree
            if(_owner != null)
            {
                TypeDescriptor.Refresh(_owner);
            }
            else
            {
                TypeDescriptor.Refresh(component);
            }
        }

        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component)
        {
        }
        public override bool ShouldSerializeValue(object component) => false;
    }

    // ====================================================
    // VIRTUAL PROPERTY FOR INDIVIDUAL SLOTS (Element [0]...)
    // ====================================================
    public class CollectionIndexDescriptor : PropertyDescriptor
    {
        private readonly IList _list;
        private readonly int _index;
        private TypeConverter _customConverter;

        // FIX: Pass a safe internal identifier "Element_X" to the base layout engine
        public CollectionIndexDescriptor(PropertyDescriptor parentProp, IList list, int index)
            : base($"Element_{index}", new Attribute[] { new CategoryAttribute("Elements"), new BrowsableAttribute(true) })
        {
            _list = list;
            _index = index;
        }

        // FIX: Override DisplayName so humans see the clean Unity-style bracket layout!
        public override string DisplayName => $"Element [{_index}]";

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

        // FIX: Inject your custom dropdown converters into your collection element rows!
        public override TypeConverter Converter
        {
            get
            {
                if(_customConverter == null)
                {
                    Type propType = this.PropertyType;

                    if(propType.IsClass && propType != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(propType))
                    {
                        if(typeof(DataComponent).IsAssignableFrom(propType))
                            _customConverter = new DataComponentReferenceConverter();
                        else if(propType.Name == "GameObject" || propType.Name.Contains("GameObject"))
                            _customConverter = new GameObjectReferenceConverter();
                        else if(propType.Name.EndsWith("Component"))
                            _customConverter = new ComponentReferenceConverter();
                        else
                            _customConverter = new EngineObjectReferenceConverter();
                    }
                    else if(typeof(System.Collections.IList).IsAssignableFrom(propType))
                    {
                        _customConverter = new InlineCollectionConverter(); // Nested collection arrays support!
                    }
                }
                return _customConverter ?? base.Converter;
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

            Type targetType = context?.PropertyDescriptor?.PropertyType ?? typeof(DataComponent);
            
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

        // 👇 ADD THIS: Tells WinForms that this converter accepts string inputs from the dropdown!
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if(sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> sceneObjects = new List<object> { null };

            if(EditorContextManager.ActiveLoadedScene != null)
            {
                var entities = EditorContextManager.ActiveLoadedScene.Entities.GetSerializableEntities();
                if(entities != null)
                {
                    foreach(var entity in entities)
                    {
                        if(entity != null)
                        {
                            sceneObjects.Add(entity);
                        }
                    }
                }
            }

            return new StandardValuesCollection(sceneObjects);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value == null)
                    return "None (GameObject)";

                if(value is string str)
                    return str;

                if(value is GameObject go)
                {
                    return string.IsNullOrWhiteSpace(go.Name)
                        ? $"Unnamed GameObject ({value.GetType().Name})"
                        : go.Name;
                }

                var nameProp = value.GetType().GetProperty("Name");
                if(nameProp != null)
                {
                    string nameVal = nameProp.GetValue(value)?.ToString();
                    if(!string.IsNullOrWhiteSpace(nameVal))
                        return nameVal;
                }

                return $"Unnamed {value.GetType().Name}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                if(str == "None (GameObject)")
                    return null;

                var standardValues = GetStandardValues(context);
                if(standardValues != null)
                {
                    foreach(object choice in standardValues)
                    {
                        if(choice == null)
                            continue;

                        string choiceDisplayName = ConvertTo(context, culture, choice, typeof(string)) as string;
                        if(choiceDisplayName == str)
                            return choice; // Returns the actual live GameObject instance!
                    }
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

        // 1. Tell WinForms this converter accepts string inputs from the dropdown selection
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if(sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> validComponents = new List<object> { null }; // Default to 'None' option

            Type targetComponentType = context?.PropertyDescriptor?.PropertyType ?? typeof(object);

            if(EditorContextManager.ActiveLoadedScene != null)
            {
                var entities = EditorContextManager.ActiveLoadedScene.Entities.GetSerializableEntities();
                if(entities != null)
                {
                    foreach(var go in entities)
                    {
                        if(go?.Components == null)
                            continue;

                        // 👇 Iterate directly over the Dictionary Values to get the GameComponent instances
                        foreach(var comp in go.Components.Values)
                        {
                            if(comp == null)
                                continue;

                            // Check if the actual component instance matches the property type
                            if(targetComponentType == typeof(object) || targetComponentType.IsAssignableFrom(comp.GetType()))
                            {
                                validComponents.Add(comp); // Adds the actual GameComponent instance!
                            }
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
                if(value == null)
                    return "None (Component)";

                // If value is ALREADY a string, return it untouched so WinForms doesn't break
                if(value is string str)
                    return str;

                string ownerName = "Detached";
                Type compType = value.GetType();

                // 1. Try retrieving the owner from a FIELD (e.g. public GameObject gameObject;)
                var ownerField = compType.GetField("gameObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                              ?? compType.GetField("owner", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                              ?? compType.GetField("entity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

                object ownerObj = ownerField?.GetValue(value);

                // 2. Fallback to checking a PROPERTY (in case some components use { get; set; })
                if(ownerObj == null)
                {
                    var ownerProp = compType.GetProperty("gameObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                 ?? compType.GetProperty("owner", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                 ?? compType.GetProperty("entity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

                    ownerObj = ownerProp?.GetValue(value);
                }

                // 3. Extract the owner GameObject's Name
                if(ownerObj is GameObject go)
                {
                    ownerName = string.IsNullOrWhiteSpace(go.Name) ? "Unnamed GameObject" : go.Name;
                }
                else if(ownerObj != null)
                {
                    var nameProp = ownerObj.GetType().GetProperty("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    string nameVal = nameProp?.GetValue(ownerObj)?.ToString();
                    if(!string.IsNullOrWhiteSpace(nameVal))
                    {
                        ownerName = nameVal;
                    }
                }

                // 👇 Clean the component type name (e.g. "TransformComponent" -> "Transform")
                string cleanCompName = compType.Name;
                if(cleanCompName.EndsWith("Component"))
                {
                    cleanCompName = cleanCompName.Substring(0, cleanCompName.Length - "Component".Length);
                }

                return $"{ownerName} -> {cleanCompName}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                if(str == "None (Component)")
                    return null;

                var choices = GetStandardValues(context);
                if(choices != null)
                {
                    foreach(object comp in choices)
                    {
                        if(comp == null)
                            continue;

                        string choiceDisplayName = ConvertTo(context, culture, comp, typeof(string)) as string;
                        if(choiceDisplayName == str)
                            return comp; // Successfully returns the live component reference!
                    }
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }


    public class DatabaseReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> options = new List<object> { null }; // "None" choice

            object realTarget = GetRealInstance(context);
            Type? componentType = realTarget?.GetType();

            if(EditorContextManager.ActiveLoadedScene?.Database != null)
            {
                var dbManager = EditorContextManager.ActiveLoadedScene.Database;
                foreach(var db in dbManager.Databases)
                {
                    bool isMatch = false;

                    if(componentType != null)
                    {
                        // 1. Check if database explicitly matches this type name
                        if(!string.IsNullOrEmpty(db.DatabaseType) &&
                            (db.DatabaseType.Equals(componentType.Name, StringComparison.OrdinalIgnoreCase) ||
                             db.DatabaseType.Equals(componentType.FullName, StringComparison.OrdinalIgnoreCase)))
                        {
                            isMatch = true;
                        }
                        // 2. Or check if database holds items assignable to this component type
                        else if(db.ComponentDatabase.Values.Any(comp => comp != null && componentType.IsAssignableFrom(comp.GetType())))
                        {
                            isMatch = true;
                        }
                    }

                    if(isMatch || string.IsNullOrEmpty(db.DatabaseType))
                    {
                        if(!options.Contains(db))
                        {
                            options.Add(db);
                        }
                    }
                }
            }

            return new StandardValuesCollection(options);
        }

        private object GetRealInstance(ITypeDescriptorContext context)
        {
            object instance = context?.Instance;
            if(instance is FilteredPropertyWrapper wrapper)
                return wrapper.Target;
            return instance;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value is Database db)
                {
                    return string.IsNullOrWhiteSpace(db.Name) ? $"Database ({db.ID})" : db.Name;
                }
                return "None (Database)";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                if(str == "None (Database)" || str == "None")
                    return null;

                var choices = GetStandardValues(context);
                foreach(object choice in choices)
                {
                    if(choice is Database db)
                    {
                        string dbName = string.IsNullOrWhiteSpace(db.Name) ? $"Database ({db.ID})" : db.Name;
                        if(dbName == str || db.Name == str)
                            return db;
                    }
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
    public class DataReferenceDropdownConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<DataComponent> choices = new List<DataComponent> { null };

            object realTarget = GetRealInstance(context);
            Database targetDb = null;

            // Retrieve the live Database instance directly from the target component
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

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

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
                if(str == "None (DataAsset)" || str == "None")
                    return null;

                var choices = GetStandardValues(context);
                foreach(DataComponent choice in choices)
                {
                    if(choice == null)
                        continue;

                    string choiceDisplayName = ConvertTo(context, culture, choice, typeof(string)) as string;
                    if(choiceDisplayName == str || choice.DisplayName == str)
                        return choice;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
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

                    // 💡 ALLOW DATABASES: Explicitly allow Database types and DatabaseReference properties through
                    bool isEngineObject = typeof(GameObject).IsAssignableFrom(propType) ||
                                          typeof(Database).IsAssignableFrom(propType) ||
                                          typeof(DataComponent).IsAssignableFrom(propType) ||
                                          propType.Name == "GameObject" ||
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

            // 💡 DYNAMIC INTERCEPTION FOR DATA LINKING PROPERTIES
            if(baseDescriptor.Name.Equals("DatabaseReference", StringComparison.OrdinalIgnoreCase) ||
                typeof(Engine.Core.Collections.Database).IsAssignableFrom(propType))
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
                else if(propType.Name.EndsWith("Component") || typeof(Engine.Core.ECS.GameComponent).IsAssignableFrom(propType))
                    _customConverter = new ComponentReferenceConverter();
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
