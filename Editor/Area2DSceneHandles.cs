using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TechCosmos.Spatial2D.Unity.Editor
{
    static class Area2DSceneHandles
    {
        const string EditKey = "TechCosmos.Spatial2D.EditArea";

        public static readonly Color HandleColor = new Color(0.2f, 0.85f, 0.45f, 1f);

        public static bool IsEditing => SessionState.GetBool(EditKey, true);

        public static bool DrawEditToggle()
        {
            bool edit = SessionState.GetBool(EditKey, true);
            bool next = EditorGUILayout.Toggle("编辑区域", edit);
            if (next != edit)
                SessionState.SetBool(EditKey, next);
            return next;
        }

        public static Vector3 Center3(Area2D area)
        {
            var c = area.WorldCenter();
            var world = area.transform.TransformPoint(new Vector3(area.Offset.x, area.Offset.y, 0f));
            return new Vector3(c.x, c.y, world.z);
        }

        public static Vector2 WorldToOffset(Area2D area, float worldX, float worldY)
        {
            var world = area.transform.TransformPoint(new Vector3(area.Offset.x, area.Offset.y, 0f));
            var local = area.transform.InverseTransformPoint(new Vector3(worldX, worldY, world.z));
            return new Vector2(local.x, local.y);
        }

        public static float HandleSize(Vector3 position)
            => HandleUtility.GetHandleSize(position) * 0.08f;

        public static bool DragAlong(int id, Vector3 position, Vector3 axis, out Vector3 next)
        {
            float size = HandleSize(position);
            EditorGUI.BeginChangeCheck();
            next = Handles.Slider(id, position, axis, size, Handles.DotHandleCap, 0f);
            return EditorGUI.EndChangeCheck();
        }

        public static bool DragFree(int id, Vector3 position, out Vector3 next)
        {
            float size = HandleSize(position);
            EditorGUI.BeginChangeCheck();
            next = Handles.FreeMoveHandle(id, position, size, Vector3.zero, Handles.DotHandleCap);
            return EditorGUI.EndChangeCheck();
        }

        public static void BeginSceneDraw()
        {
            Handles.color = HandleColor;
            Handles.zTest = CompareFunction.Always;
        }

        public static float SafeScale(float scale)
            => scale < 1e-5f ? 1e-5f : scale;
    }
}
