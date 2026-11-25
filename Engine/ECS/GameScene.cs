using Engine.Core.ECS.Components;
using Engine.Core.ECS.Managers;
using Engine.Core.Gameplay;
using Engine.Core.GamePlay;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class GameScene : Object
    {
        private int _loadOrder;
        private int _activeMapIndex = 0;

        private readonly TileMapRenderSystem _tileMapRenderer = new TileMapRenderSystem();

        // Identification properties for the Editor UI to read
        public string SceneName { get; set; } = "Untitled Scene";

        public string ProjectPath { get; set; } = string.Empty;

        // Core Data Entities
        public EntityManager Entities { get; private set; } = null!;
        public SystemsManager Systems { get; private set; } = null!;
        public ManagersManager Managers { get; private set; } = null!;
        public PhysicsManager Physics { get; private set; } = null!;

        private BindingList<Map> _sceneMaps = new BindingList<Map>();
        public Map ActiveMap
        {
            get => _sceneMaps.ElementAtOrDefault(_activeMapIndex) ?? _sceneMaps.FirstOrDefault() ?? new Map(25, 19);
        }
        public BindingList<Map> SceneMaps
        {
            get => _sceneMaps;
            set => _sceneMaps = value ?? new BindingList<Map>();
        }

        public int ActiveMapIndex
        {
            get => _activeMapIndex;
            set => _activeMapIndex = Math.Clamp(value, 0, Math.Max(0, _sceneMaps.Count - 1));
        }

        public int LoadOrder
        {
            get => _loadOrder;
            set => _loadOrder = value;
        }

        public DatabaseManager Database { get; private set; } = null!;

        public IEngineContext Context { get; }

        public SessionGlobals Session { get; }

        public Guid Id { get; set; } = Guid.NewGuid();

        private bool _managerEventsInitialized;
        private bool _databasesLoaded;
        private readonly bool _databaseExternallyOwned;

        public GameScene(
            IEngineContext? context = null,
            DatabaseManager? database = null,
            SessionGlobals? session = null)
        {
            Context = context ?? session?.Context ?? EngineContext.Current;
            Session = session ?? SessionGlobals.Current;
            Database = database ?? new DatabaseManager();
            _databaseExternallyOwned = database != null;
            InitializeManagers();

            // Ensure there is always at least one map initialized in the scene
            if(_sceneMaps.Count == 0)
            {
                _sceneMaps.Add(new Map(25, 19));
            }
        }

        public void InitializeManagerEvents()
        {
            if(_managerEventsInitialized)
                return;

            Entities.OnEntityCreated += entity => Systems.OnEntityChanged(entity);
            Entities.OnComponentAdded += (entity, comp) => Systems.OnEntityChanged(entity);
            Entities.OnComponentRemoved += (entity, comp) => Systems.OnEntityChanged(entity);
            Entities.OnEntityRemoved += entity => Systems.OnEntityDestroyed(entity);
            Entities.OnEntityActiveChanged += entity => Systems.OnEntityChanged(entity);
            _managerEventsInitialized = true;
        }

        public void InitializeManagers()
        {
            if(Entities == null)
            {
                Entities = new EntityManager() { ContextScene = this };
                Systems = new SystemsManager(Session) { ContextScene = this };
                Physics = new PhysicsManager() { ContextScene = this };
                Managers = new ManagersManager() { ContextScene = this };
            }

            InitializeManagerEvents();

            if(!_databasesLoaded && !_databaseExternallyOwned)
            {
                Database.LoadAllDatabasesFromFolder(Context.DatabasePath);
                _databasesLoaded = true;
            }

            FinalizeInitialization();
        }

        /// <summary>
        /// Canonical "scene is ready" step. Rebinds context pointers and rebuilds
        /// derived system caches without duplicating event subscriptions or managers.
        /// </summary>
        public void FinalizeInitialization()
        {
            Entities.ContextScene = this;
            Physics.ContextScene = this;
            Managers.ContextScene = this;
            Database.ContextScene = this;
            Systems.SetContextScene(this);
        }

        public void CleanupRuntimeEntities()
        {
            var allEntities = Entities.GetSerializableEntities();
            var runtimeEntities = allEntities.Where(e => e.IsRuntimeCreated).ToList();

            foreach(var entity in runtimeEntities)
            {
                entity.Destroy();
            }

            Log.Info($"[Scene] Cleaned up {runtimeEntities.Count} runtime-spawned GameObjects.");
        }

        // Centralized factory
        private GameObject CreateEntityInstance(string name, bool isRuntimeCreated = false)
        {
            var entity = new GameObject
            {
                Name = name,
                ContextScene = this,
                IsRuntimeCreated = isRuntimeCreated
            };

            return entity;
        }

        public GameObject Spawn(string name)
        {
            return Spawn(name, false);
        }

        public GameObject Spawn(string name, bool isRuntimeCreated)
        {
            var entity = CreateEntityInstance(name, isRuntimeCreated);
            Entities.AddEntity(entity);
            return entity;
        }

        public GameObject Spawn(string name, params GameComponent[] components)
        {
            var entity = CreateEntityInstance(name);
            Entities.AddEntity(entity);

            foreach(var component in components)
            {
                entity.AddComponent(component);
            }

            return entity;
        }

        public GameObject Spawn(string name, float initialX, float initialY, bool isRuntimeCreated = false)
        {
            var entity = CreateEntityInstance(name, isRuntimeCreated);

            var transform = entity.AddComponent<TransformComponent>();
            transform.X = initialX;
            transform.Y = initialY;

            Entities.AddEntity(entity);
            return entity;
        }

        public GameObject Spawn(string name, GameObject parentEntity, bool isRuntimeCreated = false)
        {
            var entity = CreateEntityInstance(name, isRuntimeCreated);

            var childTransform = entity.AddComponent<TransformComponent>();
            var parentTransform = parentEntity?.GetComponent<TransformComponent>();

            if(childTransform != null && parentTransform != null)
            {
                childTransform.X = parentTransform.X;
                childTransform.Y = parentTransform.Y;
            }

            if(parentEntity != null)
            {
                entity.SetParent(parentEntity);
            }

            Entities.AddEntity(entity);
            return entity;
        }

        public void AddGameObject(GameObject go)
        {
            go.ContextScene = this;
            Entities.AddEntity(go);
        }

        /// <summary>
        /// Retrieves a map by its display name (case-insensitive search).
        /// </summary>
        public Map? GetMapByName(string mapName)
        {
            if(string.IsNullOrEmpty(mapName))
                return null;
            return _sceneMaps.FirstOrDefault(m => m.MapName.Equals(mapName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds a new map to the scene's map collection and switches the active index to it.
        /// </summary>
        public Map AddNewMap(int width, int height)
        {
            var newMap = new Map(width, height);
            newMap.ContextScene = this;
            _sceneMaps.Add(newMap);
            _activeMapIndex = _sceneMaps.Count - 1;
            return newMap;
        }

        /// <summary>
        /// Removes a map from the scene, ensuring at least one map remains.
        /// </summary>
        public void RemoveMap(Map mapToRemove)
        {
            if(_sceneMaps.Count > 1 && _sceneMaps.Contains(mapToRemove))
            {
                _sceneMaps.Remove(mapToRemove);
                if(_activeMapIndex >= _sceneMaps.Count)
                {
                    _activeMapIndex = _sceneMaps.Count - 1;
                }
            }
        }

        #region Main Loop Execution

        public void Update(float deltaTime, bool playModeActive = false)
        {
            Systems.Update(deltaTime, playModeActive);
        }

        public void TickUpdate(float fixeddeltaTime, bool playModeActive = false)
        {
            Systems.TickUpdate(fixeddeltaTime, playModeActive);
        }

        public void Render(SpriteBatch sb, ContentManager cm)
        {
            _tileMapRenderer.Render(sb, cm, _sceneMaps);
        }

        #endregion

        public void resetContextSceneInManagers()
        {
            FinalizeInitialization();
        }
    }
}

