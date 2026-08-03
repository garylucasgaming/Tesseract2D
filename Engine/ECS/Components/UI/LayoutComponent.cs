using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Engine.Core.ECS.Components.UI
{

    public enum LayoutType
    {
        Stack, Grid, Flow 
    }

    public enum LayoutDirection
    {
        Horizontal, Vertical
    }
    public class LayoutComponent : PanelComponent
    {


        private LayoutType _layoutType = LayoutType.Stack;
        private LayoutDirection _direction = LayoutDirection.Vertical;
        private int _padding = 0;
        private int _minWidth = 0;
        private int _minHeight = 0;
        private int _preferredHeight = 0;
        private int _preferredWidth = 0;

        public LayoutType Layout
        {
            get => _layoutType;
            set => _layoutType = value;
        }

        public LayoutDirection Direction
        {
            get => _direction;
            set => _direction = value;
        }
        public int Padding
        {
            get => _padding;
            set => _padding = value;
        }
        public int MinWidth
        {
            get => _minWidth;
            set => _minWidth = value;
        }
        public int MinHeight
        {
            get => _minHeight;
            set => _minHeight = value;
        }
        public int PreferredHeight
        {
            get => _preferredHeight;
            set => _preferredHeight = value;
        }
        public int PreferredWidth
        {
            get => _preferredWidth;
            set => _preferredWidth = value;
        }


        public override void OnEnabled()
        {
            base.OnEnabled();
            // Additional initialization if needed
        }
    }
}
