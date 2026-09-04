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

        /// <summary>人的缩放是几就写几。框按 1 倍尺寸乘这个数，不是叠乘。负数当翻转，按绝对值算大小。</summary>
        public void SetScale(float scale) => world.SetScale(this, scale, scale);

        public void SetScale(float scaleX, float scaleY) => world.SetScale(this, scaleX, scaleY);

        public void GetScale(out float scaleX, out float scaleY) => world.GetScale(this, out scaleX, out scaleY);
    }
}
