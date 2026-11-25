using Engine.Core.Utilities;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// Compatibility shim over <see cref="SessionGlobals.Current"/>. New hosts
    /// should hold and inject their own <see cref="SessionGlobals"/> instance.
    /// </summary>
    public static class Globals
    {
        public static SessionGlobals Current
        {
            get => SessionGlobals.Current;
            set => SessionGlobals.Current = value;
        }

        public static InputManager? InputManager => Current.InputManager;

        public static Camera2D? EditorCamera => Current.EditorCamera;

        public static Viewport Viewport => Current.Viewport;

        public static void Initialize()
        {
            Current = new SessionGlobals();
        }

        public static void SetInputManager(InputManager im)
        {
            Current.InputManager = im;
        }

        public static void SetEditorCamera(Camera2D cam)
        {
            Current.EditorCamera = cam;
        }

        public static void SetViewport(Viewport view)
        {
            Current.SetViewport(view);
        }
    }
}
