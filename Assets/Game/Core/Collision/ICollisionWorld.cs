namespace Game.Core
{
    /// <summary>
    /// 胶囊扫掠结果。Hit=false 时其余字段无意义。
    /// </summary>
    public readonly struct CapsuleCastResult
    {
        public readonly bool Hit;
        public readonly float Distance;
        public readonly Vec3 Point;
        public readonly Vec3 Normal;

        public CapsuleCastResult(bool hit, float distance, Vec3 point, Vec3 normal)
        {
            Hit = hit;
            Distance = distance;
            Point = point;
            Normal = normal;
        }

        public static CapsuleCastResult None => new CapsuleCastResult(false, 0f, Vec3.Zero, Vec3.Zero);
    }

    /// <summary>
    /// Domain 使用的碰撞查询抽象。Unity 侧由 PhysicsCollisionWorld 实现。
    ///
    /// 坐标约定：
    /// - bottomCenter 是胶囊“脚底中心”，不是几何中心。
    /// - direction 必须是归一化向量。
    /// - 
    /// </summary>distance 的单位是米。
    ///
    /// 设计原因：Domain 不引用 UnityEngine，但自定义运动学又需要胶囊扫掠，
    /// 所以把物理查询抽象成接口，由 Infrastructure 注入。
    public interface ICollisionWorld
    {
        /// <summary>
        /// 从胶囊底部中心沿 direction 扫掠。
        /// 返回最近命中；未命中返回 CapsuleCastResult.None。
        /// </summary>
        CapsuleCastResult CapsuleCast(
            Vec3 bottomCenter,
            float radius,
            float height,
            Vec3 direction,
            float distance);

        /// <summary>
        /// 从胶囊底部中心向下检测地面。
        /// 命中时 normal 为地面法线；未命中返回 false。
        /// </summary>
        bool CheckGround(
            Vec3 bottomCenter,
            float radius,
            float distance,
            out Vec3 normal);
    }
}
