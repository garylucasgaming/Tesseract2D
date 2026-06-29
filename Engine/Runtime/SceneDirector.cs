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
        public static GameScene? ActiveScene
        {
            get; private set;
        }

        public static void LoadScene(GameScene newScene)
        {
            if(ActiveScene != null)
            {
                // Clear the systems layout
                ActiveScene.Systems.Clear();

                // Clear out the store completely by looping through its serialized references
                var activeEntities = ActiveScene.Entities.GetSerializableEntities();
                for(int i = activeEntities.Count - 1; i >= 0; i--)
                {
                    ActiveScene.DestroyGameObject(activeEntities[i]);
                }
            }

            GameEvent.ClearAllListeners();
            ActiveScene = newScene;
        }

        public static void Update(float deltaTime)
        {
            ActiveScene?.Update(deltaTime);
        }
    }
}
