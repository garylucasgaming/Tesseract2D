using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor
{
    public class NewProjectOptions
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<string> SelectedPlatforms { get; set; } = new List<string>();
    }
}
