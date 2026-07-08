using System;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GISM.Core.Serializer;
using GISM.Core.Attributes; // Added for [GISMIgnore]

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


        private Color _colour = Color.White;

        // TRAP: Texture2D is a volatile GPU resource. Trying to serialize it deep-scans 
        // XNA internal graphics state and breaks. We ignore this completely and rely 
        // on TexturePath to reconstruct it during asset loading.
        [Browsable(false)]
        [GISMIgnore]
        public Texture2D? Texture
        {
            get; set;
        }

        public string? TexturePath
        {
            get; set;
        }

        // The serializer naturally skips MulticastDelegates, but explicitly tagging it keeps intent clear.
        
        public Action<SpriteComponent>? onSpriteChanged;

        // Allows using a single master spritesheet for tiles/items/creatures
        [Browsable(false)]
        public Rectangle? SourceRectangle { get; set; } = null;
        [Browsable(true)]
        public string ? SpriteSheetPath { get; set; }
      
        [Browsable(true)]
        public Vector3 Colour {
            get
            {
               var c = new Vector3(_colour.R, _colour.G, _colour.B);
                return c;
            }
            set
            {

                
                _colour.R = (byte)value.X;
                _colour.G = (byte) value.Y;
                _colour.B = (byte) value.Z;
            } 
        }

        

        [Browsable(true)]
        public DrawLayer SortingLayer { get; set; } = DrawLayer.Default;

        //  MonoGame depth sorting requires a float between 0.0f and 1.0f
        [Browsable(true)]
        public float LayerDepth { get; set; } = 0.0f;

        [Browsable(true)]
        public SpriteEffects Effects { get; set; } = SpriteEffects.None;

        //Custom pivot overrides for precise tile/grid placement
        [Browsable(true)]
        public bool UseSpriteOrigin { get; set; } = false;

        [Browsable(true)]
        public Vector2 SprteOrigin { get; set; } = Vector2.Zero;

       
        public SpriteComponent()
        {

        }
    }
}