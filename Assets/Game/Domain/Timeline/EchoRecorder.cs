using System;
using Game.Core;

namespace Game.Domain
{
    /// <summary>
    /// 残影轨迹录制缓冲。与 TimelineRecorder 结构相同，但元素是 EchoFrame。
    /// 为什么不复用泛型：泛型在 IL2CPP + HybridCLR 下需要额外 AOT 补全，
    /// 这里只有两种固定元素类型，显式两份更稳、更快。
    /// </summary>
    public sealed class EchoRecorder
    {
        // 60Hz * 30s = 1800 帧；每帧 16 字节，约 28.8KB/残影。
        public const int DefaultCapacity = 1800;

        private readonly EchoFrame[] _frames;
        private int _head;
        private int _count;

        public EchoRecorder(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            }

            _frames = new EchoFrame[capacity];
        }

        public int Capacity => _frames.Length;
        public int Count => _count;
        public bool IsFull => _count == _frames.Length;

        /// <summary>
        /// 残影轨迹录制：写入一帧。缓冲满时 head 追上 tail，最旧帧被自然覆盖。
        /// </summary>
        /// <param name="frame"></param>
        public void Record(in EchoFrame frame)
        {
            _frames[_head] = frame;
            _head = (_head + 1) % _frames.Length;

            if (_count < _frames.Length)
            {
                _count++;
            }
        }

        /// <summary>
        /// 按逻辑顺序取帧。GetFrame(0) 是残影轨迹的起点。
        /// </summary>
        public EchoFrame GetFrame(int logicalIndex)
        {
            if ((uint)logicalIndex >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalIndex));
            }

            int start = (_head - _count + _frames.Length) % _frames.Length;
            return _frames[(start + logicalIndex) % _frames.Length];
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}
