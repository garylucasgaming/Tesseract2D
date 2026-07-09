using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class GameSpace
    {
        // Global metadata and player configurations
        public string GameTitle { get; set; } = "My Colony Simulation";
        public string EngineVersion { get; set; } = "2026.1";

        // Global Settings (Graphics, Audio, Keybindings)
        public Dictionary<string, string> GlobalSettings { get; set; } = new();

        // The Master Database of all Scenes/Worlds available to this project
        // Stores paths or IDs so they can be lazy-loaded from disk on demand
        public List<SceneMetadata> AvailableScenes { get; set; } = new();

        // Runtime Active Context (Not serialized directly, set at boot-up)
        [System.Text.Json.Serialization.JsonIgnore]
        public GameScene? ActiveScene
        {
            get; private set;
        }

       
    }

    public class SceneMetadata
    {
        public Guid SceneId
        {
            get; set;
        }
        public string SceneName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}

