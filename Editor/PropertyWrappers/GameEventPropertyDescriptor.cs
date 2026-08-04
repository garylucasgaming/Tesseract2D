using Engine.Core.Runtime;
using Engine.Core.Serialization;
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

        public bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return _propertyName == nameof(GameEvent.TargetComponentTypeName) ||
                   _propertyName == nameof(GameEvent.MethodName) ||
                   _propertyName == nameof(GameEvent.TargetGameObject);
        }

        public bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
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

}
