using Engine.Core.ECS.Components;
using Engine.Core.ECS.Components.UI;
using Engine.Core.Runtime;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Systems
{
    public class UIInputSystem : GameSystem
    {


        private readonly InputManager _inputManager;
        private Camera2D _camera;
        private Viewport _viewport;
        private bool _previousLeftMouseButtonState;

        public override IComponentQuery RequiredComponents
        {
            get;set;
        }

        public UIInputSystem(InputManager im, Camera2D cam, Viewport view)
        {
            RequiredComponents = Query.Has<UICanvasComponent>();
            _inputManager = im;
            _camera = cam;
            _viewport = view;
            UsedInEditor = true;
            UpdatePolicy = SystemUpdatePolicy.FrameUpdate;

        }


        public void SetViewport(Viewport port)
        {
            _viewport = port;
        }


        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
            Vector2 rawMousePos = _inputManager.MousePosition;
            bool isLeftDown = _inputManager.IsLeftButtonDown;
            bool isLeftPressedThisFrame = isLeftDown && !_previousLeftMouseButtonState;

            foreach(var canvasGo in gameObjects)
            {
                var canvas = canvasGo.GetComponent<UICanvasComponent>();
                if(canvas == null || !canvas.IsActive)
                    continue;

                // 1. Resolve mouse position based on canvas space (Screen vs World camera-space)
                Vector2 mousePos = (canvas.Space == UISpace.Screen)
                    ? rawMousePos
                    : _camera.ScreenToWorld(rawMousePos, _viewport);

                foreach(var uiComp in canvas.ChildElements)
                {
                    if(uiComp == null || !uiComp.IsActive)
                        continue;

                    var transform = uiComp.gameObject.GetComponent<TransformComponent>();
                    if(transform == null)
                        continue;

                    // 2. Match the exact draw position calculation from UICanvasComponent
                    Vector2 drawPosition = (canvas.Space == UISpace.Screen)
                        ? transform.GetScreenSpacePosition()
                        : transform.WorldPosition;

                    // 3. Calculate bounding box accounting for Origin and Scale
                    Vector2 topLeft = drawPosition - transform.OriginVector;
                    Vector2 size = new Vector2(transform.SizeX * transform.ScaleX, transform.SizeY * transform.ScaleY);

                    bool isInside = mousePos.X >= topLeft.X && mousePos.X <= topLeft.X + size.X &&
                                    mousePos.Y >= topLeft.Y && mousePos.Y <= topLeft.Y + size.Y;

                    // 4. Handle Hover State & Event
                    bool wasHovered = uiComp.isHovered;
                    uiComp.isHovered = isInside;

                    if(isInside && !wasHovered)
                    {
                        uiComp.Hovered?.Invoke(); // Triggers target method via reflection/cache
                    }

                    // 5. Handle Click State & Event
                    uiComp.isClicked = isInside && isLeftDown;

                    if(isInside && isLeftPressedThisFrame)
                    {
                        uiComp.Clicked?.Invoke(); // Triggers target method via reflection/cache
                    }
                }
            }

            _previousLeftMouseButtonState = isLeftDown;
        }
    }
}
