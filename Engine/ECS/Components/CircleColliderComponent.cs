using nkast.Aether.Physics2D.Collision.Shapes;

namespace Engine.Core.ECS.Components
{
    public class CircleColliderComponent : ColliderComponent
    {
        private float _radius = 20f;

        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        public override void CreateShape(float pixelsPerMeter)
        {
            float safePpm = pixelsPerMeter <= 0f ? 64f : pixelsPerMeter;
            float radiusInMeters = _radius / safePpm;

            this.shape = new CircleShape(radiusInMeters, Density);
        }
    }
}