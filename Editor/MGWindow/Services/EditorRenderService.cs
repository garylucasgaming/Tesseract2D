

using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using Engine.Editor.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Color = Microsoft.Xna.Framework.Color;

namespace Engine.Editor.MGWindow.Services
{
    public class EditorRenderService
    {
        private const float HandleLength = 50f;
        private const float ClickTolerance = 8f; // Interaction area diameter

        public void RenderSceneViewport(SpriteBatch spriteBatch, GameScene activeScene, GameObject selectedGo)
        {
            foreach(var entity in activeScene.Entities.GetSerializableEntities())
            {
                if(!entity.isActive)
                    continue;

                var transform = entity.GetComponent<TransformComponent>();
                if(transform == null)
                {
                    Log.Info($"transform of name:{entity.Name}, id:{entity.Id} returned as null");
                    continue;
                }

                Vector2 position = transform.WorldPosition;
                bool isSelected = (selectedGo != null && entity.Id == selectedGo.Id);

                // Draw base object structural anchors (Always stays at true WorldPosition pivot)
                Color centerColor = isSelected ? Color.LimeGreen : new Color(200, 200, 200, 120);
                GizmoRenderer.DrawCircle(spriteBatch, position, isSelected ? 8 : 5, 12, centerColor);
                GizmoRenderer.DrawPoint(spriteBatch, position, Color.White, 2);

                // Render axis lines and bounding size boxes if the element is currently selected
                if(isSelected)
                {
                    // Draw original X/Y translation lines directly from the pivot anchor
                    GizmoRenderer.DrawMoveGizmo(spriteBatch, position, HandleLength, 2);

                    // --- Compute Bounding Frame Size & Adjusted Layout Position ---
                    Vector2 scale = transform.Scale;
                    Vector2 size = transform.Size;

                    float currentWidth = size.X * scale.X;
                    float currentHeight = size.Y * scale.Y;

                    // NEW: Use the origin-neutral top left corner for the box layout math
                    Vector2 baseCorner = transform.RenderTopLeft;

                    Vector2 topRight = baseCorner + new Vector2(currentWidth, 0);
                    Vector2 bottomLeft = baseCorner + new Vector2(0, currentHeight);
                    Vector2 bottomRight = baseCorner + new Vector2(currentWidth, currentHeight);

                    // 1. Draw Bounding Box Outline relative to our safe base corner
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, topRight, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, bottomLeft, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, bottomLeft, bottomRight, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, topRight, bottomRight, Color.DarkGray, 1);

                    // 2. Define Handle Box Centers relative to baseCorner
                    Vector2 rightHandleCenter = baseCorner + new Vector2(currentWidth, currentHeight * 0.5f);
                    Vector2 bottomHandleCenter = baseCorner + new Vector2(currentWidth * 0.5f, currentHeight);
                    Vector2 cornerHandleCenter = bottomRight;

                    // 3. Draw the interactive handle points
                    // Right Handle (Width)
                    GizmoRenderer.DrawPoint(spriteBatch, rightHandleCenter, Color.Blue, (int) ClickTolerance);
                    GizmoRenderer.DrawPoint(spriteBatch, rightHandleCenter, Color.LightBlue, (int) ClickTolerance - 2);

                    // Bottom Handle (Height)
                    GizmoRenderer.DrawPoint(spriteBatch, bottomHandleCenter, Color.Blue, (int) ClickTolerance);
                    GizmoRenderer.DrawPoint(spriteBatch, bottomHandleCenter, Color.LightBlue, (int) ClickTolerance - 2);

                    // Corner Handle (Both)
                    GizmoRenderer.DrawPoint(spriteBatch, cornerHandleCenter, Color.DarkRed, (int) ClickTolerance);
                    GizmoRenderer.DrawPoint(spriteBatch, cornerHandleCenter, Color.Orange, (int) ClickTolerance - 2);
                }
            }
        }
    }
}

