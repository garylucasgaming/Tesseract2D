using Engine.Core.ECS;
using Engine.Core.ECS.Components; // Brought in your proper component namespace
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
        None, Center, X, Y, SizeWidth, SizeHeight, SizeCorner
    }

    public class EditorInputService
    {
        private bool _isDragging = false;
        private SelectedAxis _activeAxis = SelectedAxis.None;
        private Vector2 _initialMousePos = Vector2.Zero;
        private Vector2 _initialEntityPos = Vector2.Zero;
        private Vector2 _initialEntityScale = Vector2.Zero;
        private Vector2 _initialEntitySize = Vector2.Zero; // Added to track raw base size bounds

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
                    var transform = selectedGo.GetComponent<TransformComponent>();
                    if(transform != null)
                    {
                        Vector2 entityPos = transform.WorldPosition;
                        Vector2 scale = transform.Scale;
                        Vector2 size = transform.Size;

                        // Calculate visual box limits (matches Render pass exactly)
                        float currentWidth = size.X * scale.X;
                        float currentHeight = size.Y * scale.Y;

                        // Hit-test original axis scaling lines first
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos + new Vector2(HandleLength, 0), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.X, mousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos + new Vector2(0, HandleLength), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.Y, mousePos, transform);
                            return;
                        }

                        // --- Hit-Test: Bounding Size Handles ---
                        // Right Side Handle (Width scaling)
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos + new Vector2(currentWidth, currentHeight * 0.5f), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.SizeWidth, mousePos, transform);
                            return;
                        }
                        // Bottom Side Handle (Height scaling)
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos + new Vector2(currentWidth * 0.5f, currentHeight), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.SizeHeight, mousePos, transform);
                            return;
                        }
                        // Bottom-Right Corner Handle (Dual sizing)
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos + new Vector2(currentWidth, currentHeight), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.SizeCorner, mousePos, transform);
                            return;
                        }

                        // Fallback translation click center anchor
                        if(GizmoRenderer.HitTestPoint(mousePos, entityPos, 12f))
                        {
                            StartDrag(SelectedAxis.Center, mousePos, transform);
                            return;
                        }
                    }
                }

                // Viewport selection fallback picking pass
                foreach(var entity in activeScene.Entities.GetSerializableEntities())
                {
                    var transform = entity.GetComponent<TransformComponent>();
                    if(transform == null)
                        continue;

                    if(GizmoRenderer.HitTestPoint(mousePos, transform.WorldPosition, 10f))
                    {
                        StartDrag(SelectedAxis.Center, mousePos, transform);
                        break;
                    }
                }
            }

            // 2. Dragging Logic Pass (Runs continuously every frame while moving the mouse)
            if(mouseState.LeftButton == ButtonState.Pressed && _isDragging && selectedGo != null)
            {
                var transform = selectedGo.GetComponent<TransformComponent>();
                if(transform != null)
                {
                    Vector2 mouseDelta = mousePos - _initialMousePos;

                    if(_activeAxis == SelectedAxis.Center)
                    {
                        // Move the gameobject
                        transform.X = _initialEntityPos.X + mouseDelta.X;
                        transform.Y = _initialEntityPos.Y + mouseDelta.Y;
                    }
                    else if(_activeAxis == SelectedAxis.X)
                    {
                        // Scale gameobject on X
                        transform.ScaleX = (float) Math.Round(_initialEntityScale.X + (mouseDelta.X / 10));
                    }
                    else if(_activeAxis == SelectedAxis.Y)
                    {
                        // Scale gameobject on Y
                        transform.ScaleY = (float)Math.Round(_initialEntityScale.Y + (mouseDelta.Y / 10));
                    }
                    // --- Bounding Box Resizing Modes ---
                    else if(_activeAxis == SelectedAxis.SizeWidth)
                    {
                        // Divide mouse movement by scale so dragging remains unified regardless of object scaling
                        transform.SizeX = Math.Max(1f, _initialEntitySize.X + (mouseDelta.X / transform.ScaleX));
                    }
                    else if(_activeAxis == SelectedAxis.SizeHeight)
                    {
                        transform.SizeY = Math.Max(1f, _initialEntitySize.Y + (mouseDelta.Y / transform.ScaleY));
                    }
                    else if(_activeAxis == SelectedAxis.SizeCorner)
                    {
                        transform.SizeX = Math.Max(1f, _initialEntitySize.X + (mouseDelta.X / transform.ScaleX));
                        transform.SizeY = Math.Max(1f, _initialEntitySize.Y + (mouseDelta.Y / transform.ScaleY));
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

        private void StartDrag(SelectedAxis axis, Vector2 mousePos, TransformComponent transform)
        {
            _isDragging = true;
            _activeAxis = axis;
            _initialMousePos = mousePos;
            _initialEntityPos = transform.WorldPosition;
            _initialEntityScale = transform.Scale;
            _initialEntitySize = transform.Size; // Safely stores current size state on click down
        }
    }
}