using Game.Core;
using UnityEngine;

namespace Game.Infrastructure
{
    /// <summary>
    /// Unity Physics 视线遮挡查询。
    ///
    /// 约定：
    /// - from/to 是世界坐标；
    /// - 返回 true 表示没有遮挡；
    /// - 默认使用 Physics.DefaultRaycastLayers，忽略 Trigger；
    /// - 玩家/残影/守卫视觉都没有 Collider，所以 Linecast 只会被墙、门等阻挡。
    /// </summary>
    public sealed class PhysicsVisionWorld : IVisionWorld
    {
        private readonly LayerMask _obstacleMask;
        private readonly QueryTriggerInteraction _triggerInteraction;

        public PhysicsVisionWorld(
            LayerMask obstacleMask = default,
            QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
        {
            // mask 为 0 表示“没配置”，回退到 Unity 默认层；否则使用调用方指定的层。
            _obstacleMask = obstacleMask.value == 0
                ? Physics.DefaultRaycastLayers
                : obstacleMask;
            _triggerInteraction = triggerInteraction;
        }

        public bool HasLineOfSight(Vec3 from, Vec3 to)
        {
            Vector3 origin = new Vector3(from.X, from.Y, from.Z);
            Vector3 target = new Vector3(to.X, to.Y, to.Z);

            // Physics.Linecast 命中表示“被挡住”；所以我们取反。
            bool blocked = Physics.Linecast(
                origin,
                target,
                out _,
                _obstacleMask,
                _triggerInteraction);

            return !blocked;
        }
    }
}
