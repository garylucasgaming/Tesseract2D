using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using System;
using System.ComponentModel;

namespace Engine.Core.ECS
{
    public abstract class ColliderComponent : GameComponent
    {
        [Browsable(false)]
        public Fixture collider;

        [Browsable(false)]
        public Shape shape;

        [Browsable(false)]
        public Body physicsBody;

        private Category _collisionCategory = Category.All;
        private Category _collisionMask = Category.All;
        private bool _isTrigger = false;
        private float _friction = 0.2f;
        private float _restitution = 0.0f;
        private float _density = 1.0f;
        private Vector2 _offset;

        public Vector2 Offset
        {
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
            get => _density;
            set => _density = value;
        }

        public Action<GameObject>? OnCollisionEnterEvent;
        public Action<GameObject>? OnCollisionExitEvent;
        public Action<GameObject>? BeforeCollisionEvent;
        public Action<GameObject>? AfterCollisionEvent;

        /// <summary>
        /// Deferred shape-creation logic implemented by box, circle, or polygon sub-types.
        /// </summary>
        public abstract void CreateShape(float pixelsPerMeter);

        public void Initialize(Body body, float pixelsPerMeter)
        {
            physicsBody = body;

            // 1. Compile the physical shape with the active scene's PPM
            CreateShape(pixelsPerMeter);

            if(shape == null)
            {
                Log.Error($"[Physics Error] Shape failed to generate for collider on '{gameObject?.Name}'");
                return;
            }

            // 2. 💡 FIXED: Assign the newly created fixture back to our tracker!
            collider = body.CreateFixture(shape);

            // 3. Apply settings configured in Editor
            collider.CollisionCategories = _collisionCategory;
            collider.CollidesWith = _collisionMask;
            collider.IsSensor = _isTrigger;
            collider.Friction = _friction;
            collider.Restitution = _restitution;

            // 4. Hook up events safely now that collider is assigned
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
            Log.Info($"[Collision Event] {gameObject.Name} collided with body at {other.Body.Position*64}");
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
    }
}