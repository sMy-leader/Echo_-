using System;
using Game.Core;

namespace Game.Domain
{
    /// <summary>
    /// 自定义运动学玩家。Position 是胶囊底部中心（脚底），不是几何中心。
    ///
    /// 为什么不用 Rigidbody / CharacterController：
    /// - Rigidbody 的物理解算受求解器顺序和浮点影响，残影回放要求“可预测”；
    /// - CharacterController 的内部实现不保证跨版本一致；
    /// - 自定义胶囊扫掠约 150 行，换来完全可控的移动、贴墙和地面检测。
    ///
    /// 注意：残影不重跑这个类，残影只回放 EchoFrame；这里的确定性主要服务于本体手感。
    /// </summary>
    public sealed class PlayerMotor
    {
        private const float RadToDeg = (float)(180.0 / Math.PI);

        // 上一 tick / 当前 tick 的位置与朝向，供表现层插值。
        public Vec3 Position { get; private set; }
        public Vec3 PreviousPosition { get; private set; }
        public float Yaw { get; private set; }
        public float PreviousYaw { get; private set; }

        // 垂直速度与落地状态。M0 没有跳跃，只处理重力和地面吸附。
        public float VelocityY { get; private set; }
        public bool IsGrounded { get; private set; }

        // 录制到 EchoFrame 的表现状态：Idle / Walk / Sneak / Interact。
        public EchoState State { get; private set; }

        public float Radius { get; }
        public float Height { get; }

        public PlayerMotor(Vec3 spawn, float yaw, float radius, float height)
        {
            Position = spawn;
            PreviousPosition = spawn;
            Yaw = yaw;
            PreviousYaw = yaw;
            Radius = radius;
            Height = height;
            State = EchoState.Idle;
        }

        /// <summary>
        /// 前进一个逻辑 tick。
        /// 顺序很重要：先记录上一帧位置 → 水平移动 → 垂直重力 → 更新朝向/状态。
        /// </summary>
        public void Tick(float dt, in InputFrame input, ICollisionWorld world)
        {
            PreviousPosition = Position;
            PreviousYaw = Yaw;

            // 输入是屏幕方向：MoveY 对应世界 +Z，MoveX 对应世界 +X。
            // 斜向输入长度会超过 1，归一化保证斜着走不会更快。
            Vec3 wish = new Vec3(input.MoveXFloat, 0f, input.MoveYFloat);
            if (wish.SqrMagnitude > 1f)
            {
                wish = wish.Normalized;
            }

            float speed = input.IsSneaking ? GameConstants.SneakSpeed : GameConstants.WalkSpeed;
            Vec3 delta = wish * (speed * dt);

            MoveWithSlide(delta, world);
            ApplyGravity(dt, world);

            // 有移动输入时平滑转向目标方向；720°/s 让转身有重量但不拖沓。
            if (wish.SqrMagnitude > 1e-6f)
            {
                float targetYaw = (float)(Math.Atan2(wish.X, wish.Z) * RadToDeg);
                float deltaYaw = NormalizeAngle(targetYaw - Yaw);
                float maxStep = GameConstants.PlayerTurnSpeedDegrees * dt;
                float step = Math.Min(Math.Abs(deltaYaw), maxStep);
                if (deltaYaw < 0f)
                {
                    step = -step;
                }

                Yaw = NormalizeAngle(Yaw + step);
            }

            bool moving = wish.SqrMagnitude > 1e-4f;
            if (input.IsSneaking && moving)
            {
                State = EchoState.Sneak;
            }
            else if (moving)
            {
                State = EchoState.Walk;
            }
            else if (input.Has(InputButtons.Interact))
            {
                State = EchoState.Interact;
            }
            else
            {
                State = EchoState.Idle;
            }
        }

        /// <summary>
        /// 上升降台时由表现层调用：只移动位置，保留 PreviousPosition 做插值。
        /// 不在这里做重力/碰撞，平台本身已经负责地面支撑。
        /// </summary>
        public void MoveWithPlatform(Vec3 delta)
        {
            Position += delta;
            PreviousPosition = Position;
            VelocityY = 0f;
            IsGrounded = true;
        }

        /// <summary>
        /// 回溯/检查点恢复：同时重置当前位置与“上一帧位置”，
        /// 避免表现层在恢复的第一帧从旧位置插值过来。
        /// </summary>
        public void Teleport(Vec3 position, float yaw)
        {
            Position = position;
            PreviousPosition = position;
            Yaw = yaw;
            PreviousYaw = yaw;
            VelocityY = 0f;
            IsGrounded = false;
            State = EchoState.Idle;
        }

        /// <summary>
        /// 水平移动 + 贴墙滑行。
        ///
        /// 思路：
        /// 1. 沿 wish 方向做一次胶囊扫掠；
        /// 2. 命中后把剩余位移投影到碰撞平面上，得到沿墙方向；
        /// 3. 最多迭代两次，处理墙角/两面墙的夹角。
        ///
        /// 不变量：Position 始终在碰撞体外部；不会穿过薄墙。
        /// </summary>
        private void MoveWithSlide(Vec3 delta, ICollisionWorld world)
        {
            for (int iteration = 0; iteration < 2; iteration++)
            {
                float distance = delta.Magnitude;
                if (distance < 1e-6f)
                {
                    return;
                }

                Vec3 direction = delta / distance;

                // 多扫 SkinWidth，让胶囊在离墙一点点的位置停下，避免浮点抖动。
                CapsuleCastResult hit = world.CapsuleCast(
                    Position,
                    Radius,
                    Height,
                    direction,
                    distance + GameConstants.SkinWidth);

                if (!hit.Hit)
                {
                    Position += delta;
                    return;
                }

                // 只走到“命中点前 SkinWidth”，然后处理剩余位移。
                float safeDistance = Math.Max(0f, hit.Distance - GameConstants.SkinWidth);
                Position += direction * safeDistance;
                Vec3 remaining = delta - direction * safeDistance;

                if (hit.Normal.SqrMagnitude < 1e-6f)
                {
                    return;
                }

                // 去掉撞墙方向的分量，剩下的就是沿墙滑行方向。
                delta = Vec3.ProjectOnPlane(remaining, hit.Normal.Normalized);
            }
        }

        /// <summary>
        /// 把角度归一化到 (-180, 180]，用于平滑转向的最短路径判断。
        /// 纯 C#，不依赖 Mathf.DeltaAngle，保证 Domain 可无引擎测试。
        /// </summary>
        private static float NormalizeAngle(float degrees)
        {
            degrees %= 360f;
            if (degrees > 180f)
            {
                degrees -= 360f;
            }
            else if (degrees < -180f)
            {
                degrees += 360f;
            }

            return degrees;
        }

        /// <summary>
        /// 垂直运动 + 地面检测。
        ///
        /// 先向下/向上扫掠；命中则停在表面外 SkinWidth 处并清零速度。
        /// 再用 CheckGround 做一次兜底，避免“速度刚好为 0 时扫掠距离为 0”导致悬空。
        /// </summary>
        private void ApplyGravity(float dt, ICollisionWorld world)
        {
            VelocityY -= GameConstants.Gravity * dt;
            float dy = VelocityY * dt;
            float distance = Math.Abs(dy);

            if (distance > 1e-6f)
            {
                Vec3 direction = dy < 0f ? new Vec3(0f, -1f, 0f) : new Vec3(0f, 1f, 0f);

                //检测胶囊底部中心向下/向上扫掠，命中则停在表面外 SkinWidth 处并清零速度。
                CapsuleCastResult hit = world.CapsuleCast(
                    Position,
                    Radius,
                    Height,
                    direction,
                    distance + GameConstants.SkinWidth);

                if (hit.Hit)
                {
                    float safeDistance = Math.Max(0f, hit.Distance - GameConstants.SkinWidth);
                    Position += direction * safeDistance;
                    VelocityY = 0f;
                    IsGrounded = dy < 0f;
                }
                else
                {
                    Position += new Vec3(0f, dy, 0f);
                    IsGrounded = false;
                }
            }

            if (!IsGrounded
                && world.CheckGround(Position, Radius, GameConstants.GroundCheckDistance, out _))
            {
                IsGrounded = true;
                if (VelocityY < 0f)
                {
                    VelocityY = 0f;
                }
            }
        }
    }
}
