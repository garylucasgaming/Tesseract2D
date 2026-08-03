using Engine.Core.Runtime;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{

    public class ListBoxItemComponent : PanelComponent
    {
        private bool _isSelected = false;
        private int _index = -1;

        [Browsable(false)]
        public LabelComponent? Label
        {
            get; set;
        }

        public event Action<ListBoxItemComponent>? ItemClicked;

        [Browsable(false)]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateVisuals();
            }
        }

        public int Index
        {
            get => _index;
            set => _index = value;
        }

        public object? DataContext { get; set; } = null;

        public void UpdateItemInput()
        {
            if(isClicked)
            {
                ItemClicked?.Invoke(this);
            }

            var sprite = gameObject.GetComponent<SpriteComponent>();
            if(sprite != null && !_isSelected)
            {
                sprite.Colour = isHovered
                    ? new Vector3(0.25f, 0.25f, 0.35f) // Hover tint
                    : new Vector3(0.12f, 0.08f, 0.20f); // Default surface tint
            }
        }

        private void UpdateVisuals()
        {
            var sprite = gameObject.GetComponent<SpriteComponent>();
            if(sprite != null)
            {
                sprite.Colour = _isSelected
                    ? new Vector3(0.15f, 0.35f, 0.65f) // Selected Synthwave accent
                    : new Vector3(0.12f, 0.08f, 0.20f); // Default surface tint
            }
        }
    }

}
