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
            public string ProjectPath { get; set; } = string.Empty;

            // Core Data Entities
            public List<GameObject> GameObjects { get; private set; } = new();

            // Core Execution Logic Pipeline
            public List<GameSystem> Systems { get; private set; } = new();

            // Local Scene Managers (e.g., VoxelWorldManager, SceneAudioManager)
            private readonly Dictionary<Type, object> _sceneManagers = new();

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
                Systems.Add(system);
                // Sort dynamically by execution hierarchy (e.g., lower priorities execute first)
                Systems = Systems.OrderBy(s => s.UpdateOrder).ToList();
            }

        public void OnComponentAddedToScene(GameComponent component)
        {
            Type compType = component.GetType();

            // 1. Scan your project's available system classes (done once or cached)
            // 2. If a system's RequiredComponentType matches compType, check if it's in the scene
            if(!Systems.Any(s => s.RequiredComponentType == compType))
            {
                // Find the system class type that matches and spin it up
                Type? systemType = FindSystemTypeForComponent(compType);
                if(systemType != null)
                {
                    var newSystem = (GameSystem) Activator.CreateInstance(systemType)!;
                    AddSystem(newSystem);
                }
            }
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
            /// Registers a specialized data manager or manager utility to this local scene workspace.
            /// </summary>
            public void AddManager<T>(T managerInstance) where T : class
            {
                var type = typeof(T);
                if(_sceneManagers.ContainsKey(type))
                {
                    throw new ArgumentException($"A manager of type {type.Name} is already registered to this scene.");
                }
                _sceneManagers[type] = managerInstance;
            }

            /// <summary>
            /// Retrieves a specialized scene manager (like your VoxelWorldManager) instantly.
            /// </summary>
            public T? GetManager<T>() where T : class
            {
                var type = typeof(T);
                if(_sceneManagers.TryGetValue(type, out var manager))
                {
                    return (T) manager;
                }
                return null;
            }

            #endregion

            #region Main Loop Execution

            /// <summary>
            /// The main execution heartbeat step called 60 times a second by MonoGame.
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

