using Engine.Core.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Gameplay
{
    public class testSystem : GameSystem
    {
        public override IComponentQuery RequiredComponents
        {
            get;
            set;
        }

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
           
        }
    }
}
