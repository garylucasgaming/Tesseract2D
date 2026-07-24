using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Engine.Core.Serialization
{
    public class ProjectManifest
    {
        public string EngineVersion { get; set; } = "1.0.0";
        public string ProjectName { get; set; } = "New Project";
        public string LastUsedScene { get; set; } = "";

        /// <summary>
        /// List of target platform build targets enabled for this project (e.g., "Desktop", "Android", "iOS", "Web").
        /// </summary>
        public List<string> TargetPlatforms { get; set; } = new List<string> { "Desktop" };

        /// <summary>
        /// The absolute master index for the entire project workspace.
        /// Key: Unique Asset GUID (as a string)
        /// Value: Relative file path on disk (e.g., "Content/Databases/Items.database")
        /// </summary>
        public Dictionary<string, string> AssetRegistry { get; set; } = new();
        public static void CreateInitialManifest(string projectRootPath, string projectName, List<string> selectedPlatforms)
        {
            string manifestPath = Path.Combine(projectRootPath, "Content", "ProjectManifest.db");

            var manifest = new ProjectManifest
            {
                ProjectName = projectName,
                TargetPlatforms = selectedPlatforms
            };

            string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, json);
        }

       

    }
}


  
