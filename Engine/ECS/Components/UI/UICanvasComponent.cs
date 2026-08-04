using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Common;
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
                // Broaden this to check for ANY SpriteComponent, ensuring Panels get processed
                if(child.gameObject.HasComponent<SpriteComponent>())
                {
                    var sprite = child.gameObject.GetComponent<SpriteComponent>();

                    if(sprite != null)
                    {
                        sprite.SortingLayer = CanvasDrawLayer;
                        sprite.isUI = true;

                        if(!sprite.isSpriteLoaded)
                        {
                            sprite.LoadSprite(cm);
                            sprite.isSpriteLoaded = true;
                        }
                    }
                }

                //loads spritefonts
                if(child.gameObject.HasComponent<LabelComponent>())
                {
                    Log.Info("child has label");
                    var label = child.gameObject.GetComponent<LabelComponent>();
                    if(label != null)
                    {
                        try
                        {
                            // Load the SpriteFont using ContentManager (pass path relative to content or asset root)
                            string relativePath = AssetManager.GetContentRelativePath(label.FontAssetPath, AssetType.Font);

                            label.Font = cm.Load<SpriteFont>(relativePath);
                            Log.Info($"[UI Info] Successfully loaded SpriteFont from path '{label.FontAssetPath}' for LabelComponent on GameObject '{child.gameObject.Name}'.");
                        }
                        catch(Exception ex)
                        {
                            Log.Error($"[UI Error] Failed to load SpriteFont from path '{label.FontAssetPath}': {ex.Message}");
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

            var allDescendantGameObjects = gameObject.GetAllChildren();

            foreach(var descendant in allDescendantGameObjects)
            {
                if(descendant == null)
                    continue;

                var uiComp = descendant.GetComponent<UIElementComponent>();
                if(uiComp != null)
                {
                    ChildElements.Add(uiComp);

                    // Again, broaden this to check for SpriteComponent
                    if(descendant.HasComponent<SpriteComponent>())
                    {
                        var sprite = descendant.GetComponent<SpriteComponent>();
                        if(sprite != null)
                        {
                            sprite.SortingLayer = CanvasDrawLayer;
                            sprite.isUI = true;
                        }
                    }
                }
            }
        }

        public void CheckAndSyncHierarchy(ContentManager cm)
        {
            int currentCount = gameObject != null ? gameObject.GetAllChildren().Count : 0;

            if(currentCount != _lastKnownDescendantCount || !isInitialized)
            {
                // LoadSprites already handles calling ReloadChildren(), so we just call this once
                LoadSprites(cm);

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
            bool shouldActAsScreenSpace = (Space == UISpace.Screen) && EditorContextManager.PlayState;
            var canvasTransform = gameObject.GetComponent<TransformComponent>();

            if(canvasTransform != null)
            {
                if(Space == UISpace.Screen)
                {
                    var viewport = sb.GraphicsDevice.Viewport;
                    canvasTransform.SizeX = viewport.Width/4;
                    canvasTransform.SizeY = viewport.Height/4;
                }
            }

            Vector2 canvasWorldPos = canvasTransform != null ? canvasTransform.WorldPosition : Vector2.Zero;

            foreach(var child in ChildElements)
            {
                if(child == null || child.gameObject == null)
                    continue;

                var transform = child.gameObject.GetComponent<TransformComponent>();
                if(transform == null)
                    continue; // Safely check transform once at the top

                Vector2 drawPosition = shouldActAsScreenSpace
                            ? transform.WorldPosition - canvasWorldPos
                            : transform.WorldPosition;

                // 1. Render Sprites (Completely independent block)
                if(child.gameObject.HasComponent<SpriteComponent>())
                {
                    var sprite = child.gameObject.GetComponent<SpriteComponent>();

                    if(sprite.Texture != null)
                    {
                        Vector3 sv = sprite.Colour;
                        Color c = new Color(sv.X, sv.Y, sv.Z);

                        Vector2 rawDimension = sprite.SourceRectangle.HasValue
                            ? new Vector2(sprite.SourceRectangle.Value.Width, sprite.SourceRectangle.Value.Height)
                            : new Vector2(sprite.Texture.Width, sprite.Texture.Height);

                        // Check for valid dimensions before drawing, but don't use 'continue' here!
                        if(rawDimension.X != 0 && rawDimension.Y != 0)
                        {
                            Vector2 baseScale = new Vector2(
                                transform.SizeX / rawDimension.X,
                                transform.SizeY / rawDimension.Y
                            );

                            Vector2 finalScale = baseScale * transform.Scale;

                            sb.Draw(
                                sprite.Texture,
                                drawPosition,
                                sprite.SourceRectangle,
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

                // 2. Render Labels (Now safely outside the sprite check)
                if(child.gameObject.HasComponent<LabelComponent>())
                {
                    var label = child.gameObject.GetComponent<LabelComponent>();
                    if(label.Font != null)
                    {
                        Color c = new Color(label.TextColor.X, label.TextColor.Y, label.TextColor.Z);
                        float baseFontSize = 48f; // Assuming 48 is the base size for scaling
                        float textScale = label.TextSize / baseFontSize;
                        Vector2 finalFontScale = new Vector2(textScale) * transform.Scale;
                        sb.DrawString(
                            label.Font,
                            label.Text,
                            drawPosition,
                            c,
                            transform.Rotation,
                            transform.OriginVector,
                            finalFontScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }
        }
    }
}          

        
     



    