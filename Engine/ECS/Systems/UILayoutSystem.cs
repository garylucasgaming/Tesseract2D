using Engine.Core.ECS.Components;
using Engine.Core.ECS.Components.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Systems
{
    public class UILayoutSystem : GameSystem
    {
        public override IComponentQuery RequiredComponents
        {
            get;
            set;
        }

        public UILayoutSystem()
        {
            RequiredComponents = Query.Has<LayoutComponent>();
        }

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
            foreach(var go in gameObjects)
            {
                var transform = go.GetComponent<TransformComponent>();

                switch(go.GetComponent<LayoutComponent>().Layout)
                {
                    case LayoutType.Stack:
                        ApplyStackLayout(go);
                        break;
                    case LayoutType.Flow:
                        ApplyFlowLayout(go);
                        break;
                    case LayoutType.Grid:
                        ApplyGridLayout(go);
                        break;
                }


            }
        }

        

        public void ApplyStackLayout(GameObject go)
        {

        }

        public void ApplyFlowLayout(GameObject go)
        {

        }


        public void ApplyGridLayout(GameObject go)
        {

        }

    }
}
