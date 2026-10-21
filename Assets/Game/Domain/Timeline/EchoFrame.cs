using System;
using System.Runtime.InteropServices;
using Game.Core;

namespace Game.Domain
{
    /// <summary>
    /// 残影在每个 tick 的表现状态。只记录“看得见/机关需要”的最小信息。
    /// </summary>
    public enum EchoState : byte
    {
        Idle = 0,
        Walk = 1,
        Sneak = 2,
        Interact = 3,
        Dead = 4
    }

    /// <summary>
    /// 残影轨迹帧。布局固定为 16 字节：
    /// Vec3 Position (12) + ushort Yaw (2) + byte State (1) + byte Flags (1)。
    ///
    /// 为什么用 ushort 存朝向：65535 个刻度约 0.0055° 精度，足够表现；
    /// 用 float 会把帧撑到 20 字节，30 秒 3 个残影会多出约 65KB 内存。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct EchoFrame
    {
        public readonly Vec3 Position;
        public readonly ushort Yaw;
        public readonly byte State;
        public readonly byte Flags;

        public EchoFrame(Vec3 position, float yawDegrees, EchoState state, byte flags = 0)
        {
            Position = position;
            Yaw = EncodeYaw(yawDegrees);
            State = (byte)state;
            Flags = flags;
        }

        public EchoState EchoState => (EchoState)State;
        public float YawDegrees => Yaw * (360f / 65535f);

        /// <summary>
        /// 把角度归一化到 [0,360)，再量化到 0..65535。
        /// </summary>
        public static ushort EncodeYaw(float degrees)
        {
            float normalized = degrees % 360f;
            if (normalized < 0f)
            {
                normalized += 360f;
            }

            int quantized = (int)Math.Round(normalized / 360f * 65535f);
            return (ushort)(quantized & 0xFFFF);
        }
    }
}
