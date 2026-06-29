using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class SystemsManager
    {
        //local list of all registered systems in the scene
        public List<GameSystem> Systems { get; private set; } = new();

        public GameScene ContextScene { get;  set; } = null!;

        public SystemsManager()
        {
            InitializeSystemCache();
        }

       

        // High-speed cache lookup: Component Type -> System Type
        private static Dictionary<Type, Type>? _componentToSystemCache;
        private static readonly object _cacheLock = new object();

        // Registers a GameSystem into the execution pipeline and automatically re-sorts the update stack.
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
            Systems = Systems.OrderBy(s => s.UpdatePolicy).ToList();
        }

        // Retrieves a specific active system (like your MovementSystem) by its derived subclass type.
        // Useful for editor inspection or cross-system configuration.
        public T? GetSystem<T>() where T : GameSystem
        {
            return Systems.OfType<T>().FirstOrDefault();
        }

        // Removes a system from the active processing pipeline.
        public void RemoveSystem(GameSystem system)
        {
            Systems.Remove(system);
        }

        //returns a read-only list of all registered systems in the scene
        public IReadOnlyList<GameSystem> GetRegisteredSystems()
        {
            return Systems;
        }


       
        // Scans all loaded assemblies via reflection once to map components to their systems.
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
                            //TODO: give it a specific folder to look at. perhaps even a json file. 
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

    }
}
