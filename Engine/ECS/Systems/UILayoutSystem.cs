using Engine.Core.ECS.Components;
using Engine.Core.ECS.Components.UI;
using Microsoft.Xna.Framework;
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
            UsedInEditor = true;
            UpdatePolicy = SystemUpdatePolicy.FrameUpdate;
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
            var layoutComp = go.GetComponent<LayoutComponent>();
            if(layoutComp == null)
                return;

            float currentOffset = layoutComp.Padding;
            bool isVertical = layoutComp.Direction == LayoutDirection.Vertical;

            foreach(var child in go.Children)
            {
                var childTransform = child.GetComponent<TransformComponent>();
                if(childTransform == null)
                    continue;

                if(isVertical)
                {
                    childTransform.LocalPosition = new Vector2(layoutComp.Padding, currentOffset);
                    currentOffset += (childTransform.SizeY * childTransform.ScaleY) + layoutComp.Padding;
                }
                else
                {
                    childTransform.LocalPosition = new Vector2(currentOffset, layoutComp.Padding);
                    currentOffset += (childTransform.SizeX * childTransform.ScaleX) + layoutComp.Padding;
                }
            }
        }

        public void ApplyFlowLayout(GameObject go)
        {
            //todo
        }


        public void ApplyGridLayout(GameObject go)
        {
            //todo
        }

    }
}
