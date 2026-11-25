using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// Reads the current MonoGame keyboard and mouse state.
    /// </summary>
    public sealed class MonoGameInputSource : IInputSource
    {
        public InputSnapshot Capture()
        {
            var mouse = Mouse.GetState();
            return new InputSnapshot(
                new Vector2(mouse.X, mouse.Y),
                mouse.LeftButton == ButtonState.Pressed,
                Keyboard.GetState());
        }
    }
}
