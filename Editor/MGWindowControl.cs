
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

namespace Editor
{
    public class MGWindowControl : MonoGameControl
    {
        private EditorInputService _inputService;
        private EditorRenderService _renderService;
        private InputManager _inputManager;
        private float deltaTime;
        private GameScene _activeScene;

        public bool SimulationRunning { get; set; } = false;
        public bool SimulationPaused { get; set; } = false;

        protected override void Initialize()
        {
            // Set up our centralized native asset matrices
            GizmoRenderer.Initialize(Editor.GraphicsDevice);

            // Spin up our single responsibility services
            _inputManager = new InputManager();
            _inputService = new EditorInputService(_inputManager);
            _renderService = new EditorRenderService();

            _inputService.OnTransformModified = () =>
            {
                // Find whichever entity is currently selected in the tree view
                if(Form1.ActiveHierarchyTreeView?.SelectedNode?.Tag is GameObject selectedGo)
                {
                    // Get its live transform instance
                    foreach(var kvp in selectedGo.Components)
                    {
                        if(kvp.Key.Name == "TransformComponent")
                        {
                            // Direct specific card refresh!
                            Form1.RefreshComponentInspector(kvp.Value);
                            break;
                        }
                    }
                }
            };
        }

        protected override void Update(GameTime gameTime)
        {
            //base deltatime
            deltaTime = (float) gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;
           
            // 1. Tick the hardware state sensors to fire input events!
            _inputManager.Update();
            _activeScene = EditorContextManager.ActiveLoadedScene;
            if(_activeScene != null)
            {
                bool playModeActive = SimulationRunning && !SimulationPaused;
                _activeScene.Update(deltaTime);
                GameObject selectedGo = GetSelectedGameObject();

                // 2. Feed the active selection contexts down into the service pipeline
                // This replaces ProcessInputs() completely since events handle the heavy lifting now.
                _inputService.SetContext(_activeScene, selectedGo);
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

            // Delegate completely to your dedicated rendering service module
            _renderService.RenderSceneViewport(Editor.spriteBatch, _activeScene, selectedGo);
            _activeScene.Systems.Render(Editor.spriteBatch);
            Editor.spriteBatch.End();
        }


        public void StartSimulation()
        {
            if(SimulationRunning && SimulationPaused)
            {
                SimulationPaused = false;
                Log.Info("[Simulation] Simulation unpaused.");
            } else if(!SimulationRunning)
            {
               
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
                Log.Info("[Simulation] Simulation stopped.");
                
            }
        }
        

        public void UpdateSimulationSystems(float deltaTime)
        {
            // Determine if we are running standard gameplay loop systems right now
            bool playModeActive = SimulationRunning && !SimulationPaused;

          
        }




        private GameObject GetSelectedGameObject()
        {
            if(Form1.ActiveHierarchyTreeView?.SelectedNode?.Tag is GameObject go)
            {
                return go;
            }
            return null;
        }
    }
}
