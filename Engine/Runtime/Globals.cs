using Engine.Core.Utilities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Runtime
{
    public static class Globals
    {

        public static InputManager? InputManager
        {
        get; private set; 
        } = new InputManager();

        public static Camera2D? EditorCamera
        {
            get; private set;
        } = new Camera2D();

        public static Viewport Viewport
        {
            get; private set;
        }

        public static void Initialize()
        {
            InputManager = new InputManager();
            EditorCamera = new Camera2D();
        }

        public static void SetInputManager(InputManager im)
        {
            InputManager = im;
        }

        public static void SetEditorCamera(Camera2D cam)
        {
            EditorCamera = cam;
        }

        public static void SetViewport(Viewport view)
        {
            Viewport = view;
        }

    }
}
