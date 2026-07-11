using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Serialization
{
    public class ProjectManifest
    {
        public string EngineVersion { get; set; } = "1.0.0";
        public string ProjectName { get; set; } = "New Project";

        public string LastUsedScene { get; set; } = "";

       
        /// <summary>
        /// The absolute master index for the entire project workspace.
        /// Key: Unique Asset GUID (as a string)
        /// Value: Relative file path on disk (e.g.,  "Content/Databases/Items.database")
        /// </summary>
        public Dictionary<string, string> AssetRegistry { get; set; } = new();
    }
}
