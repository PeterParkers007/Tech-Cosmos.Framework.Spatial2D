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
            GetDrawScale(out float sx, out _);
            DrawWireCircle(center, radius * sx, 32);
        }

        protected override void OnValidate()
        {
            radius = Mathf.Max(0f, radius);
            if (Body != null && Body.IsValid)
                Body.SetCircle(radius);
            base.OnValidate();
        }

        static void DrawWireCircle(Vector3 center, float r, int segments)
        {
            if (r <= 0f || segments < 3)
                return;

            Vector3 prev = center + new Vector3(r, 0f, 0f);
            float step = Mathf.PI * 2f / segments;
            for (int i = 1; i <= segments; i++)
            {
                float a = step * i;
                var next = center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
