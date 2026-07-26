using Engine.Core.ECS;
using Engine.Core.Utilities;
using Gum;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Runtime
{
    public static class EngineUI
    {

        private static MockGame? _mockGame = null;
        public static GumService GumUI => GumService.Default;


        public static void Initialize(Game game)
        {
            GumUI.Initialize(game);
        }

        public static void Initialize(GraphicsDevice gm, ContentManager cm)
        {
            _mockGame = new MockGame(gm, cm);
            GumUI.Initialize(_mockGame);
        }

        public static void InitializeProjectUI(GraphicsDevice graphicsDevice, ContentManager contentManager, string gumProjectFilePath)
        {
            // Initialize the mock game wrapper if running in the WinForms editor
            _mockGame = new MockGame(graphicsDevice, contentManager);

            // Initialize Gum with the external project file path
            var gumProject = GumUI.Initialize(_mockGame, gumProjectFilePath);
        }


        public static void Initialize(Game game, string gumProjectFilePath)
        {
            GumUI.Initialize(game, gumProjectFilePath);

        }

        public static void Update(GameTime gameTime)
        {
            GumUI.Update(gameTime);
        }

        public static void Draw()
        {
            GumUI.Draw();

        }

        public static void LoadSceneUI(GameScene scene)
        {
            // 1. Clear out any previous scene's UI root children
            GumUI.Root.Children.Clear();

            // 2. Grab the project via ObjectFinder
            var gumProject = ObjectFinder.Self.GumProjectSave;
            if(gumProject == null)
                return;

            // 3. Iterate through the scene's requested screen names
            foreach(string screenName in scene.GumUIScreens)
            {
                var screenDef = gumProject.Screens.Find(s => s.Name == screenName);
                if(screenDef != null)
                {
                    var screenRuntime = screenDef.ToGraphicalUiElement();
                    screenRuntime.AddToRoot();
                }
            }
        }


    }
}
