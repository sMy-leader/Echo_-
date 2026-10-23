namespace Game.Domain
{
    /// <summary>
    /// 守卫参数。M1 第一块先用纯 C# 默认值；
    /// 后续接入关卡配置时，可以由 ScriptableObject 转成这个结构。
    ///
    /// 单位：距离 = 米，速度 = 米/秒，角度 = 度，时间 = 秒。
    /// </summary>
    public sealed class GuardConfig
    {
        // 视觉：水平总视角 60°、垂直总视角 40°、距离 8m。
        // 代码里比较的是“总角度的一半”，所以水平半角 30°、垂直半角 20°。
        public float ViewDistance = 8f;
        public float ViewHorizontalAngle = 60f;
        public float ViewVerticalAngle = 40f;

        // 眼睛高度与目标瞄准高度。目标 Position 是脚底，所以瞄准点在胸口附近。
        public float EyeHeight = 1.6f;
        public float TargetAimHeight = 1.0f;

        // 10Hz 感知：每 0.1s 做一次视锥/遮挡判断；警戒值仍然每 tick 更新。
        public float VisionSampleInterval = 0.1f;

        // 近距离感知：目标进入这个半径时，即使不在视锥中心也能察觉（包括背后）。
        public float CloseAwarenessDistance = 2f;

        // 追击时保持的最小距离，避免守卫和玩家胶囊/模型重叠。
        public float MinApproachDistance = 1.5f;

        // 攻击：进入范围后先蓄力 0.4s，再判定命中；冷却防止连续扑击。
        public float AttackRange = 1.8f;
        public float AttackWindupSeconds = 0.4f;
        public float AttackCooldownSeconds = 1f;

        // 命中判定给一点距离容差，避免目标刚好在边缘时“看起来打到却没中”。
        public float AttackHitDistanceBuffer = 0.5f;

        // 警戒值 0..100。视觉内每秒 +40；脱离视觉每秒 -15。
        public float SuspicionRisePerSecond = 40f;
        public float SuspicionDecayPerSecond = 15f;
        public float SuspicionInvestigateThreshold = 60f;
        public float SuspicionAlertThreshold = 100f;

        // 各状态移动速度。
        public float PatrolSpeed = 1.6f;
        public float InvestigateSpeed = 2.2f;
        public float AlertSpeed = 3.6f;
        public float ReturnSpeed = 1.8f;

        // 到达判定与路点停留。
        public float WaypointReachDistance = 0.15f;
        public float WaypointWaitSeconds = 2f;

        // 搜索/丢失目标的持续时间。
        public float SearchDurationSeconds = 8f;
        public float AlertLostSightSeconds = 6f;
        public float SearchTurnSpeedDegrees = 90f;
    }
}
