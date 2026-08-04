using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.PropertyWrappers
{
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
}
