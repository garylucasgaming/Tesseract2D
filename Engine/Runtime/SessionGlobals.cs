using Engine.Core.Utilities;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// Per-session input and view state. Hosts create one of these for a window
    /// or a headless test; scenes and systems read the instance they were given
    /// instead of a process-wide singleton.
    /// </summary>
    public sealed class SessionGlobals
    {
        /// <summary>
        /// Compatibility session for callers that have not been given one yet.
        /// New hosts should construct and inject their own instance.
        /// </summary>
        public static SessionGlobals Current { get; set; } = new();

        public SessionGlobals(IEngineContext? context = null, IInputSource? inputSource = null)
        {
            Context = context ?? EngineContext.Current;
            InputSource = inputSource ?? new MonoGameInputSource();
            InputManager = new InputManager(InputSource);
            EditorCamera = new Camera2D();
            Viewport = new Viewport(0, 0, 1, 1);
        }

        public IEngineContext Context { get; }
        public IInputSource InputSource { get; }
        public InputManager InputManager { get; set; }
        public Camera2D EditorCamera { get; set; }
        public Viewport Viewport { get; set; }

        public void SetViewport(Viewport viewport)
        {
            Viewport = viewport;
        }

        public void UpdateInput()
        {
            InputManager.Update();
        }
    }
}
