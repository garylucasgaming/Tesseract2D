using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Engine.Core.Utilities;
using System.ComponentModel;

namespace Engine.Core.ECS.Components
{
    public class TransformComponent : GameComponent
    {

        [JsonIgnore]
        private float _x = 0.0f;
        [JsonIgnore]
        private float _y = 0.0f;
        [JsonIgnore]
        private float _xOFF = 0.0f;
        private float _yOFF = 0.0f;


        // 1. Raw layout fields that serialize perfectly as primitive floats
        [Browsable(true)]
        public float X
        {
            get => _x;
            set
            {
                _x = value;

                if(Parent != null)
                {
                    var parentTransform = Parent.GetComponent<TransformComponent>();
                    if(parentTransform != null)
                    {
                        // If moved directly in world space, recalculate local offset from parent
                        _xOFF = _x - parentTransform.X;
                    }
                }

                // Update your helper vector structures
                WorldPosition.X = _x;
                LocalPosition.X = _xOFF;

                // Push recursively down to sub-children
                UpdateChildrenPositionsX();
            }
        }

        [Browsable(true)]
        public float Y
        {
            get => _y;
            set
            {
                _y = value;

                if(Parent != null)
                {
                    var parentTransform = Parent.GetComponent<TransformComponent>();
                    if(parentTransform != null)
                    {
                        // Recalculate local offset from parent
                        _yOFF = _y - parentTransform.Y;
                    }
                }

                WorldPosition.Y = _y;
                LocalPosition.Y = _yOFF;

                UpdateChildrenPositionsY();
            }
        }
        public float XOffset
        {
            get => _xOFF;
            set
            {
                _xOFF = value;

                if(Parent != null)
                {
                    var parentTransform = Parent.GetComponent<TransformComponent>();
                    if(parentTransform != null)
                    {
                        // World position shifts based on parent + new local offset
                        _x = parentTransform.X + _xOFF;
                    }
                }

                WorldPosition.X = _x;
                LocalPosition.X = _xOFF;

                UpdateChildrenPositionsX();
            }
        }

        public float YOffset
        {
            get => _yOFF;
            set
            {
                _yOFF = value;

                if(Parent != null)
                {
                    var parentTransform = Parent.GetComponent<TransformComponent>();
                    if(parentTransform != null)
                    {
                        _y = parentTransform.Y + _yOFF;
                    }
                }

                WorldPosition.Y = _y;
                LocalPosition.Y = _yOFF;

                UpdateChildrenPositionsY();
            }
        }
        public float ScaleX { get; set; } = 1.0f;
        public float ScaleY { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0.0f;

        [Browsable(true)]
        public Vector2 WorldPosition = new Vector2(0,0);
        [Browsable(true)]
        public Vector2 LocalPosition = new Vector2(0, 0);


        private void UpdateChildrenPositionsX()
        {
             if(Owner == null || Owner.Children == null)
                return;

            foreach(var child in Owner.Children)
            {
                var childTransform = child.GetComponent<TransformComponent>();
                if(childTransform == null)
                    continue;
                // Calculate the child's new absolute world position using its offset
                childTransform.X = _x + childTransform.XOffset;

            }
        }

        public void UpdateChildren()
        {
            UpdateChildrenPositionsX();
            UpdateChildrenPositionsY();
        }

        private void UpdateChildrenPositionsY()
        {
            if(Owner == null || Owner.Children == null)
                return;

            foreach(var child in Owner.Children)
            {
                var childTransform = child.GetComponent<TransformComponent>();
                if(childTransform == null)
                    continue;

                // Calculate the child's new absolute world position using its offset
                childTransform.Y = _y + childTransform.YOffset;

            }
        }



    }
}
