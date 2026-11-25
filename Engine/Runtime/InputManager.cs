using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Core.Runtime
{
    public class InputManager
    {
        private readonly IInputSource _source;
        private InputSnapshot _current;
        private InputSnapshot _previous;

        // Clean, decoupled events for services to subscribe to
        public event Action<Vector2>? OnMouseLeftDown;
        public event Action<Vector2>? OnMouseLeftUp;
        public event Action<Vector2, Vector2>? OnMouseMoved; // Pass (CurrentPos, Delta)

        public event Action<Keys> OnKeyPressDown;
        public event Action<Keys> OnKeyPressUp;
        public event Action<Keys> OnKeyHeld;

        public InputManager(IInputSource? source = null)
        {
            _source = source ?? new MonoGameInputSource();
        }

        public Vector2 MousePosition => _current.MousePosition;
        public bool IsLeftButtonDown => _current.LeftButtonDown;

        public void Update()
        {
            _previous = _current;
            _current = _source.Capture();
            RaiseMouseEvents();
            RaiseKeyboardEvents();
        }

        // Preserved for callers that update one device family directly.
        public void UpdateKeyboardState() => Update();

        public void UpdateMouseState() => Update();

        private void RaiseMouseEvents()
        {
            Vector2 currentPos = _current.MousePosition;
            Vector2 previousPos = _previous.MousePosition;

            if(_current.LeftButtonDown && !_previous.LeftButtonDown)
            {
                OnMouseLeftDown?.Invoke(currentPos);
            }

            if(!_current.LeftButtonDown && _previous.LeftButtonDown)
            {
                OnMouseLeftUp?.Invoke(currentPos);
            }

            if(currentPos != previousPos)
            {
                Vector2 delta = currentPos - previousPos;
                OnMouseMoved?.Invoke(currentPos, delta);
            }
        }

        private void RaiseKeyboardEvents()
        {
            KeyboardState current = _current.KeyboardState;
            KeyboardState previous = _previous.KeyboardState;

            foreach(var key in previous.GetPressedKeys())
            {
                if(current.IsKeyUp(key))
                {
                    OnKeyPressUp?.Invoke(key);
                }
            }

            foreach(var key in current.GetPressedKeys())
            {
                if(previous.IsKeyDown(key))
                {
                    OnKeyHeld?.Invoke(key);
                }
                else
                {
                    OnKeyPressDown?.Invoke(key);
                }
            }
        }
    }
}
