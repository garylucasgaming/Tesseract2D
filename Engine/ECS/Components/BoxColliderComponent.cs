using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public class BoxColliderComponent : ColliderComponent
    {
        private float ppm
        {
            get => gameObject?.ContextScene.Physics.PixelsPerMeter ?? 0f;
            
        } 

        private Vector2 _size = new Vector2(20,20);
       
        public Vector2 Size
        {
            get => _size;
            set
            {
                _size = value;
                this.shape = new PolygonShape(PolygonTools.CreateRectangle((_size.X/ppm)/2, (_size.Y/ppm)/2), Density);
                
                RebuildFixture();
            }
        }

    }
}
