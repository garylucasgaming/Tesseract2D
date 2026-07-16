using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Managers
{

    public enum GravityDirection
    {
        Up, Down, Left, Right
    }
    public class PhysicsManager
    {

      
        private float _gravityMagnitude = 1f;
        private GravityDirection _gravityDirection = GravityDirection.Down;

        public GameScene ContextScene
        {
            get;
            set;
        }

        [Browsable(false)]
        public World PhysicsSpace;
       
        /// <summary>
        /// this must always be a positive number
        /// </summary>
        public float GravityMagnitude
        {
            get => _gravityMagnitude;
            set
            {
                _gravityMagnitude = value;
            }
        }

        public float PixelsPerMeter { get; set; } = 64;

        /// <summary>
        /// this sets the gravity vector to the specified magnitude and converts it into the correct direction
        /// </summary>
        public GravityDirection DirectionOfGravity
        { 
            get => _gravityDirection;
            set
            {
                switch(value)
                {
                    case GravityDirection.Up:
                        Gravity = new Vector2(0, GravityMagnitude);
                        break;
                    case GravityDirection.Down:
                        Gravity = new Vector2(0, -GravityMagnitude);
                        break;
                    case GravityDirection.Left:
                        Gravity = new Vector2(-GravityMagnitude, 0);
                        break;
                    case GravityDirection.Right:
                        Gravity = new Vector2(GravityMagnitude, 0);
                        break;
                    default:
                        //gravity direction set to down
                        Gravity = new Vector2(0, -GravityMagnitude);
                        break;
                }
                _gravityDirection = value;
            }
        }

  
        public Vector2 Gravity
        {
            get;
            set;
        }

        public PhysicsManager()
        {
            DirectionOfGravity = GravityDirection.Down;
           
            PhysicsSpace = new World(Gravity);

        }
        

    }
}
