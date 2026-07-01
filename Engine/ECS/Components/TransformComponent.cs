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
        
        public enum TransformOrigin
        {
            TopLeft, Top, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, Bottom, BottomRight
        }
        private float _x = 0.0f;
        private float _y = 0.0f;
        private float _xOFF = 0.0f;
        private float _yOFF = 0.0f;
        private float _sizeX = 16f;
        private float _sizeY = 16f;
        private float _scaleX = 4f;
        private float _scaleY = 4f;
        public TransformOrigin Origin
        {
            get;
            set;
        } = TransformOrigin.TopLeft;


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
                        // Calculate offset relative to parent's true structural bounds corner
                        _xOFF = _x - parentTransform.X;
                    }
                }
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

        /// <summary>
        /// Calculates the pixel pivot offset vector relative to the Top-Left (0,0) corner.
        /// </summary>
        public Vector2 GetOriginOffset()
        {
            float x = 0f;
            float y = 0f;

            switch(Origin)
            {
                case TransformOrigin.TopLeft:
                    x = 0f;
                    y = 0f;
                    break;
                case TransformOrigin.Top:
                    x = _sizeX * 0.5f;
                    y = 0f;
                    break;
                case TransformOrigin.TopRight:
                    x = _sizeX;
                    y = 0f;
                    break;

                case TransformOrigin.MiddleLeft:
                    x = 0f;
                    y = _sizeY * 0.5f;
                    break;
                case TransformOrigin.Center:
                    x = _sizeX * 0.5f;
                    y = _sizeY * 0.5f;
                    break;
                case TransformOrigin.MiddleRight:
                    x = _sizeX;
                    y = _sizeY * 0.5f;
                    break;

                case TransformOrigin.BottomLeft:
                    x = 0f;
                    y = _sizeY;
                    break;
                case TransformOrigin.Bottom:
                    x = _sizeX * 0.5f;
                    y = _sizeY;
                    break;
                case TransformOrigin.BottomRight:
                    x = _sizeX;
                    y = _sizeY;
                    break;
            }

            return new Vector2(x, y);
        }


        private void UpdateChildrenPositions()
        {
            if(gameObject == null || gameObject.Children == null)
                return;

            foreach(var child in gameObject.Children)
            {
                var childTransform = child.GetComponent<TransformComponent>();
                if(childTransform == null)
                    continue;

                childTransform.SetWorldPositionFromParent(this);
            }
        }

        private void SetWorldPositionFromParent(TransformComponent parentTransform)
        {
            // Children are safely pinned to the parent's stable WorldPosition pivot!
            _x = parentTransform.X + _xOFF;
            _y = parentTransform.Y + _yOFF;

            UpdateChildrenPositions();
        }



        /// <summary>
        /// Calculates the absolute world space coordinate of the Top-Left boundary corner, 
        /// accounting for the local origin adjustment.
        /// </summary>
        [Browsable(false)]
        public Vector2 RenderTopLeft
        {
            get
            {
                Vector2 originOffset = GetOriginOffset() * Scale;
                return WorldPosition - originOffset;
            }
        }



    }
}
