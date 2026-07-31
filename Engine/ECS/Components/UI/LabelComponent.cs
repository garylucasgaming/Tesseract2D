using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{
    public class LabelComponent : UIElementComponent
    {

        private string _text = "";
        private SpriteFont? _font;
        private int _size = 12;
        private int _spacing = 1;
        private bool _useKerning = false;
        private bool _isBold = false;
        private bool _isItalic = false;
        private Color _textColor = Color.White;

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        public SpriteFont? Font
        {
            get => _font;
            set => _font = value;
        }

      

        public Color TextColor
        {
            get => _textColor;
            set => _textColor = value;
        }

        public void Draw(SpriteBatch sb)
        {
            sb.DrawString(Font, Text, gameObject.GetComponent<TransformComponent>().WorldPosition, TextColor);
        }

    }
}
