namespace TechCosmos.Spatial2D.Unity
{
    /// <summary>默认世界。场景里有 <see cref="Spatial2DWorld"/> 时用它的实例。</summary>
    public static class Spatial2D
    {
        static SpatialWorld _fallback;

        public static SpatialWorld Default
        {
            get
            {
                if (Spatial2DWorld.Instance != null)
                    return Spatial2DWorld.Instance.World;
                return _fallback ??= new SpatialWorld();
            }
        }
    }
}
