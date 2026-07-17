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

namespace Editor
{
    public class MGWindowControl : MonoGameControl
    {
        private EditorInputService _inputService;
        private EditorRenderService _renderService;
        private InputManager _inputManager;
        private float deltaTime;
        private GameScene _activeScene;

        // --- Timestep Accumulator Variables ---
        private const float TargetTickTime = 1f / 60f; // 60 Ticks per second (~0.01667s)
        private float _physicsAccumulator = 0.0f;

        public bool SimulationRunning { get; set; } = false;
        public bool SimulationPaused { get; set; } = false;

        protected override void Initialize()
        {
            GizmoRenderer.Initialize(Editor.GraphicsDevice);
            
            _inputManager = new InputManager();
            _inputService = new EditorInputService(_inputManager);
            _renderService = new EditorRenderService();

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

            _inputManager.Update();
            _activeScene = EditorContextManager.ActiveLoadedScene;
           

            if(_activeScene != null)
            {
                
                bool playModeActive = SimulationRunning && !SimulationPaused;
                
                GameObject selectedGo = GetSelectedGameObject();
                object selectedComponent = Engine.Editor.WinFormsApp1.ComponentCardFactory.SelectedComponentInstance;
                _inputService.SetContext(_activeScene, selectedGo);
                _inputService.Update();
                
                // 1. Cap deltaTime to 0.25s (prevents massive physics spikes/crashes when dragging window/debugging)
                float clampedDelta = Math.Min(deltaTime, 0.25f);

                // 2. Only tick the rigid simulation systems (Physics) when play mode is active
                if(playModeActive)
                {
                    _physicsAccumulator += clampedDelta;
                    while(_physicsAccumulator >= TargetTickTime)
                    {
                        // Run TickUpdate (including your PhysicsSystem)
                        _activeScene.TickUpdate(TargetTickTime, playModeActive);
                        _physicsAccumulator -= TargetTickTime;
                    }
                }
                else
                {
                    // Clear the buffer if simulation is not active so it doesn't 
                    // "catch up" instantly when you resume/start the simulation.
                    _physicsAccumulator = 0.0f;
                }

                // 3. Update all standard systems (FrameUpdate & FixedUpdate)
                // Note: Ensure your Scene's Update handles passing this parameter down to Systems.Update()
                _activeScene.Update(clampedDelta, playModeActive);

               
            }
        }

        protected override void Draw()
        {
            Editor.GraphicsDevice.Clear(new Color(33, 33, 33));

            _activeScene = EditorContextManager.ActiveLoadedScene;
            if(_activeScene == null)
                return;

            GameObject selectedGo = GetSelectedGameObject();

            Editor.spriteBatch.Begin();

            _activeScene.Systems.Render(Editor.spriteBatch, Editor.Content);
            _renderService.RenderSceneViewport(Editor.spriteBatch, _activeScene, selectedGo, Engine.Editor.WinFormsApp1.ComponentCardFactory.SelectedComponentInstance, _inputService.CurrentMode);

            Editor.spriteBatch.End();
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


    }
}