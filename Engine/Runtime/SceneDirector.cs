using Engine.Core.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Runtime
{
    public static class SceneDirector
    {
        /// <summary>
        /// The single active scene currently being simulated and rendered.
        /// </summary>
        public static Scene? ActiveScene
        {
            get; private set;
        }

        /// <summary>
        /// Safely unloads the old scene state and activates a new level configuration.
        /// </summary>
        public static void LoadScene(Scene newScene)
        {
            // 1. Perform cleanup on the outgoing scene to release resources
            if(ActiveScene != null)
            {
                ActiveScene.GameObjects.Clear();
                ActiveScene.Systems.Clear();
            }

            // 2. Assign the fresh scene sandbox
            ActiveScene = newScene;
        }

        /// <summary>
        /// Global heartbeat tick that drives the active scene's system execution pipeline.
        /// </summary>
        public static void Update(float deltaTime)
        {
            ActiveScene?.Update(deltaTime);
        }
    }
}
