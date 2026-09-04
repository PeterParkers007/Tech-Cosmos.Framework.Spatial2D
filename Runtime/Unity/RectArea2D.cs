using UnityEngine;

namespace TechCosmos.Spatial2D.Unity
{
    [AddComponentMenu("Tech-Cosmos/Spatial2D/Rect Area 2D")]
    public sealed class RectArea2D : Area2D
    {
        [SerializeField, Min(0f)] float width = 1f;
        [SerializeField, Min(0f)] float height = 1f;
        [SerializeField] float angle;

        public float Width
        {
            get => width;
            set
            {
                width = Mathf.Max(0f, value);
                PushSize();
            }
        }

        public float Height
        {
            get => height;
            set
            {
                height = Mathf.Max(0f, value);
                PushSize();
            }
        }

        public float Angle
        {
            get => angle;
            set
            {
                angle = value;
                if (Body != null && Body.IsValid)
                    Body.SetRotation(angle);
            }
        }

        protected override SpatialBody Register(SpatialWorld world, float x, float y)
        {
            var body = world.AddRect(x, y, width, height, angle, Layer);
            return body;
        }

        protected override void OnDrawAreaGizmos(Vector3 center)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            var a = center + new Vector3(-hw, -hh, 0f);
            var b = center + new Vector3(hw, -hh, 0f);
            var c = center + new Vector3(hw, hh, 0f);
            var d = center + new Vector3(-hw, hh, 0f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        void PushSize()
        {
            if (Body != null && Body.IsValid)
                Body.SetRect(width, height);
        }
    }
}
