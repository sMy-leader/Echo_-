namespace Game.Core
{
    /// <summary>
    /// 视线遮挡查询契约。
    ///
    /// Domain 只关心“两点之间有没有被挡住”，不关心 LayerMask、Physics、Trigger。
    /// Unity 侧由 PhysicsVisionWorld 实现；测试侧可以注入 FakeVisionWorld。
    /// </summary>
    public interface IVisionWorld
    {
        /// <summary>
        /// 返回 true 表示 from 到 to 之间没有遮挡，视线可见。
        /// from/to 都是世界坐标；调用方负责传入眼睛/目标瞄准点。
        /// </summary>
        bool HasLineOfSight(Vec3 from, Vec3 to);
    }
}
