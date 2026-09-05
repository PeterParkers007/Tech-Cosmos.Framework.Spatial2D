using System;
using UnityEngine;

namespace TechCosmos.Spatial2D.Unity.Editor
{
    /// <summary>自定义 Inspector 开头的挂钩。谁要画谁订，这里不认任何业务类型。</summary>
    public static class InspectorAboveDraw
    {
        public static Action<UnityEngine.Object> Draw;
    }
}
