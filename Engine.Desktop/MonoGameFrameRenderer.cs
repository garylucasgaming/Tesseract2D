using Engine.Core.ECS;
using Engine.Core.ECS.Components.UI;
using Engine.Core.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Engine.Desktop
{
    /// <summary>
    /// Presents a host scene through the real MonoGame DesktopGL window.
    /// </summary>
    public sealed class MonoGameFrameRenderer : IFrameRenderer
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly ContentManager _content;
        private SpriteBatch? _spriteBatch;

        public MonoGameFrameRenderer(GraphicsDevice graphicsDevice, ContentManager content)
        {
            _graphicsDevice = graphicsDevice;
            _content = content;
        }

        public void Initialize(SessionGlobals session)
        {
            _spriteBatch ??= new SpriteBatch(_graphicsDevice);
            session.SetViewport(_graphicsDevice.Viewport);
        }

        public void Render(GameScene scene, SessionGlobals session)
        {
            var spriteBatch = _spriteBatch ?? throw new InvalidOperationException(
                "[MonoGameFrameRenderer] Initialize must be called before Render.");

            _graphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            scene.Render(spriteBatch, _content);
            scene.Systems.Render(spriteBatch, _content);
            scene.Systems.RenderUI(spriteBatch, _content, UISpace.World);
            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            scene.Systems.RenderUI(spriteBatch, _content, UISpace.Screen);
            spriteBatch.End();
        }

        public void Shutdown()
        {
            _spriteBatch?.Dispose();
            _spriteBatch = null;
        }
    }
}
