using Engine.Core.ECS.Components;
using Engine.Core.ECS.Managers;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Systems
{
    public class PhysicsSystem : GameSystem
    {
        
        
        public override IComponentQuery RequiredComponents
        {
            get;
            set;
        }

        
        

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

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
           
            if(ContextScene != null)
            {
                World physicsSpace = ContextScene.Physics.PhysicsSpace;


                // initlialization step
                foreach(var go in gameObjects)
                {
                    var bodyComp = go.GetComponent<PhysicsBodyComponent>();
                    var colliderComp = go.GetComponent<ColliderComponent>();
                    var transform = go.GetComponent<TransformComponent>();

                    //lazy initialize if body hasn't been created
                    if(bodyComp.physicsBody == null)
                    {
                        Vector2 initialPosition = transform.WorldPosition / ContextScene.Physics.PixelsPerMeter;

                        bodyComp.Initialize(physicsSpace, initialPosition, transform.Rotation);

                        bodyComp.physicsBody.BodyType = bodyComp.bodyType switch
                        {
                            BodyType.Static => BodyType.Static,
                            BodyType.Kinematic => BodyType.Kinematic,
                            BodyType.Dynamic => BodyType.Dynamic,
                            _ => BodyType.Static
                        };

                        bodyComp.physicsBody.SetTransform(initialPosition, transform.Rotation);
                        colliderComp.AttachToBody(bodyComp);
                    }
                }

                foreach(var go in gameObjects)
                {
                    var bodycomp = go.GetComponent<PhysicsBodyComponent>();
                    Log.Warning("gameobject physics position: "+ go.Name + bodycomp.physicsBody.Position);
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
