using Engine.Core.ECS;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// Renderer for headless tests and non-visual hosts.
    /// </summary>
    public sealed class NullFrameRenderer : IFrameRenderer
    {
        public void Initialize(SessionGlobals session)
        {
        }

        public void Render(GameScene scene, SessionGlobals session)
        {
        }

        public void Shutdown()
        {
        }
    }
}
