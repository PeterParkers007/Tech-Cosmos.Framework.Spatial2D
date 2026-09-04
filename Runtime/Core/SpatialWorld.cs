using System;
using System.Collections.Generic;

namespace TechCosmos.Spatial2D
{
    /// <summary>2D 空间库。不跑物理；位置由外部 <see cref="SetPosition"/> 写入。</summary>
    public sealed class SpatialWorld
    {
        const int CoordBias = 1 << 20;
        public const int AllLayers = -1;

        struct Slot
        {
            public bool alive;
            public int generation;
            public ShapeType shape;
            public float x, y;
            public float a, b;
            public float restA, restB;
            public float scaleX, scaleY;
            public float angle;
            public int layer;
            public object userData;
            public SpatialBody body;
        }

        readonly float _cellSize;
        readonly List<Slot> _slots = new List<Slot>(64);
        readonly Stack<int> _free = new Stack<int>();
        readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>(128);
        readonly List<List<int>> _bucketPool = new List<List<int>>(32);
        readonly List<int> _scratch = new List<int>(32);
        int _visitStamp = 1;
        int[] _visitMarks = new int[64];

        public SpatialWorld(float cellSize = 4f)
        {
            _cellSize = cellSize > 0.01f ? cellSize : 4f;
        }

        public int BodyCount => _slots.Count - _free.Count;

        public SpatialBody AddCircle(float x, float y, float radius, int layer = 1)
        {
            if (radius < 0f)
                radius = 0f;
            return Add(ShapeType.Circle, x, y, radius, 0f, 0f, layer);
        }

        public SpatialBody AddRect(float x, float y, float width, float height, int layer = 1)
            => AddRect(x, y, width, height, 0f, layer);

        public SpatialBody AddRect(float x, float y, float width, float height, float angle, int layer = 1)
        {
            if (width < 0f) width = 0f;
            if (height < 0f) height = 0f;
            return Add(ShapeType.Rect, x, y, width * 0.5f, height * 0.5f, angle, layer);
        }

        public void Remove(SpatialBody body)
        {
            if (!IsAlive(body))
                return;

            Unindex(body.index);
            Slot slot = _slots[body.index];
            slot.alive = false;
            slot.generation++;
            slot.userData = null;
            slot.body = null;
            _slots[body.index] = slot;
            _free.Push(body.index);
            body.world = null;
        }

        public bool IsAlive(SpatialBody body)
            => body != null
               && ReferenceEquals(body.world, this)
               && (uint)body.index < (uint)_slots.Count
               && _slots[body.index].alive
               && _slots[body.index].generation == body.generation;

        public void SetPosition(SpatialBody body, float x, float y)
        {
            if (!IsAlive(body))
                return;

            int i = body.index;
            Slot slot = _slots[i];
            if (slot.x == x && slot.y == y)
                return;

            Unindex(i);
            slot.x = x;
            slot.y = y;
            _slots[i] = slot;
            Index(i);
        }

        public void GetPosition(SpatialBody body, out float x, out float y)
        {
            if (!IsAlive(body))
            {
                x = 0f;
                y = 0f;
                return;
            }

            Slot slot = _slots[body.index];
            x = slot.x;
            y = slot.y;
        }

        public void SetRotation(SpatialBody body, float angle)
        {
            if (!IsAlive(body))
                return;

            Slot slot = _slots[body.index];
            slot.angle = angle;
            _slots[body.index] = slot;
        }

        public float GetRotation(SpatialBody body)
            => IsAlive(body) ? _slots[body.index].angle : 0f;

        public void SetCircle(SpatialBody body, float radius)
        {
            if (!IsAlive(body) || _slots[body.index].shape != ShapeType.Circle)
                return;
            if (radius < 0f)
                radius = 0f;

            int i = body.index;
            Unindex(i);
            Slot slot = _slots[i];
            slot.restA = radius;
            WriteCurrentSize(ref slot);
            _slots[i] = slot;
            Index(i);
        }

        public void SetRect(SpatialBody body, float width, float height)
        {
            if (!IsAlive(body) || _slots[body.index].shape != ShapeType.Rect)
                return;
            if (width < 0f) width = 0f;
            if (height < 0f) height = 0f;

            int i = body.index;
            Unindex(i);
            Slot slot = _slots[i];
            slot.restA = width * 0.5f;
            slot.restB = height * 0.5f;
            WriteCurrentSize(ref slot);
            _slots[i] = slot;
            Index(i);
        }

        /// <summary>人的缩放是几就写几。框按 1 倍时的尺寸乘这个数。不是叠乘。</summary>
        public void SetScale(SpatialBody body, float scaleX, float scaleY)
        {
            if (!IsAlive(body))
                return;

            int i = body.index;
            Slot slot = _slots[i];
            if (slot.scaleX == scaleX && slot.scaleY == scaleY)
                return;

            Unindex(i);
            slot.scaleX = scaleX;
            slot.scaleY = scaleY;
            WriteCurrentSize(ref slot);
            _slots[i] = slot;
            Index(i);
        }

        public void GetScale(SpatialBody body, out float scaleX, out float scaleY)
        {
            if (!IsAlive(body))
            {
                scaleX = 1f;
                scaleY = 1f;
                return;
            }

            Slot slot = _slots[body.index];
            scaleX = slot.scaleX;
            scaleY = slot.scaleY;
        }

        public ShapeType GetShape(SpatialBody body)
            => IsAlive(body) ? _slots[body.index].shape : ShapeType.Circle;

        public int GetLayer(SpatialBody body)
            => IsAlive(body) ? _slots[body.index].layer : 0;

        public void SetLayer(SpatialBody body, int layer)
        {
            if (!IsAlive(body))
                return;
            Slot slot = _slots[body.index];
            slot.layer = layer;
            _slots[body.index] = slot;
        }

        public object GetUserData(SpatialBody body)
            => IsAlive(body) ? _slots[body.index].userData : null;

        public void SetUserData(SpatialBody body, object userData)
        {
            if (!IsAlive(body))
                return;
            Slot slot = _slots[body.index];
            slot.userData = userData;
            _slots[body.index] = slot;
        }

        public List<SpatialHit> OverlapPoint(float x, float y, int mask = AllLayers)
        {
            var hits = new List<SpatialHit>();
            OverlapPoint(x, y, hits, mask);
            return hits;
        }

        public void OverlapPoint(float x, float y, List<SpatialHit> hits, int mask = AllLayers)
        {
            if (hits == null)
                throw new ArgumentNullException(nameof(hits));
            hits.Clear();
            Collect(x, y, x, y);
            for (int n = 0; n < _scratch.Count; n++)
            {
                Slot s = _slots[_scratch[n]];
                if (!PassMask(s.layer, mask) || !PointHits(s, x, y))
                    continue;
                hits.Add(MakeOverlapHit(s, x, y));
            }
        }

        public List<SpatialHit> OverlapCircle(float x, float y, float radius, int mask = AllLayers)
        {
            var hits = new List<SpatialHit>();
            OverlapCircle(x, y, radius, hits, mask);
            return hits;
        }

        public void OverlapCircle(float x, float y, float radius, List<SpatialHit> hits, int mask = AllLayers)
        {
            if (hits == null)
                throw new ArgumentNullException(nameof(hits));
            hits.Clear();
            if (radius < 0f)
                radius = 0f;
            Collect(x - radius, y - radius, x + radius, y + radius);
            for (int n = 0; n < _scratch.Count; n++)
            {
                Slot s = _slots[_scratch[n]];
                if (!PassMask(s.layer, mask) || !CircleHits(s, x, y, radius))
                    continue;
                hits.Add(MakeOverlapHit(s, x, y));
            }
        }

        public List<SpatialHit> OverlapRect(float x, float y, float width, float height, int mask = AllLayers)
        {
            var hits = new List<SpatialHit>();
            OverlapRect(x, y, width, height, hits, mask);
            return hits;
        }

        public void OverlapRect(
            float x, float y, float width, float height, List<SpatialHit> hits, int mask = AllLayers)
        {
            if (hits == null)
                throw new ArgumentNullException(nameof(hits));
            hits.Clear();
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            if (hw < 0f) hw = 0f;
            if (hh < 0f) hh = 0f;
            Collect(x - hw, y - hh, x + hw, y + hh);
            for (int n = 0; n < _scratch.Count; n++)
            {
                Slot s = _slots[_scratch[n]];
                if (!PassMask(s.layer, mask) || !RectHits(s, x, y, hw, hh))
                    continue;
                hits.Add(MakeOverlapHit(s, x, y));
            }
        }

        public List<SpatialBody> QueryRect(float x, float y, float width, float height, int mask = AllLayers)
        {
            var bodies = new List<SpatialBody>();
            QueryRect(x, y, width, height, bodies, mask);
            return bodies;
        }

        public void QueryRect(
            float x, float y, float width, float height, List<SpatialBody> bodies, int mask = AllLayers)
        {
            if (bodies == null)
                throw new ArgumentNullException(nameof(bodies));
            bodies.Clear();
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            Collect(x - hw, y - hh, x + hw, y + hh);
            for (int n = 0; n < _scratch.Count; n++)
            {
                Slot s = _slots[_scratch[n]];
                if (!PassMask(s.layer, mask) || !RectHits(s, x, y, hw, hh))
                    continue;
                bodies.Add(s.body);
            }
        }

        public SpatialHit CastCircle(float x, float y, float radius, float dirX, float dirY, int mask = AllLayers)
            => Cast(x, y, radius, 0f, ShapeType.Circle, dirX, dirY, mask);

        public SpatialHit CastRect(
            float x, float y, float width, float height, float dirX, float dirY, int mask = AllLayers)
            => Cast(x, y, width * 0.5f, height * 0.5f, ShapeType.Rect, dirX, dirY, mask);

        SpatialBody Add(ShapeType shape, float x, float y, float a, float b, float angle, int layer)
        {
            int index;
            int generation = 1;
            if (_free.Count > 0)
            {
                index = _free.Pop();
                generation = _slots[index].generation;
                if (generation == 0)
                    generation = 1;
            }
            else
            {
                index = _slots.Count;
                _slots.Add(default);
            }

            var body = new SpatialBody
            {
                world = this,
                index = index,
                generation = generation
            };

            _slots[index] = new Slot
            {
                alive = true,
                generation = generation,
                shape = shape,
                x = x,
                y = y,
                a = a,
                b = b,
                restA = a,
                restB = b,
                scaleX = 1f,
                scaleY = 1f,
                angle = angle,
                layer = layer,
                body = body
            };
            Index(index);
            return body;
        }

        SpatialHit Cast(
            float x, float y, float a, float b, ShapeType shape, float dirX, float dirY, int mask)
        {
            SweepBounds(x, y, a, b, shape, dirX, dirY, out float minX, out float minY, out float maxX, out float maxY);
            Collect(minX, minY, maxX, maxY);

            float bestT = 2f;
            int best = -1;
            float bestNx = 0f;
            float bestNy = 0f;

            for (int n = 0; n < _scratch.Count; n++)
            {
                int i = _scratch[n];
                Slot s = _slots[i];
                if (!PassMask(s.layer, mask))
                    continue;
                if (!CastAgainst(s, x, y, a, b, shape, dirX, dirY, out float t, out float nx, out float ny))
                    continue;
                if (t >= bestT)
                    continue;
                bestT = t;
                best = i;
                bestNx = nx;
                bestNy = ny;
            }

            if (best < 0)
                return default;

            return new SpatialHit
            {
                other = _slots[best].body,
                pointX = x + dirX * bestT,
                pointY = y + dirY * bestT,
                normalX = bestNx,
                normalY = bestNy,
                distance = bestT * (float)Math.Sqrt(dirX * dirX + dirY * dirY)
            };
        }

        static bool CastAgainst(
            Slot s, float x, float y, float a, float b, ShapeType shape,
            float dirX, float dirY, out float t, out float nx, out float ny)
        {
            nx = 0f;
            ny = 0f;
            t = 0f;

            if (shape == ShapeType.Circle)
            {
                if (s.shape == ShapeType.Circle)
                {
                    if (!SpatialMath.RayCircle(x, y, dirX, dirY, s.x, s.y, a + s.a, out t))
                        return false;
                    FillNormalFromCenters(x + dirX * t, y + dirY * t, s.x, s.y, out nx, out ny);
                    return true;
                }

                if (!SpatialMath.RayAabb(
                        x, y, dirX, dirY,
                        s.x - s.a - a, s.y - s.b - a, s.x + s.a + a, s.y + s.b + a,
                        out t, out nx, out ny))
                    return false;
                return true;
            }

            if (s.shape == ShapeType.Circle)
            {
                if (!SpatialMath.RayCircle(s.x, s.y, -dirX, -dirY, x, y, a + s.a, out t))
                    return false;
                FillNormalFromCenters(x + dirX * t, y + dirY * t, s.x, s.y, out nx, out ny);
                return true;
            }

            return SpatialMath.RayAabb(
                x, y, dirX, dirY,
                s.x - s.a - a, s.y - s.b - b, s.x + s.a + a, s.y + s.b + b,
                out t, out nx, out ny);
        }

        static void FillNormalFromCenters(float px, float py, float cx, float cy, out float nx, out float ny)
        {
            nx = px - cx;
            ny = py - cy;
            float len = (float)Math.Sqrt(nx * nx + ny * ny);
            if (len < SpatialMath.Eps)
            {
                nx = 1f;
                ny = 0f;
                return;
            }

            nx /= len;
            ny /= len;
        }

        static void SweepBounds(
            float x, float y, float a, float b, ShapeType shape,
            float dirX, float dirY,
            out float minX, out float minY, out float maxX, out float maxY)
        {
            float hw = shape == ShapeType.Circle ? a : a;
            float hh = shape == ShapeType.Circle ? a : b;
            minX = Math.Min(x, x + dirX) - hw;
            maxX = Math.Max(x, x + dirX) + hw;
            minY = Math.Min(y, y + dirY) - hh;
            maxY = Math.Max(y, y + dirY) + hh;
        }

        static bool PointHits(Slot s, float x, float y)
            => s.shape == ShapeType.Circle
                ? SpatialMath.CircleContains(s.x, s.y, s.a, x, y)
                : SpatialMath.AabbContains(s.x, s.y, s.a, s.b, x, y);

        static bool CircleHits(Slot s, float x, float y, float radius)
            => s.shape == ShapeType.Circle
                ? SpatialMath.CircleCircle(x, y, radius, s.x, s.y, s.a)
                : SpatialMath.CircleAabb(x, y, radius, s.x, s.y, s.a, s.b);

        static bool RectHits(Slot s, float x, float y, float hw, float hh)
            => s.shape == ShapeType.Circle
                ? SpatialMath.CircleAabb(s.x, s.y, s.a, x, y, hw, hh)
                : SpatialMath.AabbAabb(x, y, hw, hh, s.x, s.y, s.a, s.b);

        static SpatialHit MakeOverlapHit(Slot s, float fromX, float fromY)
        {
            float px, py, nx, ny, dist;
            if (s.shape == ShapeType.Circle)
            {
                nx = fromX - s.x;
                ny = fromY - s.y;
                float len = (float)Math.Sqrt(nx * nx + ny * ny);
                if (len < SpatialMath.Eps)
                {
                    nx = 1f;
                    ny = 0f;
                    len = 0f;
                }
                else
                {
                    nx /= len;
                    ny /= len;
                }

                px = s.x + nx * s.a;
                py = s.y + ny * s.a;
                dist = s.a - len;
                if (dist < 0f)
                    dist = 0f;
            }
            else
            {
                SpatialMath.ClosestOnAabb(fromX, fromY, s.x, s.y, s.a, s.b, out px, out py);
                nx = fromX - px;
                ny = fromY - py;
                float len = (float)Math.Sqrt(nx * nx + ny * ny);
                if (len < SpatialMath.Eps)
                {
                    nx = 1f;
                    ny = 0f;
                    dist = 0f;
                }
                else
                {
                    nx /= len;
                    ny /= len;
                    dist = 0f;
                }
            }

            return new SpatialHit
            {
                other = s.body,
                pointX = px,
                pointY = py,
                normalX = nx,
                normalY = ny,
                distance = dist
            };
        }

        static bool PassMask(int layer, int mask)
            => mask == AllLayers || (layer & mask) != 0;

        static float Abs(float v) => v < 0f ? -v : v;

        static void WriteCurrentSize(ref Slot slot)
        {
            float sx = Abs(slot.scaleX);
            float sy = Abs(slot.scaleY);
            if (slot.shape == ShapeType.Circle)
            {
                slot.a = slot.restA * sx;
                slot.b = 0f;
                return;
            }

            slot.a = slot.restA * sx;
            slot.b = slot.restB * sy;
        }

        void Index(int i)
        {
            Slot s = _slots[i];
            BodyBounds(s, out float minX, out float minY, out float maxX, out float maxY);
            int x0 = CellCoord(minX);
            int x1 = CellCoord(maxX);
            int y0 = CellCoord(minY);
            int y1 = CellCoord(maxY);
            for (int cx = x0; cx <= x1; cx++)
            {
                for (int cy = y0; cy <= y1; cy++)
                    Cell(Pack(cx, cy)).Add(i);
            }
        }

        void Unindex(int i)
        {
            Slot s = _slots[i];
            BodyBounds(s, out float minX, out float minY, out float maxX, out float maxY);
            int x0 = CellCoord(minX);
            int x1 = CellCoord(maxX);
            int y0 = CellCoord(minY);
            int y1 = CellCoord(maxY);
            for (int cx = x0; cx <= x1; cx++)
            {
                for (int cy = y0; cy <= y1; cy++)
                {
                    long key = Pack(cx, cy);
                    if (!_cells.TryGetValue(key, out var bucket))
                        continue;
                    bucket.Remove(i);
                    if (bucket.Count == 0)
                    {
                        _cells.Remove(key);
                        _bucketPool.Add(bucket);
                    }
                }
            }
        }

        void Collect(float minX, float minY, float maxX, float maxY)
        {
            _scratch.Clear();
            _visitStamp++;
            if (_visitStamp == int.MaxValue)
            {
                Array.Clear(_visitMarks, 0, _visitMarks.Length);
                _visitStamp = 1;
            }

            int x0 = CellCoord(minX);
            int x1 = CellCoord(maxX);
            int y0 = CellCoord(minY);
            int y1 = CellCoord(maxY);
            for (int cx = x0; cx <= x1; cx++)
            {
                for (int cy = y0; cy <= y1; cy++)
                {
                    if (!_cells.TryGetValue(Pack(cx, cy), out var bucket))
                        continue;
                    for (int n = 0; n < bucket.Count; n++)
                    {
                        int i = bucket[n];
                        if ((uint)i >= (uint)_slots.Count || !_slots[i].alive)
                            continue;
                        EnsureVisitCapacity(i);
                        if (_visitMarks[i] == _visitStamp)
                            continue;
                        _visitMarks[i] = _visitStamp;
                        _scratch.Add(i);
                    }
                }
            }
        }

        void EnsureVisitCapacity(int i)
        {
            if (i < _visitMarks.Length)
                return;
            int cap = _visitMarks.Length;
            while (cap <= i)
                cap *= 2;
            Array.Resize(ref _visitMarks, cap);
        }

        static void BodyBounds(Slot s, out float minX, out float minY, out float maxX, out float maxY)
        {
            float hw = s.shape == ShapeType.Circle ? s.a : s.a;
            float hh = s.shape == ShapeType.Circle ? s.a : s.b;
            minX = s.x - hw;
            maxX = s.x + hw;
            minY = s.y - hh;
            maxY = s.y + hh;
        }

        List<int> Cell(long key)
        {
            if (_cells.TryGetValue(key, out var bucket))
                return bucket;
            if (_bucketPool.Count > 0)
            {
                bucket = _bucketPool[_bucketPool.Count - 1];
                _bucketPool.RemoveAt(_bucketPool.Count - 1);
            }
            else
            {
                bucket = new List<int>(4);
            }

            _cells[key] = bucket;
            return bucket;
        }

        int CellCoord(float v) => (int)Math.Floor(v / _cellSize);

        static long Pack(int x, int y)
        {
            uint ux = (uint)(x + CoordBias);
            uint uy = (uint)(y + CoordBias);
            return ((long)ux << 32) | uy;
        }
    }
}
