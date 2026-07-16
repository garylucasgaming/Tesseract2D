using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{



    public class ColliderComponent : GameComponent
    {
        [Browsable(false)]
        public Fixture collider;

        [Browsable(false)]
        public Shape shape;

        [Browsable(false)]
        public Body physicsBody;

        // Backing fields for Editor / Serialization
        private Category _collisionCategory = Category.All;
        private Category _collisionMask = Category.All;
        private bool _isTrigger = false;
        private float _friction = 0.2f;
        private float _restitution = 0.0f;
        private float _density = 1.0f;
        private Vector2 _offset;
        public Vector2 Offset {
            get => _offset;
            set => _offset = value;
        }
        public Category collisionCategory
        {
            get => collider != null ? collider.CollisionCategories : _collisionCategory;
            set
            {
                _collisionCategory = value;
                if(collider != null)
                    collider.CollisionCategories = value;
            }
        }

        public Category collisionMask
        {
            get => collider != null ? collider.CollidesWith : _collisionMask;
            set
            {
                _collisionMask = value;
                if(collider != null)
                    collider.CollidesWith = value;
            }
        }

        public bool isTrigger
        {
            get => collider != null ? collider.IsSensor : _isTrigger;
            set
            {
                _isTrigger = value;
                if(collider != null)
                    collider.IsSensor = value;
            }
        }

        public float friction
        {
            get => collider != null ? collider.Friction : _friction;
            set
            {
                _friction = value;
                if(collider != null)
                    collider.Friction = value;
            }
        }

        public float restitution
        {
            get => collider != null ? collider.Restitution : _restitution;
            set
            {
                _restitution = value;
                if(collider != null)
                    collider.Restitution = value;
            }
        }

        public float Density
        {
            get => _density; set => _density = value;
        }

        public Action<GameObject>? OnCollisionEnterEvent;
        public Action<GameObject>? OnCollisionExitEvent;
        public Action<GameObject>? BeforeCollisionEvent;
        public Action<GameObject>? AfterCollisionEvent;

        // Callbacks stay the same...

        public void AttachToBody(PhysicsBodyComponent bodyComp)
        {
            if(shape == null)
                return; // Prevent attaching if derived collider hasn't set its shape yet
            physicsBody = bodyComp.physicsBody;
            collider = physicsBody.CreateFixture(shape);
            

            // Apply settings configured in Editor
            collider.CollisionCategories = _collisionCategory;
            collider.CollidesWith = _collisionMask;
            
            collider.IsSensor = _isTrigger;

            collider.Friction = _friction;
            collider.Restitution = _restitution;

            // Hook up events
            collider.BeforeCollision += BeforeCollision;
            collider.AfterCollision += AfterCollision;
            collider.OnCollision += OnCollisionEnter;
            collider.OnSeparation += OnCollisionExit;
        }

        private void OnCollisionExit(Fixture sender, Fixture other, Contact contact)
        {
            OnCollisionExitEvent?.Invoke(gameObject);
        }

        private bool OnCollisionEnter(Fixture sender, Fixture other, Contact contact)
        {
            Log.Info("collision info:" + sender.Body.FixtureList.First().Shape + " " + other.Body.Position);
            OnCollisionEnterEvent?.Invoke(gameObject);
            return true;
        }

        private void AfterCollision(Fixture sender, Fixture other, Contact contact, ContactVelocityConstraint impulse)
        {
            AfterCollisionEvent?.Invoke(gameObject);
            
        }

        private bool BeforeCollision(Fixture sender, Fixture other)
        {
            BeforeCollisionEvent?.Invoke(gameObject);
            return true;
        }

        protected void RebuildFixture()
        {
            // Only rebuild if we are already attached to an active physics body
            if(physicsBody != null && collider != null)
            {
                // 1. Remove the old fixture from the active Aether body
                physicsBody.Remove(collider);

                // 2. Create the new fixture using the updated shape
                collider = physicsBody.CreateFixture(shape);

                // 3. Re-apply the current settings (restoring categories, triggers, etc.)
                collider.CollisionCategories = collisionCategory;
                collider.CollidesWith = collisionMask;
                collider.IsSensor = isTrigger;
                collider.Friction = friction;
                collider.Restitution = restitution;
                Density = Density;

                // 4. Re-hook the collision event delegates
                collider.BeforeCollision += BeforeCollision;
                collider.AfterCollision += AfterCollision;
                collider.OnCollision += OnCollisionEnter;
                collider.OnSeparation += OnCollisionExit;
            }
        }
    }
}
