using Engine.Core.ECS.Components;
using Engine.Core.ECS.Managers;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Engine.Core.ECS.Systems
{
    public class PhysicsSystem : GameSystem
    {
        
        
        public override IComponentQuery RequiredComponents
        {
            get;
            set;
        }

        private HashSet<GameObject> cachedObjects = new HashSet<GameObject>();
        private readonly HashSet<ColliderComponent> initializedColliders = new HashSet<ColliderComponent>();

        private World physicsSpace;
        public PhysicsSystem()
        {
         

            RequiredComponents = Query
                .Has<PhysicsBodyComponent>()
                .And<TransformComponent>()
                .And(Query
                    .Or(Query
                        .Has<BoxColliderComponent>(), Query
                        .Has<CircleColliderComponent>(), Query
                        .Has<PolygonColliderComponent>()
                        ));
                
            UpdatePolicy = SystemUpdatePolicy.TickUpdate;
            UsedInEditor = false;
            ContextScene = base.ContextScene;
        }

        public void ResetPhysicsTransforms()
        {
            if(ContextScene?.Physics == null)
                return;

            float ppm = PixelsPerMeter;
            foreach(var go in cachedObjects)
            {
                var bodyComp = go.GetComponent<PhysicsBodyComponent>();
                var transform = go.GetComponent<TransformComponent>();
                if(bodyComp?.physicsBody == null || transform == null)
                    continue;

                Log.Info("resetting physics bodies");
                var convertedPosition = transform.WorldPosition / ppm;
                bodyComp.physicsBody.SetTransform(convertedPosition, transform.Rotation);
            }

        }

        private float PixelsPerMeter =>
            ContextScene?.Physics != null && ContextScene.Physics.PixelsPerMeter > 0f
                ? ContextScene.Physics.PixelsPerMeter
                : 64f;

        /// <summary>
        /// Initializes an entity's body once and each collider once. Late-spawned entities and
        /// colliders attached after the first tick are picked up by a later tick.
        /// </summary>
        private void InitializeEntity(GameObject go)
        {
            var bodyComp = go.GetComponent<PhysicsBodyComponent>();
            var transform = go.GetComponent<TransformComponent>();
            if(bodyComp == null || transform == null)
            {
                Log.Error($"[Physics Error] Cannot initialize physics for '{go.Name}': body or transform is missing.");
                return;
            }

            float ppm = PixelsPerMeter;
            physicsSpace = ContextScene.Physics.PhysicsSpace;

            if(!cachedObjects.Contains(go))
            {
                bodyComp.Initialize(physicsSpace, transform.WorldPosition / ppm, transform.Rotation);
                cachedObjects.Add(go);
            }

            if(bodyComp.physicsBody == null)
                return;

            foreach(var collider in go.Components.Values.OfType<ColliderComponent>())
            {
                if(initializedColliders.Contains(collider))
                    continue;

                collider.Initialize(bodyComp.physicsBody, ppm);
                initializedColliders.Add(collider);
            }
        }

        public void Initialize(HashSet<GameObject> gameobjects)
        {
            foreach(var go in gameobjects)
            {
                InitializeEntity(go);
            }
        }

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
            if(ContextScene?.Physics == null)
                return;

            physicsSpace = ContextScene.Physics.PhysicsSpace;
            if(physicsSpace == null)
            {
                Log.Error("[Physics Error] Cannot tick PhysicsSystem: physics space is not initialized.");
                return;
            }

            float ppm = PixelsPerMeter;

            // Late spawns become part of the simulation on the next tick.
            foreach(var go in gameObjects)
            {
                InitializeEntity(go);
            }

            // Editor-authored transform -> Aether body. Comparing against the body's
            // current pose keeps the post-step physics -> transform sync from feeding back.
            foreach(var go in gameObjects)
            {
                var bodyComp = go.GetComponent<PhysicsBodyComponent>();
                var transform = go.GetComponent<TransformComponent>();
                if(bodyComp?.physicsBody == null || transform == null)
                    continue;

                var targetPosition = new Vector2(transform.WorldPosition.X / ppm, transform.WorldPosition.Y / ppm);
                bool moved = Vector2.DistanceSquared(targetPosition, bodyComp.physicsBody.Position) > 0.000001f;
                bool rotated = Math.Abs(transform.Rotation - bodyComp.physicsBody.Rotation) > 0.0001f;
                if(moved || rotated)
                {
                    bodyComp.physicsBody.SetTransform(targetPosition, transform.Rotation);
                }
            }

            physicsSpace.Step(deltaTime);

            // post-step sync physics -> transform
            foreach(var go in gameObjects)
            {
                var bodyComp = go.GetComponent<PhysicsBodyComponent>();
                var transform = go.GetComponent<TransformComponent>();
                if(bodyComp?.physicsBody == null || transform == null)
                    continue;

                if(bodyComp.bodyType == BodyType.Dynamic || bodyComp.bodyType == BodyType.Kinematic)
                {
                    transform.WorldPosition = bodyComp.physicsBody.Position * ppm;
                    transform.Rotation = bodyComp.physicsBody.Rotation;
                }
            }
        }
    }
}
