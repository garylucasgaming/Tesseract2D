using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.Utilities
{
    public class ProjectMetadata
    {
        public string Name { get; set; } = "New Project";
        public string FolderPath { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; } = DateTime.Now;
        public string EngineVersion { get; set; } = "1.0.0";
        public string? ThumbnailPath
        {
            get; set;
        }
    }
}
