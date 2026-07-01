using Engine.Core.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Serialization
{
    public class SceneFileContract
    {
        public string SceneName { get; set; } = "Untitled Scene";
        public Guid Id { get; set; } = Guid.NewGuid();
        public List<GameObject> Entities { get; set; } = new();
    }
}
