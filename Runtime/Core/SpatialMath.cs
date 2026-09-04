using System;

namespace TechCosmos.Spatial2D
{
    static class SpatialMath
    {
        public const float Eps = 1e-8f;

        public static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public static bool CircleContains(float cx, float cy, float r, float px, float py)
        {
            float dx = px - cx;
            float dy = py - cy;
            return dx * dx + dy * dy <= r * r;
        }

        public static bool AabbContains(float cx, float cy, float hw, float hh, float px, float py)
            => Math.Abs(px - cx) <= hw && Math.Abs(py - cy) <= hh;

        public static bool CircleCircle(float ax, float ay, float ar, float bx, float by, float br)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float rr = ar + br;
            return dx * dx + dy * dy <= rr * rr;
        }

        public static bool CircleAabb(float cx, float cy, float r, float rx, float ry, float hw, float hh)
        {
            float nx = Clamp(cx, rx - hw, rx + hw);
            float ny = Clamp(cy, ry - hh, ry + hh);
            float dx = cx - nx;
            float dy = cy - ny;
            return dx * dx + dy * dy <= r * r;
        }

        public static bool AabbAabb(
            float ax, float ay, float ahx, float ahy,
            float bx, float by, float bhx, float bhy)
            => Math.Abs(ax - bx) <= ahx + bhx && Math.Abs(ay - by) <= ahy + bhy;

        public static void ClosestOnAabb(
            float px, float py, float cx, float cy, float hw, float hh,
            out float nx, out float ny)
        {
            nx = Clamp(px, cx - hw, cx + hw);
            ny = Clamp(py, cy - hh, cy + hh);
        }

        public static bool RayCircle(
            float ox, float oy, float dx, float dy,
            float cx, float cy, float r,
            out float t)
        {
            float fx = ox - cx;
            float fy = oy - cy;
            float a = dx * dx + dy * dy;
            float c = fx * fx + fy * fy - r * r;
            if (c <= 0f)
            {
                t = 0f;
                return true;
            }

            if (a < Eps)
            {
                t = 0f;
                return false;
            }

            float b = fx * dx + fy * dy;
            float disc = b * b - a * c;
            if (disc < 0f)
            {
                t = 0f;
                return false;
            }

            t = (-b - (float)Math.Sqrt(disc)) / a;
            return t >= 0f && t <= 1f;
        }

        public static bool RayAabb(
            float ox, float oy, float dx, float dy,
            float minX, float minY, float maxX, float maxY,
            out float t, out float nx, out float ny)
        {
            nx = 0f;
            ny = 0f;
            if (ox >= minX && ox <= maxX && oy >= minY && oy <= maxY)
            {
                t = 0f;
                return true;
            }

            float tMin = 0f;
            float tMax = 1f;
            int hitAxis = -1;
            float hitSign = 0f;

            if (!ClipAxis(ox, dx, minX, maxX, ref tMin, ref tMax, 0, ref hitAxis, ref hitSign))
            {
                t = 0f;
                return false;
            }

            if (!ClipAxis(oy, dy, minY, maxY, ref tMin, ref tMax, 1, ref hitAxis, ref hitSign))
            {
                t = 0f;
                return false;
            }

            t = tMin;
            if (hitAxis == 0) nx = hitSign;
            else if (hitAxis == 1) ny = hitSign;
            return t >= 0f && t <= 1f;
        }

        static bool ClipAxis(
            float origin, float dir, float min, float max,
            ref float tMin, ref float tMax,
            int axis, ref int hitAxis, ref float hitSign)
        {
            if (Math.Abs(dir) < Eps)
                return origin >= min && origin <= max;

            float inv = 1f / dir;
            float t1 = (min - origin) * inv;
            float t2 = (max - origin) * inv;
            float sign = -1f;
            if (t1 > t2)
            {
                float tmp = t1;
                t1 = t2;
                t2 = tmp;
                sign = 1f;
            }
            else
            {
                sign = -1f;
            }

            if (t1 > tMin)
            {
                tMin = t1;
                hitAxis = axis;
                hitSign = sign;
            }

            if (t2 < tMax)
                tMax = t2;

            return tMin <= tMax;
        }
    }
}
