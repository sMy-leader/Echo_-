using System;

namespace Game.Core
{
    /// <summary>
    /// 纯 C# 三维向量，供 Domain 使用，不依赖 UnityEngine。
    /// 使用 struct 是为了在 tick 中避免堆分配；所有运算返回新值，不修改原对象。
    /// </summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 Zero => new Vec3(0f, 0f, 0f);
        public static Vec3 One => new Vec3(1f, 1f, 1f);
        public static Vec3 Up => new Vec3(0f, 1f, 0f);
        public static Vec3 Forward => new Vec3(0f, 0f, 1f);
        public static Vec3 Right => new Vec3(1f, 0f, 0f);

        /// <summary>
        /// 向量长度平方。需要比较长度时优先用它，省一次开方。
        /// </summary>
        public float SqrMagnitude => X * X + Y * Y + Z * Z;

        /// <summary>
        /// 向量长度。需要比较长度时优先用 SqrMagnitude，省一次开方。
        /// </summary>
        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        /// <summary>
        /// 归一化。长度接近 0 时返回 Zero，避免除零产生 NaN。
        /// </summary>
        public Vec3 Normalized
        {
            get
            {
                float magnitude = Magnitude;
                return magnitude > 1e-6f ? this / magnitude : Zero;
            }
        }

        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3 operator -(Vec3 a)
        {
            return new Vec3(-a.X, -a.Y, -a.Z);
        }

        public static Vec3 operator *(Vec3 a, float scalar)
        {
            return new Vec3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }

        public static Vec3 operator *(float scalar, Vec3 a)
        {
            return a * scalar;
        }

        public static Vec3 operator /(Vec3 a, float scalar)
        {
            // 除零保护：调用方通常已经检查，但这里再兜底，避免污染整个模拟。
            if (Math.Abs(scalar) < 1e-8f)
            {
                return Zero;
            }

            return new Vec3(a.X / scalar, a.Y / scalar, a.Z / scalar);
        }

        public static float Dot(Vec3 a, Vec3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        /// <summary>
        /// 线性插值。t 会被截断到 [0,1]，避免表现层传入异常 alpha 时把角色拉飞。
        /// </summary>
        public static Vec3 Lerp(Vec3 a, Vec3 b, float t)
        {
            if (t < 0f)
            {
                t = 0f;
            }
            else if (t > 1f)
            {
                t = 1f;
            }

            return new Vec3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t);
        }

        /// <summary>
        /// 去除沿 normal 方向的分量。normal 必须已归一化。
        /// 玩家贴墙移动时，用它把“撞墙后剩下的位移”转成沿墙滑行。
        /// </summary>
        public static Vec3 ProjectOnPlane(Vec3 vector, Vec3 normal)
        {
            return vector - normal * Dot(vector, normal);
        }

        /// <summary>
        /// 朝目标移动最多 maxDelta 的距离。用于表现层平滑，不用于逻辑判定。
        /// </summary>
        public static Vec3 MoveTowards(Vec3 current, Vec3 target, float maxDelta)
        {
            Vec3 delta = target - current;
            float distance = delta.Magnitude;
            if (distance <= maxDelta || distance < 1e-6f)
            {
                return target;
            }

            return current + delta / distance * maxDelta;
        }

        /// <summary>
        /// 带容差的近似比较。测试和表现层用它，避免浮点精确相等。
        /// </summary>
        public bool Approximately(Vec3 other, float epsilon = 1e-4f)
        {
            return Math.Abs(X - other.X) <= epsilon
                && Math.Abs(Y - other.Y) <= epsilon
                && Math.Abs(Z - other.Z) <= epsilon;
        }

        public bool Equals(Vec3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is Vec3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                hash = (hash * 397) ^ Z.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format("({0:0.###}, {1:0.###}, {2:0.###})", X, Y, Z);
        }
    }
}
