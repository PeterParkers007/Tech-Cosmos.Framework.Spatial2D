using UnityEngine;

namespace TechCosmos.Spatial2D.Unity
{
    public static class SpatialBodyExtensions
    {
        public static void SetPosition(this SpatialBody body, Vector2 position)
        {
            if (body == null)
                return;
            body.SetPosition(position.x, position.y);
        }

        public static Vector2 GetPosition(this SpatialBody body)
        {
            if (body == null)
                return Vector2.zero;
            body.GetPosition(out float x, out float y);
            return new Vector2(x, y);
        }

        public static void SetScale(this SpatialBody body, Vector2 scale)
        {
            if (body == null)
                return;
            body.SetScale(scale.x, scale.y);
        }

        public static Vector2 GetScale(this SpatialBody body)
        {
            if (body == null)
                return Vector2.one;
            body.GetScale(out float x, out float y);
            return new Vector2(x, y);
        }
    }
}
