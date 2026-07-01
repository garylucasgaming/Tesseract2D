using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Engine.Core.Utilities;
using System.ComponentModel;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Engine.Core.ECS.Components
{
    public class TransformComponent : GameComponent
    {

        
        private float _x = 0.0f;
        private float _y = 0.0f;
        private float _xOFF = 0.0f;
        private float _yOFF = 0.0f;
        private float _sizeX = 0.0f;
        private float _sizeY = 0.0f;
        private float _scaleX = 1f;
        private float _scaleY = 1f;
   

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

                // Push recursively down to sub-children
                UpdateChildrenPositions();
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

                UpdateChildrenPositions();
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

                UpdateChildrenPositions();
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

                UpdateChildrenPositions();
            }
        }

        public float SizeX
        {
            get => _sizeX;
            set
            {
                _sizeX = value;
            }
        }

        public float SizeY
        {
            get => _sizeY;
            set
            {
                _sizeY = value;
            }
        }

        public float ScaleX {
            get => _scaleX;
            set
            {
                _scaleX = value;
            }
        } 
        public float ScaleY {

            get => _scaleY;
            set
            {
                _scaleY = value;
            }

        } 

       
        public float Rotation { get; set; } = 0.0f;

        
        public Vector2 WorldPosition
        {
            get => new Vector2(_x, _y);
            set
            {
                
                X = value.X;
                Y = value.Y;
                
            }
        }
        
        public Vector2 LocalPosition
        {
            get => new Vector2(_xOFF, _yOFF);
            set
            {
                XOffset = value.X;
                YOffset = value.Y;
               
            }

        }

        public Vector2 Size
        {
            get => new Vector2 (_sizeX, _sizeY);
            set
            {
                SizeX = value.X;
                SizeY = value.Y;
                
            }
        }

        public Vector2 Scale
        {
            get => new Vector2(_scaleX, _scaleY);
            set
            {
                ScaleX = value.X;
                ScaleY = value.Y;
               
            }
        }


        private void UpdateChildrenPositions()
        {
             if(Owner == null || Owner.Children == null)
                return;

            foreach(var child in Owner.Children)
            {
                var childTransform = child.GetComponent<TransformComponent>();
                if(childTransform == null)
                    continue;
                // Calculate the child's new absolute world position using its offset
                childTransform.SetWorldPositionFromParent(WorldPosition);

            }
        }

        private void SetWorldPositionFromParent(Vector2 parentPosition)
        {
            X = parentPosition.X + XOffset;
            Y = parentPosition.Y + YOffset;

            
        }

      

       



    }
}
