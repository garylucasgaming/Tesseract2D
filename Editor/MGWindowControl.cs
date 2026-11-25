using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Forms.NET.Controls;
using Engine.Core.ECS;
using WinFormsApp1;
using Engine.Core.Serialization;
using Color = Microsoft.Xna.Framework.Color;
using Engine.Editor.Utilities;
using Engine.Editor.MGWindow.Services;
using Engine.Core.Runtime;
using Engine.Core.Utilities;
using Engine.Core.ECS.Components;
using Engine.Core.ECS.Systems;
using System;
using MonoGame.Forms.NET.Services;

namespace Editor
{
    public class MGWindowControl : MonoGameControl
    {
        private EditorInputService _inputService;
        private EditorRenderService _renderService;
        private InputManager _inputManager;
        private GameHost _host;
        private float deltaTime;
        private GameScene _activeScene;

        public int WindowWidth;
        public int WindowHeight;
       

        // --- Timestep Accumulator Variables ---
        private const float TargetTickTime = 1f / 60f; // 60 Ticks per second (~0.01667s)
        private float _physicsAccumulator = 0.0f;

        public bool SimulationRunning { get; set; } = false;
        public bool SimulationPaused { get; set; } = false;
        public Camera2D EditorCamera
        {
            get; private set;
        } = new Camera2D();

        public SessionGlobals Session { get; private set; } = SessionGlobals.Current;


        public MGWindowControl()
        {
            this.GraphicsProfile = GraphicsProfile.HiDef;
        }
        protected override void Initialize()
        {
            GizmoRenderer.Initialize(Editor.GraphicsDevice);

            Session = new SessionGlobals(EditorContextManager.Context, new MonoGameInputSource());
            Globals.Current = Session;

            EditorCamera = Session.EditorCamera;
            Session.SetViewport(Editor.GraphicsDevice.Viewport);

            var viewport = Editor.GraphicsDevice.Viewport;
            EditorCamera.Position = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            _inputManager = Session.InputManager;
            _host = new GameHost(EditorContextManager.Context, Session, new EditorFrameRenderer(this));
            _inputService = new EditorInputService(_inputManager, EditorCamera);
            _renderService = new EditorRenderService();
            Editor.FPSCounter.Visible = false;

            _inputService.OnTransformModified = () =>
            {
                RefreshInspector("TransformComponent");
            };

            // Handle Collider modifications (BoxCollider, SphereCollider, etc.)
            _inputService.OnColliderModified = () =>
            {
                RefreshInspector("Collider");
            };
        }
        protected override void Update(GameTime gameTime)
        {
            deltaTime = (float) gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;

            _activeScene = EditorContextManager.ActiveLoadedScene;
            if(_activeScene == null)
                return;

            if(!ReferenceEquals(_host.Scene, _activeScene))
                _host.AttachScene(_activeScene);

            EditorContextManager.PlayState = SimulationRunning && !SimulationPaused;

            GameObject selectedGo = GetSelectedGameObject();
            object selectedComponent = Engine.Editor.WinFormsApp1.ComponentCardFactory.SelectedComponentInstance;
            _inputService.SetContext(_activeScene, selectedGo, Editor.GraphicsDevice.Viewport);

            // Cap deltaTime to prevent huge physics steps after dragging/debugging.
            float clampedDelta = Math.Min(deltaTime, 0.25f);

            _host.Update(clampedDelta, () =>
            {
                _inputService.Update(deltaTime);

                if(EditorContextManager.PlayState)
                {
                    _physicsAccumulator += clampedDelta;
                    while(_physicsAccumulator >= TargetTickTime)
                    {
                        _host.TickUpdate(TargetTickTime);
                        _physicsAccumulator -= TargetTickTime;
                    }
                }
                else
                {
                    _physicsAccumulator = 0.0f;
                }
            });
        }

        protected override void Draw()
        {
            _activeScene = EditorContextManager.ActiveLoadedScene;
            if(_activeScene == null)
                return;

            if(!ReferenceEquals(_host.Scene, _activeScene))
                _host.AttachScene(_activeScene);

            _host.Render();
        }

        /// <summary>
        /// Editor-specific frame drawing, invoked by <see cref="GameHost.Render"/>.
        /// </summary>
        private void RenderScene(GameScene scene)
        {
            Editor.GraphicsDevice.Clear(Color.Black);

            GameObject selectedGo = GetSelectedGameObject();

            //world space render
            Matrix viewMatrix = EditorCamera.GetViewMatrix(Editor.GraphicsDevice.Viewport);
            Editor.spriteBatch.Begin(transformMatrix: viewMatrix);

            if(!EditorContextManager.PlayState)
            {
                _renderService.RenderSceneGridAndBounds(Editor.spriteBatch, scene, EditorCamera.Zoom);
            }

            scene.Render(Editor.spriteBatch, Editor.Content);
            scene.Systems.Render(Editor.spriteBatch, Editor.Content);
            scene.Systems.RenderUI(Editor.spriteBatch, Editor.Content, Engine.Core.ECS.Components.UI.UISpace.World);
            if(!EditorContextManager.PlayState)
            {
                scene.Systems.RenderUI(Editor.spriteBatch, Editor.Content, Engine.Core.ECS.Components.UI.UISpace.Screen);
            }
            if(!EditorContextManager.PlayState)
            {
                _renderService.RenderSceneViewport(
                 Editor.spriteBatch,
                 scene,
                 selectedGo,
                 Engine.Editor.WinFormsApp1.ComponentCardFactory.SelectedComponentInstance,
                 _inputService.CurrentMode,
                 EditorCamera.Zoom
             );
            }
            

            Editor.spriteBatch.End();

            if(EditorContextManager.PlayState)
            {
                Editor.spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                scene.Systems.RenderUI(Editor.spriteBatch, Editor.Content, Engine.Core.ECS.Components.UI.UISpace.Screen);
                Editor.spriteBatch.End();
            }
            // screenspace/ui render
           
        }

        public void StartSimulation()
        {
            if(SimulationRunning && SimulationPaused)
            {
                SimulationPaused = false;
                Log.Info("[Simulation] Simulation unpaused.");
            }
            else if(!SimulationRunning)
            {
                // Reset the accumulator to fresh 0.0s before the simulation starts
                _physicsAccumulator = 0.0f;
                _activeScene.Systems.physicsSystem.ResetPhysicsTransforms();
                SimulationRunning = true;
                SimulationPaused = false;
                Log.Info("[Simulation] Simulation started.");
            }
        }

        public void pauseSimulation()
        {
            if(SimulationRunning && !SimulationPaused)
            {
                SimulationPaused = true;
                Log.Info("[Simulation] Simulation paused.");
            }
        }

        public void StopSimulation()
        {
            if(SimulationRunning)
            {
                SimulationRunning = false;
                SimulationPaused = false;
                _physicsAccumulator = 0.0f; // Flush buffer

                if(_activeScene.Systems != null)
                {
                    _activeScene.Systems.ResetSimulationStates();
                    _activeScene.CleanupRuntimeEntities();
                    var form = FindForm() as Form1;
                    form.RefreshEditor();
                }
                
                Log.Info("[Simulation] Simulation stopped.");
            }
        }

        private GameObject GetSelectedGameObject()
        {
            if(Form1.ActiveHierarchyTreeView?.SelectedNode?.Tag is GameObject go)
            {
                return go;
            }
            return null;
        }

     

        private void RefreshInspector(string fallbackComponentTypeName)
        {
            // 1. Try to refresh the active component card to keep focus/selection
            object selectedComponent = Engine.Editor.WinFormsApp1.ComponentCardFactory.SelectedComponentInstance;
            if(selectedComponent != null)
            {
                Form1.RefreshComponentInspector(selectedComponent);
                return;
            }

            // 2. Fallback: If no card is active, find the component on the selected GameObject
            if(Form1.ActiveHierarchyTreeView?.SelectedNode?.Tag is GameObject selectedGo)
            {
                foreach(var kvp in selectedGo.Components)
                {
                    // Using Contains allows "Collider" to match BoxColliderComponent, SphereColliderComponent, etc.
                    if(kvp.Key.Name.Contains(fallbackComponentTypeName))
                    {
                        Form1.RefreshComponentInspector(kvp.Value);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Adapts the editor's drawing code to the engine's host-owned render
        /// pass. This is editor-only; the engine depends only on IFrameRenderer.
        /// </summary>
        private sealed class EditorFrameRenderer : IFrameRenderer
        {
            private readonly MGWindowControl _owner;

            public EditorFrameRenderer(MGWindowControl owner)
            {
                _owner = owner;
            }

            public void Initialize(SessionGlobals session)
            {
            }

            public void Render(GameScene scene, SessionGlobals session)
            {
                _owner.RenderScene(scene);
            }

            public void Shutdown()
            {
            }
        }


    }
}