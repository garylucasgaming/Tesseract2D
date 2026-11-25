using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// Deterministic input source for headless hosts and tests.
    /// </summary>
    public sealed class NullInputSource : IInputSource
    {
        public Vector2 MousePosition { get; set; }
        public bool IsLeftButtonDown { get; set; }
        public KeyboardState KeyboardState { get; set; } = new KeyboardState();

        public InputSnapshot Capture()
        {
            return new InputSnapshot(MousePosition, IsLeftButtonDown, KeyboardState);
        }
    }
}
