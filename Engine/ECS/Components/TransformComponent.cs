using System;
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
        private float _rotation = 0.0f;
        private TransformOrigin _origin = TransformOrigin.TopLeft;

        // Event for the TransformSystem to hook into when edited via UI/code
        public event Action<TransformComponent>? OnTransformChanged;

        private void NotifyChange() => OnTransformChanged?.Invoke(this);

        public TransformOrigin Origin
        {
            get => _origin;
            set
            {
                _origin = value;
                NotifyChange();
            }
        }

        [Browsable(true)]
        public float X
        {
            get => _x;
            set
            {
                _x = value;
                NotifyChange();
            }
        }

        [Browsable(true)]
        public float Y
        {
            get => _y;
            set
            {
                _y = value;
                NotifyChange();
            }
        }

        public float XOffset
        {
            get => _xOFF;
            set
            {
                _xOFF = value;
                NotifyChange();
            }
        }

        public float YOffset
        {
            get => _yOFF;
            set
            {
                _yOFF = value;
                NotifyChange();
            }
        }

        public float SizeX
        {
            get => _sizeX;
            set
            {
                _sizeX = value;
                NotifyChange();
            }
        }

        public float SizeY
        {
            get => _sizeY;
            set
            {
                _sizeY = value;
                NotifyChange();
            }
        }

        public float ScaleX
        {
            get => _scaleX;
            set
            {
                _scaleX = value;
                NotifyChange();
            }
        }

        public float ScaleY
        {
            get => _scaleY;
            set
            {
                _scaleY = value;
                NotifyChange();
            }
        }

        public float Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                NotifyChange();
            }
        }

        public Vector2 WorldPosition
        {
            get => new Vector2(_x, _y);
            set
            {
                _x = value.X;
                _y = value.Y;
                NotifyChange();
            }
        }

        public Vector2 LocalPosition
        {
            get => new Vector2(_xOFF, _yOFF);
            set
            {
                _xOFF = value.X;
                _yOFF = value.Y;
                NotifyChange();
            }
        }

        public Vector2 Size
        {
            get => new Vector2(_sizeX, _sizeY);
            set
            {
                _sizeX = value.X;
                _sizeY = value.Y;
                NotifyChange();
            }
        }

        public Vector2 Scale
        {
            get => new Vector2(_scaleX, _scaleY);
            set
            {
                _scaleX = value.X;
                _scaleY = value.Y;
                NotifyChange();
            }
        }

        public Vector2 GetOriginOffset()
        {
            float x = 0f;
            float y = 0f;
            switch(_origin)
            {
                case TransformOrigin.Top:
                    x = _sizeX * 0.5f;
                    break;
                case TransformOrigin.TopRight:
                    x = _sizeX;
                    break;
                case TransformOrigin.MiddleLeft:
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

        [Browsable(false)]
        public Vector2 RenderTopLeft
        {
            get => WorldPosition - (GetOriginOffset() * Scale);
        }
    }
}