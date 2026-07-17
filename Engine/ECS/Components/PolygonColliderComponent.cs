using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using System.Collections.Generic;

namespace Engine.Core.ECS.Components
{
    public class PolygonColliderComponent : ColliderComponent
    {
        private List<Vector2> _vertices = new List<Vector2>();

        public List<Vector2> Vertices
        {
            get => _vertices;
            set => _vertices = value;
        }

        public override void CreateShape(float pixelsPerMeter)
        {
            if(_vertices == null || _vertices.Count == 0)
                return;

            float safePpm = pixelsPerMeter <= 0f ? 64f : pixelsPerMeter;

            var scaledVertices = new Vertices(_vertices.Count);
            foreach(var vertex in _vertices)
            {
                scaledVertices.Add(vertex / safePpm);
            }

            this.shape = new PolygonShape(scaledVertices, Density);
        }
    }
}