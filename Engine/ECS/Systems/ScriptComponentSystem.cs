using Engine.Core.ECS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Systems
{
    public class ScriptComponentSystem : GameSystem
    {
        public override IComponentQuery RequiredComponents
        {
            get;
            set;
        }


        public ScriptComponentSystem()
        {
            RequiredComponents = Query.Has<ScriptComponent>();
            UsedInEditor = false;
        }


        

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {

            foreach(var gameObject in gameObjects)
            {   
                
                var comp = gameObject.GetComponent<ScriptComponent>();
                if(!gameObject.isActive)
                {
                   
                    if(comp.hasStarted)
                    {
                        comp.hasStarted = false;
                    }
                }

                
                if(!comp.hasStarted && gameObject.isActive)
                {
                    comp.Start();
                    comp.hasStarted = true;
                }


                if(gameObject.isActive && comp.IsActive)
                {

                    comp.Update();
                }
                
               
                



            }


        }
    }
}
