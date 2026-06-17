using Engine.Core.Runtime;
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
            public List<GameObject> GameObjects { get; private set; } = new();

            // Core Execution Logic Pipeline
            public List<GameSystem> Systems { get; private set; } = new();

            // Local Scene Managers (e.g., VoxelWorldManager, SceneAudioManager)
            private readonly List<GameManager> Managers = new();

            // High-speed cache lookup: Component Type -> System Type
            private static Dictionary<Type, Type>? _componentToSystemCache;
            private static readonly object _cacheLock = new object();

            public GameScene()
            {
            // Ensure the global system types are indexed once across the whole application lifecycle
            InitializeSystemCache();
            }


        /// <summary>
        /// Looks up the cached system class type associated with a specific component type.
        /// </summary>
        private Type? FindSystemTypeForComponent(Type componentType)
        {
            if(_componentToSystemCache != null && _componentToSystemCache.TryGetValue(componentType, out var systemType))
            {
                return systemType;
            }
            return null;
        }

        /// <summary>
        /// Scans all loaded assemblies via reflection once to map components to their systems.
        /// </summary>
        private static void InitializeSystemCache()
        {
            // Lock ensures thread safety if multiple threads initialize scenes simultaneously
            lock(_cacheLock)
            {
                if(_componentToSystemCache != null)
                    return; // Already initialized!

                _componentToSystemCache = new Dictionary<Type, Type>();

                // Get all code assemblies loaded in the current game application instance
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach(var assembly in assemblies)
                {
                    // Skip Microsoft/System assemblies to save processing time
                    string? assemblyName = assembly.FullName;
                    if(assemblyName != null && (assemblyName.StartsWith("System") || assemblyName.StartsWith("Microsoft")))
                    {
                        continue;
                    }


                    try
                    {
                        var types = assembly.GetTypes();

                        foreach(var type in types)
                        {
                            // skip any types in the Engine.Core.Runtime namespace to avoid accidentally linking internal utilities as systems
                            if(type.Namespace != null && type.Namespace.StartsWith("Engine.Core.Runtime"))
                            {
                                continue; // Skip infrastructure utilities, they don't contain GameSystems with components!
                            }

                            // We are looking for concrete classes that inherit from our base GameSystem
                            if(type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(GameSystem)))
                            {
                                // Create a temporary "blueprint" instance to read its default virtual properties
                                // (This lets us read RequiredComponentType without executing the system)
                                var instance = Activator.CreateInstance(type) as GameSystem;

                                if(instance?.RequiredComponentType != null)
                                {
                                    Type componentRequirement = instance.RequiredComponentType;

                                    // Map the component type directly to this system type
                                    if(!_componentToSystemCache.ContainsKey(componentRequirement))
                                    {
                                        _componentToSystemCache[componentRequirement] = type;
                                    }
                                }
                            }
                        }
                    }
                    catch(ReflectionTypeLoadException)
                    {
                        // Safely ignore assemblies that cannot be fully scanned in the current context
                        continue;
                    }
                }
            }
        }
        #region GameObject Lifecycle

        /// <summary>
        /// Instantiates a new GameObject directly inside the running scene environment.
        /// </summary>
        public GameObject CreateGameObject(string name)
            {
                var go = new GameObject
                {
                    id = Guid.NewGuid(),
                    name = name
                };
                GameObjects.Add(go);
                return go;
            }

            /// <summary>
            /// Registers an existing GameObject instance (useful for loading or dragging assets into view).
            /// </summary>
            public void RegisterGameObject(GameObject gameObject)
            {
                if(!GameObjects.Contains(gameObject))
                {
                    GameObjects.Add(gameObject);
                }
            }

            /// <summary>
            /// Safely tears down an entity and scrubs it from the active runtime execution sweeps.
            /// </summary>
            public void DestroyGameObject(GameObject gameObject)
            {
                gameObject.isActive = false;
                GameObjects.Remove(gameObject);

                // Wipe component links to prevent dangling memory leaks
                foreach(var component in gameObject.Components)
                {
                    component.Owner = null;
                }
                gameObject.Components.Clear();
            }

        #endregion

        #region System Management

        /// <summary>
        /// Registers a GameSystem into the execution pipeline and automatically re-sorts the update stack.
        /// </summary>
        public void AddSystem(GameSystem system)
        {
            if(system == null)
                return;

            Type systemType = system.GetType();

            // Prevent duplicate system types of the exact same subclass from piling up
            if(Systems.Any(s => s.GetType() == systemType))
            {
                return; // System is already tracking
            }

            Systems.Add(system);

            // Sort dynamically by execution hierarchy (e.g., lower priority indices execute first)
            Systems = Systems.OrderBy(s => s.UpdateOrder).ToList();
        }

        /// <summary>
        /// Retrieves a specific active system (like your MovementSystem) by its derived subclass type.
        /// Useful for editor inspection or cross-system configuration.
        /// </summary>
        public T? GetSystem<T>() where T : GameSystem
        {
            return Systems.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Removes a system from the active processing pipeline.
        /// </summary>
        public void RemoveSystem(GameSystem system)
        {
            Systems.Remove(system);
        }

        #endregion

        #region Local Scene Managers (Services)

        /// <summary>
        /// Registers a specialized data manager or service to this local scene workspace.
        /// </summary>
        public void AddManager(GameManager managerInstance)
        {
            if(managerInstance == null)
                return;

            Type managerType = managerInstance.GetType();

            // Prevent duplicate manager types of the exact same derived subclass from piling up
            if(Managers.Any(m => m.GetType() == managerType))
            {
                throw new ArgumentException($"A manager of type {managerType.Name} is already registered to this scene.");
            }

            Managers.Add(managerInstance);
        }

        /// <summary>
        /// Retrieves a specialized scene manager by its derived subclass type.
        /// </summary>
        public T? GetManager<T>() where T : GameManager
        {
            // Search the polymorphic list for any instance that matches or derives from T
            return Managers.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Exposes the flat list of scene-bound managers 
        /// </summary>
        public IReadOnlyList<GameManager> GetRegisteredManagers()
        {
            return Managers;
        }

        #endregion

        #region Main Loop Execution

        /// <summary>
        /// The main execution  step called 60 times a second by MonoGame.
        /// </summary>
        /// <param name="deltaTime">The elapsed timestamp scale in seconds since the last frame draw.</param>
        public void Update(float deltaTime)
            {
                // Execute each active system worker sequentially down the pipeline assembly line
                for(int i = 0; i < Systems.Count; i++)
                {
                    if(Systems[i].IsEnabled)
                    {
                        Systems[i].Update(GameObjects, deltaTime);
                    }
                }
            }

            #endregion
        }

}

