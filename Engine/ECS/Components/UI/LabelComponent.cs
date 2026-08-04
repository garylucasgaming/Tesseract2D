using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{
    public class LabelComponent : UIElementComponent
    {

        private string _text = "";
        private SpriteFont? _font;
        private string _fontAssetPath = "";
        private float _size = 12;
        private int _spacing = 1;
        private bool _useKerning = false;
        private bool _isBold = false;
        private bool _isItalic = false;
        private Vector3 _textColor = new Vector3(255,255,255);

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        [Browsable(false)]
        public SpriteFont? Font
        {
            get => _font;
            set => _font = value;
        }

      public float TextSize
        {
            get => _size;
            set => _size = value;
        }

        public Vector3 TextColor
        {
            get => _textColor;
            set => _textColor = value;
        }
        public string FontAssetPath
        {
            get => _fontAssetPath;
            set => _fontAssetPath = value;
        }


        public void SetText(string text)
        {
            Text = text;
        }

        public void SetTextColor(Vector3 color)
        {
            TextColor = color;
        }

        public void SetTextSize(float size)
        {
            TextSize = size;
        }

    }
}
