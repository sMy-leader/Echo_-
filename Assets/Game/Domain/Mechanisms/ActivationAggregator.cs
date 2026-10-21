using System;
using Game.Core;

namespace Game.Domain
{
    public enum ActivationMode
    {
        Or = 0,
        And = 1,
        Count = 2
    }

    /// <summary>
    /// 机关激活聚合器。核心思想：每 tick 根据“当前激活源数量”重新求值，
    /// 而不是由踩板的对象直接改门的状态。
    ///
    /// 好处：
    /// - 本体 + 多个残影同时踩板时，结果天然正确；
    /// - 残影消散时不需要额外清理，下一 tick 自动重算；
    /// - 机关状态可被快照/恢复，不会出现“门开着但没人踩”的脏状态。
    /// </summary>
    public sealed class ActivationAggregator
    {
        public ActivationMode Mode { get; set; } = ActivationMode.Or;
        public int RequiredCount { get; set; } = 1;

        // 离开后延迟关闭，避免边缘抖动导致门反复开关。
        public float ReleaseDelay { get; set; } = GameConstants.PressurePlateReleaseDelay;

        public bool IsActive { get; private set; }

        private float _releaseTimer;

        /// <summary>
        /// activationCount 是“当前站在板上的本体 + 残影数量”。
        /// </summary>
        public void Tick(float dt, int activationCount)
        {
            bool shouldBeActive;
            switch (Mode)
            {
                case ActivationMode.And:
                case ActivationMode.Count:
                    // M0 没有“总源数”概念，先用 RequiredCount 表达“至少 N 个”。
                    shouldBeActive = activationCount >= Math.Max(1, RequiredCount);
                    break;
                default:
                    shouldBeActive = activationCount > 0;
                    break;
            }

            if (shouldBeActive)
            {
                IsActive = true;
                _releaseTimer = 0f;
                return;
            }

            if (!IsActive)
            {
                return;
            }

            _releaseTimer += dt;
            if (_releaseTimer >= ReleaseDelay)
            {
                IsActive = false;
                _releaseTimer = 0f;
            }
        }

        /// <summary>
        /// 回溯/失败恢复时立即清空状态；下一 tick 会按新位置重新计算。
        /// </summary>
        public void Reset()
        {
            IsActive = false;
            _releaseTimer = 0f;
        }
    }
}
