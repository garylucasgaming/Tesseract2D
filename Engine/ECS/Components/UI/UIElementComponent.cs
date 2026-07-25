using Engine.Core.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Color = Microsoft.Xna.Framework.Color;

namespace Engine.Core.ECS.Components.UI
{
    public enum UISPACE
    {
        ScreenSpace, WorldSpace
    }

    public abstract class UIElementComponent : GameComponent
    {

        private bool __visible;
        private bool _enabled;
        private Color? _bgColor;
        private Color? _textColor;
        private float? _fontSize;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;
        private UISPACE _uiSpace;
        private bool _usePrecentageSize = false;
        private float _widthPercent = 0.2f;
        private float _heightPercent = 0.1f;

        public bool IsVisible {
            get => __visible; 
            set => __visible = value;
        }
        public bool IsEnabled { 
            get => _enabled;
            set => _enabled = value;
        }

        public bool UsePercentageSize { get => _usePrecentageSize; set => _usePrecentageSize = value; } 
        public float WidthPercentage {
            get => _widthPercent; set => _widthPercent = value;
        }
        public float HeightPercentage { get => _heightPercent; set => _heightPercent = value; }

        public UISPACE UISpace
        {
            get => _uiSpace;
            set => _uiSpace = value;
        }
        public List<string> StyleTags
        {
            get; set;
        } = new List<string>();

        public Color? LocalBackgroundColor { 
            get => _bgColor;
            set => _bgColor = value;
        }
        public float? LocalFontSize { 
            get => _fontSize;
            set => _fontSize = value;
        }

        public bool IsHovered
        {
            get => _isHovered;
            set => _isHovered = value;
        }

        public Color? TextColor
        {
            get => _textColor;
            set => _textColor = value;
        }
        public bool IsPressed
        {
            get => _isPressed;
            set => _isPressed = value;
        }
        public bool IsFocused
        {
            get => _isFocused;
            set => _isFocused = value;
        }

        // Event callbacks
        public Action<UIElementComponent>? OnClick
        {
            get; set;
        }
        public Action<UIElementComponent>? OnHoverEnter
        {
            get; set;
        }
        public Action<UIElementComponent>? OnHoverExit
        {
            get; set;
        }
      

        public Color ResolveBackgroundColor()
        {
            // 1. Check if there's a local override set directly on this element
            if(LocalBackgroundColor.HasValue)
                return LocalBackgroundColor.Value;

            // 2. Check the assigned StyleTags in order
            foreach(var tagName in StyleTags)
            {
                var style = StyleManager.GetStyle(tagName);
                if(style?.BackgroundColor != null)
                    return style.BackgroundColor.Value;
            }

            // 3. Absolute engine default fallback
            return Color.Gray;
        }

        public float ResolveFontSize()
        {
            if(LocalFontSize.HasValue)
                return LocalFontSize.Value;

            foreach(var tagName in StyleTags)
            {
                var style = StyleManager.GetStyle(tagName);
                if(style?.FontSize != null)
                    return style.FontSize.Value;
            }

            return 12f; // Default font size
        }

    }
}
