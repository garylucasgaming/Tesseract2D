using System;
using System.ComponentModel;
using GISM.Core.Attributes;
using GISM.Core.Serializer; // Added to expose [GISMIgnore]
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Engine.Core.ECS.Components
{
    public class TransformComponent : GameComponent
    {
        public enum TransformOrigin
        {
            TopLeft, Top, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, Bottom, BottomRight
        }

        // IGNORE: Transient runtime flag used for reentrancy control. Never serialize.
        
        private bool _isSettingOffset = false;

        [Browsable(false)]
        // IGNORE: State tracking property with no setter.
        [GISMIgnore]
        public bool IsSettingOffset => _isSettingOffset;

        // KEEP PRIVATE FIELDS: Since the primitive floats are your raw data source,
        // we KEEP the private scalar fields to represent your state, but we will
        // IGNORE the public properties that wrap them to prevent double-saving.
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

        // IGNORE: Computed cache or duplicate structural state.
        
        private Vector2 _originVector = Vector2.Zero;

        // The serializer naturally skips MulticastDelegates like this event.
        public event Action<TransformComponent>? OnTransformChanged;

        private void NotifyChange() => OnTransformChanged?.Invoke(this);

        // IGNORE PROPERTIES: All of these properties map directly to the private fields 
        // we're already capturing. Ignoring them saves processing time and text file bloat.
        [GISMIgnore]
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
        [GISMIgnore]
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
        [GISMIgnore]
        public float Y
        {
            get => _y;
            set
            {
                _y = value;
                NotifyChange();
            }
        }

        [GISMIgnore]
        public float XOffset
        {
            get => _xOFF;
            set
            {
                _isSettingOffset = true;
                try
                {
                    _xOFF = value;
                    NotifyChange();
                }
                finally
                {
                    _isSettingOffset = false;
                }
            }
        }

        [GISMIgnore]
        public float YOffset
        {
            get => _yOFF;
            set
            {
                _isSettingOffset = true;
                try
                {
                    _yOFF = value;
                    NotifyChange();
                }
                finally
                {
                    _isSettingOffset = false;
                }
            }
        }

        [GISMIgnore]
        public float SizeX
        {
            get => _sizeX;
            set
            {
                _sizeX = value;
                NotifyChange();
            }
        }

        [GISMIgnore]
        public float SizeY
        {
            get => _sizeY;
            set
            {
                _sizeY = value;
                NotifyChange();
            }
        }

        [GISMIgnore]
        public float ScaleX
        {
            get => _scaleX;
            set
            {
                _scaleX = value;
                NotifyChange();
            }
        }

        [GISMIgnore]
        public float ScaleY
        {
            get => _scaleY;
            set
            {
                _scaleY = value;
                NotifyChange();
            }
        }

        [GISMIgnore]
        public float Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                NotifyChange();
            }
        }

        // IGNORE COMPOUND PROPERTIES: These generate completely fresh MonoGame Vector2 structs. 
        // Deserializing into these properties would crash or double-assign values over your raw scalar floats.
        [GISMIgnore]
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

        [GISMIgnore]
        public Vector2 LocalPosition
        {
            get => new Vector2(_xOFF, _yOFF);
            set
            {
                _isSettingOffset = true;
                try
                {
                    _xOFF = value.X;
                    _yOFF = value.Y;
                    NotifyChange();
                }
                finally
                {
                    _isSettingOffset = false;
                }
            }
        }

        [GISMIgnore]
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

        [GISMIgnore]
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

        [Browsable(false)]
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

        // IGNORE READ-ONLY VALUE PROPERTIES
        [Browsable(false)]
        [GISMIgnore]
        public Vector2 OriginVector
        {
            get => GetOriginOffset();
        }

        [Browsable(false)]
        [GISMIgnore]
        public Vector2 RenderTopLeft
        {
            get => WorldPosition - (GetOriginOffset() * Scale);
        }
    }
}