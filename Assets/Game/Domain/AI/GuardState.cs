namespace Game.Domain
{
    /// <summary>
    /// 守卫状态。状态迁移的完整规则见 Docs/DesignRules.md 的 M1 守卫章节。
    /// </summary>
    public enum GuardState
    {
        Patrol = 0,
        Suspicious = 1,
        Investigate = 2,
        Alert = 3,
        Search = 4,
        Return = 5
    }
}
