using System;
using System.Collections.Generic;
using Engine.Core.Utilities;

namespace Engine.Core.Collections
{
    public static class GameDatabaseDirector
    {
        // A registry of active game databases, keyed by their type for easy retrieval.
        private static readonly Dictionary<Type, object> _registeredDatabases = new();

        public static void RegisterDatabase<T>(GameDatabase<T> database) where T : DataResource
        {
            // Register the provided database instance under its resource type.
            _registeredDatabases[typeof(T)] = database;
        }

        /// <summary>
        /// Retrieves an active database instance by its resource type. 
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

        public static T? FindResource<T>(Guid id) where T : DataResource
        {
            if(_registeredDatabases.TryGetValue(typeof(T), out var dbObj) && dbObj is GameDatabase<T> database)
            {
                // Attempt to retrieve the resource by ID from the appropriate database. Returns null if not found.
                return database.GetById(id);
            }
            else
            {
                Log.Error($"[GameDatabaseDirector Error] No active database found for resource type {typeof(T).Name}. Ensure a GameDatabase<{typeof(T).Name}> is registered before attempting to find resources.");
                return null; // Return null if no database is registered for the requested type.
            }
        }
    }
}