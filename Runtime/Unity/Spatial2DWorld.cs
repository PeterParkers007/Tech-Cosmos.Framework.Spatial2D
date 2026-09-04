using UnityEngine;

namespace TechCosmos.Spatial2D.Unity
{
    /// <summary>可选。挂场景上则组件都登记进这份 World。</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class Spatial2DWorld : MonoBehaviour
    {
        public static Spatial2DWorld Instance { get; private set; }

        [SerializeField, Min(0.1f)] float cellSize = 4f;

        public SpatialWorld World { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Spatial2D] 场景里已有 Spatial2DWorld，忽略后来者。", this);
                return;
            }

            Instance = this;
            World = new SpatialWorld(cellSize);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
