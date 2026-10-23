using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 激光门的表现 + 线段命中查询。
    ///
    /// 只负责：
    /// - 用 LineRenderer 画光束；
    /// - 根据 Domain 的 IsActive 切换颜色/亮度；
    /// - 提供点到线段的距离判断。
    ///
    /// 不负责“什么时候开/关”，那由 Game.Domain.LaserGate 决定。
    /// </summary>
    public sealed class LaserGateView : MonoBehaviour
    {
        public LineRenderer beam;
        public float hitRadius = 0.25f;
        public Color activeColor = new Color(1f, 0.12f, 0.08f);
        public Color inactiveColor = new Color(0.2f, 0.04f, 0.04f);

        private bool _active;
        private bool _hasApplied;

        private void Awake()
        {
            if (beam == null)
            {
                beam = GetComponent<LineRenderer>();
            }

            SetActive(false);
        }

        /// <summary>
        /// 视觉上保留暗淡光束，让玩家能预判激光位置；激活时变亮红。
        /// </summary>
        public void SetActive(bool active)
        {
            if (beam == null)
            {
                return;
            }

            if (_hasApplied && _active == active)
            {
                return;
            }

            _active = active;
            _hasApplied = true;
            beam.startColor = active ? activeColor : inactiveColor;
            beam.endColor = active ? activeColor : inactiveColor;
        }

        /// <summary>
        /// 判断一个点（通常是角色胸口）是否碰到当前激光线段。
        /// padding 用来补偿角色半径；只在 GameLoop 判断 IsActive 后调用。
        /// </summary>
        public bool Intersects(Vector3 point, float padding = 0f)
        {
            if (beam == null || beam.positionCount < 2)
            {
                return false;
            }

            Vector3 a = beam.GetPosition(0);
            Vector3 b = beam.GetPosition(1);
            return DistancePointToSegment(point, a, b) <= hitRadius + padding;
        }

        private static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lengthSqr = ab.sqrMagnitude;
            if (lengthSqr <= 1e-6f)
            {
                return Vector3.Distance(point, a);
            }

            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSqr);
            Vector3 closest = a + ab * t;
            return Vector3.Distance(point, closest);
        }
    }
}
