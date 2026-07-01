using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Forms.NET.Controls;
using Engine.Core.ECS; // Adjust namespace to match your GameScene path
using WinFormsApp1;
using Engine.Core.Serialization;
using Color = Microsoft.Xna.Framework.Color;
using Engine.Editor.Utilities;
using Engine.Editor.MGWindow.Services;

namespace Editor
{
    public class MGWindowControl : MonoGameControl
    {
        private EditorInputService _inputService;
        private EditorRenderService _renderService;

        protected override void Initialize()
        {
            // Set up our centralized native asset matrices
            GizmoRenderer.Initialize(Editor.GraphicsDevice);

            // Spin up our single responsibility services
            _inputService = new EditorInputService();
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
            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene == null)
                return;

            GameObject selectedGo = GetSelectedGameObject();

            // Delegate completely to your input service module
            _inputService.ProcessInputs(activeScene, selectedGo);
        }

        protected override void Draw()
        {
            Editor.GraphicsDevice.Clear(new Color(33, 33, 33));

            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene == null)
                return;

            GameObject selectedGo = GetSelectedGameObject();

            Editor.spriteBatch.Begin();

            // Delegate completely to your dedicated rendering service module
            _renderService.RenderSceneViewport(Editor.spriteBatch, activeScene, selectedGo);

            Editor.spriteBatch.End();
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
