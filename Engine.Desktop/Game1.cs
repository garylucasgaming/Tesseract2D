using Engine.Core.ECS;
using Engine.Core.ECS.Components.UI;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace Engine.Desktop
{
    /// <summary>
    /// Standalone DesktopGL front end. The MonoGame window owns the platform
    /// lifecycle; the shared <see cref="GameHost"/> owns the engine frame loop.
    /// </summary>
    public class Game1 : Game
    {
        private const float TargetTickTime = 1f / 60f;

        private readonly GraphicsDeviceManager _graphics;
        private EngineContext _context = null!;
        private SessionGlobals _session = null!;
        private GameHost _host = null!;
        private float _physicsAccumulator;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            var projectRoot = Environment.GetEnvironmentVariable("TESSERACT2D_PROJECT_ROOT");
            _context = new EngineContext
            {
                CurrentProjectRoot = string.IsNullOrWhiteSpace(projectRoot) ? null : projectRoot
            };

            _session = new SessionGlobals(_context, new MonoGameInputSource());
            Globals.Current = _session;

            var renderer = new MonoGameFrameRenderer(GraphicsDevice, Content);
            _host = new GameHost(_context, _session, renderer);
            _host.Initialize();

            var scenePath = Environment.GetEnvironmentVariable("TESSERACT2D_SCENE");
            if(!string.IsNullOrWhiteSpace(scenePath) && File.Exists(scenePath))
            {
                _host.LoadScene(scenePath);
            }
            else
            {
                _host.AttachScene(new GameScene(_context, session: _session));
            }

            _context.PlayState = true;

            Exiting += (_, _) => _host.Shutdown();
        }

        protected override void Update(GameTime gameTime)
        {
            if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
               Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _host.Update(deltaTime, () =>
            {
                _physicsAccumulator += deltaTime;
                while(_physicsAccumulator >= TargetTickTime)
                {
                    _host.TickUpdate(TargetTickTime);
                    _physicsAccumulator -= TargetTickTime;
                }
            });

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _host.Render();
            base.Draw(gameTime);
        }
    }
}
