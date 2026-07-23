using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Utilities
{
    public class Camera2D
    {

        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Zoom { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0.0f;

        public float MinZoom { get; set; } = 0.1f;
        public float MaxZoom { get; set; } = 10.0f;

        /// <summary>
        /// Generates the View Matrix centering the camera on Position.
        /// </summary>
        public Matrix GetViewMatrix(Viewport viewport)
        {
            return Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(new Vector3(Zoom, Zoom, 1f)) *
                   Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f));
        }

        /// <summary>
        /// Converts Screen/Viewport coordinates (e.g. raw Mouse position) into World space coordinates.
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPos, Viewport viewport)
        {
            return Vector2.Transform(screenPos, Matrix.Invert(GetViewMatrix(viewport)));
        }

        /// <summary>
        /// Converts World space coordinates into Screen/Viewport coordinates.
        /// </summary>
        public Vector2 WorldToScreen(Vector2 worldPos, Viewport viewport)
        {
            return Vector2.Transform(worldPos, GetViewMatrix(viewport));
        }
    }
}
