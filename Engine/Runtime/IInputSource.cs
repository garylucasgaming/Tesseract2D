using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// A single frame of input captured by a session's input source.
    /// </summary>
    public readonly struct InputSnapshot
    {
        public InputSnapshot(Vector2 mousePosition, bool leftButtonDown, KeyboardState keyboardState)
        {
            MousePosition = mousePosition;
            LeftButtonDown = leftButtonDown;
            KeyboardState = keyboardState;
        }

        public Vector2 MousePosition { get; }
        public bool LeftButtonDown { get; }
        public KeyboardState KeyboardState { get; }
    }

    /// <summary>
    /// Supplies input to an <see cref="InputManager"/>. Hosts choose the source so
    /// headless sessions and the MonoGame window can run the same engine code.
    /// </summary>
    public interface IInputSource
    {
        InputSnapshot Capture();
    }
}
