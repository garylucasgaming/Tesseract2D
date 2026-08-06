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

        [Browsable(false)]
        public ListBoxComponent? ParentListBox { get; set; } = null;

        public event Action<ListBoxItemComponent>? ItemClicked;

        [Browsable(false)]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateVisualState();
            }
        }

        public string Text
        {
            get => Label?.Text ?? string.Empty;
            set
            {
                if(Label != null)
                {
                    Label.Text = value;
                }
            }
        }

        public float TextSize
        {
            get => Label?.TextSize ?? 12;
            set
            {
                if(Label != null)
                {
                    Label.TextSize = value;
                }
            }
        }

        public int Index
        {
            get => _index;
            set => _index = value;
        }

        public object? DataContext { get; set; } = null;

        public override void OnEnabled()
        {
            base.OnEnabled();

            if(gameObject.HasComponent<LabelComponent>())
            {
                Label = gameObject.GetComponent<LabelComponent>();
            }
            else
            {
                Label = gameObject.AddComponent<LabelComponent>();
                
            }


            InitLabel();
            UpdateVisualState();
        }

        public void InitLabel()
        {
            if(Label != null)
            {
                Label.FontAssetPath = "defaultFont";
                Label.gameObject.GetComponent<TransformComponent>().Scale = new Vector2(1f, 1f);

            }
        }

        private void UpdateVisualState()
        {
            // Automatically tint the panel sprite to show selection state visually
            if(Sprite != null)
            {
                if(_isSelected)
                {
                    Sprite.Colour = new Vector3(0.2f, 0.4f, 0.8f); // Selection highlight tint
                }
                else
                {
                    Sprite.Colour = new Vector3(1f, 1f, 1f); // Default background tint
                }
            }
        }

        // Call this from your input/mouse interaction system when clicked
        public void OnClick()
        {
            ItemClicked?.Invoke(this);
            if(ParentListBox != null)
            {
                ParentListBox.SelectedIndex = Index;
            }
        }
    }

}
