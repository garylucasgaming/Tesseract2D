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
        public virtual bool isHovered
        {
            get; set;
        }

        [Browsable(false)]
        public virtual bool isClicked
        {
            get; set;
        }


        public GameEvent? Hovered { get; set; } = new GameEvent();
        public GameEvent? HoverExit { get; set; } = new GameEvent();

        public GameEvent? Clicked { get; set; } = new GameEvent();
        public GameEvent? Released { get; set; } = new GameEvent();


    }
}
