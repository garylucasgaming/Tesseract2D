using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Engine.Core.ECS.Components
{
    public class TransformComponent : GameComponent
    {

        // 1. Raw layout fields that serialize perfectly as primitive floats
        public float X { get; set; } = 0.0f;
        public float Y { get; set; } = 0.0f;
        public float ScaleX { get; set; } = 1.0f;
        public float ScaleY { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0.0f;
    

        // Pointer link tracking the structural parent's spatial matrix
        [JsonIgnore]
        public TransformComponent? ParentTransform
        {
            get; set;
        }

        [JsonIgnore]
        public Vector2 LocalPosition
        {
            get => new Vector2(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// Evaluates absolute world coordinates cleanly by climbing up the tree recursively.
        /// </summary>
        [JsonIgnore]
        public Vector2 WorldPosition
        {
            get
            {
                if(ParentTransform == null)
                    return LocalPosition;

                return ParentTransform.WorldPosition + LocalPosition;
            }
        }
    }
}
