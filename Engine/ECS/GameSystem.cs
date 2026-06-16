using Engine.Core.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public abstract class GameSystem<T> : GameSystem where T : GameComponent
    {
        // The engine can read this type property completely automatically!
        public override Type RequiredComponentType => typeof(T);

        public override void Update(List<GameObject> gameObjects, float deltaTime)
        {
            foreach(var go in gameObjects)
            {
                if(!go.isActive)
                    continue;

                var component = go.GetComponent<T>();
                if(component != null)
                {
                    UpdateEntity(go, component, deltaTime);
                }
            }
        }

        // The user only has to implement this simple, clean method
        protected abstract void UpdateEntity(GameObject entity, T component, float deltaTime);
    }

    // The non-generic baseline for global/infrastructure systems
    public abstract class GameSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int UpdateOrder { get; set; } = 0;

        // Default to null for global systems that don't belong to a specific component
        public virtual Type? RequiredComponentType => null;

        public abstract void Update(List<GameObject> gameObjects, float deltaTime);
    }
}

