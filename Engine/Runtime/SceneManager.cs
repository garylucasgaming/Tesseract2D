using Engine.Core.ECS;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
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
        private static int _currentSceneIndex = 0;
        public static GameScene? ActiveScene
        {
            get; private set;
        }

        public static GameScene? NextScene
        {
            get;
            set;
        }

        public static Dictionary<int, GameScene> TotalScenes = new Dictionary<int, GameScene>();


        public static void AddScene(int loadOrder, GameScene newScene)
        {

            if(!TotalScenes.Keys.Contains(loadOrder))
            {
                TotalScenes.Add(loadOrder, newScene);
            }
            else
            {
                //recursively attempt to addscene at the next available load order
                loadOrder++;
                Log.Warning("Load order for scene: " + newScene.SceneName + " was taken, attempting to add scene to next available load slot");
                AddScene(loadOrder, newScene);
            }

           


        }


        public static void LoadFirstScene()
        {
            if(TotalScenes != null)
            {
                //TODO change to serializing the scene;
                ActiveScene = TotalScenes[_currentSceneIndex];
                SetNextScene();
               
            }
               

           
        }

        private static void SetNextScene()
        {
            if(_currentSceneIndex + 1 >= 0 && _currentSceneIndex + 1 < TotalScenes.Count)
            {
                //todo, change to preloading scene
                NextScene = TotalScenes[_currentSceneIndex + 1];
            }
        }


        public static void LoadNextScene()
        {
            //TODO change to serializing the scene;
            ActiveScene = NextScene;

            EngineUI.LoadSceneUI(ActiveScene);
            _currentSceneIndex++;
            SetNextScene();
        }

        public static void LoadScene(GameScene newScene)
        {


            //todo

            EngineUI.LoadSceneUI(newScene);
            
            //ActiveScene = SceneSerializer.LoadScene(newScene);

        }


      

        public static void PreLoadScene(GameScene newScene)
        {
            //NextScene = SceneSerializer.LoadScene(newScene);
        }

        public static void Update(float deltaTime, bool playModeActive = false)
        {
            ActiveScene?.Update(deltaTime, playModeActive);
        }

        public static void TickUpdate(float fixedDeltaTime, bool playModeActive = false)
        {
            ActiveScene?.TickUpdate(fixedDeltaTime, playModeActive);
        }
    }
}
