using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Engine.Editor.Utilities
{
    public static class GizmoRenderer
    {
        private static Texture2D _pixelTexture;

        /// <summary>
        /// Initializes the gizmo asset context. Call this once from your Control's Initialize method.
        /// </summary>
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Draws a flat center-point marker for abstract or empty entities.
        /// </summary>
        public static void DrawPoint(SpriteBatch spriteBatch, Vector2 position, Color color, int size = 2)
        {
            int offset = size / 2;
            spriteBatch.Draw(_pixelTexture, new Rectangle((int) position.X - offset, (int) position.Y - offset, size, size), color);
        }

        /// <summary>
        /// Draws a procedural wireframe circle outline.
        /// </summary>
        public static void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, int segments, Color color, int thickness = 1)
        {
            Vector2[] vertices = new Vector2[segments + 1];
            double increment = Math.PI * 2.0 / segments;

            for(int i = 0; i < segments; i++)
            {
                double angle = i * increment;
                vertices[i] = center + new Vector2((float) Math.Cos(angle), (float) Math.Sin(angle)) * radius;
            }
            vertices[segments] = vertices[0]; // Close path cleanly

            for(int i = 0; i < segments; i++)
            {
                DrawLine(spriteBatch, vertices[i], vertices[i + 1], color, thickness);
            }
        }

        /// <summary>
        /// Draws a clean vector line between two points using a scaled 1x1 white pixel.
        /// </summary>
        public static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            Vector2 edge = end - start;
            float angle = (float) Math.Atan2(edge.Y, edge.X);

            spriteBatch.Draw(_pixelTexture,
                new Rectangle((int) start.X, (int) start.Y, (int) edge.Length(), thickness),
                null,
                color,
                angle,
                Vector2.Zero,
                SpriteEffects.None,
                0);
        }

        public static void DrawMoveGizmo(SpriteBatch spriteBatch, Vector2 position, float handleLength = 50f, int thickness = 3)
        {
            // X-Axis Handle (Red line pointing right)
            DrawLine(spriteBatch, position, position + new Vector2(handleLength, 0), Color.Red, thickness);
            // Y-Axis Handle (Green line pointing down)
            DrawLine(spriteBatch, position, position + new Vector2(0, handleLength), Color.Green, thickness);

            // Tip boxes for selection targeting
            DrawPoint(spriteBatch, position + new Vector2(handleLength, 0), Color.Red, 6);
            DrawPoint(spriteBatch, position + new Vector2(0, handleLength), Color.Green, 6);
        }

        /// <summary>
        /// Simple hit-test check to see if a screen coordinate falls within a small radius of a point.
        /// </summary>
        public static bool HitTestPoint(Vector2 testPoint, Vector2 targetPosition, float toleranceRadius)
        {
            return Vector2.Distance(testPoint, targetPosition) <= toleranceRadius;
        }
    }
}