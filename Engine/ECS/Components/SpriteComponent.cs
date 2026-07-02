using System;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.ECS.Components
{
    public enum DrawLayer
    {
        Background = 0,
        Default = 1,
        Foreground = 2,
        UI = 3
    }

    public class SpriteComponent : GameComponent
    {
        [Browsable(false)]
        public Texture2D? Texture
        {
            get; set;
        }

        public string? TexturePath
        {
            get; set;
        }

        public Action<SpriteComponent>? onSpriteChanged;

        // Allows using a single master spritesheet for tiles/items/creatures
        [Browsable(true)]
        public Rectangle? SourceRectangle { get; set; } = null;

        [Browsable(true)]
        public Color Color { get; set; } = Color.White;

        [Browsable(true)]
        public DrawLayer SortingLayer { get; set; } = DrawLayer.Default;

        //  MonoGame depth sorting requires a float between 0.0f and 1.0f
        [Browsable(true)]
        public float LayerDepth { get; set; } = 0.0f;

        [Browsable(true)]
        public SpriteEffects Effects { get; set; } = SpriteEffects.None;

        //Custom pivot overrides for precise tile/grid placement
        [Browsable(true)]
        public bool UseCustomOrigin { get; set; } = false;

        [Browsable(true)]
        public Vector2 CustomOrigin { get; set; } = Vector2.Zero;

        

        public SpriteComponent()
        {
            
        }
    }
}