using Engine.Core.Utilities;
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
        // Identification properties for the Editor UI to read
        public string SceneName { get; set; } = "Untitled Scene";

        public string ProjectPath { get; set; } = string.Empty;

        // Core Data Entities
        public EntityManager Entities { get; private set; } = null!;

        public SystemsManager Systems { get; private set; } = null!;

        public ManagersManager Managers { get; private set; } = null!;

 

       

        public GameScene()
        {
            InitializeManagers();
           
        }

        public void InitializeManagers() 
        {
            Entities = new EntityManager() { ContextScene = this };
            Systems = new SystemsManager() { ContextScene = this};
            Managers = new ManagersManager() { ContextScene = this };
            
            InitializeManagerEvents();

        }

        public void InitializeManagerEvents()
        {
            Entities.OnEntityCreated += entity => Systems.OnEntityChanged(entity);
            Entities.OnComponentAdded += (entity, comp) => Systems.OnEntityChanged(entity);
            Entities.OnComponentRemoved += (entity, comp) => Systems.OnEntityChanged(entity);
            Entities.OnEntityRemoved += entity => Systems.OnEntityDestroyed(entity);
        }

        
        //creates and returns a gameobject
        public GameObject Spawn(string name) 
        {

            var entity = new GameObject
            {
                Name = name,
                ContextScene = this
            };
            
            Entities.AddEntity(entity);
            return entity;
        }

        public GameObject Spawn(string name, params GameComponent[] components)
        {
            var entity = new GameObject
            {
                Name = name,
                ContextScene = this
            };

            // 1. Register with the manager first so events are fully wired
            Entities.AddEntity(entity);

            // 2. Add components now; they will correctly bubble up through the event chain
            foreach(var component in components)
            {
                entity.AddComponent(component);
            }

            return entity;
        }

        public void AddGameObject(GameObject go)
        {
            go.ContextScene = this;
            Entities.AddEntity(go);
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

        public void TickUpdate(float fixeddeltaTime)
        {
           // Systems.TickUpdate(fixeddeltaTime);
        }

            #endregion
        }

}

