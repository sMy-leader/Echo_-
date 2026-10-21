using System;
using Game.Core;

namespace Game.Domain
{
    /// <summary>
    /// 固定容量输入录制缓冲。满了以后覆盖最旧帧。
    ///
    /// 不变量：
    /// - GetFrame(0) 永远是最旧的可回溯帧；
    /// - GetFrame(Count-1) 永远是最近一帧；
    /// - 底层数组容量固定，Record 不产生堆分配。
    /// </summary>
    public sealed class TimelineRecorder
    {
        // 60Hz * 30s = 1800 帧；与 GameConstants.RecordLimitSeconds 保持一致。
        public const int DefaultCapacity = 1800;

        private readonly InputFrame[] _frames;
        private int _head;
        private int _count;

        public TimelineRecorder(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            }

            _frames = new InputFrame[capacity];
        }

        public int Capacity => _frames.Length;
        public int Count => _count;
        public bool IsFull => _count == _frames.Length;

        /// <summary>
        /// 写入一帧。缓冲满时 head 追上 tail，最旧帧被自然覆盖。
        /// </summary>
        public void Record(in InputFrame frame)
        {
            _frames[_head] = frame;
            _head = (_head + 1) % _frames.Length;

            if (_count < _frames.Length)
            {
                _count++;
            }
        }

        /// <summary>
        /// 按“逻辑顺序”取帧：0 = 最旧，Count-1 = 最新。
        /// 环形缓冲里数据可能跨过数组末尾，所以先算 start，再取模。
        /// </summary>
        public InputFrame GetFrame(int logicalIndex)
        {
            // 逻辑索引必须在 [0, Count) 范围内。
            if ((uint)logicalIndex >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalIndex));
            }

            int start = (_head - _count + _frames.Length) % _frames.Length;
            return _frames[(start + logicalIndex) % _frames.Length];
        }

        /// <summary>
        /// 清空逻辑计数。不清零数组内容：下一次 Record 会覆盖旧数据。
        /// </summary>
        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}
