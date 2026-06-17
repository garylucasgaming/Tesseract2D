using Engine.Core.Collections;
using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Handles all file reading and writing operations specifically for database container files.
    /// </summary>
    public static class DatabaseSerializer
    {
        private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        /// <summary>
        /// Serializes an entire database container and its items directly to a file path.
        /// </summary>
        public static void SaveDatabaseToFile<T>(GameDatabase<T> database, string absolutePath) where T : class
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(database, JsonConfiguration.Options);

                string? directory = Path.GetDirectoryName(absolutePath);
                if(!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(absolutePath, jsonString);
                Log.Info($"[Database Serializer] Baked database file to disk: {absolutePath}");
            }
            catch(Exception ex)
            {
                Log.Error($"[Database Serializer Error] Failed to save database. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Deserializes a database container file back into an in-memory collection object.
        /// </summary>
        public static GameDatabase<T>? LoadDatabaseFromFile<T>(string absolutePath) where T : class
        {
            try
            {
                if(!File.Exists(absolutePath))
                {
                    Log.Error($"[Database Serializer Error] File not found at: {absolutePath}");
                    return null;
                }

                string jsonString = File.ReadAllText(absolutePath);
                return JsonSerializer.Deserialize<GameDatabase<T>>(jsonString, _options);
            }
            catch(Exception ex)
            {
                Log.Error($"[Database Serializer Error] Failed to read database at '{absolutePath}'. Reason: {ex.Message}");
                return null;
            }
        }
    }
}
