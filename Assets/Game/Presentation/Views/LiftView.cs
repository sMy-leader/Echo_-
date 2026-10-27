using Game.Core;
using Game.Domain;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 升降台表现 + 载人判断。
    ///
    /// 根节点位置 = 平台顶面高度；平台网格作为子节点向下偏移。
    /// 这样玩家脚底坐标可以直接和平台顶面对齐，避免脚陷进平台。
    /// </summary>
    public sealed class LiftView : MonoBehaviour
    {
        public float bottomY = 0.1f;
        public float topY = 4.1f;
        public float speed = 1.6f;
        public bool autoActivateOnStand = true;
        public Vector2 halfExtents = new Vector2(1.5f, 1.5f);

        private Lift _lift;

        public bool IsAtTop => _lift != null && _lift.IsAtTop;
        public bool IsAtBottom => _lift != null && _lift.IsAtBottom;

        private void Awake()
        {
            _lift = new Lift(bottomY, topY, speed);
            Vector3 p = transform.position;
            p.y = _lift.Height;
            transform.position = p;
        }

        public bool Contains(Vec3 feetPosition)
        {
            Vector3 center = transform.position;
            return Mathf.Abs(feetPosition.X - center.x) <= halfExtents.x
                && Mathf.Abs(feetPosition.Z - center.z) <= halfExtents.y
                && feetPosition.Y >= center.y - 0.5f
                && feetPosition.Y <= center.y + 1.5f;
        }

        /// <summary>
        /// 返回本 tick 平台上升/下降的高度差，供 GameLoop 把玩家一起带上去。
        /// </summary>
        public float Tick(float dt, bool active)
        {
            if (_lift == null)
            {
                return 0f;
            }

            float before = _lift.Height;
            _lift.Tick(dt, active);
            Vector3 p = transform.position;
            p.y = _lift.Height;
            transform.position = p;
            return _lift.Height - before;
        }

        public void Reset()
        {
            _lift?.Reset();
            if (_lift != null)
            {
                Vector3 p = transform.position;
                p.y = _lift.Height;
                transform.position = p;
            }
        }
    }
}
