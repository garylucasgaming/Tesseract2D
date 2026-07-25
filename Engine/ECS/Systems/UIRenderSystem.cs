using Engine.Core.ECS.Components;
using Engine.Core.ECS.Components.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Engine.Core.ECS.Systems
{
    public class UIRenderSystem : GameSystem
    {

        public List<SpriteComponent> _hookedComponents = new List<SpriteComponent>();
        public bool _needsSorting = false;

        public override IComponentQuery RequiredComponents
        {
            get; set;
        }

        public UISPACE CurrentRenderSpace { get; set; } = UISPACE.WorldSpace;

        public UIRenderSystem()
        {
            RequiredComponents = Query.Has<UIElementComponent>().And<TransformComponent>().And<SpriteComponent>();
            UsedInEditor = true;
            UpdatePolicy = SystemUpdatePolicy.FrameUpdate;
        }

        public override void Render(HashSet<GameObject> gameObjects, SpriteBatch spriteBatch)
        {
            
            foreach(var entity in gameObjects)
            {
                var uiComp = entity.GetComponent<UIElementComponent>();
                var transform = entity.GetComponent<TransformComponent>();
                var sprite = entity.GetComponent<SpriteComponent>(); // Optional: UI elements might have a background sprite

                if(uiComp == null || transform == null || !uiComp.IsVisible)
                    continue;

                if(uiComp.UISpace != CurrentRenderSpace)
                    continue;

                // Resolve background color using  tag/local override system
                Color bgColor = uiComp.ResolveBackgroundColor();

                // If the UI element has a sprite component (like a panel texture or button backing)
                if(sprite != null && sprite.Texture != null)
                {
                    Vector2 rawDimension = sprite.SourceRectangle.HasValue
                        ? new Vector2(sprite.SourceRectangle.Value.Width, sprite.SourceRectangle.Value.Height)
                        : new Vector2(sprite.Texture.Width, sprite.Texture.Height);

                    if(rawDimension.X > 0 && rawDimension.Y > 0)
                    {
                        Vector2 baseScale = new Vector2(transform.SizeX / rawDimension.X, transform.SizeY / rawDimension.Y);
                        Vector2 finalScale = baseScale * transform.Scale;

                        spriteBatch.Draw(
                            sprite.Texture,
                            transform.WorldPosition,
                            sprite.SourceRectangle,
                            bgColor,
                            transform.Rotation,
                            transform.OriginVector,
                            finalScale,
                            sprite.Effects,
                            sprite.LayerDepth
                        );
                    }
                }
                else
                {
                    // Fallback: If no texture is assigned, you can draw a solid color block 
                    
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

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
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
        }
    }
}