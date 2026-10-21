namespace Game.Core
{
    /// <summary>
    /// 全局玩法常量。单位约定：距离 = 米，速度 = 米/秒，时间 = 秒。
    /// 所有数值集中在这里，避免 Domain / Presentation 各写一份。
    /// </summary>
    public static class GameConstants
    {
        // 逻辑固定 60Hz。渲染帧率可以更高，表现层用插值补齐。
        public const float TickDelta = 1f / 60f;

        // 玩家移动：步行用于跑图，潜行用于躲守卫（M1 才会真正用到噪音）。
        public const float WalkSpeed = 3.2f;
        public const float SneakSpeed = 1.2f;
        public const float Gravity = 20f;

        // 胶囊尺寸：Position 是脚底中心，所以胶囊中心 = Position + Up * (Height / 2)。
        public const float PlayerRadius = 0.3f;
        public const float PlayerHeight = 1.8f;

        // 碰撞皮肤：让胶囊在离墙/地面极小距离处停下，避免浮点抖动导致“贴墙卡死”。
        public const float SkinWidth = 0.02f;
        public const float GroundCheckDistance = 0.15f;
        public const float InteractDistance = 2f;

        // 残影预算：3 个槽位，FIFO；单轮最长 30 秒（60Hz * 30s = 1800 帧）。
        public const int MaxEchoes = 3;
        public const float RecordLimitSeconds = 30f;

        // 压力板离开后延迟 0.3 秒关闭，避免玩家在边缘抖动时门反复开关。
        public const float PressurePlateReleaseDelay = 0.3f;

        // 回溯冷却与回溯后的短暂无敌，防止连按和“刚恢复就被抓”。
        public const float RewindCooldown = 0.5f;
        public const float RewindInvulnerability = 0.3f;
    }
}
