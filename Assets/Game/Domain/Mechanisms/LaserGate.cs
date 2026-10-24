using System;

namespace Game.Domain
{
    /// <summary>
    /// 激光门的纯逻辑状态：只负责“现在是开还是关”，不碰几何/渲染。
    ///
    /// 命中判定在 Presentation 层用 LaserGateView 的线段距离完成，
    /// 因为激光的“宽度/位置”是场景数据；Domain 只决定时间窗口。
    /// </summary>
    public enum LaserGateMode
    {
        AlwaysOn = 0,
        Periodic = 1
    }

    /// <summary>
    /// 周期激光状态机。
    ///
    /// 时间约定：
    /// - OnDuration 内 IsActive = true；
    /// - 之后 OffDuration 内 IsActive = false；
    /// - 循环周期 = OnDuration + OffDuration。
    ///
    /// 以“从周期起点开始计时”实现，避免记录上一帧状态；
    /// Reset 用于检查点/回溯，让激光相位与关卡时间对齐。
    /// </summary>
    public sealed class LaserGate
    {
        private readonly LaserGateMode _mode;
        private readonly float _onDuration;
        private readonly float _offDuration;
        private readonly float _phaseOffset;
        private float _timer;

        public bool IsActive { get; private set; }

        public LaserGate()
            : this(LaserGateMode.Periodic, 2f, 3f, 0f)
        {
        }
        /// <summary>
        /// 激光门的属性
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="onDuration"></param>
        /// <param name="offDuration"></param>
        /// <param name="phaseOffset"></param>
        public LaserGate(
            LaserGateMode mode,
            float onDuration,
            float offDuration,
            float phaseOffset = 0f)
        {
            _mode = mode;
            _onDuration = Math.Max(0.01f, onDuration);
            _offDuration = Math.Max(0f, offDuration);
            _phaseOffset = Math.Max(0f, phaseOffset);

            Reset();
        }

        /// <summary>
        /// 推进一个固定 tick。AlwaysOn 永远为开；Periodic 按周期取模。
        /// </summary>
        public void Tick(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }
            //状态为开->激光
            if (_mode == LaserGateMode.AlwaysOn)
            {
                IsActive = true;
                return;
            }

            float cycle = _onDuration + _offDuration;
            _timer = (_timer + dt) % cycle;
            IsActive = _timer < _onDuration;
        }

        /// <summary>
        /// 回到配置的相位起点。检查点/回溯时调用，保证同一关卡时间轴一致。
        /// </summary>
        public void Reset()
        {
            if (_mode == LaserGateMode.AlwaysOn)
            {
                IsActive = true;
                _timer = 0f;
                return;
            }

            float cycle = _onDuration + _offDuration;
            _timer = _phaseOffset % cycle;
            IsActive = _timer < _onDuration;
        }
    }
}
