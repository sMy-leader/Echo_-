using System;

namespace Game.Domain
{
    /// <summary>
    /// 垂直升降台的纯逻辑状态。
    ///
    /// 只负责“当前高度 → 目标高度”的线性移动；
    /// 平台碰撞、载人、视觉由 Presentation 的 LiftView 处理。
    /// 这样以后新增“按钮/拉杆/压力板触发”时，不需要改移动算法。
    /// </summary>
    public sealed class Lift
    {
        public float BottomY { get; }
        public float TopY { get; }
        public float Speed { get; }
        public float Height { get; private set; }

        public bool IsAtTop => Height >= TopY - 0.001f;
        public bool IsAtBottom => Height <= BottomY + 0.001f;

        public Lift(float bottomY, float topY, float speed)
        {
            if (topY <= bottomY)
            {
                throw new ArgumentException("topY must be greater than bottomY");
            }

            BottomY = bottomY;
            TopY = topY;
            Speed = Math.Max(0.01f, speed);
            Height = bottomY;
        }

        /// <summary>
        /// 激活时向顶层移动，否则回到底层。
        /// 目标高度用 MoveTowards 限速，避免瞬间传送造成玩家穿透平台。
        /// </summary>
        public void Tick(float dt, bool active)
        {
            float target = active ? TopY : BottomY;
            float maxDelta = Speed * Math.Max(0f, dt);
            if (Height < target)
            {
                Height = Math.Min(target, Height + maxDelta);
            }
            else if (Height > target)
            {
                Height = Math.Max(target, Height - maxDelta);
            }
        }

        public void Reset()
        {
            Height = BottomY;
        }
    }
}
