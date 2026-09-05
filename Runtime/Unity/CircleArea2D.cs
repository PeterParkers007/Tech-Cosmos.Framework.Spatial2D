using UnityEngine;

namespace TechCosmos.Spatial2D.Unity
{
    [AddComponentMenu("Tech-Cosmos/Spatial2D/Circle Area 2D")]
    public sealed class CircleArea2D : Area2D
    {
        [SerializeField, Min(0f)] float radius = 0.5f;

        public float Radius
        {
            get => radius;
            set
            {
                radius = Mathf.Max(0f, value);
                if (Body != null && Body.IsValid)
                    Body.SetCircle(radius);
            }
        }

        protected override SpatialBody Register(SpatialWorld world, float x, float y)
            => world.AddCircle(x, y, radius, Layer);

        protected override void OnDrawAreaGizmos(Vector3 center)
        {
            float sx = Mathf.Abs(transform.lossyScale.x);
            DrawWireCircle(center, radius * sx, 32);
        }

        protected override void OnValidate()
        {
            radius = Mathf.Max(0f, radius);
            if (Body != null && Body.IsValid)
                Body.SetCircle(radius);
            base.OnValidate();
        }
    }
}
