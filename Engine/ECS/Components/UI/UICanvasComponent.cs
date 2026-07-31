using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{
    public enum UISpace
    {
        Screen, World
    }

    public class UICanvasComponent : UIElementComponent
    {

        private UISpace _space;
        private DrawLayer _drawLayer;
        [Browsable(false)]
        public bool isInitialized = false;
        public UISpace Space
        {
            get => _space;
            set => _space = value;
        }
        private int _lastKnownDescendantCount = -1;


        public DrawLayer CanvasDrawLayer
        {
            get => _drawLayer;
            set => _drawLayer = value;
        }

        public void LoadSprites(ContentManager cm)
        {
            ReloadChildren();

            foreach(var child in ChildElements)
            {
                if(child.gameObject.HasComponent<UIImageComponent>())
                {
                    var imageComp = child.gameObject.GetComponent<UIImageComponent>();
                    var sprite = imageComp.Sprite;

                    if(sprite != null)
                    {
                        // Force canvas-level rules onto the child element
                        sprite.SortingLayer = CanvasDrawLayer;
                        sprite.isUI = true;

                        if(!sprite.isSpriteLoaded)
                        {
                            sprite.LoadSprite(cm);
                            sprite.isSpriteLoaded = true;
                        }
                    }
                }
            }
        }

        public void ReloadChildren()
        {
            ChildElements.Clear();

            if(gameObject == null)
                return;

            // 1. Grab every single descendant GameObject in the entire tree below this canvas
            var allDescendantGameObjects = gameObject.GetAllChildren();

            // 2. Extract any UI components found on those objects
            foreach(var descendant in allDescendantGameObjects)
            {
                if(descendant == null)
                    continue;

                var uiComp = descendant.GetComponent<UIElementComponent>();
                if(uiComp != null)
                {
                    ChildElements.Add(uiComp);

                    // Enforce canvas settings immediately upon rediscovery
                    if(uiComp is UIImageComponent imgComp && imgComp.Sprite != null)
                    {
                        imgComp.Sprite.SortingLayer = CanvasDrawLayer;
                        imgComp.Sprite.isUI = true;
                    }
                }
            }
        }

        public void CheckAndSyncHierarchy(ContentManager cm)
        {
            int currentCount = gameObject != null ? gameObject.GetAllChildren().Count : 0;

            // If a child was added/removed in the editor or at runtime, or we haven't initialized yet:
            if(currentCount != _lastKnownDescendantCount || !isInitialized)
            {
                ReloadChildren();
                LoadSprites(cm); // Re-applies textures and canvas layer rules to the new elements

                _lastKnownDescendantCount = currentCount;
                isInitialized = true;
            }
        }

        private void CollectUIElements(GameObject gameObject, List<UIElementComponent> childElements)
        {
            foreach(var child in gameObject.Children)
            {
                if(child == null)
                    continue;
                if(child.HasComponent<UIElementComponent>())
                {
                    var uiComp = child.GetComponent<UIElementComponent>();
                    childElements.Add(uiComp);

                    CollectUIElements(child, childElements);
                }
            }
        }

        public void Render(SpriteBatch sb, ContentManager cm)
        {

            CheckAndSyncHierarchy(cm);
            foreach(var child in ChildElements)
            {
                if(child == null)
                    continue;
                if(child.gameObject.HasComponent<SpriteComponent>())
                {
                    var sprite = child.gameObject.GetComponent<SpriteComponent>();
                    var transform = child.gameObject.GetComponent<TransformComponent>();

                    if(transform != null)
                    {
                        if(sprite.Texture == null)
                        {
                            continue;
                        }

                        Vector3 sv = sprite.Colour;
                        Color c = new Color(sv.X, sv.Y, sv.Z);

                        // 1. Determine the raw dimensions of the texture we are sampling.
                        // If the sprite component has a specific source rectangle (e.g. for spritesheets),
                        // we use that size. Otherwise, we use the entire texture's width and height.
                        Vector2 rawDimension = sprite.SourceRectangle.HasValue
                            ? new Vector2(sprite.SourceRectangle.Value.Width, sprite.SourceRectangle.Value.Height)
                            : new Vector2(sprite.Texture.Width, sprite.Texture.Height);

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

                        Vector2 drawPosition = (Space == UISpace.Screen)
                            ? transform.GetScreenSpacePosition()
                            : transform.WorldPosition;

                        // 5. Draw it! Pass the calculated finalScale instead of transform.Scale.
                        sb.Draw(
                            sprite.Texture,
                            drawPosition,
                            sprite.SourceRectangle, // Keeps its clean value (null for full image, or a spritesheet slice)
                            c,
                            transform.Rotation,
                            transform.OriginVector,
                            finalScale, 
                            sprite.Effects,
                            sprite.LayerDepth
                        );
                    }
                }
            }
        }
    }
}          

        
     



    