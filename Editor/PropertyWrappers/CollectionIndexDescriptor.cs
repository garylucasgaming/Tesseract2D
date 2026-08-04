using Engine.Core.ECS.Components;
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
}
