using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{
    public class StyleTag
    {
        public string Name
        {
        get; set; }

        public float? FontSize
        {
            get;set;
        }

        public Color? TextColor { get; set; }

        public Color? BackgroundColor { get; set; }

        public Color? HoverBackgroundColor { get; set; }

        public Color? PressedBackgroundColor { get; set; }

        public string FontAssetPath { get; set; }



    }
}
