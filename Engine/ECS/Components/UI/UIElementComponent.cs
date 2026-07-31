using Engine.Core.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{
    public abstract class UIElementComponent : GameComponent
    {

        private bool _isActive = true;

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }


        [Browsable(false)]
        public List<UIElementComponent> ChildElements = new List<UIElementComponent>();


        [Browsable(false)]
        public bool isHovered
        {
            get; set;
        }

        [Browsable(false)]
        public bool isClicked
        {
            get; set;
        }


        public GameEvent? Hovered { get; set; }

        public GameEvent? Clicked
        {
            get; set;
        }


    }
}
