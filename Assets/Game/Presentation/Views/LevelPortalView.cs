using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 关卡传送门：用几何包含判断玩家是否进入，不用 OnTriggerEnter。
    ///
    /// 原因：玩家视觉/根节点没有 Collider，Movement 是自定义胶囊扫掠；
    /// Trigger 检测不到玩家。这里沿用压力板的做法，由 GameLoop 每 tick 调 Contains。
    /// </summary>
    public sealed class LevelPortalView : MonoBehaviour
    {
        public string nextScene;
        public float radius = 1.2f;
        public bool requirePlateActive = true;
        public Color portalColor = new Color(0.2f, 0.9f, 1f);

        /// <summary>
        /// 只比较 XZ 平面距离；Y 用宽松范围，避免地面/台阶高度差漏判。
        /// </summary>
        public bool Contains(Vec3 feetPosition)
        {
            float dx = feetPosition.X - transform.position.x;
            float dz = feetPosition.Z - transform.position.z;
            if (dx * dx + dz * dz > radius * radius)
            {
                return false;
            }

            return Mathf.Abs(feetPosition.Y - transform.position.y) <= 2f;
        }

        private void Awake()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = portalColor;
            }
        }
    }
}
