using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Core.Utilities;

namespace Engine.Core.Collections
{
    public static class GameDatabaseDirector
    {
        // 1. Storage Registry: Keeps whole database containers organized by their resource class type
        private static readonly Dictionary<Type, object> _registeredDatabases = new();

        // 2. High-Speed Route Index: Maps ANY individual resource GUID directly to its live memory instance
        private static readonly Dictionary<Guid, DataResource> _globalResourceRegistry = new();

        /// <summary>
        /// Registers a loaded database instance under its resource type and dynamically indexes its items.
        /// </summary>
        public static void RegisterDatabase<T>(GameDatabase<T> database) where T : DataResource
        {
            if(database == null)
                return;

            // Register the provided database container instance under its resource type.
            _registeredDatabases[typeof(T)] = database;

            // Automation Pass: Unpack the collection items and index them globally for O(1) lookups
            foreach(var resource in database.Resources)
            {
                if(resource != null)
                {
                    if(_globalResourceRegistry.ContainsKey(resource.Id))
                    {
                        Log.Warning($"[Database Director Warning] Duplicate Resource GUID detected: {resource.Id}. Overwriting route pointer.");
                    }

                    _globalResourceRegistry[resource.Id] = resource;
                }
            }

            Log.Info($"[GameDatabaseDirector] Registered '{typeof(T).Name}' container table. Globally indexed {database.Resources.Count} item routes.");
        }

        /// <summary>
        /// Retrieves an active database instance container by its resource type. 
        /// Essential for loading asset files from disk or populating WinForms editor lists.
        /// </summary>
        public static GameDatabase<T>? GetDatabase<T>() where T : DataResource
        {
            if(_registeredDatabases.TryGetValue(typeof(T), out var dbObj) && dbObj is GameDatabase<T> database)
            {
                return database;
            }

            Log.Error($"[GameDatabaseDirector Error] Failed to retrieve database for type {typeof(T).Name}. It has not been registered.");
            return null;
        }

        /// <summary>
        /// Type-Safe Lookup: Finds a resource anywhere across ALL registered databases instantly and casts it.
        /// </summary>
        public static T? FindResource<T>(Guid resourceId) where T : DataResource
        {
            // Query our global route dictionary directly instead of scanning individual container files
            if(_globalResourceRegistry.TryGetValue(resourceId, out var resource))
            {
                return resource as T;
            }

            Log.Error($"[Database Director Error] Cannot query resource '{resourceId}'. It does not exist in any registered database workspace.");
            return null;
        }

        /// <summary>
        /// Global Polymorphic Lookup: Finds a raw resource record by its GUID without needing to specify its exact type.
        /// Great for generic engine systems, data-linking components, or property inspectors!
        /// </summary>
        public static DataResource? FindResource(Guid resourceId)
        {
            return _globalResourceRegistry.TryGetValue(resourceId, out var resource) ? resource : null;
        }
    }
}