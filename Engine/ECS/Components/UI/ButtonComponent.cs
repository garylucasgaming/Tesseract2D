using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Engine.Core.ECS.Components.UI
{
    public class ButtonComponent : UIElementComponent
    {

        private string _baseSprite = "";
        private string _hoverSprite = "";
        private string _clickedSprite = "";
        private Vector3 _hoverColor;
        private Vector3 _clickedColor;


        [Browsable(false)]
        public SpriteComponent? Sprite
        {
            get; set;
        }

        [Browsable(true)]
        [TypeConverter(typeof(TexturePathConverter))]
        public string? BaseSprite
        {
            get => _baseSprite;
            set
            {
                _baseSprite = value;
            }
        }

        [Browsable(true)]
        [TypeConverter(typeof(TexturePathConverter))]
        public string? HoverSprite
        {
            get => _hoverSprite;
            set
            {
                _hoverSprite = value;
            }
        }
        [Browsable(true)]
        public Vector3 HoverColor
        {
            get => _hoverColor;
            set => _hoverColor = value;
        }

        [Browsable(true)]
        [TypeConverter(typeof(TexturePathConverter))]
        public string? ClickedSprite
        {
            get => _clickedSprite;
            set
            {
                _clickedSprite = value;
            }
        }

        [Browsable (true)]
        public Vector3 ClickedColor
        {
            get => _clickedColor;
            set => _clickedColor = value;
        }

        public override void OnEnabled()
        {
            if(!gameObject.HasComponent<SpriteComponent>())
            {
                Sprite = gameObject.AddComponent<SpriteComponent>();
                Sprite.isUI = true;
            }
            else
            {
                Sprite = gameObject.GetComponent<SpriteComponent>();
                Sprite.isUI = true;
            }
        }


    }
}
