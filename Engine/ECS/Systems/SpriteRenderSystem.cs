using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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

        public override SystemUpdatePolicy UpdatePolicy => SystemUpdatePolicy.FrameUpdate;

        private List<SpriteComponent> _hookedComponents = new List<SpriteComponent>();

      

        public SpriteRenderSystem()
        {

            RequiredComponents = Query.Has<SpriteComponent>().And<TransformComponent>();
            UsedInEditor = true;

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
                    if(sc.SourceRectangle != null && sc.SourceRectangle.HasValue)
                    {
                        sb.Draw(sc.Texture, transform.WorldPosition, sc.SourceRectangle.Value, sc.Color, transform.Rotation, transform.OriginVector, transform.Scale, sc.Effects, sc.LayerDepth);
                    }
                    else
                    {
                        sb.Draw(sc.Texture, transform.WorldPosition, null, sc.Color, transform.Rotation, transform.OriginVector, transform.Scale, sc.Effects, sc.LayerDepth);
                    }
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
