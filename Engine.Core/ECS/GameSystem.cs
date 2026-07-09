using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{

    // The non-generic baseline for global/infrastructure systems
    public abstract class GameSystem
    {
        public bool IsEnabled { get; set; } = true;

        public bool UsedInEditor { get; set; } = false;

        public virtual SystemUpdatePolicy UpdatePolicy => SystemUpdatePolicy.FrameUpdate;


        public float UpdateInterval = 0.0f; // For FixedUpdate and EntityUpdate policies

       public float timer
        {
            get; set;
        } // Internal timer for FixedUpdate and EntityUpdate policies

       public bool shouldUpdate { get; set; } = true; // Internal flag to determine if the system should update this frame

        public virtual GameScene? ContextScene { get; set; } = null;

        // Default to null for global systems that don't belong to a specific component
        public abstract IComponentQuery RequiredComponents{ get; set; }

        public abstract void Update(HashSet<GameObject> gameObjects, float deltaTime);

        public virtual void Render(HashSet<GameObject> gameObjects, SpriteBatch spriteBatch)
        {
            // Default: Do nothing. Only visual systems override this.
        }
    }
}

