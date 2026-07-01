using Engine.Core.ECS;
using Engine.Editor.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.MGWindow.Services
{
    public class EditorRenderService
    {
        private const float HandleLength = 50f;

        public void RenderSceneViewport(SpriteBatch spriteBatch, GameScene activeScene, GameObject selectedGo)
        {
            foreach(var entity in activeScene.Entities.GetSerializableEntities())
            {
                if(!entity.isActive)
                    continue;

                dynamic transform = GetTransform(entity);
                if(transform == null)
                    continue;

                Vector2 position = new Vector2((float) transform.X, (float) transform.Y);
                bool isSelected = (selectedGo != null && entity.Id == selectedGo.Id);

                // Draw base object structural anchors
                Microsoft.Xna.Framework.Color centerColor = isSelected ? Microsoft.Xna.Framework.Color.LimeGreen : new Microsoft.Xna.Framework.Color(200, 200, 200, 120);
                GizmoRenderer.DrawCircle(spriteBatch, position, isSelected ? 8 : 5, 12, centerColor);
                GizmoRenderer.DrawPoint(spriteBatch, position, Microsoft.Xna.Framework.Color.White, 2);

                // Render axis transformation lines if the element is currently active
                if(isSelected)
                {
                    GizmoRenderer.DrawMoveGizmo(spriteBatch, position, HandleLength, 2);
                }
            }
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
