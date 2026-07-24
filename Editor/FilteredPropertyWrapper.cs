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
    // 1. DATA COMPONENT REFERENCE CONVERTER
    public class DataComponentReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false; // Allow cell editing

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            Type targetType = context?.PropertyDescriptor?.PropertyType ?? typeof(DataComponent);
            List<DataComponent> choices = new List<DataComponent> { null };

            if(EditorContextManager.ActiveLoadedScene?.Database?.Databases != null)
            {
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
                str = str.Trim();
                if(string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return null;

                var choices = GetStandardValues(context);
                if(choices != null)
                {
                    // 1. Exact match
                    foreach(DataComponent choice in choices)
                    {
                        if(choice == null)
                            continue;
                        if(choice.DisplayName != null && choice.DisplayName.Equals(str, StringComparison.OrdinalIgnoreCase))
                            return choice;
                    }

                    // 2. Partial match
                    foreach(DataComponent choice in choices)
                    {
                        if(choice == null)
                            continue;
                        if(choice.DisplayName != null && choice.DisplayName.Contains(str, StringComparison.OrdinalIgnoreCase))
                            return choice;
                    }
                }

                // 💡 Safe Fallback: Unrecognized string safely reverts to null (None) instead of throwing an exception!
                return null;
            }
            return null;
        }
    }

    // 2. GAME OBJECT REFERENCE CONVERTER
    public class GameObjectReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

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
                str = str.Trim();
                if(string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return null;

                var choices = GetStandardValues(context);
                if(choices != null)
                {
                    // 1. Exact match
                    foreach(object choice in choices)
                    {
                        if(choice == null)
                            continue;
                        string choiceDisplayName = ConvertTo(context, culture, choice, typeof(string)) as string;
                        if(choiceDisplayName != null && choiceDisplayName.Equals(str, StringComparison.OrdinalIgnoreCase))
                            return choice;
                    }

                    // 2. Partial match
                    foreach(object choice in choices)
                    {
                        if(choice == null)
                            continue;
                        string choiceDisplayName = ConvertTo(context, culture, choice, typeof(string)) as string;
                        if(choiceDisplayName != null && choiceDisplayName.Contains(str, StringComparison.OrdinalIgnoreCase))
                            return choice;
                    }
                }

                return null; // Fallback safely to None
            }

            return null;
        }
    }

    // 3. COMPONENT REFERENCE CONVERTER
    public class ComponentReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> validComponents = new List<object> { null };
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

                        foreach(var comp in go.Components.Values)
                        {
                            if(comp == null)
                                continue;

                            if(targetComponentType == typeof(object) || targetComponentType.IsAssignableFrom(comp.GetType()))
                            {
                                validComponents.Add(comp);
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

                if(value is string str)
                    return str;

                string ownerName = "Detached";
                Type compType = value.GetType();

                var ownerField = compType.GetField("gameObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                              ?? compType.GetField("owner", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                              ?? compType.GetField("entity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

                object ownerObj = ownerField?.GetValue(value);

                if(ownerObj == null)
                {
                    var ownerProp = compType.GetProperty("gameObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                 ?? compType.GetProperty("owner", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                 ?? compType.GetProperty("entity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

                    ownerObj = ownerProp?.GetValue(value);
                }

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
                str = str.Trim();
                if(string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return null;

                var choices = GetStandardValues(context);
                if(choices != null)
                {
                    // 1. Exact match
                    foreach(object comp in choices)
                    {
                        if(comp == null)
                            continue;
                        string choiceDisplayName = ConvertTo(context, culture, comp, typeof(string)) as string;
                        if(choiceDisplayName != null && choiceDisplayName.Equals(str, StringComparison.OrdinalIgnoreCase))
                            return comp;
                    }

                    // 2. Partial match
                    foreach(object comp in choices)
                    {
                        if(comp == null)
                            continue;
                        string choiceDisplayName = ConvertTo(context, culture, comp, typeof(string)) as string;
                        if(choiceDisplayName != null && choiceDisplayName.Contains(str, StringComparison.OrdinalIgnoreCase))
                            return comp;
                    }
                }

                return null; // Fallback safely to None
            }

            return null;
        }
    }

    // 4. DATABASE REFERENCE CONVERTER
    public class DatabaseReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> options = new List<object> { null };

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
                        if(!string.IsNullOrEmpty(db.DatabaseType) &&
                            (db.DatabaseType.Equals(componentType.Name, StringComparison.OrdinalIgnoreCase) ||
                             db.DatabaseType.Equals(componentType.FullName, StringComparison.OrdinalIgnoreCase)))
                        {
                            isMatch = true;
                        }
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

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

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
                str = str.Trim();
                if(string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return null;

                var choices = GetStandardValues(context);
                foreach(object choice in choices)
                {
                    if(choice is Database db)
                    {
                        string dbName = string.IsNullOrWhiteSpace(db.Name) ? $"Database ({db.ID})" : db.Name;
                        if(dbName.Equals(str, StringComparison.OrdinalIgnoreCase) || (db.Name != null && db.Name.Contains(str, StringComparison.OrdinalIgnoreCase)))
                            return db;
                    }
                }

                return null;
            }
            return null;
        }
    }

    // 5. DATA REFERENCE DROPDOWN CONVERTER
    public class DataReferenceDropdownConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<DataComponent> choices = new List<DataComponent> { null };

            object realTarget = GetRealInstance(context);
            Database targetDb = null;

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

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

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
                str = str.Trim();
                if(string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return null;

                var choices = GetStandardValues(context);
                foreach(DataComponent choice in choices)
                {
                    if(choice == null)
                        continue;

                    string choiceDisplayName = ConvertTo(context, culture, choice, typeof(string)) as string;
                    if(choiceDisplayName != null && (choiceDisplayName.Equals(str, StringComparison.OrdinalIgnoreCase) || choiceDisplayName.Contains(str, StringComparison.OrdinalIgnoreCase)))
                        return choice;
                }

                return null;
            }
            return null;
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

    public class GameEventPropertyDescriptor : PropertyDescriptor
    {
        private readonly string _propertyName;
        private readonly Type _propertyType;
        private readonly TypeConverter? _customConverter;

        public GameEventPropertyDescriptor(string propertyName, Type propertyType)
            : base(propertyName, new Attribute[] { new CategoryAttribute("Event Binding"), new BrowsableAttribute(true) })
        {
            _propertyName = propertyName;
            _propertyType = propertyType;

            if(_propertyName == nameof(GameEvent.TargetGameObject))
            {
                _customConverter = new GameObjectReferenceConverter();
            }
            else if(_propertyName == nameof(GameEvent.TargetComponentTypeName))
            {
                _customConverter = new GameEventComponentTypeConverter();
            }
            else if(_propertyName == nameof(GameEvent.MethodName))
            {
                _customConverter = new GameEventMethodConverter(); // 💡 Hook up the method dropdown converter here!
            }
        }

        public override TypeConverter Converter => _customConverter ?? base.Converter;

        public override string DisplayName => _propertyName switch
        {
            nameof(GameEvent.TargetGameObject) => "Target Object",
            nameof(GameEvent.TargetComponentTypeName) => "Target Component",
            nameof(GameEvent.MethodName) => "Target Method",
            _ => _propertyName
        };

        public override Type ComponentType => typeof(GameEvent);
        public override bool IsReadOnly => false;
        public override Type PropertyType => _propertyType;

        public override object GetValue(object component)
        {
            if(component is GameEvent gameEvent)
            {
                var prop = typeof(GameEvent).GetProperty(_propertyName);
                return prop?.GetValue(gameEvent);
            }
            return null;
        }

        public override void SetValue(object component, object value)
        {
            if(component is GameEvent gameEvent)
            {
                var prop = typeof(GameEvent).GetProperty(_propertyName);
                if(prop != null)
                {
                    // If a string value comes through the grid edit cell, use the custom converter to parse it back to an object
                    if(value is string strValue && Converter != null && Converter.CanConvertFrom(typeof(string)))
                    {
                        value = Converter.ConvertFrom(strValue);
                    }

                    // Reset downstream dependencies if upstream target changes
                    if(_propertyName == nameof(GameEvent.TargetGameObject))
                    {
                        gameEvent.TargetComponentTypeName = string.Empty;
                        gameEvent.MethodName = string.Empty;
                        gameEvent.ClearCache();
                    }
                    else if(_propertyName == nameof(GameEvent.TargetComponentTypeName))
                    {
                        gameEvent.MethodName = string.Empty;
                        gameEvent.ClearCache();
                    }

                    prop.SetValue(gameEvent, value);
                    gameEvent.ClearCache();

                    TypeDescriptor.Refresh(component);
                }
            }
        }

        public  bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return _propertyName == nameof(GameEvent.TargetComponentTypeName) ||
                   _propertyName == nameof(GameEvent.MethodName) ||
                   _propertyName == nameof(GameEvent.TargetGameObject);
        }

        public  bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public  StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var gameEvent = context?.Instance as GameEvent;

            // 1. Target GameObject Dropdown
            if(_propertyName == nameof(GameEvent.TargetGameObject))
            {
                var objects = new List<object> { null };
                if(EditorContextManager.ActiveLoadedScene != null)
                {
                    var entities = EditorContextManager.ActiveLoadedScene.Entities.GetSerializableEntities();
                    if(entities != null)
                        objects.AddRange(entities.Where(e => e != null));
                }
                return new StandardValuesCollection(objects);
            }

            // 2. Component Type Dropdown
            if(_propertyName == nameof(GameEvent.TargetComponentTypeName))
            {
                var componentNames = new List<string> { string.Empty };
                if(gameEvent?.TargetGameObject?.Components != null)
                {
                    foreach(var comp in gameEvent.TargetGameObject.Components.Values)
                    {
                        if(comp != null)
                        {
                            string typeName = comp.GetType().Name;
                            if(!componentNames.Contains(typeName))
                            {
                                componentNames.Add(typeName);
                            }
                        }
                    }
                }
                return new StandardValuesCollection(componentNames);
            }

            // 3. Method Name Dropdown
            if(_propertyName == nameof(GameEvent.MethodName))
            {
                var methodNames = new List<string> { string.Empty };
                if(gameEvent?.TargetGameObject != null && !string.IsNullOrEmpty(gameEvent.TargetComponentTypeName))
                {
                    var targetComp = gameEvent.TargetGameObject.Components.Values
                        .FirstOrDefault(c => c.GetType().Name == gameEvent.TargetComponentTypeName || c.GetType().FullName == gameEvent.TargetComponentTypeName);

                    if(targetComp != null)
                    {
                        var validMethods = targetComp.GetType()
                            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
                            .Select(m => m.Name);

                        methodNames.AddRange(validMethods);
                    }
                }
                return new StandardValuesCollection(methodNames);
            }

            return GetStandardValues(context);
        }

        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component)
        {
        }
        public override bool ShouldSerializeValue(object component) => true;
    }

    public class GameEventComponentTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true; // Forces strict dropdown selection

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var choices = new List<string> { "None" };

            // Find the parent GameEvent instance from the context
            GameEvent? gameEvent = null;
            if (context?.Instance is GameEvent ge)
            {
                gameEvent = ge;
            }
            else if (context?.Instance is FilteredPropertyWrapper wrapper && wrapper.Target is GameEvent geTarget)
            {
                gameEvent = geTarget;
            }

            if (gameEvent?.TargetGameObject?.Components != null)
            {
                foreach (var comp in gameEvent.TargetGameObject.Components.Values)
                {
                    if (comp != null)
                    {
                        string fullName = comp.GetType().FullName ?? comp.GetType().Name;
                        if (!choices.Contains(fullName))
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
            if (destinationType == typeof(string))
            {
                if (value is string str && !string.IsNullOrEmpty(str) && str != "None")
                {
                    // Clean up namespace/suffix for display purposes in the cell
                    string displayName = str.Contains('.') ? str.Substring(str.LastIndexOf('.') + 1) : str;
                    if (displayName.EndsWith("Component"))
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
            if (value is string str)
            {
                str = str.Trim();
                if (string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return string.Empty;

                // If user selected a short display name, match it back to the full type name stored in the model
                var choices = GetStandardValues(context);
                foreach (string choice in choices)
                {
                    if (choice == "None") continue;
                    string shortName = choice.Contains('.') ? choice.Substring(choice.LastIndexOf('.') + 1) : choice;
                    if (shortName.Equals(str, StringComparison.OrdinalIgnoreCase) || choice.Equals(str, StringComparison.OrdinalIgnoreCase))
                    {
                        return choice;
                    }
                }
                return str;
            }
            return string.Empty;
        }
    }

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
