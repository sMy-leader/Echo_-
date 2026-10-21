using Game.Core;

namespace Game.Domain
{
    /// <summary>
    /// 一个残影的只读轨迹回放器。
    ///
    /// 关键决策：创建时把录制帧拷贝一份，之后不再读 EchoRecorder。
    /// 这样下一轮录制清空/覆盖时，已有残影的轨迹不会被改坏。
    /// </summary>
    public sealed class EchoPlayback
    {
        private readonly EchoFrame[] _frames;
        private int _cursor;

        public int Id { get; }
        public int FrameCount => _frames.Length;

        /// <summary>
        /// 已经播放完全部帧。注意：EchoManager 会在“下一 tick”才移除它，
        /// 保证最后一帧至少被渲染一次。
        /// </summary>
        public bool IsFinished { get; private set; }

        // Previous/Current 供表现层插值：alpha=0 显示 Previous，alpha=1 显示 Current。
        public EchoFrame Previous { get; private set; }
        public EchoFrame Current { get; private set; }

        public Vec3 PreviousPosition => Previous.Position;
        public Vec3 CurrentPosition => Current.Position;
        public float PreviousYaw => Previous.YawDegrees;
        public float CurrentYaw => Current.YawDegrees;

        public EchoPlayback(int id, EchoRecorder recorder)
        {
            Id = id;
            _frames = new EchoFrame[recorder.Count];

            for (int i = 0; i < _frames.Length; i++)
            {
                _frames[i] = recorder.GetFrame(i);
            }

            if (_frames.Length > 0)
            {
                Previous = _frames[0];
                Current = _frames[0];
            }
            else
            {
                IsFinished = true;
            }
        }

        /// <summary>
        /// 前进一个 tick。第一帧在构造时已经作为 Current，因此这里从 cursor=0 再放一次，
        /// 视觉上相当于“残影在第 0 tick 出现在起点”，不会跳过第一帧。
        /// </summary>
        public void Tick()
        {
            if (IsFinished || _frames.Length == 0)
            {
                IsFinished = true;
                return;
            }

            Previous = Current;
            Current = _frames[_cursor];
            _cursor++;

            if (_cursor >= _frames.Length)
            {
                IsFinished = true;
            }
        }

        /// <summary>
        /// 回溯时所有残影统一回到轨迹起点。
        /// 这是“所有残影与本体时间轴对齐”的核心。
        /// </summary>
        public void Reset()
        {
            _cursor = 0;
            IsFinished = _frames.Length == 0;

            if (_frames.Length > 0)
            {
                Previous = _frames[0];
                Current = _frames[0];
            }
        }
    }
}
