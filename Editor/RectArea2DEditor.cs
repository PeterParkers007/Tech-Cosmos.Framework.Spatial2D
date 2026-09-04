using UnityEditor;
using UnityEngine;

namespace TechCosmos.Spatial2D.Unity.Editor
{
    [CustomEditor(typeof(RectArea2D))]
    [CanEditMultipleObjects]
    sealed class RectArea2DEditor : UnityEditor.Editor
    {
        static readonly int RightId = "TechCosmos.Spatial2D.Rect.Right".GetHashCode();
        static readonly int LeftId = "TechCosmos.Spatial2D.Rect.Left".GetHashCode();
        static readonly int TopId = "TechCosmos.Spatial2D.Rect.Top".GetHashCode();
        static readonly int BottomId = "TechCosmos.Spatial2D.Rect.Bottom".GetHashCode();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(4f);
            Area2DSceneHandles.DrawEditToggle();
            EditorGUILayout.HelpBox("选中后在 Scene 里拖四条边中间的点改大小，对边不动。宽高按缩放 1 时填写。", MessageType.None);
        }

        void OnSceneGUI()
        {
            if (!Area2DSceneHandles.IsEditing)
                return;

            var area = (RectArea2D)target;
            if (area == null)
                return;

            area.GetDrawScale(out float sx, out float sy);
            sx = Area2DSceneHandles.SafeScale(Mathf.Abs(sx));
            sy = Area2DSceneHandles.SafeScale(Mathf.Abs(sy));

            Vector3 center = Area2DSceneHandles.Center3(area);
            float hw = area.Width * 0.5f * sx;
            float hh = area.Height * 0.5f * sy;

            Vector3 right = center + new Vector3(hw, 0f, 0f);
            Vector3 left = center + new Vector3(-hw, 0f, 0f);
            Vector3 top = center + new Vector3(0f, hh, 0f);
            Vector3 bottom = center + new Vector3(0f, -hh, 0f);

            Area2DSceneHandles.BeginSceneDraw();

            if (Area2DSceneHandles.DragAlong(RightId, right, Vector3.right, out Vector3 nextRight))
            {
                ApplyHorizontal(area, center.x - hw, nextRight.x, center.y, sx);
                return;
            }

            if (Area2DSceneHandles.DragAlong(LeftId, left, Vector3.right, out Vector3 nextLeft))
            {
                ApplyHorizontal(area, nextLeft.x, center.x + hw, center.y, sx);
                return;
            }

            if (Area2DSceneHandles.DragAlong(TopId, top, Vector3.up, out Vector3 nextTop))
            {
                ApplyVertical(area, nextTop.y, center.y - hh, center.x, sy);
                return;
            }

            if (Area2DSceneHandles.DragAlong(BottomId, bottom, Vector3.up, out Vector3 nextBottom))
            {
                ApplyVertical(area, center.y + hh, nextBottom.y, center.x, sy);
            }
        }

        static void ApplyHorizontal(
            RectArea2D area, float worldLeft, float worldRight, float worldY, float sx)
        {
            float worldW = Mathf.Abs(worldRight - worldLeft);
            float worldX = (worldLeft + worldRight) * 0.5f;
            Apply(area, worldW / sx, area.Height, worldX, worldY);
        }

        static void ApplyVertical(
            RectArea2D area, float worldTop, float worldBottom, float worldX, float sy)
        {
            float worldH = Mathf.Abs(worldTop - worldBottom);
            float worldY = (worldTop + worldBottom) * 0.5f;
            Apply(area, area.Width, worldH / sy, worldX, worldY);
        }

        static void Apply(RectArea2D area, float width, float height, float worldX, float worldY)
        {
            var so = new SerializedObject(area);
            so.Update();
            so.FindProperty("width").floatValue = Mathf.Max(0f, width);
            so.FindProperty("height").floatValue = Mathf.Max(0f, height);
            so.FindProperty("offset").vector2Value = Area2DSceneHandles.WorldToOffset(area, worldX, worldY);
            so.ApplyModifiedProperties();
        }
    }
}
