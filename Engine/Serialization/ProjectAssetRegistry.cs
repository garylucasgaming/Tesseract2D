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
    /// The master file registry for the active project workspace. 
    /// Manages the project's .db manifest file and coordinates project-level file maps.
    /// </summary>
    /// <summary>
    /// A pure foundational storage index. Maps asset GUIDs to their relative physical file paths.
    /// </summary>
    public static class ProjectAssetRegistry
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private static readonly string ManifestFileName = "ProjectManifest.db";

        public static ProjectManifest ActiveManifest { get; private set; } = new();

        /// <summary>
        /// Registers any universal file asset into the master map.
        /// </summary>
        public static void RegisterAsset(Guid id, string relativePath)
        {
            string idStr = id.ToString();
            if(!ActiveManifest.AssetRegistry.ContainsKey(idStr))
            {
                ActiveManifest.AssetRegistry.Add(idStr, relativePath);
                SaveProjectWorkspace();
            }
        }

        /// <summary>
        /// Retrieves a relative file path from the registry using its unique identifier.
        /// Returns null if the asset is not tracked.
        /// </summary>
        public static string? TryGetAssetPath(Guid id)
        {
            return ActiveManifest.AssetRegistry.TryGetValue(id.ToString(), out string? path) ? path : null;
        }

        /// <summary>
        /// Pulls the entire project manifest into live memory from the permanent drive.
        /// </summary>
        public static void LoadProjectWorkspace()
        {
            try
            {
                string absolutePath = AssetPathProvider.ResolveProjectPath(ManifestFileName);

                if(!File.Exists(absolutePath))
                {
                    Log.Info("[Asset Registry] No manifest found. Initializing a fresh, empty workspace registry.");
                    ActiveManifest = new ProjectManifest();
                    SaveProjectWorkspace();
                    return;
                }

                string jsonStr = File.ReadAllText(absolutePath);
                ActiveManifest = JsonSerializer.Deserialize<ProjectManifest>(jsonStr, _jsonOptions) ?? new();
                Log.Info($"[Asset Registry] Successfully fetched project manifest. Tracking {ActiveManifest.AssetRegistry.Count} asset endpoints.");
            }
            catch(Exception ex)
            {
                Log.Error($"[Asset Registry Error] Failed to retrieve project manifest layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Commits the active asset map directly back to the physical .db file.
        /// </summary>
        public static void SaveProjectWorkspace()
        {
            try
            {
                string absolutePath = AssetPathProvider.ResolveProjectPath(ManifestFileName);
                string jsonStr = JsonSerializer.Serialize(ActiveManifest, _jsonOptions);
                File.WriteAllText(absolutePath, jsonStr);
            }
            catch(Exception ex)
            {
                Log.Error($"[Asset Registry Error] Failed to bake manifest update to disk: {ex.Message}");
            }
        }
    }
}
