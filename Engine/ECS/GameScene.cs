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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class GameScene : Object
    {


        private int _loadOrder;

        private Map _sceneMap = new Map(25,19);

        private readonly TileMapRenderSystem _tileMapRenderer = new TileMapRenderSystem();

        // Identification properties for the Editor UI to read
        public string SceneName { get; set; } = "Untitled Scene";

        public string ProjectPath { get; set; } = string.Empty;

        // Core Data Entities
        public EntityManager Entities { get; private set; } = null!;

        public SystemsManager Systems { get; private set; } = null!;

        public ManagersManager Managers { get; private set; } = null!;

        public PhysicsManager Physics { get; private set; } = null!;

        private List<Map> _sceneMaps = new List<Map>();

        public List<Map> SceneMaps
        {
            get => _sceneMaps;
            set => _sceneMaps = value;
        }
        public Map SceneMap
        {
            get => _sceneMap;
            set => _sceneMap = value;
        }


        public int LoadOrder {
            get => _loadOrder;
            set => _loadOrder = value;
        }

        public DatabaseManager Database
        {
            get; private set; } = null!;

        public Guid Id { get; set; } = Guid.NewGuid();
       

        public GameScene()
        {
            InitializeManagers();
            if(!_sceneMaps.Contains(_sceneMap))
            {
                _sceneMaps.Add(_sceneMap);
            }

        }

        

        public void InitializeManagers() 
        {
            Entities = new EntityManager() { ContextScene = this };
            Systems = new SystemsManager() { ContextScene = this};
            Physics = new PhysicsManager() { ContextScene = this };
            Managers = new ManagersManager() { ContextScene = this };
            Database = new DatabaseManager() { ContextScene = this };
            
            InitializeManagerEvents();
            Database.LoadAllDatabasesFromFolder(EditorContextManager.DatabasePath);

        }

        public void InitializeManagerEvents()
        {
            Entities.OnEntityCreated += entity => Systems.OnEntityChanged(entity);
            Entities.OnComponentAdded += (entity, comp) => Systems.OnEntityChanged(entity);
            Entities.OnComponentRemoved += (entity, comp) => Systems.OnEntityChanged(entity);
            Entities.OnEntityRemoved += entity => Systems.OnEntityDestroyed(entity);
        }

        public void SetMapSize(int width, int height)
        {
            SceneMap = new Map(width, height);
        }

        public void ResizeMap(int newWidth, int newHeight)
        {
            var newMap = new Map(newWidth, newHeight);
            // Copy existing data to the new map
            for(int x = 0; x < Math.Min(SceneMap.Width, newWidth); x++)
            {
                for(int y = 0; y < Math.Min(SceneMap.Height, newHeight); y++)
                {
                    newMap.Grid[x, y] = SceneMap.Grid[x, y];
                }
            }
            SceneMap = newMap;
        }

        public void CleanupRuntimeEntities()
        {
            // Grab all entities currently in the scene
            var allEntities = Entities.GetSerializableEntities();

            // Find everything flagged as runtime-created
            var runtimeEntities = allEntities.Where(e => e.IsRuntimeCreated).ToList();

            foreach(var entity in runtimeEntities)
            {
                // Call your existing Destroy method to safely unlink components, systems, and parent trees
                entity.Destroy();
            }

            Log.Info($"[Scene] Cleaned up {runtimeEntities.Count} runtime-spawned GameObjects.");
        }

        //centralized factory
        private GameObject CreateEntityInstance(string name)
        {
            var entity = new GameObject
            {
                Name = name,
                ContextScene = this,
                // Automatically flag as runtime-created if the simulation is active OR if running a deployed build
                IsRuntimeCreated = EditorContextManager.PlayState
            };

            return entity;
        }
        //creates and returns a gameobject
        public GameObject Spawn(string name) 
        {

            var entity = CreateEntityInstance(name);
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

        // 1. In GameScene.cs, add an overload that handles initial placement:
        public GameObject Spawn(string name, float initialX, float initialY)
        {
            var entity = CreateEntityInstance(name);

            var transform = entity.AddComponent<TransformComponent>();
            transform.X = initialX;
            transform.Y = initialY;

            Entities.AddEntity(entity);
            return entity;
        }

        // 1. In GameScene.cs, add an overload that handles initial placement:
       
        public GameObject Spawn(string name, GameObject parentEntity)
        {
            var entity = CreateEntityInstance(name);

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
        /// Adds a new map to the scene's map collection and makes it active.
        /// </summary>
        public Map AddNewMap(int width, int height)
        {
            var newMap = new Map(width, height);
            _sceneMaps.Add(newMap);
            SceneMap = newMap;
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
                SceneMap = _sceneMaps.Last();
            }
        }



        #region Main Loop Execution

        /// <summary>
        /// The main execution  step called 60 times a second by MonoGame.
        /// </summary>
        /// <param name="deltaTime">The elapsed timestamp scale in seconds since the last frame draw.</param>
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
            Entities.ContextScene = this;
            Systems.ContextScene = this;
            Physics.ContextScene = this;
            Managers.ContextScene = this;

            foreach(var system in Systems._systemEntityCache.Keys)
            {
                system.ContextScene = this;
            }
        }


       

    }


    

}

