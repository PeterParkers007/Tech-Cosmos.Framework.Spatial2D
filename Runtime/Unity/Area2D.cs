using UnityEngine;

namespace TechCosmos.Spatial2D.Unity
{
    /// <summary>
    /// 判定区域组件基类：Enable 注册、Disable 注销、Gizmos 画框。
    /// 不自动跟 Transform。移动后请对外面的 <see cref="Body"/> 调 <see cref="SpatialBody.SetPosition"/>，
    /// 或调 <see cref="SyncPosition"/> / <see cref="SyncScale"/> / <see cref="SyncPose"/>。
    /// </summary>
    public abstract class Area2D : MonoBehaviour
    {
        [SerializeField] Spatial2DWorld worldOverride;
        [SerializeField] Vector2 offset;
        [SerializeField] int layer = 1;

        public SpatialBody Body { get; private set; }

        public int Layer
        {
            get => layer;
            set
            {
                layer = value;
                if (Body != null && Body.IsValid)
                    Body.Layer = value;
            }
        }

        public Vector2 Offset
        {
            get => offset;
            set
            {
                offset = value;
                if (Body != null && Body.IsValid)
                    SyncPosition();
            }
        }

        protected SpatialWorld ResolveWorld()
        {
            if (worldOverride != null && worldOverride.World != null)
                return worldOverride.World;
            return Spatial2D.Default;
        }

        public Vector2 WorldCenter()
        {
            var p = transform.TransformPoint(new Vector3(offset.x, offset.y, 0f));
            return new Vector2(p.x, p.y);
        }

        /// <summary>只把当前位置写进 Body。不会自己调用。</summary>
        public void SyncPosition()
        {
            if (Body == null || !Body.IsValid)
                return;
            var c = WorldCenter();
            Body.SetPosition(c.x, c.y);
        }

        /// <summary>只把当前缩放写进 Body。不会自己调用。人的缩放是几，框就是几。</summary>
        public void SyncScale()
        {
            if (Body == null || !Body.IsValid)
                return;
            var s = transform.lossyScale;
            Body.SetScale(s.x, s.y);
        }

        /// <summary>位置 + 缩放一起写进 Body。不会自己调用。</summary>
        public void SyncPose()
        {
            SyncPosition();
            SyncScale();
        }

        public void GetDrawScale(out float scaleX, out float scaleY)
        {
            if (Body != null && Body.IsValid)
            {
                Body.GetScale(out scaleX, out scaleY);
                scaleX = Mathf.Abs(scaleX);
                scaleY = Mathf.Abs(scaleY);
                return;
            }

            var s = transform.lossyScale;
            scaleX = Mathf.Abs(s.x);
            scaleY = Mathf.Abs(s.y);
        }

        protected abstract SpatialBody Register(SpatialWorld world, float x, float y);

        protected abstract void OnDrawAreaGizmos(Vector3 center);

        void OnEnable()
        {
            var c = WorldCenter();
            Body = Register(ResolveWorld(), c.x, c.y);
            if (Body == null)
                return;
            Body.Layer = layer;
            Body.UserData = this;
            SyncScale();
        }

        void OnDisable()
        {
            if (Body != null && Body.IsValid)
                Body.World.Remove(Body);
            Body = null;
        }

        void OnDrawGizmos()
        {
            var c = WorldCenter();
            Gizmos.color = new Color(0.2f, 0.85f, 0.45f, 0.9f);
            OnDrawAreaGizmos(new Vector3(c.x, c.y, 0f));
        }

        void OnDrawGizmosSelected()
        {
            var c = WorldCenter();
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 1f);
            OnDrawAreaGizmos(new Vector3(c.x, c.y, 0f));
        }

        protected virtual void OnValidate()
        {
            if (Body != null && Body.IsValid)
                SyncPosition();
        }
    }
}
