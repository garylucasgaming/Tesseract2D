using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using Engine.Editor.MGWindow.Services.Engine.Editor.MGWindow.Services;
using Engine.Editor.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Color = Microsoft.Xna.Framework.Color;

namespace Engine.Editor.MGWindow.Services
{
    public class EditorRenderService
    {
        private const float BaseHandleLength = 50f;
        private const float ClickTolerance = 8f;
        private static readonly Color ColliderColor = new Color(50, 205, 50, 255); // Lime green

        public void RenderSceneViewport(SpriteBatch spriteBatch, GameScene activeScene, GameObject selectedGo, object selectedComponent, GizmoMode activeMode, float cameraZoom = 1f)
        {
            // Calculate inverse zoom so gizmo screen size remains constant regardless of camera zoom
            float invZoom = 1f / Math.Max(cameraZoom, 0.001f);

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

                // Draw central pivot point anchor scaled by invZoom
                float pivotRadius = (isSelected ? 8f : 5f) * invZoom;
                int centerPointSize = Math.Max(1, (int) (2f * invZoom));
                Color centerColor = isSelected ? Color.LimeGreen : new Color(200, 200, 200, 120);

                GizmoRenderer.DrawCircle(spriteBatch, position, pivotRadius, 12, centerColor, Math.Max(1, (int) (1f * invZoom)));
                GizmoRenderer.DrawPoint(spriteBatch, position, Color.White, centerPointSize);

                if(isSelected)
                {
                    if(selectedComponent != null && selectedComponent is ColliderComponent)
                    {
                        RenderColliderGizmo(spriteBatch, transform, selectedComponent, invZoom);
                    }
                    else if(selectedComponent != null && selectedComponent is TransformComponent)
                    {
                        DrawTransformGizmos(spriteBatch, transform, position, activeMode, invZoom);
                    }
                }
            }
        }

        private void RenderColliderGizmo(SpriteBatch spriteBatch, TransformComponent transform, object component, float invZoom)
        {
            Vector2 position = transform.WorldPosition;
            int outlineThickness = Math.Max(1, (int) (2f * invZoom));
            int handleRadius = (int) (6f * invZoom);
            int pointSize = (int) (8f * invZoom);

            // --- BOX COLLIDER GIZMO ---
            if(component is BoxColliderComponent box)
            {
                Vector2 center = position + box.Offset;
                Vector2 halfSize = box.Size * 0.5f;

                Vector2 topLeft = center - halfSize;
                Vector2 topRight = center + new Vector2(halfSize.X, -halfSize.Y);
                Vector2 bottomLeft = center + new Vector2(-halfSize.X, halfSize.Y);
                Vector2 bottomRight = center + halfSize;

                GizmoRenderer.DrawLine(spriteBatch, topLeft, topRight, ColliderColor, outlineThickness);
                GizmoRenderer.DrawLine(spriteBatch, topRight, bottomRight, ColliderColor, outlineThickness);
                GizmoRenderer.DrawLine(spriteBatch, bottomRight, bottomLeft, ColliderColor, outlineThickness);
                GizmoRenderer.DrawLine(spriteBatch, bottomLeft, topLeft, ColliderColor, outlineThickness);

                GizmoRenderer.DrawCircle(spriteBatch, center, handleRadius, 8, Color.Yellow, outlineThickness);
                GizmoRenderer.DrawPoint(spriteBatch, center + new Vector2(halfSize.X, 0), Color.DodgerBlue, pointSize);
                Editor.Utilities.GizmoRenderer.DrawPoint(spriteBatch, center + new Vector2(0, halfSize.Y), Color.DodgerBlue, pointSize);
            }

            // --- CIRCLE COLLIDER GIZMO ---
            if(component is CircleColliderComponent circle)
            {
                Vector2 center = position + circle.Offset;

                GizmoRenderer.DrawCircle(spriteBatch, center, circle.Radius, 24, ColliderColor, outlineThickness);

                GizmoRenderer.DrawCircle(spriteBatch, center, handleRadius, 8, Color.Yellow, outlineThickness);
                GizmoRenderer.DrawPoint(spriteBatch, center + new Vector2(circle.Radius, 0), Color.DodgerBlue, pointSize);
            }

            // --- POLYGON COLLIDER GIZMO ---
            if(component is PolygonColliderComponent poly)
            {
                if(poly.Vertices == null || poly.Vertices.Count < 2)
                    return;

                Vector2 center = position + poly.Offset;

                for(int i = 0; i < poly.Vertices.Count; i++)
                {
                    Vector2 p1 = center + poly.Vertices[i];
                    Vector2 p2 = center + poly.Vertices[(i + 1) % poly.Vertices.Count];
                    GizmoRenderer.DrawLine(spriteBatch, p1, p2, ColliderColor, outlineThickness);
                }

                GizmoRenderer.DrawCircle(spriteBatch, center, handleRadius, 8, Color.Yellow, outlineThickness);
                for(int i = 0; i < poly.Vertices.Count; i++)
                {
                    GizmoRenderer.DrawPoint(spriteBatch, center + poly.Vertices[i], Color.DodgerBlue, pointSize);
                }
            }
        }

        public void RenderSceneGridAndBounds(SpriteBatch spriteBatch, GameScene activeScene, float cameraZoom = 1f)
        {
            if(activeScene == null || activeScene.SceneMap == null)
                return;

            var map = activeScene.SceneMap;
            int tileSize = map.TileSize;
            int mapWidthPx = map.Width * tileSize;
            int mapHeightPx = map.Height * tileSize;

            float invZoom = 1f / Math.Max(cameraZoom, 0.001f);
            int lineThickness = Math.Max(1, (int) (1f * invZoom));

            Color gridColor = new Color(25, 25, 25, 100); // Subtle gray for grid lines
            Color boundaryColor = new Color(255, 165, 0, 200); // Orange border for scene limits

            // 1. Draw Interior Grid Lines based on Map Dimensions & TileSize
            for(int x = 0; x <= map.Width; x++)
            {
                Vector2 start = new Vector2(x * tileSize, 0);
                Vector2 end = new Vector2(x * tileSize, mapHeightPx);
                GizmoRenderer.DrawLine(spriteBatch, start, end, gridColor, lineThickness);
            }

            for(int y = 0; y <= map.Height; y++)
            {
                Vector2 start = new Vector2(0, y * tileSize);
                Vector2 end = new Vector2(mapWidthPx, y * tileSize);
                GizmoRenderer.DrawLine(spriteBatch, start, end, gridColor, lineThickness);
            }

            // 2. Draw Outer Scene Boundary Box (RPGMaker-style hard limit)
            Vector2 topLeft = Vector2.Zero;
            Vector2 topRight = new Vector2(mapWidthPx, 0);
            Vector2 bottomLeft = new Vector2(0, mapHeightPx);
            Vector2 bottomRight = new Vector2(mapWidthPx, mapHeightPx);

            int boundaryThickness = Math.Max(2, (int) (2f * invZoom));
            GizmoRenderer.DrawLine(spriteBatch, topLeft, topRight, boundaryColor, boundaryThickness);
            GizmoRenderer.DrawLine(spriteBatch, topRight, bottomRight, boundaryColor, boundaryThickness);
            GizmoRenderer.DrawLine(spriteBatch, bottomRight, bottomLeft, boundaryColor, boundaryThickness);
            GizmoRenderer.DrawLine(spriteBatch, bottomLeft, topLeft, boundaryColor, boundaryThickness);
        }

        private void DrawTransformGizmos(SpriteBatch spriteBatch, TransformComponent transform, Vector2 position, GizmoMode activeMode, float invZoom)
        {
            Vector2 scale = transform.Scale;
            Vector2 size = transform.Size;

            float currentWidth = size.X * scale.X;
            float currentHeight = size.Y * scale.Y;
            Vector2 baseCorner = transform.RenderTopLeft;

            Vector2 topRight = baseCorner + new Vector2(currentWidth, 0);
            Vector2 bottomLeft = baseCorner + new Vector2(0, currentHeight);
            Vector2 bottomRight = baseCorner + new Vector2(currentWidth, currentHeight);

            int borderThickness = Math.Max(1, (int) (1f * invZoom));

            switch(activeMode)
            {
                case GizmoMode.Scale:
                    float handleLength = BaseHandleLength * invZoom;
                    int gizmoThickness = Math.Max(1, (int) (2f * invZoom));
                    GizmoRenderer.DrawScaleGizmo(spriteBatch, position, handleLength, gizmoThickness);

                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, topRight, Color.DarkGray, borderThickness);
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, bottomLeft, Color.DarkGray, borderThickness);
                    GizmoRenderer.DrawLine(spriteBatch, bottomLeft, bottomRight, Color.DarkGray, borderThickness);
                    GizmoRenderer.DrawLine(spriteBatch, topRight, bottomRight, Color.DarkGray, borderThickness);
                    break;

                case GizmoMode.Size:
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, topRight, Color.DarkGray, borderThickness);
                    GizmoRenderer.DrawLine(spriteBatch, baseCorner, bottomLeft, Color.DarkGray, borderThickness);
                    GizmoRenderer.DrawLine(spriteBatch, bottomLeft, bottomRight, Color.DarkGray, borderThickness);
                    GizmoRenderer.DrawLine(spriteBatch, topRight, bottomRight, Color.DarkGray, borderThickness);

                    Vector2 rightHandleCenter = baseCorner + new Vector2(currentWidth, currentHeight * 0.5f);
                    Vector2 bottomHandleCenter = baseCorner + new Vector2(currentWidth * 0.5f, currentHeight);
                    Vector2 cornerHandleCenter = bottomRight;

                    int outerSize = (int) (ClickTolerance * invZoom);
                    int innerSize = Math.Max(2, outerSize - (int) (2f * invZoom));

                    GizmoRenderer.DrawPoint(spriteBatch, rightHandleCenter, Color.Blue, outerSize);
                    GizmoRenderer.DrawPoint(spriteBatch, rightHandleCenter, Color.LightBlue, innerSize);

                    GizmoRenderer.DrawPoint(spriteBatch, bottomHandleCenter, Color.Blue, outerSize);
                    GizmoRenderer.DrawPoint(spriteBatch, bottomHandleCenter, Color.LightBlue, innerSize);

                    GizmoRenderer.DrawPoint(spriteBatch, cornerHandleCenter, Color.DarkRed, outerSize);
                    GizmoRenderer.DrawPoint(spriteBatch, cornerHandleCenter, Color.Orange, innerSize);
                    break;
            }
        }
    }
}