using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public class PhysicsBodyComponent : GameComponent
    {
        [Browsable(false)]
        public Body physicsBody;

        // Backing fields for Editor / Serialization
        private BodyType _bodyType = BodyType.Dynamic;
        private bool _ignoreGravity = false;
        private bool _isActive = true;
        private bool _fixedRotation = false;
        private float _mass = 1.0f;

        public BodyType bodyType
        {
            get => (physicsBody != null && physicsBody.World != null) ? physicsBody.BodyType : _bodyType;
            set
            {
                _bodyType = value;
                if(physicsBody != null && physicsBody.World != null)
                    physicsBody.BodyType = value;
            }
        }

        public bool ignoreGravity
        {
            get => (physicsBody != null && physicsBody.World != null) ? physicsBody.IgnoreGravity : _ignoreGravity;
            set
            {
                _ignoreGravity = value;
                if(physicsBody != null && physicsBody.World != null)
                    physicsBody.IgnoreGravity = value;
            }
        }

        public bool isActive
        {
            get => (physicsBody != null && physicsBody.World != null) ? physicsBody.Enabled : _isActive;
            set
            {
                _isActive = value;
                if(physicsBody != null && physicsBody.World != null)
                    physicsBody.Enabled = value;
            }
        }

        public bool fixedRotation
        {
            get => (physicsBody != null && physicsBody.World != null) ? physicsBody.FixedRotation : _fixedRotation;
            set
            {
                _fixedRotation = value;
                if(physicsBody != null && physicsBody.World != null)
                    physicsBody.FixedRotation = value;
            }
        }

        public float Mass
        {
            get => (physicsBody != null && physicsBody.World != null) ? physicsBody.Mass : _mass;
            set
            {
                _mass = value;
                if(physicsBody != null && physicsBody.World != null)
                    physicsBody.Mass = value;
            }
        }

       
        public void Initialize(World world, Vector2 initialPosition, float initialRotation)
        {
            
            physicsBody = world.CreateBody(initialPosition, initialRotation, _bodyType);

            // Apply all the settings configured in the Editor
           
            physicsBody.IgnoreGravity = _ignoreGravity;
            physicsBody.Enabled = _isActive;
            physicsBody.FixedRotation = _fixedRotation;
            physicsBody.Mass = _mass;
            
        }
    }
}
