namespace TechCosmos.Spatial2D
{
    public struct SpatialHit
    {
        public SpatialBody other;
        public float pointX;
        public float pointY;
        public float normalX;
        public float normalY;
        public float distance;

        public bool IsValid => other != null && other.IsValid;
    }
}
