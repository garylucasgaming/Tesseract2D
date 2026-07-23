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

        public bool isInitialized = false;

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
            foreach(var go in cachedObjects)
            {
                Log.Info("resetting physics bodies");
               var bodyComp = go.GetComponent<PhysicsBodyComponent>();
                var transform = go.GetComponent<TransformComponent>();
                var convertedPosition = transform.WorldPosition / ContextScene.Physics.PixelsPerMeter;
                bodyComp.physicsBody.SetTransform(convertedPosition, transform.Rotation);
            }

        }

        public void Initialize(HashSet<GameObject> gameobjects)
        {
            float ppm = ContextScene.Physics.PixelsPerMeter;
            physicsSpace = ContextScene.Physics.PhysicsSpace;

            foreach(var go in gameobjects)
            {
                if(!cachedObjects.Contains(go))
                {
                    cachedObjects.Add(go);
                }

                // Grab components
                var bodyComp = go.GetComponent<PhysicsBodyComponent>();
                var colliderComp = go.GetComponent<ColliderComponent>();
                var transform = go.GetComponent<TransformComponent>();

                // 1. Setup physics body
                bodyComp.Initialize(physicsSpace, transform.WorldPosition / ppm, transform.Rotation);

                // 2. Setup physics collider with contextual PPM scale
                colliderComp.Initialize(bodyComp.physicsBody, ppm);
            }

            isInitialized = true; // Mark initialization phase complete!
        }

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
           
            if(ContextScene != null)
            {
                

                if(!isInitialized)
                {
                    Initialize(gameObjects);
                }

                foreach(var go in gameObjects)
                {
                    var bodycomp = go.GetComponent<PhysicsBodyComponent>();
                   
                }
                physicsSpace.Step(deltaTime);

                // post-step sync  physics -> transform
                foreach(var go in gameObjects) 
                {
                    var bodyComp = go.GetComponent<PhysicsBodyComponent>();
                    var transform = go.GetComponent<TransformComponent>();


                    if(bodyComp.bodyType == BodyType.Dynamic || bodyComp.bodyType == BodyType.Kinematic)
                    {
                        transform.WorldPosition = bodyComp.physicsBody.Position * ContextScene.Physics.PixelsPerMeter;
                        transform.Rotation = bodyComp.physicsBody.Rotation;
                    }
                }

            }
        }
    }
}
