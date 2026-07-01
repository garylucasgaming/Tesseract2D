using Engine.Core.ECS;
using Engine.Editor.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;

namespace Engine.Editor.MGWindow.Services
{
    public enum SelectedAxis
    {
        None, Center, X, Y
    }

    public class EditorInputService
    {
        private bool _isDragging = false;
        private SelectedAxis _activeAxis = SelectedAxis.None;
        private Vector2 _initialMousePos = Vector2.Zero;
        private Vector2 _initialEntityPos = Vector2.Zero;
        private const float HandleLength = 50f;
        private const float ClickTolerance = 8f;

        public System.Action OnTransformModified
        {
            get; set;
        }

        public void ProcessInputs(GameScene activeScene, GameObject selectedGo)
        {
            var mouseState = Mouse.GetState();
            Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

            // 1. Mouse Button Pressed (Click Down - First Frame Selection/Initialization)
            if(mouseState.LeftButton == ButtonState.Pressed && !_isDragging)
            {
                if(selectedGo != null)
                {
                    dynamic transform = GetTransform(selectedGo);
                    if(transform != null)
                    {
                        Vector2 entityPos = new Vector2((float) transform.X, (float) transform.Y);

                        // Hit-test axis handles first
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos + new Vector2(HandleLength, 0), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.X, mousePos, entityPos);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos + new Vector2(0, HandleLength), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.Y, mousePos, entityPos);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos, 12f))
                        {
                            StartDrag(SelectedAxis.Center, mousePos, entityPos);
                            return;
                        }
                    }
                }

                // Viewport selection fallback picking pass
                foreach(var entity in activeScene.Entities.GetSerializableEntities())
                {
                    dynamic transform = GetTransform(entity);
                    if(transform == null)
                        continue;

                    Vector2 pos = new Vector2((float) transform.X, (float) transform.Y);
                    if(GizmoRenderer.HitTestPoint(mousePos, pos, 10f))
                    {
                        StartDrag(SelectedAxis.Center, mousePos, pos);
                        break;
                    }
                }
            } // 👈 Bracket cleanly closes the Click-Down block!

            // 2. Dragging Logic Pass (Runs continuously every frame while moving the mouse)
            if(mouseState.LeftButton == ButtonState.Pressed && _isDragging && selectedGo != null)
            {
                dynamic transform = GetTransform(selectedGo);
                if(transform != null)
                {
                    Vector2 mouseDelta = mousePos - _initialMousePos;

                    if(_activeAxis == SelectedAxis.Center)
                    {
                        transform.X = _initialEntityPos.X + mouseDelta.X;
                        transform.Y = _initialEntityPos.Y + mouseDelta.Y;
                    }
                    else if(_activeAxis == SelectedAxis.X)
                    {
                        transform.X = _initialEntityPos.X + mouseDelta.X;
                    }
                    else if(_activeAxis == SelectedAxis.Y)
                    {
                        transform.Y = _initialEntityPos.Y + mouseDelta.Y;
                    }

                    TypeDescriptor.Refresh(transform);

                    // Trigger our dynamic sidebar update hook layout
                    OnTransformModified?.Invoke();
                }
            }

            // 3. Mouse Released (Drop)
            if(mouseState.LeftButton == ButtonState.Released && _isDragging)
            {
                _isDragging = false;
                _activeAxis = SelectedAxis.None;
            }
        }

        private void StartDrag(SelectedAxis axis, Vector2 mousePos, Vector2 entityPos)
        {
            _isDragging = true;
            _activeAxis = axis;
            _initialMousePos = mousePos;
            _initialEntityPos = entityPos;
        }

        private dynamic GetTransform(GameObject entity)
        {
            foreach(var kvp in entity.Components)
            {
                if(kvp.Key.Name == "TransformComponent")
                    return kvp.Value;
            }
            return null;
        }
    }
}