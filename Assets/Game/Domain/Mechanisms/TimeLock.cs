using System;

namespace Game.Domain
{
    /// <summary>
    /// 多源时间锁：只有达到 RequiredCount 个源同时激活才打开。
    ///
    /// 用固定槽位而不是“激活源列表”，因为 L3 的每块板对应一个支线，
    /// 表现层可以稳定地把槽位映射到板/灯，不需要维护去重集合。
    /// </summary>
    public sealed class TimeLock
    {
        private readonly bool[] _sources;

        public TimeLock(int requiredCount)
        {
            if (requiredCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredCount));
            }

            RequiredCount = requiredCount;
            _sources = new bool[requiredCount];
        }

        public int RequiredCount { get; }

        public int ActiveCount { get; private set; }

        public bool IsOpen { get; private set; }

        /// <summary>
        /// 设置某个槽位的激活状态。重复设置相同值不会改变结果。
        /// 每 tick 由表现层根据玩家/残影位置重新计算，避免 Trigger 回调顺序问题。
        /// </summary>
        public void SetSource(int index, bool active)
        {
            if (index < 0 || index >= _sources.Length || _sources[index] == active)
            {
                return;
            }

            _sources[index] = active;
            ActiveCount += active ? 1 : -1;

            // 用户确认的规则：三源同时亮过后锁存，玩家可以离开板通过；
            // 只有 Reset（回溯/检查点）才会重新关闭。
            if (ActiveCount >= RequiredCount)
            {
                IsOpen = true;
            }
        }

        public void Reset()
        {
            Array.Clear(_sources, 0, _sources.Length);
            ActiveCount = 0;
            IsOpen = false;
        }
    }
}
