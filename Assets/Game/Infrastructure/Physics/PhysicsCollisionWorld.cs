using Game.Core;
using UnityEngine;

namespace Game.Infrastructure
{
    /// <summary>
    /// Unity Physics 查询实现。只做查询，不驱动 Rigidbody。
    ///
    /// 关键点：
    /// - Domain 传进来的是“脚底中心”，这里要换算成 Unity CapsuleCast 需要的两个球心；
    /// - 默认使用 Physics.DefaultRaycastLayers，避免打到 IgnoreRaycast 层；
    /// - 忽略 Trigger，机关触发器不参与角色碰撞。
    /// </summary>
    public sealed class PhysicsCollisionWorld : ICollisionWorld
    {
        private readonly LayerMask _mask;
        private readonly QueryTriggerInteraction _triggerInteraction;

        public PhysicsCollisionWorld(
            LayerMask mask = default,
            QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
        {
            // mask 为 0 表示“没配置”，回退到 Unity 默认层；否则使用调用方指定的层。
            _mask = mask.value == 0 ? Physics.DefaultRaycastLayers : mask;
            _triggerInteraction = triggerInteraction;
        }

        /// <summary>
        /// 胶囊扫掠。bottomCenter 是脚底中心，height 是总高度。
        /// Unity 的 CapsuleCast 需要上下两个球心，所以：
        /// point1 = bottom + up * radius
        /// point2 = bottom + up * (height - radius)
        /// </summary>
        public CapsuleCastResult CapsuleCast(
            Vec3 bottomCenter,
            float radius,
            float height,
            Vec3 direction,
            float distance)
        {
            if (direction.SqrMagnitude < 1e-8f || distance <= 0f)
            {
                return CapsuleCastResult.None;
            }

            // 防止外部传入 height < 2*radius 时两个球心翻转。
            float safeHeight = Mathf.Max(height, radius * 2f + 0.01f);
            Vector3 bottom = ToUnity(bottomCenter);
            Vector3 point1 = bottom + Vector3.up * radius;
            Vector3 point2 = bottom + Vector3.up * (safeHeight - radius);
            Vector3 dir = ToUnity(direction).normalized;

            if (Physics.CapsuleCast(
                    point1,
                    point2,
                    radius,
                    dir,
                    out RaycastHit hit,
                    distance,
                    _mask,
                    _triggerInteraction))
            {
                return new CapsuleCastResult(
                    true,
                    hit.distance,
                    FromUnity(hit.point),
                    FromUnity(hit.normal));
            }

            return CapsuleCastResult.None;
        }

        /// <summary>
        /// 地面检测：从脚底中心向上 offset 一个半径，再向下做 SphereCast。
        /// 这样球体底部刚好在脚底，能稳定检测到地面，而不会从胶囊内部起始。
        /// </summary>
        public bool CheckGround(Vec3 bottomCenter, float radius, float distance, out Vec3 normal)
        {
            Vector3 origin = ToUnity(bottomCenter) + Vector3.up * radius;

            if (Physics.SphereCast(
                    origin,
                    radius,
                    Vector3.down,
                    out RaycastHit hit,
                    distance,
                    _mask,
                    _triggerInteraction))
            {
                normal = FromUnity(hit.normal);
                return true;
            }

            normal = Vec3.Zero;
            return false;
        }

        // Vec3 与 UnityEngine.Vector3 的边界转换只出现在 Infrastructure，
        // Domain/Presentation 不直接互相污染坐标系。
        private static Vector3 ToUnity(Vec3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static Vec3 FromUnity(Vector3 value)
        {
            return new Vec3(value.x, value.y, value.z);
        }
    }
}
