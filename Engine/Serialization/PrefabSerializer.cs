using Engine.Core.ECS;
using Engine.Core.Utilities;
using System;
using System.IO;
using System.Text.Json;

namespace Engine.Core.Serialization
{
    public static class PrefabSerializer
    {
        /// <summary>
        /// Bakes a standalone GameObject and its children out to an independent .prefab file asset.
        /// </summary>
        public static void SavePrefabToFile(GameObject rootGameObject, string relativePath)
        {
            try
            {
                string absolutePath = AssetPathProvider.ResolveProjectPath(relativePath);

                // Unified coding standard: Point to our universal polymorphic configuration options
                string jsonString = JsonSerializer.Serialize(rootGameObject, JsonConfiguration.Options);

                string? directory = Path.GetDirectoryName(absolutePath);
                if(!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(absolutePath, jsonString);
                Log.Info($"[Prefab Serializer] Successfully baked prefab asset: {relativePath}");
            }
            catch(Exception ex)
            {
                Log.Error($"[Prefab Serializer Error] Failed to save prefab. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads a .prefab file asset and reconstructs the GameObject tree back into memory.
        /// </summary>
        public static GameObject? LoadPrefabFromFile(string relativePath)
        {
            try
            {
                string absolutePath = AssetPathProvider.ResolveProjectPath(relativePath);
                if(!File.Exists(absolutePath))
                {
                    Log.Error($"[Prefab Serializer Error] Prefab file not found at: {absolutePath}");
                    return null;
                }

                string jsonString = File.ReadAllText(absolutePath);

                // Unified coding standard: Point to our universal polymorphic configuration options
                GameObject? root = JsonSerializer.Deserialize<GameObject>(jsonString, JsonConfiguration.Options);

                if(root != null)
                {
                    // CRUCIAL PASS: Re-stitch all Component Owners, Parent Backing Fields, and Transform Links!
                    SceneSerializer.FixHierarchyPointers(root);
                    Log.Info($"[Prefab Serializer] Successfully loaded prefab asset and repaired hierarchy loops from: {relativePath}");
                }

                return root;
            }
            catch(Exception ex)
            {
                Log.Error($"[Prefab Serializer Error] Failed to load prefab file: {ex.Message}");
                return null;
            }
        }
    }
}