namespace TechCosmos.Spatial2D
{
    /// <summary>登记在 <see cref="SpatialWorld"/> 里的一块区域。位置只通过 <see cref="SetPosition"/> 更新。</summary>
    public sealed class SpatialBody
    {
        internal SpatialWorld world;
        internal int index;
        internal int generation;

        public SpatialWorld World => world;

        public bool IsValid => world != null && world.IsAlive(this);

        public ShapeType Shape => world.GetShape(this);

        public int Layer
        {
            get => world.GetLayer(this);
            set => world.SetLayer(this, value);
        }

        public object UserData
        {
            get => world.GetUserData(this);
            set => world.SetUserData(this, value);
        }

        public void GetPosition(out float x, out float y) => world.GetPosition(this, out x, out y);

        public void SetPosition(float x, float y) => world.SetPosition(this, x, y);

        /// <summary>第一版不生效，仅预留。</summary>
        public void SetRotation(float angle) => world.SetRotation(this, angle);

        public float GetRotation() => world.GetRotation(this);

        public void SetCircle(float radius) => world.SetCircle(this, radius);

        public void SetRect(float width, float height) => world.SetRect(this, width, height);
    }
}
