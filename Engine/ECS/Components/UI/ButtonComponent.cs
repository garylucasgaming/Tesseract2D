using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{
    public class ButtonComponent : UIElementComponent
    {
        [Browsable(false)]
        public SpriteComponent? Sprite
        {
            get; set;
        }

        


        public override void OnEnabled()
        {
            if(!gameObject.HasComponent<SpriteComponent>())
            {
                Sprite = gameObject.AddComponent<SpriteComponent>();
                Sprite.isUI = true;
            }
            else
            {
                Sprite = gameObject.GetComponent<SpriteComponent>();
                Sprite.isUI = true;
            }
        }


    }
}
