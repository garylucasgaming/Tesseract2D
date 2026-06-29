using Engine.Core.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Runtime
{
    public static class SceneManager
    {
      
        /// The single active scene currently being simulated and rendered.
     
        public static GameScene? ActiveScene
        {
            get; private set;
        }

        public static void LoadScene(GameScene newScene)
        {
           

           //todo
        }

        public static void Update(float deltaTime)
        {
            ActiveScene?.Update(deltaTime);
        }
    }
}
