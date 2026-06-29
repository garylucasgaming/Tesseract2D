using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class GameScene
    {
        // Identification properties for the Editor UI to read
        public string SceneName { get; set; } = "Untitled Scene";

        public Guid Id { get; set; } = Guid.NewGuid();
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
            Systems = new SystemsManager();
            Systems.ContextScene = this;
            Managers = new ManagersManager();
            Managers.ContextScene = this;
            Entities = new EntityManager();
            Entities.ContextScene = this;
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
            
            Entities.AddEntity(entity);
            foreach(var component in components)
            {
                entity.AddComponent(component);
            }
            return entity;
        }

       

        #region Main Loop Execution

        /// <summary>
        /// The main execution  step called 60 times a second by MonoGame.
        /// </summary>
        /// <param name="deltaTime">The elapsed timestamp scale in seconds since the last frame draw.</param>
        public void Update(float deltaTime)
        {
            // Execute each active system worker sequentially down the pipeline assembly line
            for(int i = 0; i < Systems.Systems.Count; i++)
            {
                if(Systems.Systems[i].IsEnabled)
                {
                    Type? targetComponent = Systems.Systems[i].RequiredComponentType;

                    if(targetComponent != null)
                    {
                        // OPTIMIZATION: System only processes entities that actually care about it!
                        var filteredEntities = Entities.GetEntitiesWithComponent(targetComponent);
                        Systems.Systems[i].Update(filteredEntities, deltaTime);
                    }
                    else
                    {
                        // Fallback for global background systems that don't need components
                        Systems.Systems[i].Update(Entities.GetSerializableEntities(), deltaTime);
                    }
                }
            }
        }

            #endregion
        }

}

