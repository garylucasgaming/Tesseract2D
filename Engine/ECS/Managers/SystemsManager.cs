using Engine.Core.ECS.Components.UI;
using Engine.Core.ECS.Systems;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core.ECS.Managers
{
    public class SystemsManager
    {
        // 1. Category Buckets: Policy Type -> List of matching systems
        private readonly Dictionary<SystemUpdatePolicy, List<GameSystem>> _policyBuckets = new();

        // 2. The Golden Nugget: A warm entity bucket mapped explicitly to each active system
        public readonly Dictionary<GameSystem, HashSet<GameObject>> _systemEntityCache = new();

        public GameScene ContextScene { get; set; } = null!;

        public  TransformSystem transformSystem { get; private set; } = null!;
        public  SpriteRenderSystem spriteRenderSystem { get; private set; } = null!;

        public PhysicsSystem physicsSystem { get; private set; } = null!;

        public ScriptComponentSystem scriptComponentSystem { get; private set; } = null!;

        public UIRenderSystem uiRenderSystem { get; private set; } = null!;

        public UILayoutSystem uiLayoutSystem { get; private set; } = null!;

        public UIInputSystem uiInputSystem { get; private set; } = null!;



        public SystemsManager()
        {
            // Automatically initialize a bucket list for every single policy enum type
            foreach(SystemUpdatePolicy policy in Enum.GetValues(typeof(SystemUpdatePolicy)))
            {
                _policyBuckets[policy] = new List<GameSystem>();
            }

            if(transformSystem == null)
            {
                transformSystem = new TransformSystem() { ContextScene = ContextScene};
                AddSystem(transformSystem);
            }
            if(spriteRenderSystem == null)
            {
                spriteRenderSystem = new SpriteRenderSystem() { ContextScene = ContextScene };
                AddSystem(spriteRenderSystem);
            }

            if(physicsSystem == null)
            {
                physicsSystem = new PhysicsSystem() { ContextScene = ContextScene };
                AddSystem(physicsSystem);
            }
            if(scriptComponentSystem == null)
            {
                scriptComponentSystem = new ScriptComponentSystem() { ContextScene = ContextScene };
                AddSystem(scriptComponentSystem);
            }
            if(uiRenderSystem == null)
            {
                uiRenderSystem = new UIRenderSystem() { ContextScene = ContextScene };
                AddSystem(uiRenderSystem);
            }
            if(uiLayoutSystem == null)
            {
                uiLayoutSystem = new UILayoutSystem() { ContextScene = ContextScene };
                AddSystem(uiLayoutSystem);
            }
            if(uiInputSystem == null)
            {
                uiInputSystem = new UIInputSystem(Globals.InputManager,Globals.EditorCamera, Globals.Viewport ) { ContextScene = ContextScene };
                AddSystem(uiInputSystem);
            }


        }

        /// Registers a system, assigns it to its timing bucket, and builds its initial matching cache.
        public void AddSystem(GameSystem system)
        {
            if(system == null)
                return;

            // Prevent duplicate system types from running in the same scene context
            Type systemType = system.GetType();
            if(_systemEntityCache.Keys.Any(s => s.GetType() == systemType))
                return;

            system.ContextScene = ContextScene;

            // Route directly into its corresponding enum dictionary bucket
            _policyBuckets[system.UpdatePolicy].Add(system);

            // Allocate the dedicated warm cache pool for this system
            var matchingSet = new HashSet<GameObject>();
            _systemEntityCache[system] = matchingSet;

            // Ingest any pre-existing entities in the scene that match this query immediately
            if(ContextScene?.Entities != null)
            {
                var matchedList = ContextScene.Entities.GetQuery(system.RequiredComponents);
                foreach(var entity in matchedList)
                {
                    matchingSet.Add(entity);
                }
            }
        }

        /// Global execution ticks called by your core engine loop.
        /// Handles FrameUpdate and processes individual FixedUpdate custom interval clocks.
        public void Update(float deltaTime, bool playModeActive)
        {
            // FrameUpdate: Variable frame rate execution
            ExecuteSystemBucket(SystemUpdatePolicy.FrameUpdate, deltaTime, playModeActive);

            // FixedUpdate: Custom intervals (clocks ticked behind the scenes)
            var customIntervalSystems = _policyBuckets[SystemUpdatePolicy.FixedUpdate];
            for(int i = 0; i < customIntervalSystems.Count; i++)
            {
                var system = customIntervalSystems[i];
                if(!system.IsEnabled)
                    continue;

                // Filter FixedUpdate execution based on playMode status vs Editor rules
                if(!playModeActive && !system.UsedInEditor)
                    continue;

                system.timer += deltaTime;
                if(system.timer >= system.UpdateInterval)
                {
                    system.timer = 0.0f; // Reset the clock
                    system.Update(_systemEntityCache[system], deltaTime);
                }
            }
        }

        /// <summary>
        /// Sweeps through all enabled systems and gives them an opportunity to draw via SpriteBatch.
        /// </summary>
        public void Render(SpriteBatch spriteBatch, ContentManager cm)
        {
            // Directly iterate through your pre-existing warm cache dictionary!
            foreach(var kvp in _systemEntityCache)
            {
                var system = kvp.Key;
                HashSet<GameObject> cachedEntities = kvp.Value;

                if(!system.IsEnabled || !system.shouldUpdate)
                    continue;

                if(system is SpriteRenderSystem srs && EditorContextManager.IsProjectLoaded)
                {
                    srs.LoadSprites(cm);

                }

                // Call the generic render pass safely (Render always runs so editor previews display)
                system.Render(cachedEntities, spriteBatch);
            }
        }

        public void RenderUI(SpriteBatch sb, ContentManager cm, UISpace space)
        {
            uiRenderSystem.Initialize(cm);

            uiRenderSystem.Render(sb,cm, space);
 
        }

        /// Runs TickUpdate systems on your locked, rigid simulation step ticker.
        public void TickUpdate(float fixedDeltaTime, bool playModeActive)
        {
           
            ExecuteSystemBucket(SystemUpdatePolicy.TickUpdate, fixedDeltaTime, playModeActive);
        }

        /// Drives Manual systems. Call this explicitly to trigger a manual system by type.
        public void TriggerManualSystem<T>(float deltaTime) where T : GameSystem
        {
            var system = GetSystem<T>();
            if(system == null || !system.IsEnabled || system.UpdatePolicy != SystemUpdatePolicy.Manual)
                return;

            system.Update(_systemEntityCache[system], deltaTime);
        }

        /// Helper to sweep through standard execution pipelines cleanly.
        private void ExecuteSystemBucket(SystemUpdatePolicy policy, float deltaTime, bool playModeActive)
        {
            var systems = _policyBuckets[policy];
            for(int i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if(!system.IsEnabled || !system.shouldUpdate)
                    continue;

                // 💡 FIX: Drop out early if simulation is paused/stopped and the system isn't allowed in the editor
                if(!playModeActive && !system.UsedInEditor)
                    continue;

                system.Update(_systemEntityCache[system], deltaTime);
            }
        }

        // --- Reactive Engine Listeners: Hooked to your scene spawn/component change events ---

        /// Call this whenever an entity spawns, gets activated, or gains a new component.
        public void OnEntityChanged(GameObject entity, float deltaTime = 0.0f, bool playModeActive = true)
        {
            foreach(var kvp in _systemEntityCache)
            {
                var system = kvp.Key;
                var cache = kvp.Value;

                bool wasInCache = cache.Contains(entity);

                // Ingest any pre-existing entities in the scene that match this query immediately
                if(ContextScene?.Entities != null)
                {
                    if(wasInCache)
                    {
                        continue;
                    }
                    else
                    {
                        if(system.RequiredComponents.IsMatched(entity))
                        {
                            cache.Add(entity);
                        }
                    }

                    // EntityUpdate Policy: If an entity mutates and matches, tick this system instantly!
                    if(system.UpdatePolicy == SystemUpdatePolicy.EntityUpdate && system.IsEnabled && !wasInCache)
                    {
                        // 💡 FIX: Respect playModeActive context state during inline mutations
                        if(!playModeActive && !system.UsedInEditor)
                            continue;

                        var singleEntityBatch = new HashSet<GameObject> { entity };
                        system.Update(singleEntityBatch, deltaTime);
                    }
                }
            }
        }

        

        /// Call this whenever an entity is explicitly destroyed or removed from the active scene tree.
        public void OnEntityDestroyed(GameObject entity)
        {
            foreach(var cache in _systemEntityCache.Values)
            {
                cache.Remove(entity);
            }
        }

        // --- Utilities ---

        public T? GetSystem<T>() where T : GameSystem
        {
            return _systemEntityCache.Keys.OfType<T>().FirstOrDefault();
        }

        public void RemoveSystem(GameSystem system)
        {
            _policyBuckets[system.UpdatePolicy].Remove(system);
            _systemEntityCache.Remove(system);
        }
    }
}