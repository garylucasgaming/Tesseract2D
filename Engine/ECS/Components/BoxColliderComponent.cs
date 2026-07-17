using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using System.ComponentModel;

namespace Engine.Core.ECS.Components
{
    public class BoxColliderComponent : ColliderComponent
    {
        private Vector2 _size = new Vector2(20, 20);

        [Browsable(true)]
        public Vector2 Size
        {
            get => _size;
            set => _size = value; // Simple assignment; no math during loading!
        }

        public override void CreateShape(float pixelsPerMeter)
        {
            float safePpm = pixelsPerMeter <= 0f ? 64f : pixelsPerMeter;

            // Half-width and half-height in meters
            float hx = (_size.X / safePpm) / 2f;
            float hy = (_size.Y / safePpm) / 2f;

            this.shape = new PolygonShape(PolygonTools.CreateRectangle(hx, hy), Density);
        }
    }
}