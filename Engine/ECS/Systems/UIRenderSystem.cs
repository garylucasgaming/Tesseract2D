using Engine.Core.ECS.Components.UI;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Engine.Core.ECS.Systems
{
    public class UIRenderSystem : GameSystem
    {
        public override IComponentQuery RequiredComponents
        {
            get; set;
        }

        public HashSet<GameObject> Canvases = new HashSet<GameObject>();

        public List<GameObject> ScreenSpaceCanvases = new List<GameObject>();
        public List<GameObject> WorldSpaceCanvases = new List<GameObject>();

        public bool NeedsToBeSorted = true;

        public UIRenderSystem()
        {
            RequiredComponents = Query.Has<UICanvasComponent>();
            UsedInEditor = true;
            UpdatePolicy = SystemUpdatePolicy.FrameUpdate;
        }

        public void Initialize(ContentManager cm)
        {
            foreach(var canvas in Canvases)
            {
                if(canvas.GetComponent<UICanvasComponent>().isInitialized)
                    continue;

                LoadSprites(canvas, cm);
                canvas.GetComponent<UICanvasComponent>().isInitialized = true;
            }
        }

        public void LoadSprites(GameObject go, ContentManager cm)
        {
            var canva = go.GetComponent<UICanvasComponent>();

            canva.LoadSprites(cm);

        }

        
        

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
            if(ContextScene == null)
                return;
            SortCanvases(gameObjects);



        }


        public void Render(SpriteBatch sb, ContentManager cm ,UISpace space)
        {
           
            if(space == UISpace.Screen)
            {
                foreach(var canvas in ScreenSpaceCanvases)
                {
                    canvas.GetComponent<UICanvasComponent>().Render(sb, cm);
                }
            }
            else if(space == UISpace.World)
            {
                foreach(var canvas in WorldSpaceCanvases)
                {
                    canvas.GetComponent<UICanvasComponent>().Render(sb, cm);
                }

            }
            
            

        }



        public void SortCanvases(HashSet<GameObject> gameObjects)
        {
            // Clear existing lists to prevent duplication or stale tracking
            ScreenSpaceCanvases.Clear();
            WorldSpaceCanvases.Clear();

            foreach(var go in gameObjects)
            {
                var canvasComp = go.GetComponent<UICanvasComponent>();
                if(canvasComp == null)
                    continue;

                if(canvasComp.Space == UISpace.Screen)
                {
                    ScreenSpaceCanvases.Add(go);
                }
                else if(canvasComp.Space == UISpace.World)
                {
                    WorldSpaceCanvases.Add(go);
                }
            }
        }

    }
}
