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
        
        public override Type RequiredComponentType => typeof(T);


        // The user only has to implement this simple, clean method
        protected abstract void UpdateEntity(GameObject entity, T component, float deltaTime);
    }

    // The non-generic baseline for global/infrastructure systems
    public abstract class GameSystem
    {
        public bool IsEnabled { get; set; } = true;

        public virtual SystemUpdatePolicy UpdatePolicy => SystemUpdatePolicy.FrameUpdate;

        public float UpdateInterval = 0.0f; // For FixedUpdate and EntityUpdate policies

        internal float timer
        {
            get; set;
        } // Internal timer for FixedUpdate and EntityUpdate policies

        internal bool shouldUpdate { get; set; } = true; // Internal flag to determine if the system should update this frame

        public virtual GameScene? ContextScene { get; set; } = null;

        // Default to null for global systems that don't belong to a specific component
        public virtual Type? RequiredComponentType => null;

        public abstract void UpdateEntity(IReadOnlyList<GameObject> gameObjects, float deltaTime);
    }
}

