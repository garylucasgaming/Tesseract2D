using nkast.Aether.Physics2D.Collision.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public class CircleColliderComponent : ColliderComponent
    {

        private float _radius = 20f;

        
        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                this.shape = new CircleShape(_radius, Density);
                RebuildFixture();
            }
        }

    }
}
