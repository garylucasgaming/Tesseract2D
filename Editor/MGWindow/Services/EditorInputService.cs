using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Core.Utilities;
using Engine.Editor.MGWindow.Services.Engine.Editor.MGWindow.Services;
using Engine.Editor.Utilities;
using Engine.Editor.WinFormsApp1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Engine.Editor.MGWindow.Services
{
    namespace Engine.Editor.MGWindow.Services
    {
        public enum GizmoMode
        {
            Translate, // Q
            Scale,     // W
            Size,      // E
            ColliderBox,
            ColliderCircle,
            ColliderPolygon
        }

        public enum SelectedAxis
        {
            None,
            Center,
            TranslateX,
            TranslateY,
            ScaleX,
            ScaleY,
            ScaleCorner,
            SizeWidth,
            SizeHeight,
            SizeCorner,
            // --- Collider Handles ---
            ColliderOffset,
            ColliderBoxWidth,
            ColliderBoxHeight,
            ColliderCircleRadius,
            ColliderPolyVertex
        }
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

        // --- Minimal Persistent Drag States for Colliders ---
        private Vector2 _initialColliderOffset = Vector2.Zero;
        private Vector2 _initialColliderSize = Vector2.Zero;
        private float _initialColliderRadius = 0f;
        private Vector2 _initialVertexPos = Vector2.Zero;
        private int _selectedVertexIndex = -1;

        private const float HandleLength = 50f;
        private const float ClickTolerance = 8f;

        // Current Active Gizmo Mode - defaults to Translate (Q)
        public GizmoMode CurrentMode { get; set; } = GizmoMode.Translate;
        private KeyboardState _prevKeyboardState;

        public Action OnTransformModified
        {
            get; set;
        }

        public Action OnColliderModified
        {
            get; set;
        }

        public EditorInputService(InputManager inputManager)
        {
            _inputManager = inputManager;
            _inputManager.OnMouseLeftDown += HandleMouseLeftDown;
            _inputManager.OnMouseLeftUp += HandleMouseLeftUp;
            _inputManager.OnMouseMoved += HandleMouseMoved;
        }

        private GameScene _activeScene;
        private GameObject _selectedGo;

        public void SetContext(GameScene activeScene, GameObject selectedGo)
        {
            _activeScene = activeScene;
            _selectedGo = selectedGo;
        }

        /// <summary>
        /// Call this in your MGWindowControl's Update loop to handle hotkey switches!
        /// </summary>
        public void Update()
        {
            if(_selectedGo == null)
                return;

            // 1. Check which component is actively focused in WinForms
            object selectedComponent = ComponentCardFactory.SelectedComponentInstance;

            // 2. Set the mode automatically if a collider is selected
            if(selectedComponent is BoxColliderComponent)
            {
                CurrentMode = GizmoMode.ColliderBox;
            }
            else if(selectedComponent is CircleColliderComponent)
            {
                CurrentMode = GizmoMode.ColliderCircle;
            }
            else if(selectedComponent is PolygonColliderComponent)
            {
                CurrentMode = GizmoMode.ColliderPolygon;
            }
            else if(selectedComponent is TransformComponent)
            {
                // 3. Fallback: Transform or other component is active. Listen to Q, W, E hotkeys.

             

                KeyboardState currentKeyboardState = Keyboard.GetState();
                
                // Check for key down transitions (on key press, not hold)
                if(currentKeyboardState.IsKeyDown(Keys.Q) && !_prevKeyboardState.IsKeyDown(Keys.Q) && CurrentMode != GizmoMode.Translate)
                {
                    CurrentMode = GizmoMode.Translate;
                    Log.Info("[Editor] Gizmo Mode changed to: Translate (Q)");
                }
                else if(currentKeyboardState.IsKeyDown(Keys.W) && !_prevKeyboardState.IsKeyDown(Keys.W) && CurrentMode != GizmoMode.Scale)
                {
                    CurrentMode = GizmoMode.Scale;
                    Log.Info("[Editor] Gizmo Mode changed to: Scale (W)");
                }
                else if(currentKeyboardState.IsKeyDown(Keys.E) && !_prevKeyboardState.IsKeyDown(Keys.E) && CurrentMode != GizmoMode.Size)
                {
                    CurrentMode = GizmoMode.Size;
                    Log.Info("[Editor] Gizmo Mode changed to: Size (E)");
                }

                _prevKeyboardState = currentKeyboardState;
            }
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
                    Vector2 baseCorner = transform.RenderTopLeft;


                    // --- 1. TRANSLATE MODE INTERACTIONS ---
                    if(CurrentMode == GizmoMode.Translate)
                    {
                        if(GizmoRenderer.HitTestPoint(mousePos, pivotPos + new Vector2(HandleLength, 0), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.TranslateX, mousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(mousePos, pivotPos + new Vector2(0, HandleLength), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.TranslateY, mousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(mousePos, pivotPos, 12f))
                        {
                            StartDrag(SelectedAxis.Center, mousePos, transform);
                            return;
                        }
                    }

                    // --- 2. SCALE MODE INTERACTIONS ---
                    else if(CurrentMode == GizmoMode.Scale)
                    {
                        // Hit-test diagonal uniform scale box first
                        Vector2 diagonalHandle = pivotPos + new Vector2(HandleLength * 0.7f, HandleLength * 0.7f);
                        if(GizmoRenderer.HitTestPoint(mousePos, diagonalHandle, ClickTolerance))
                        {
                            StartDrag(SelectedAxis.ScaleCorner, mousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(mousePos, pivotPos + new Vector2(HandleLength, 0), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.ScaleX, mousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(mousePos, pivotPos + new Vector2(0, HandleLength), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.ScaleY, mousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(mousePos, pivotPos, 12f))
                        {
                            StartDrag(SelectedAxis.Center, mousePos, transform);
                            return;
                        }
                    }

                    // --- 3. SIZE MODE INTERACTIONS ---
                    else if(CurrentMode == GizmoMode.Size)
                    {
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
                    }

                    // --- 4. COLLIDER BOX INTERACTIONS ---
                    else if(CurrentMode == GizmoMode.ColliderBox)
                    {
                        var box = _selectedGo.GetComponent<BoxColliderComponent>();
                        if(box != null)
                        {
                            Vector2 center = pivotPos + box.Offset;
                            Vector2 halfSize = box.Size * 0.5f;

                            if(GizmoRenderer.HitTestPoint(mousePos, center, ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderOffset, mousePos, box.Offset);
                                return;
                            }
                            if(GizmoRenderer.HitTestPoint(mousePos, center + new Vector2(halfSize.X, 0), ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderBoxWidth, mousePos, box.Offset);
                                _initialColliderSize = box.Size;
                                return;
                            }
                            if(GizmoRenderer.HitTestPoint(mousePos, center + new Vector2(0, halfSize.Y), ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderBoxHeight, mousePos, box.Offset);
                                _initialColliderSize = box.Size;
                                return;
                            }
                        }
                    }

                    // --- 5. COLLIDER CIRCLE INTERACTIONS ---
                    else if(CurrentMode == GizmoMode.ColliderCircle)
                    {
                        var circle =  _selectedGo.GetComponent<CircleColliderComponent>();
                        if(circle != null)
                        {
                            Vector2 center = pivotPos + circle.Offset;

                            if(GizmoRenderer.HitTestPoint(mousePos, center, ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderOffset, mousePos, circle.Offset);
                                return;
                            }
                            if(GizmoRenderer.HitTestPoint(mousePos, center + new Vector2(circle.Radius, 0), ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderCircleRadius, mousePos, circle.Offset);
                                _initialColliderRadius = circle.Radius;
                                return;
                            }
                        }
                    }

                    // --- 6. COLLIDER POLYGON INTERACTIONS ---
                    else if(CurrentMode == GizmoMode.ColliderPolygon)
                    {
                        var poly = _selectedGo.GetComponent<PolygonColliderComponent>();
                        if(poly != null)
                        {
                            Vector2 center = pivotPos + poly.Offset;

                            if(GizmoRenderer.HitTestPoint(mousePos, center, ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderOffset, mousePos, poly.Offset);
                                return;
                            }

                            for(int i = 0; i < poly.Vertices.Count; i++)
                            {
                                Vector2 worldVertex = center + poly.Vertices[i];
                                if(GizmoRenderer.HitTestPoint(mousePos, worldVertex, ClickTolerance))
                                {
                                    StartColliderDrag(SelectedAxis.ColliderPolyVertex, mousePos, poly.Offset);
                                    _selectedVertexIndex = i;
                                    _initialVertexPos = poly.Vertices[i];
                                    return;
                                }
                            }
                        }
                    }
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

            Vector2 totalMouseDelta = currentMousePos - _dragStartMousePos;

            switch(_activeAxis)
            {
                // Move/Translate operations
                case SelectedAxis.Center:
                    transform.X = _initialEntityPos.X + totalMouseDelta.X;
                    transform.Y = _initialEntityPos.Y + totalMouseDelta.Y;
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.TranslateX:
                    transform.X = _initialEntityPos.X + totalMouseDelta.X;
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.TranslateY:
                    transform.Y = _initialEntityPos.Y + totalMouseDelta.Y;
                    OnTransformModified?.Invoke();
                    break;

                // Scale operations
                case SelectedAxis.ScaleX:
                    transform.ScaleX = (float) Math.Round(_initialEntityScale.X + (totalMouseDelta.X / 10f));
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.ScaleY:
                    transform.ScaleY = (float) Math.Round(_initialEntityScale.Y + (totalMouseDelta.Y / 10f));
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.ScaleCorner:
                    float scaleDelta = (totalMouseDelta.X + totalMouseDelta.Y) / 20f;
                    transform.ScaleX = Math.Max(1f, (float) Math.Round(_initialEntityScale.X + scaleDelta));
                    transform.ScaleY = Math.Max(1f, (float) Math.Round(_initialEntityScale.Y + scaleDelta));
                    OnTransformModified?.Invoke();
                    break;

                // Dimension Size operations
                case SelectedAxis.SizeWidth:
                    transform.SizeX = (int) Math.Round(_initialEntitySize.X + ((totalMouseDelta.X) / transform.ScaleX));
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.SizeHeight:
                    transform.SizeY = (int) Math.Round(_initialEntitySize.Y + ((totalMouseDelta.Y) / transform.ScaleY));
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.SizeCorner:
                    transform.SizeX = (int) Math.Round(_initialEntitySize.X + ((totalMouseDelta.X) / transform.ScaleX));
                    transform.SizeY = (int) Math.Round(_initialEntitySize.Y + ((totalMouseDelta.Y) / transform.ScaleY));
                    OnTransformModified?.Invoke();
                    break;

                // --- Safe Collider Drag Operations (Null checked inside!) ---
                case SelectedAxis.ColliderOffset:
                {
                    var collider = _selectedGo.GetComponent<ColliderComponent>();
                    if(collider != null)
                    {
                        collider.Offset = _initialColliderOffset + totalMouseDelta;
                        OnColliderModified?.Invoke();
                    }
                }
                break;

                case SelectedAxis.ColliderBoxWidth:
                {
                    var collider = _selectedGo.GetComponent<ColliderComponent>();
                    if(collider is BoxColliderComponent boxW)
                    {
                        float newWidth = Math.Max(4f, _initialColliderSize.X + (totalMouseDelta.X * 2f));
                        boxW.Size = new Vector2((int) Math.Round(newWidth), boxW.Size.Y);
                        OnColliderModified?.Invoke();
                    }
                }
                break;

                case SelectedAxis.ColliderBoxHeight:
                {
                    var collider = _selectedGo.GetComponent<ColliderComponent>();
                    if(collider is BoxColliderComponent boxH)
                    {
                        float newHeight = Math.Max(4f, _initialColliderSize.Y + (totalMouseDelta.Y * 2f));
                        boxH.Size = new Vector2(boxH.Size.X, (int) Math.Round(newHeight));
                        OnColliderModified?.Invoke();
                    }
                }
                break;

                case SelectedAxis.ColliderCircleRadius:
                {
                    var collider = _selectedGo.GetComponent<ColliderComponent>();
                    if(collider is CircleColliderComponent circleR)
                    {
                        float newRadius = Math.Max(2f, _initialColliderRadius + totalMouseDelta.X);
                        circleR.Radius = (float) Math.Round(newRadius);
                        OnColliderModified?.Invoke();
                    }
                }
                break;

                case SelectedAxis.ColliderPolyVertex:
                {
                    var collider = _selectedGo.GetComponent<ColliderComponent>();
                    if(collider is PolygonColliderComponent polyV && _selectedVertexIndex >= 0)
                    {
                        polyV.Vertices[_selectedVertexIndex] = _initialVertexPos + totalMouseDelta;
                        OnColliderModified?.Invoke();
                    }
                }
                break;

                default:
                    Log.Warning("no axis selected");
                    break;
            }
        }
        private void HandleMouseLeftUp(Vector2 mousePos)
        {
            if(_isDragging)
            {
                _isDragging = false;
                _activeAxis = SelectedAxis.None;
                _selectedVertexIndex = -1;
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

        private void StartColliderDrag(SelectedAxis axis, Vector2 mousePos, Vector2 initialOffset)
        {
            _isDragging = true;
            _activeAxis = axis;
            _dragStartMousePos = mousePos;
            _initialColliderOffset = initialOffset;
        }
    }
}