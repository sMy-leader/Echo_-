namespace Game.Core
{
    /// <summary>
    /// 守卫感知的最小目标描述。
    ///
    /// 为什么单独抽这个结构：
    /// - Domain 的守卫不应该直接依赖 PlayerMotor / EchoPlayback；
    /// - 本体、残影、未来的可交互 NPC 都可以统一成“目标”；
    /// - 测试里可以直接构造目标，不需要跑整个 GameSession。
    ///
    /// Position 约定为脚底中心；守卫内部会加 TargetAimHeight 再判断视线。
    /// </summary>
    public readonly struct PerceptionTarget
    {
        public readonly int Id;
        public readonly Vec3 Position;
        public readonly bool IsPlayer;
        public readonly bool IsAlive;

        /// <summary>
        /// 检测id、位置、是否玩家、是否存活。存活状态用于守卫的“残影”感知逻辑：死掉的玩家残影不再被守卫追踪。
        /// </summary>
        /// <param name="id">（玩家固定 0，残影用 EchoPlayback.Id）</param>
        /// <param name="position"></param>
        /// <param name="isPlayer"></param>
        /// <param name="isAlive"></param>
        public PerceptionTarget(int id, Vec3 position, bool isPlayer, bool isAlive = true)
        {
            Id = id;
            Position = position;
            IsPlayer = isPlayer;
            IsAlive = isAlive;
        }
    }
}
