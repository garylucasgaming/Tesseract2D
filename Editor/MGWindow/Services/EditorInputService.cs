using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Engine.Editor.MGWindow.Services.Engine.Editor.MGWindow.Services;
using Engine.Editor.Utilities;
using Engine.Editor.WinFormsApp1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel;
using WinFormsApp1;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Engine.Editor.MGWindow.Services
{
    namespace Engine.Editor.MGWindow.Services
    {
        public enum GizmoMode
        {
            Translate, // 1
            Scale,     // 2
            Size,      // 3
            TilePaint, //4 tile painting tool
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
        public Camera2D Camera { get; private set; }
        public Viewport CurrentViewport { get; private set; }

        private int _previousScrollWheelValue = 0;

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
        private GameScene _activeScene;
        private GameObject _selectedGo;

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

        public EditorInputService(InputManager inputManager, Camera2D camera)
        {
            _inputManager = inputManager;
            Camera = camera;
            _inputManager.OnMouseLeftDown += HandleMouseLeftDown;
            _inputManager.OnMouseLeftUp += HandleMouseLeftUp;
            _inputManager.OnMouseMoved += HandleMouseMoved;
            _inputManager.OnKeyPressUp += HandleKeyReleased;
            _inputManager.OnKeyPressDown += HandleKeyPressed;
            _inputManager.OnKeyHeld += HandleKeyHeld;
        }

        public void SetContext(GameScene activeScene, GameObject selectedGo, Viewport viewport)
        {
            _activeScene = activeScene;
            _selectedGo = selectedGo;
            CurrentViewport = viewport;
        }

        private void HandleKeyHeld(Keys keys)
        {
            
        }

        private void HandleKeyPressed(Keys key)
        {



            switch(key)
            {
                case Keys.A:
                   
                    break;
                case Keys.S:
                   
                    break;
                case Keys.D:
                    
                    break;
                case Keys.W:
                   
                    break;
                        
            }
        }

        private void HandleKeyReleased(Keys keys)
        {
            
        }

        /// <summary>
        /// Call this in your MGWindowControl's Update loop to handle hotkey switches!
        /// </summary>
        public void Update(float deltaTime)
        {
            // 1. Camera controls should always be active
            HandleCameraControls(deltaTime);

            KeyboardState currentKeyboardState = Keyboard.GetState();

            // 2. Allow mode hotkeys (1, 2, 3, 4) to be pressed at any time, 
            // regardless of whether a GameObject is selected.
            if(currentKeyboardState.IsKeyDown(Keys.D1) && !_prevKeyboardState.IsKeyDown(Keys.D1) && CurrentMode != GizmoMode.Translate)
            {
                CurrentMode = GizmoMode.Translate;
                Log.Info("[Editor] Gizmo Mode changed to: Translate (1)");
            }
            else if(currentKeyboardState.IsKeyDown(Keys.D2) && !_prevKeyboardState.IsKeyDown(Keys.D2) && CurrentMode != GizmoMode.Scale)
            {
                CurrentMode = GizmoMode.Scale;
                Log.Info("[Editor] Gizmo Mode changed to: Scale (2)");
            }
            else if(currentKeyboardState.IsKeyDown(Keys.D3) && !_prevKeyboardState.IsKeyDown(Keys.D3) && CurrentMode != GizmoMode.Size)
            {
                CurrentMode = GizmoMode.Size;
                Log.Info("[Editor] Gizmo Mode changed to: Size (3)");
            }
            else if(currentKeyboardState.IsKeyDown(Keys.D4) && !_prevKeyboardState.IsKeyDown(Keys.D4) && CurrentMode != GizmoMode.TilePaint)
            {
                CurrentMode = GizmoMode.TilePaint;
                Log.Info("[Editor] Gizmo Mode changed to: TilePaint (4)");
            }

            _prevKeyboardState = currentKeyboardState;

            // 3. If we are currently in TilePaint mode, we don't need a selected GameObject or component checks.
            if(CurrentMode == GizmoMode.TilePaint)
            {
                return;
            }

            // 4. Transform and Gizmo modes require a selected GameObject
            if(_selectedGo == null)
                return;

            // 5. Check which component is actively focused in WinForms for colliders
            object selectedComponent = ComponentCardFactory.SelectedComponentInstance;

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
        }
        private void HandleCameraControls(float deltaTime)
        {
            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // --- 1. CAMERA PANNING (Hold Right-Click + WASD or Arrow Keys) ---
            bool isRightMouseHeld = mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            Vector2 panInput = Vector2.Zero;

            // --- FOCUS HOTKEY ('F' Key) ---
            if(keyState.IsKeyDown(Keys.F) && !_prevKeyboardState.IsKeyDown(Keys.F))
            {
                FocusOnSelected();
            }

            if(isRightMouseHeld)
            {
                if(keyState.IsKeyDown(Keys.W))
                    panInput.Y -= 1;
                if(keyState.IsKeyDown(Keys.S))
                    panInput.Y += 1;
                if(keyState.IsKeyDown(Keys.A))
                    panInput.X -= 1;
                if(keyState.IsKeyDown(Keys.D))
                    panInput.X += 1;
            }

            // Fallback Arrow Keys panning without right click
            if(keyState.IsKeyDown(Keys.Up))
                panInput.Y -= 1;
            if(keyState.IsKeyDown(Keys.Down))
                panInput.Y += 1;
            if(keyState.IsKeyDown(Keys.Left))
                panInput.X -= 1;
            if(keyState.IsKeyDown(Keys.Right))
                panInput.X += 1;

            if(panInput != Vector2.Zero)
            {
                panInput.Normalize();
                float basePanSpeed = 600f;
                // Adjust speed inversely with zoom so zooming out doesn't feel sluggish
                Camera.Position += panInput * (basePanSpeed / Camera.Zoom) * deltaTime;
            }

            // --- 2. CAMERA ZOOMING (+ / - Keys) ---
            if(keyState.IsKeyDown(Keys.OemPlus) || keyState.IsKeyDown(Keys.Add))
            {
                Camera.Zoom = MathHelper.Clamp(Camera.Zoom + (2f * deltaTime), Camera.MinZoom, Camera.MaxZoom);
            }
            if(keyState.IsKeyDown(Keys.OemMinus) || keyState.IsKeyDown(Keys.Subtract))
            {
                Camera.Zoom = MathHelper.Clamp(Camera.Zoom - (2f * deltaTime), Camera.MinZoom, Camera.MaxZoom);
            }

            // --- 3. CAMERA ZOOMING (Mouse Wheel) ---
            int scrollDelta = mouseState.ScrollWheelValue - _previousScrollWheelValue;
            if(scrollDelta != 0)
            {
                float zoomStep = scrollDelta > 0 ? 0.15f : -0.15f;
                Camera.Zoom = MathHelper.Clamp(Camera.Zoom + zoomStep, Camera.MinZoom, Camera.MaxZoom);
            }
            _previousScrollWheelValue = mouseState.ScrollWheelValue;
        }

        private void HandleMouseLeftDown(Vector2 screenMousePos)
        {
            if(_activeScene == null)
                return;

            if(CurrentMode == GizmoMode.TilePaint)
            {
                PaintTileAtMouse(screenMousePos);
                _isDragging = true; // Enable drag-painting
                _activeAxis = SelectedAxis.None;
                return;
            }

            Vector2 worldMousePos = Camera.ScreenToWorld(screenMousePos, CurrentViewport);

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
                        if(GizmoRenderer.HitTestPoint(worldMousePos, pivotPos + new Vector2(HandleLength, 0), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.TranslateX, worldMousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(worldMousePos, pivotPos + new Vector2(0, HandleLength), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.TranslateY, worldMousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(worldMousePos, pivotPos, 12f))
                        {
                            StartDrag(SelectedAxis.Center, worldMousePos, transform);
                            return;
                        }
                    }

                    // --- 2. SCALE MODE INTERACTIONS ---
                    else if(CurrentMode == GizmoMode.Scale)
                    {
                        // Hit-test diagonal uniform scale box first
                        Vector2 diagonalHandle = pivotPos + new Vector2(HandleLength * 0.7f, HandleLength * 0.7f);
                        if(GizmoRenderer.HitTestPoint(worldMousePos, diagonalHandle, ClickTolerance))
                        {
                            StartDrag(SelectedAxis.ScaleCorner, worldMousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(worldMousePos, pivotPos + new Vector2(HandleLength, 0), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.ScaleX, worldMousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(worldMousePos, pivotPos + new Vector2(0, HandleLength), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.ScaleY, worldMousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(worldMousePos, pivotPos, 12f))
                        {
                            StartDrag(SelectedAxis.Center, worldMousePos, transform);
                            return;
                        }
                    }

                    // --- 3. SIZE MODE INTERACTIONS ---
                    else if(CurrentMode == GizmoMode.Size)
                    {
                        if(GizmoRenderer.HitTestPoint(worldMousePos, baseCorner + new Vector2(currentWidth, currentHeight * 0.5f), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.SizeWidth, worldMousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(worldMousePos, baseCorner + new Vector2(currentWidth * 0.5f, currentHeight), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.SizeHeight, worldMousePos, transform);
                            return;
                        }
                        if(GizmoRenderer.HitTestPoint(worldMousePos, baseCorner + new Vector2(currentWidth, currentHeight), ClickTolerance))
                        {
                            StartDrag(SelectedAxis.SizeCorner, worldMousePos, transform);
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

                            if(GizmoRenderer.HitTestPoint(worldMousePos, center, ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderOffset, worldMousePos, box.Offset);
                                return;
                            }
                            if(GizmoRenderer.HitTestPoint(worldMousePos, center + new Vector2(halfSize.X, 0), ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderBoxWidth, worldMousePos, box.Offset);
                                _initialColliderSize = box.Size;
                                return;
                            }
                            if(GizmoRenderer.HitTestPoint(worldMousePos, center + new Vector2(0, halfSize.Y), ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderBoxHeight, worldMousePos, box.Offset);
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

                            if(GizmoRenderer.HitTestPoint(worldMousePos, center, ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderOffset, worldMousePos, circle.Offset);
                                return;
                            }
                            if(GizmoRenderer.HitTestPoint(worldMousePos, center + new Vector2(circle.Radius, 0), ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderCircleRadius, worldMousePos, circle.Offset);
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

                            if(GizmoRenderer.HitTestPoint(worldMousePos, center, ClickTolerance))
                            {
                                StartColliderDrag(SelectedAxis.ColliderOffset, worldMousePos, poly.Offset);
                                return;
                            }

                            for(int i = 0; i < poly.Vertices.Count; i++)
                            {
                                Vector2 worldVertex = center + poly.Vertices[i];
                                if(GizmoRenderer.HitTestPoint(worldMousePos, worldVertex, ClickTolerance))
                                {
                                    StartColliderDrag(SelectedAxis.ColliderPolyVertex, worldMousePos, poly.Offset);
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


        /// <summary>
        /// Instantly centers the camera on the currently selected GameObject.
        /// </summary>
        public void FocusOnSelected()
        {
            if(_selectedGo == null)
                return;

            var transform = _selectedGo.GetComponent<TransformComponent>();
            if(transform != null)
            {
                // Center camera directly on the object's world position
                // (If using a centered pivot, use WorldPosition. Otherwise offset by half size)
                Vector2 targetPos = transform.WorldPosition;

                // Optional: If object has dimensions, center on its visual bounding box center
                if(transform.Size != Vector2.Zero)
                {
                    targetPos = transform.RenderTopLeft + (transform.Size * transform.Scale * 0.5f);
                }

                Camera.Position = targetPos;
            }
        }

        private void HandleMouseMoved(Vector2 currentMousePos, Vector2 mouseDelta)
        {
            // --- TILE PAINT DRAG PAINTING ---
            if(_isDragging && CurrentMode == GizmoMode.TilePaint)
            {
                PaintTileAtMouse(currentMousePos);
                return;
            }

            if(!_isDragging || _selectedGo == null)
                return;
            if(!_isDragging || _selectedGo == null)
                return;
            Vector2 currentWorldMousePos = Camera.ScreenToWorld(currentMousePos, CurrentViewport);
            Vector2 totalWorldMouseDelta = currentWorldMousePos - _dragStartMousePos;

            var transform = _selectedGo.GetComponent<TransformComponent>();
            if(transform == null)
                return;

            Vector2 totalMouseDelta = currentMousePos - _dragStartMousePos;

            switch(_activeAxis)
            {
                // Move/Translate operations
                case SelectedAxis.Center:
                    transform.X = _initialEntityPos.X + totalWorldMouseDelta.X;
                    transform.Y = _initialEntityPos.Y + totalWorldMouseDelta.Y;
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.TranslateX:
                    transform.X = _initialEntityPos.X + totalWorldMouseDelta.X;
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.TranslateY:
                    transform.Y = _initialEntityPos.Y + totalWorldMouseDelta.Y;
                    OnTransformModified?.Invoke();
                    break;

                // Scale operations
                case SelectedAxis.ScaleX:
                    transform.ScaleX = (float) Math.Round(_initialEntityScale.X + (totalWorldMouseDelta.X / 10f));
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.ScaleY:
                    transform.ScaleY = (float) Math.Round(_initialEntityScale.Y + (totalWorldMouseDelta.Y / 10f));
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.ScaleCorner:
                    float scaleDelta = (totalWorldMouseDelta.X + totalWorldMouseDelta.Y) / 20f;
                    transform.ScaleX = Math.Max(1f, (float) Math.Round(_initialEntityScale.X + scaleDelta));
                    transform.ScaleY = Math.Max(1f, (float) Math.Round(_initialEntityScale.Y + scaleDelta));
                    OnTransformModified?.Invoke();
                    break;

                // Dimension Size operations
                case SelectedAxis.SizeWidth:
                    transform.SizeX = (int) Math.Round(_initialEntitySize.X + ((totalWorldMouseDelta.X) / transform.ScaleX));
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.SizeHeight:
                    transform.SizeY = (int) Math.Round(_initialEntitySize.Y + ((totalWorldMouseDelta.Y) / transform.ScaleY));
                    OnTransformModified?.Invoke();
                    break;

                case SelectedAxis.SizeCorner:
                    transform.SizeX = (int) Math.Round(_initialEntitySize.X + ((totalWorldMouseDelta.X) / transform.ScaleX));
                    transform.SizeY = (int) Math.Round(_initialEntitySize.Y + ((totalWorldMouseDelta.Y) / transform.ScaleY));
                    OnTransformModified?.Invoke();
                    break;

                // --- Safe Collider Drag Operations (Null checked inside!) ---
                case SelectedAxis.ColliderOffset:
                {
                    var collider = _selectedGo.GetComponent<ColliderComponent>();
                    if(collider != null)
                    {
                        collider.Offset = _initialColliderOffset + totalWorldMouseDelta;
                        OnColliderModified?.Invoke();
                    }
                }
                break;

                case SelectedAxis.ColliderBoxWidth:
                {
                    var collider = _selectedGo.GetComponent<ColliderComponent>();
                    if(collider is BoxColliderComponent boxW)
                    {
                        float newWidth = Math.Max(4f, _initialColliderSize.X + (totalWorldMouseDelta.X * 2f));
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
                        float newHeight = Math.Max(4f, _initialColliderSize.Y + (totalWorldMouseDelta.Y * 2f));
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
                        float newRadius = Math.Max(2f, _initialColliderRadius + totalWorldMouseDelta.X);
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
                        polyV.Vertices[_selectedVertexIndex] = _initialVertexPos + totalWorldMouseDelta;
                        OnColliderModified?.Invoke();
                    }
                }
                break;

                default:
                    Log.Warning("no axis selected");
                    break;
            }
        }

        private void PaintTileAtMouse(Vector2 screenMousePos)
        {
            if(_activeScene == null || _activeScene.SceneMap == null)
                return;

            var map = _activeScene.SceneMap;
            Vector2 worldMousePos = Camera.ScreenToWorld(screenMousePos, CurrentViewport);
            int tileSize = map.TileSize;

            int tileX = (int) Math.Floor(worldMousePos.X / tileSize);
            int tileY = (int) Math.Floor(worldMousePos.Y / tileSize);

            if(tileX >= 0 && tileX < map.Width && tileY >= 0 && tileY < map.Height)
            {
                int selectedTileIndex = EditorContextManager.SelectedTileIndex;
                if(selectedTileIndex >= 0)
                {
                    // Cleanly delegate the lookup logic to the Map class
                    int valueToStore = map.GetCustomValueForTile(selectedTileIndex);

                    map.SetGridValue(tileX, tileY, valueToStore);
                    Form1.NeedsToBeSaved = true;
                }
            }
        }
        private void HandleMouseLeftUp(Vector2 mousePos)
        {
            if(CurrentMode == GizmoMode.TilePaint)
            {
                _isDragging = false;
                _activeAxis = SelectedAxis.None;
                return;
            }
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