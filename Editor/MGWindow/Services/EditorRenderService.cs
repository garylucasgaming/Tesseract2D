using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using Engine.Editor.MGWindow.Services.Engine.Editor.MGWindow.Services;
using Engine.Editor.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Color = Microsoft.Xna.Framework.Color;

namespace Engine.Editor.MGWindow.Services
{
    public class EditorRenderService
    {
        private const float HandleLength = 50f;
        private const float ClickTolerance = 8f;
        private static readonly Color ColliderColor = new Color(50, 205, 50, 255); // Lime green

        public void RenderSceneViewport(SpriteBatch spriteBatch, GameScene activeScene, GameObject selectedGo, object selectedComponent, GizmoMode activeMode)
        {
            // Render basic objects
            foreach(var entity in activeScene.Entities.GetSerializableEntities())
            {
                if(!entity.isActive)
                    continue;

                var transform = entity.GetComponent<TransformComponent>();
                if(transform == null)
                    continue;

                Vector2 position = transform.WorldPosition;
                bool isSelected = (selectedGo != null && entity.Id == selectedGo.Id);

                // Draw central pivot point anchor
                Color centerColor = isSelected ? Color.LimeGreen : new Color(200, 200, 200, 120);
                GizmoRenderer.DrawCircle(spriteBatch, position, isSelected ? 8 : 5, 12, centerColor);
                GizmoRenderer.DrawPoint(spriteBatch, position, Color.White, 2);

                if(isSelected)
                {
                    // Render Collider Gizmos if a collider component is active!
                    if(selectedComponent != null && selectedComponent is ColliderComponent)
                    {
                        RenderColliderGizmo(spriteBatch, transform, selectedComponent);
                        
                    }
                    else if(selectedComponent != null && selectedComponent is TransformComponent)
                    {
                        // Otherwise, draw standard Transform tools based on current Q/W/E selection
                        DrawTransformGizmos(spriteBatch, transform, position, activeMode);
                    }

                    
                }
            }
        }

        private void RenderColliderGizmo(SpriteBatch spriteBatch, TransformComponent transform, object component)
        {
            Vector2 position = transform.WorldPosition;

            // --- BOX COLLIDER GIZMO ---
            if(component is BoxColliderComponent box)
            {
                Vector2 center = position + box.Offset;
                Vector2 halfSize = box.Size * 0.5f;

                Vector2 topLeft = center - halfSize;
                Vector2 topRight = center + new Vector2(halfSize.X, -halfSize.Y);
                Vector2 bottomLeft = center + new Vector2(-halfSize.X, halfSize.Y);
                Vector2 bottomRight = center + halfSize;

                // Draw green outline
                GizmoRenderer.DrawLine(spriteBatch, topLeft, topRight, ColliderColor, 2);
                GizmoRenderer.DrawLine(spriteBatch, topRight, bottomRight, ColliderColor, 2);
                GizmoRenderer.DrawLine(spriteBatch, bottomRight, bottomLeft, ColliderColor, 2);
                GizmoRenderer.DrawLine(spriteBatch, bottomLeft, topLeft, ColliderColor, 2);

                // Interaction Handles: Center (for Offset) and Right/Bottom (for resizing size)
                GizmoRenderer.DrawCircle(spriteBatch, center, 6, 8, Color.Yellow, 2); // Center handle
                GizmoRenderer.DrawPoint(spriteBatch, center + new Vector2(halfSize.X, 0), Color.DodgerBlue, 8); // Width handle
                GizmoRenderer.DrawPoint(spriteBatch, center + new Vector2(0, halfSize.Y), Color.DodgerBlue, 8); // Height handle
                
            }

            // --- CIRCLE COLLIDER GIZMO ---
            if(component is CircleColliderComponent circle)
            {
                Vector2 center = position + circle.Offset;

                // Draw circle bounds
                GizmoRenderer.DrawCircle(spriteBatch, center, circle.Radius, 24, ColliderColor, 2);

                // Interaction Handles: Center (Offset) and Edge (Radius)
                GizmoRenderer.DrawCircle(spriteBatch, center, 6, 8, Color.Yellow, 2);
                GizmoRenderer.DrawPoint(spriteBatch, center + new Vector2(circle.Radius, 0), Color.DodgerBlue, 8);
                
            }

            // --- POLYGON COLLIDER GIZMO ---
            if(component is PolygonColliderComponent poly)
            {
                if(poly.Vertices == null || poly.Vertices.Count < 2)
                    return;

                Vector2 center = position + poly.Offset;

                // Draw connecting edge lines
                for(int i = 0; i < poly.Vertices.Count; i++)
                {
                    Vector2 p1 = center + poly.Vertices[i];
                    Vector2 p2 = center + poly.Vertices[(i + 1) % poly.Vertices.Count]; // Loop around to index 0

                    GizmoRenderer.DrawLine(spriteBatch, p1, p2, ColliderColor, 2);
                }

                // Interaction Handles: Center (Offset) and Vertex point adjusters
                GizmoRenderer.DrawCircle(spriteBatch, center, 6, 8, Color.Yellow, 2);
                for(int i = 0; i < poly.Vertices.Count; i++)
                {
                    Vector2 worldVertex = center + poly.Vertices[i];
                    GizmoRenderer.DrawPoint(spriteBatch, worldVertex, Color.DodgerBlue, 8);
                }
               
            }

           
        }

        private void DrawTransformGizmos(SpriteBatch spriteBatch, TransformComponent transform, Vector2 position, GizmoMode activeMode)
        {
            Vector2 scale = transform.Scale;
            Vector2 size = transform.Size;

            float currentWidth = size.X * scale.X;
            float currentHeight = size.Y * scale.Y;
            Vector2 baseCorner = transform.RenderTopLeft;

            Vector2 topRight = baseCorner + new Vector2(currentWidth, 0);
            Vector2 bottomLeft = baseCorner + new Vector2(0, currentHeight);
            Vector2 bottomRight = baseCorner + new Vector2(currentWidth, currentHeight);

            switch(activeMode)
            {
                case GizmoMode.Scale:
                    GizmoRenderer.DrawScaleGizmo(spriteBatch, position, HandleLength, 2);
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, topRight, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, bottomLeft, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, bottomLeft, bottomRight, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, topRight, bottomRight, Color.DarkGray, 1);
                    break;

                case GizmoMode.Size:
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, topRight, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, bottomLeft, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, bottomLeft, bottomRight, Color.DarkGray, 1);
                    GizmoRenderer.DrawLine(spriteBatch, topRight, bottomRight, Color.DarkGray, 1);

                    Vector2 rightHandleCenter = baseCorner + new Vector2(currentWidth, currentHeight * 0.5f);
                    Vector2 bottomHandleCenter = baseCorner + new Vector2(currentWidth * 0.5f, currentHeight);
                    Vector2 cornerHandleCenter = bottomRight;

                    GizmoRenderer.DrawPoint(spriteBatch, rightHandleCenter, Color.Blue, (int) ClickTolerance);
                    GizmoRenderer.DrawPoint(spriteBatch, rightHandleCenter, Color.LightBlue, (int) ClickTolerance - 2);

                    GizmoRenderer.DrawPoint(spriteBatch, bottomHandleCenter, Color.Blue, (int) ClickTolerance);
                    GizmoRenderer.DrawPoint(spriteBatch, bottomHandleCenter, Color.LightBlue, (int) ClickTolerance - 2);

                    GizmoRenderer.DrawPoint(spriteBatch, cornerHandleCenter, Color.DarkRed, (int) ClickTolerance);
                    GizmoRenderer.DrawPoint(spriteBatch, cornerHandleCenter, Color.Orange, (int) ClickTolerance - 2);
                    break;
            }
        }
    }
}