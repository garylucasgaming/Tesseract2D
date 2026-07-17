using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;

namespace Engine.Core.ECS.Systems
{
    public class SpriteRenderSystem : GameSystem
    {
        public override IComponentQuery RequiredComponents
        {
            get;
            set;
        }

        private bool _needsSorting = true;

        
        private List<SpriteComponent> _hookedComponents = new List<SpriteComponent>();

      

        public SpriteRenderSystem()
        {

            RequiredComponents = Query.Has<SpriteComponent>().And<TransformComponent>();
            UsedInEditor = true;
            UpdatePolicy = SystemUpdatePolicy.FrameUpdate;

        }


        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
            

            // hook new components
            foreach(var entity in gameObjects)
            {
                var sprite = entity.GetComponent<SpriteComponent>();
                if(sprite == null)
                    continue;

                if(!_hookedComponents.Contains(sprite))
                {
                    sprite.onSpriteChanged += HandleSpriteModified;
                    _hookedComponents.Add(sprite);
                    _needsSorting = true;
                }
            }

            if(_needsSorting)
            {
                ReSortSprites();
            }
            
               

        }


        

        public override void Render(HashSet<GameObject> gameObjects, SpriteBatch spriteBatch)
        {
            DrawSprites(spriteBatch);
        }



        public void ReSortSprites()
        {
            _hookedComponents.Sort((a, b) => a.SortingLayer.CompareTo(b.SortingLayer));
            _needsSorting = false;
        }

        public void DrawSprites(SpriteBatch sb)
        {
            foreach(var sc in _hookedComponents)
            {
                var transform = sc.gameObject?.GetComponent<TransformComponent>();
                if(transform != null)
                {
                    if(sc.Texture == null)
                    {
                        continue;
                    }

                    Vector3 sv = sc.Colour;
                    Color c = new Color(sv.X, sv.Y, sv.Z);

                    // 1. Determine the raw dimensions of the texture we are sampling.
                    // If the sprite component has a specific source rectangle (e.g. for spritesheets),
                    // we use that size. Otherwise, we use the entire texture's width and height.
                    Vector2 rawDimension = sc.SourceRectangle.HasValue
                        ? new Vector2(sc.SourceRectangle.Value.Width, sc.SourceRectangle.Value.Height)
                        : new Vector2(sc.Texture.Width, sc.Texture.Height);

                    // 2. Prevent a nasty division-by-zero crash if an asset fails to load
                    if(rawDimension.X == 0 || rawDimension.Y == 0)
                        continue;

                    // 3. Calculate the scale factor required to force the raw image 
                    // into the bounding box defined by transform.SizeX and SizeY.
                    Vector2 baseScale = new Vector2(
                        transform.SizeX / rawDimension.X,
                        transform.SizeY / rawDimension.Y
                    );

                    // 4. Combine it with the entity's local scale modifier (for dynamic squash/stretch)
                    Vector2 finalScale = baseScale * transform.Scale;

                    // 5. Draw it! Pass the calculated finalScale instead of transform.Scale.
                    sb.Draw(
                        sc.Texture,
                        transform.WorldPosition,
                        sc.SourceRectangle, // Keeps its clean value (null for full image, or a spritesheet slice)
                        c,
                        transform.Rotation,
                        transform.OriginVector,
                        finalScale, // 👈 The magic happens here
                        sc.Effects,
                        sc.LayerDepth
                    );
                }
            }
        }

        public void LoadSprites(ContentManager cm)
        {
            foreach(var sc in _hookedComponents)
            {
                if(!sc.isSpriteLoaded)
                {
                    sc.LoadSprite(cm);
                }
            }
        }

        public void HandleSpriteModified(SpriteComponent sprite)
        {
            // Handle sprite changes here, e.g., update rendering data
            // This is where you would implement logic to respond to sprite changes
            _needsSorting = true;

        }
    }
}
