using UnityEditor;
using UnityEngine;

namespace TechCosmos.Spatial2D.Unity.Editor
{
    [CustomEditor(typeof(CircleArea2D))]
    [CanEditMultipleObjects]
    sealed class CircleArea2DEditor : UnityEditor.Editor
    {
        static readonly int RightId = "TechCosmos.Spatial2D.Circle.Right".GetHashCode();
        static readonly int LeftId = "TechCosmos.Spatial2D.Circle.Left".GetHashCode();
        static readonly int TopId = "TechCosmos.Spatial2D.Circle.Top".GetHashCode();
        static readonly int BottomId = "TechCosmos.Spatial2D.Circle.Bottom".GetHashCode();
        static readonly int CenterId = "TechCosmos.Spatial2D.Circle.Center".GetHashCode();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(4f);
            Area2DSceneHandles.DrawEditToggle();
            EditorGUILayout.HelpBox("拖上下左右：对边不动，圆心跟着走。拖中间：只挪位置。半径按缩放 1 时填写。", MessageType.None);
        }

        void OnSceneGUI()
        {
            if (!Area2DSceneHandles.IsEditing)
                return;

            var area = (CircleArea2D)target;
            if (area == null)
                return;

            area.GetDrawScale(out float sx, out _);
            sx = Area2DSceneHandles.SafeScale(Mathf.Abs(sx));

            Vector3 center = Area2DSceneHandles.Center3(area);
            float worldR = area.Radius * sx;
            float handleR = worldR;
            float minR = Area2DSceneHandles.HandleSize(center) * 0.5f;
            if (handleR < minR)
                handleR = minR;

            Vector3 right = center + new Vector3(handleR, 0f, 0f);
            Vector3 left = center + new Vector3(-handleR, 0f, 0f);
            Vector3 top = center + new Vector3(0f, handleR, 0f);
            Vector3 bottom = center + new Vector3(0f, -handleR, 0f);

            float worldLeft = center.x - worldR;
            float worldRight = center.x + worldR;
            float worldTop = center.y + worldR;
            float worldBottom = center.y - worldR;

            Area2DSceneHandles.BeginSceneDraw();

            if (Area2DSceneHandles.DragAlong(RightId, right, Vector3.right, out Vector3 nextRight))
            {
                ApplyDiameter(area, worldLeft, nextRight.x, center.y, center.y, sx);
                return;
            }

            if (Area2DSceneHandles.DragAlong(LeftId, left, Vector3.right, out Vector3 nextLeft))
            {
                ApplyDiameter(area, nextLeft.x, worldRight, center.y, center.y, sx);
                return;
            }

            if (Area2DSceneHandles.DragAlong(TopId, top, Vector3.up, out Vector3 nextTop))
            {
                ApplyDiameter(area, center.x, center.x, nextTop.y, worldBottom, sx);
                return;
            }

            if (Area2DSceneHandles.DragAlong(BottomId, bottom, Vector3.up, out Vector3 nextBottom))
            {
                ApplyDiameter(area, center.x, center.x, worldTop, nextBottom.y, sx);
                return;
            }

            if (Area2DSceneHandles.DragFree(CenterId, center, out Vector3 nextCenter))
                Apply(area, area.Radius, nextCenter.x, nextCenter.y);
        }

        static void ApplyDiameter(
            CircleArea2D area, float worldLeft, float worldRight, float worldTop, float worldBottom, float sx)
        {
            float worldW = Mathf.Abs(worldRight - worldLeft);
            float worldH = Mathf.Abs(worldTop - worldBottom);
            float worldD = worldW > worldH ? worldW : worldH;
            float worldX = (worldLeft + worldRight) * 0.5f;
            float worldY = (worldTop + worldBottom) * 0.5f;
            Apply(area, worldD * 0.5f / sx, worldX, worldY);
        }

        static void Apply(CircleArea2D area, float radius, float worldX, float worldY)
        {
            var so = new SerializedObject(area);
            so.Update();
            so.FindProperty("radius").floatValue = Mathf.Max(0f, radius);
            so.FindProperty("offset").vector2Value = Area2DSceneHandles.WorldToOffset(area, worldX, worldY);
            so.ApplyModifiedProperties();
        }
    }
}
