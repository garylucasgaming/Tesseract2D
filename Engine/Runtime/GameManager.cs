using Engine.Core.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Runtime
{
    public abstract class GameManager
    {

        
        public GameScene ContextScene { get; internal set; } = null!; // A reference to the specific scene this manager is attached to. This gives the manager access to the entities and systems it orchestrates.

        public string ManagerName { get; set; } = "New GameManager"; // Default name for new managers, should be overridden by specific implementations for clarity.

        public bool IsActive { get; set; } = true; // Dictates whether this manager should execute its internal logic updates.


        
        public bool IsPersistent { get; protected set; } = false; // Dictates whether this manager should persist across GameScene transitions. True for global hubs (like an SaveGameManager); False for localized maps (like a CombatManager).


        // Lifecycle Initialization Hook. Called automatically by the GameScene once the entire scene has loaded into memory,
        // ensuring all GameObjects and Systems exist.
        public virtual void Initialize()
        {
        }


        // The high-level execution tick. Unlike systems, this doesn't automatically loop through 
        // GameObjects; it runs once per frame to update overarching state logic.
        // <param name="deltaTime">Time elapsed in seconds since the last frame tick.</param>
        public virtual void Update(float deltaTime)
        {
        }


        // Lifecycle Shutdown Hook. Called automatically when the scene is being torn down 
        // by the SceneDirector. Perfect for cleanly unsubscribing from GameEvents to prevent memory leaks.
        public virtual void Shutdown()
        {
        }

        

    }
}
