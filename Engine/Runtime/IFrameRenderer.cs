using Engine.Core.ECS;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// Host-owned presentation boundary. The engine loop calls this interface;
    /// the MonoGame window and headless tests provide different implementations.
    /// </summary>
    public interface IFrameRenderer
    {
        void Initialize(SessionGlobals session);

        void Render(GameScene scene, SessionGlobals session);

        void Shutdown();
    }
}
