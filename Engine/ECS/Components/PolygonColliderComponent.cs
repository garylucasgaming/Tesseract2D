using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public class PolygonColliderComponent : ColliderComponent
    {

        private List<Vector2> _vertices;
        public List<Vector2> Vertices
        {
            get => _vertices;
            set
            {
                _vertices = value;
                // Create the polygon shape from the list of vertices
                this.shape = new PolygonShape(new Vertices(_vertices), Density);
                RebuildFixture();
            }
        }
    }
}
