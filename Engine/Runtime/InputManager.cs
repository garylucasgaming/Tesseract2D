
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using YamlDotNet.Core.Tokens;

namespace Engine.Core.Runtime
{
    public class InputManager
    {
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;
        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;


        // Clean, decoupled events for services to subscribe to
        public event Action<Vector2>? OnMouseLeftDown;
        public event Action<Vector2>? OnMouseLeftUp;
        public event Action<Vector2, Vector2>? OnMouseMoved; // Pass (CurrentPos, Delta)

        public event Action<Keys> OnKeyPressDown;
        public event Action<Keys> OnKeyPressUp;
        public event Action<Keys> OnKeyHeld;

        public Vector2 MousePosition => new Vector2(_currentMouseState.X, _currentMouseState.Y);
        public bool IsLeftButtonDown => _currentMouseState.LeftButton == ButtonState.Pressed;

        public void Update()
        {
            UpdateMouseState();
            UpdateKeyboardState();
        }

        public void UpdateKeyboardState()
        {
            _previousKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            // 1. Keys released this frame (Up)
            var oldPressedKeys = _previousKeyboardState.GetPressedKeys();
            foreach(var key in oldPressedKeys)
            {
                if(_currentKeyboardState.IsKeyUp(key))
                {
                    OnKeyPressUp?.Invoke(key);
                }
            }

            // 2. Keys held or freshly pressed this frame (Held / Down)
            var currentPressedKeys = _currentKeyboardState.GetPressedKeys();
            foreach(var key in currentPressedKeys)
            {
                if(_previousKeyboardState.IsKeyDown(key))
                {
                    OnKeyHeld?.Invoke(key);
                }
                else
                {
                    OnKeyPressDown?.Invoke(key);
                }
            }
        }

        public void UpdateMouseState()
        {
            _previousMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();

            Vector2 currentPos = MousePosition;
            Vector2 previousPos = new Vector2(_previousMouseState.X, _previousMouseState.Y);

            // 1. Detect Mouse Down Event (First frame pressed)
            if(_currentMouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                OnMouseLeftDown?.Invoke(currentPos);
            }

            // 2. Detect Mouse Up Event (First frame released)
            if(_currentMouseState.LeftButton == ButtonState.Released &&
                _previousMouseState.LeftButton == ButtonState.Pressed)
            {
                OnMouseLeftUp?.Invoke(currentPos);
            }

            // 3. Detect Mouse Drag/Movement
            if(currentPos != previousPos)
            {
                Vector2 delta = currentPos - previousPos;
                OnMouseMoved?.Invoke(currentPos, delta);
            }
        }
    }
}

