using System;

namespace Game.Core
{
    /// <summary>
    /// 输入按钮位。一个 byte 最多表达 8 个按钮；这里用位运算组合，
    /// 便于 4 字节 InputFrame 直接录制和回放。
    /// </summary>
    [Flags]
    public enum InputButtons : byte
    {
        None = 0,
        Sneak = 1 << 0,
        Interact = 1 << 1,
        Rewind = 1 << 2,
        Timeline = 1 << 3,
        Pause = 1 << 4
    }

    /// <summary>
    /// 逻辑层唯一接收的输入结构。布局：2 个轴 + 1 个按钮位 + 1 个朝向 = 4 字节。
    /// 30 秒 = 1800 帧 ≈ 7.2KB（Plan.md 里写的 8 字节/帧是估算，实际结构体是 4 字节）。
    ///
    /// 约定：
    /// - 轴使用 sbyte（-127..127），避免浮点在录制/回放中产生平台差异。
    /// - Buttons 是“本 tick 是否按住”的位集合。
    /// - Rewind/Pause 由 InputService 做边沿检测，只在按下的那一帧置位。
    /// </summary>
    public readonly struct InputFrame : IEquatable<InputFrame>
    {
        public const float AxisScale = 127f;

        public readonly sbyte MoveX;
        public readonly sbyte MoveY;
        public readonly byte Buttons;
        public readonly byte LookYaw;

        public InputFrame(sbyte moveX, sbyte moveY, byte buttons, byte lookYaw = 0)
        {
            MoveX = moveX;
            MoveY = moveY;
            Buttons = buttons;
            LookYaw = lookYaw;
        }

        public float MoveXFloat => MoveX / AxisScale;
        public float MoveYFloat => MoveY / AxisScale;
        public bool IsSneaking => Has(InputButtons.Sneak);

        public bool Has(InputButtons button)
        {
            return (Buttons & (byte)button) != 0;
        }

        /// <summary>
        /// 从 Unity/手柄的浮点轴构造逻辑输入帧。
        /// 这里做量化和死区，Domain 永远看不到 InputSystem 的浮点值。
        /// </summary>
        public static InputFrame FromAxes(
            float moveX,
            float moveY,
            InputButtons buttons = InputButtons.None,
            float yawDegrees = 0f)
        {
            return new InputFrame(
                QuantizeAxis(moveX),
                QuantizeAxis(moveY),
                (byte)buttons,
                QuantizeYaw(yawDegrees));
        }

        /// <summary>
        /// 把 [-1,1] 轴量化到 sbyte。超出范围会 clamp；
        /// 小于 1% 的输入视为 0，过滤摇杆漂移。
        /// </summary>
        public static sbyte QuantizeAxis(float value)
        {
            if (value > 1f)
            {
                value = 1f;
            }
            else if (value < -1f)
            {
                value = -1f;
            }

            if (Math.Abs(value) < 0.01f)
            {
                return 0;
            }

            return (sbyte)Math.Round(value * AxisScale);
        }

        /// <summary>
        /// 朝向量化到 0..255。固定相机下 M0 不用朝向输入，但结构预留。
        /// </summary>
        public static byte QuantizeYaw(float degrees)
        {
            float normalized = degrees % 360f;
            if (normalized < 0f)
            {
                normalized += 360f;
            }

            int quantized = (int)Math.Round(normalized / 360f * 255f);
            return (byte)(quantized & 0xFF);
        }

        public float YawDegrees => LookYaw * (360f / 255f);

        public bool Equals(InputFrame other)
        {
            return MoveX == other.MoveX
                && MoveY == other.MoveY
                && Buttons == other.Buttons
                && LookYaw == other.LookYaw;
        }

        public override bool Equals(object obj)
        {
            return obj is InputFrame other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = MoveX.GetHashCode();
                hash = (hash * 397) ^ MoveY.GetHashCode();
                hash = (hash * 397) ^ Buttons.GetHashCode();
                hash = (hash * 397) ^ LookYaw.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "InputFrame(Move=({0}, {1}), Buttons={2}, Yaw={3:0.#})",
                MoveX,
                MoveY,
                Buttons,
                YawDegrees);
        }
    }
}
