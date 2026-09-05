using UnityEngine;

namespace TechCosmos.Spatial2D.Unity
{
    /// <summary>
    /// 判定区域组件基类：Enable 注册、Disable 注销、Gizmos 画框。
    /// 不自动跟 Transform。移动后请对外面的 <see cref="Body"/> 调 <see cref="SpatialBody.SetPosition"/>，
    /// 或调 <see cref="SyncPosition"/> / <see cref="SyncScale"/> / <see cref="SyncPose"/>。
    /// Scene Gizmo：绿/黄跟 Transform，品红/橙跟 Body 真实判定；对不上就是没 Sync。
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

        /// <summary>用当前 Transform 当原点，加 offset 后写进 Body。不会自己调用。</summary>
        public void SyncPosition()
        {
            var p = transform.position;
            SyncPosition(p.x, p.y);
        }

        /// <summary>
        /// 用给定世界原点（例如 ECS 位置），加上本地 offset 后写进 Body。
        /// offset 按当前 Transform 的缩放/旋转变成世界增量。
        /// </summary>
        public void SyncPosition(float originX, float originY)
        {
            if (Body == null || !Body.IsValid)
                return;
            var delta = transform.TransformVector(new Vector3(offset.x, offset.y, 0f));
            Body.SetPosition(originX + delta.x, originY + delta.y);
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
            DrawBodyGizmos(new Color(0.95f, 0.2f, 0.55f, 0.95f));
        }

        void OnDrawGizmosSelected()
        {
            var c = WorldCenter();
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 1f);
            OnDrawAreaGizmos(new Vector3(c.x, c.y, 0f));
            DrawBodyGizmos(new Color(1f, 0.4f, 0.1f, 1f));
        }

        void DrawBodyGizmos(Color color)
        {
            if (Body == null || !Body.IsValid)
                return;

            Body.GetPosition(out float x, out float y);
            Body.GetExtents(out float a, out float b);
            Gizmos.color = color;
            var center = new Vector3(x, y, 0f);
            if (Body.Shape == TechCosmos.Spatial2D.ShapeType.Circle)
                DrawWireCircle(center, a, 32);
            else
                DrawWireAabb(center, a, b);
        }

        protected static void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            if (radius <= 0f || segments < 3)
                return;

            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            float step = Mathf.PI * 2f / segments;
            for (int i = 1; i <= segments; i++)
            {
                float ang = step * i;
                var next = center + new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        protected static void DrawWireAabb(Vector3 center, float halfWidth, float halfHeight)
        {
            var a = center + new Vector3(-halfWidth, -halfHeight, 0f);
            var b = center + new Vector3(halfWidth, -halfHeight, 0f);
            var c = center + new Vector3(halfWidth, halfHeight, 0f);
            var d = center + new Vector3(-halfWidth, halfHeight, 0f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        protected virtual void OnValidate()
        {
            if (Body != null && Body.IsValid)
                SyncPosition();
        }
    }
}
