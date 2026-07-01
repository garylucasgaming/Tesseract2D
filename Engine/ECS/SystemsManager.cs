using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core.ECS
{
    public class SystemsManager
    {
        // 1. Category Buckets: Policy Type -> List of matching systems
        private readonly Dictionary<SystemUpdatePolicy, List<GameSystem>> _policyBuckets = new();

        // 2. The Golden Nugget: A warm entity bucket mapped explicitly to each active system
        private readonly Dictionary<GameSystem, HashSet<GameObject>> _systemEntityCache = new();

        public GameScene ContextScene { get; set; } = null!;

        public SystemsManager()
        {
            // Automatically initialize a bucket list for every single policy enum type
            foreach(SystemUpdatePolicy policy in Enum.GetValues(typeof(SystemUpdatePolicy)))
            {
                _policyBuckets[policy] = new List<GameSystem>();
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
        
        public void Update(float deltaTime)
        {
            // FrameUpdate: Variable frame rate execution
            ExecuteSystemBucket(SystemUpdatePolicy.FrameUpdate, deltaTime);

            // FixedUpdate: Custom intervals (clocks ticked behind the scenes)
            var customIntervalSystems = _policyBuckets[SystemUpdatePolicy.FixedUpdate];
            for(int i = 0; i < customIntervalSystems.Count; i++)
            {
                var system = customIntervalSystems[i];
                if(!system.IsEnabled)
                    continue;

                system.timer += deltaTime;
                if(system.timer >= system.UpdateInterval)
                {
                    system.timer = 0.0f; // Reset the clock
                    system.Update(_systemEntityCache[system], deltaTime);
                }
            }
        }

        
        /// Runs TickUpdate systems on your locked, rigid simulation step ticker.
        public void TickUpdate(float fixedDeltaTime)
        {
            ExecuteSystemBucket(SystemUpdatePolicy.TickUpdate, fixedDeltaTime);
        }

        
        /// Drives Manual systems. Call this explicitly to trigger a manual system by type.
        //probably not needed as can just use events instead. 
        public void TriggerManualSystem<T>(float deltaTime) where T : GameSystem
        {
            var system = GetSystem<T>();
            if(system == null || !system.IsEnabled || system.UpdatePolicy != SystemUpdatePolicy.Manual)
                return;

            system.Update(_systemEntityCache[system], deltaTime);
        }

        
        /// Helper to sweep through standard execution pipelines cleanly.
        
        private void ExecuteSystemBucket(SystemUpdatePolicy policy, float deltaTime)
        {
            var systems = _policyBuckets[policy];
            for(int i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if(!system.IsEnabled || !system.shouldUpdate)
                    continue;

                system.Update(_systemEntityCache[system], deltaTime);
            }
        }

        // --- Reactive Engine Listeners: Hooked to your scene spawn/component change events ---

        
        /// Call this whenever an entity spawns, gets activated, or gains a new component.
        
        public void OnEntityChanged(GameObject entity, float deltaTime = 0.0f)
        {
            foreach(var kvp in _systemEntityCache)
            {
                var system = kvp.Key;
                var cache = kvp.Value;

                bool currentlyMatches = entity.isActive && system.RequiredComponents.IsMatched(entity);
                bool wasInCache = cache.Contains(entity);

                if(currentlyMatches)
                {
                    cache.Add(entity);

                    // EntityUpdate Policy: If an entity mutates and matches, tick this system instantly!
                    if(system.UpdatePolicy == SystemUpdatePolicy.EntityUpdate && system.IsEnabled && !wasInCache)
                    {
                        var singleEntityBatch = new HashSet<GameObject> { entity };
                        system.Update(singleEntityBatch, deltaTime);
                    }
                }
                else
                {
                    cache.Remove(entity);
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