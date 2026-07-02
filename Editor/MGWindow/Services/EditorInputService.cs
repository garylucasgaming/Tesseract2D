
using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Editor.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.ComponentModel;

namespace Engine.Editor.MGWindow.Services
{
    public enum SelectedAxis
    {
        None, Center, X, Y, SizeWidth, SizeHeight, SizeCorner
    }

    public class EditorInputService
    {
        private readonly InputManager _inputManager;

        private bool _isDragging = false;
        private SelectedAxis _activeAxis = SelectedAxis.None;

        private Vector2 _initialEntityPos = Vector2.Zero;
        private Vector2 _initialEntityScale = Vector2.Zero;
        private Vector2 _initialEntitySize = Vector2.Zero;
        private Vector2 _dragStartMousePos = Vector2.Zero;

        private const float HandleLength = 50f;
        private const float ClickTolerance = 8f;

        public Action OnTransformModified
        {
            get; set;
        }

        // Inject the centralized InputManager via Constructor
        public EditorInputService(InputManager inputManager)
        {
            _inputManager = inputManager;

            // Wire up the clean event hooks!
            _inputManager.OnMouseLeftDown += HandleMouseLeftDown;
            _inputManager.OnMouseLeftUp += HandleMouseLeftUp;
            _inputManager.OnMouseMoved += HandleMouseMoved;
        }

        // Cache your active objects during execution ticks
        private GameScene _activeScene;
        private GameObject _selectedGo;

        public void SetContext(GameScene activeScene, GameObject selectedGo)
        {
            _activeScene = activeScene;
            _selectedGo = selectedGo;
        }

        private void HandleMouseLeftDown(Vector2 mousePos)
        {
            if(_activeScene == null)
                return;

            if(_selectedGo != null)
            {
                var transform = _selectedGo.GetComponent<TransformComponent>();
                if(transform != null)
                {
                    Vector2 pivotPos = transform.WorldPosition;
                    Vector2 scale = transform.Scale;
                    Vector2 size = transform.Size;

                    float currentWidth = size.X * scale.X;
                    float currentHeight = size.Y * scale.Y;

                    // NEW: Origin-neutral base corner anchor calculation for sizing boundaries
                    Vector2 baseCorner = transform.RenderTopLeft;

                    // Hit-test original translation handles from the stable Pivot Point
                    if(GizmoRenderer.HitTestPoint(mousePos, pivotPos + new Vector2(HandleLength, 0), ClickTolerance))
                    {
                        StartDrag(SelectedAxis.X, mousePos, transform);
                        return;
                    }
                    if(GizmoRenderer.HitTestPoint(mousePos, pivotPos + new Vector2(0, HandleLength), ClickTolerance))
                    {
                        StartDrag(SelectedAxis.Y, mousePos, transform);
                        return;
                    }

                    // Hit-test bounding resizing squares mapped to the clean baseCorner layout
                    if(GizmoRenderer.HitTestPoint(mousePos, baseCorner + new Vector2(currentWidth, currentHeight * 0.5f), ClickTolerance))
                    {
                        StartDrag(SelectedAxis.SizeWidth, mousePos, transform);
                        return;
                    }
                    if(GizmoRenderer.HitTestPoint(mousePos, baseCorner + new Vector2(currentWidth * 0.5f, currentHeight), ClickTolerance))
                    {
                        StartDrag(SelectedAxis.SizeHeight, mousePos, transform);
                        return;
                    }
                    if(GizmoRenderer.HitTestPoint(mousePos, baseCorner + new Vector2(currentWidth, currentHeight), ClickTolerance))
                    {
                        StartDrag(SelectedAxis.SizeCorner, mousePos, transform);
                        return;
                    }

                    // Translation central click selection fallback
                    if(GizmoRenderer.HitTestPoint(mousePos, pivotPos, 12f))
                    {
                        StartDrag(SelectedAxis.Center, mousePos, transform);
                        return;
                    }
                }
            }

            // Picking selection engine fallback pass across screen items
            foreach(var entity in _activeScene.Entities.GetSerializableEntities())
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

        private void HandleMouseMoved(Vector2 currentMousePos, Vector2 mouseDelta)
        {
            if(!_isDragging || _selectedGo == null)
                return;

            var transform = _selectedGo.GetComponent<TransformComponent>();
            if(transform == null)
                return;

            // Total accumulated displacement delta relative to where our drag began
            Vector2 totalMouseDelta = currentMousePos - _dragStartMousePos;

            switch(_activeAxis)
            {
                case SelectedAxis.Center:
                    transform.X = _initialEntityPos.X + totalMouseDelta.X;
                    transform.Y = _initialEntityPos.Y + totalMouseDelta.Y;
                    break;

                case SelectedAxis.X:
                    transform.ScaleX = (float) Math.Round(_initialEntityScale.X + (totalMouseDelta.X / 10f));
                    break;

                case SelectedAxis.Y:
                    transform.ScaleY = (float) Math.Round(_initialEntityScale.Y + (totalMouseDelta.Y / 10f));
                    break;

                case SelectedAxis.SizeWidth:
                    transform.SizeX = Math.Max(1f, _initialEntitySize.X + (totalMouseDelta.X / transform.ScaleX));
                    break;

                case SelectedAxis.SizeHeight:
                    transform.SizeY = Math.Max(1f, _initialEntitySize.Y + (totalMouseDelta.Y / transform.ScaleY));
                    break;

                case SelectedAxis.SizeCorner:
                    transform.SizeX = Math.Max(1f, _initialEntitySize.X + (totalMouseDelta.X / transform.ScaleX));
                    transform.SizeY = Math.Max(1f, _initialEntitySize.Y + (totalMouseDelta.Y / transform.ScaleY));
                    break;
            }

            TypeDescriptor.Refresh(transform);
            OnTransformModified?.Invoke();
        }

        private void HandleMouseLeftUp(Vector2 mousePos)
        {
            if(_isDragging)
            {
                _isDragging = false;
                _activeAxis = SelectedAxis.None;
            }
        }

        private void StartDrag(SelectedAxis axis, Vector2 mousePos, TransformComponent transform)
        {
            _isDragging = true;
            _activeAxis = axis;
            _dragStartMousePos = mousePos;
            _initialEntityPos = transform.WorldPosition;
            _initialEntityScale = transform.Scale;
            _initialEntitySize = transform.Size;
        }
    }
}


