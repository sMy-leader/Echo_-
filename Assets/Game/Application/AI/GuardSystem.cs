using System.Collections.Generic;
using Game.Core;
using Game.Domain;

namespace Game.Application
{
    /// <summary>
    /// 守卫集合的用例编排。
    ///
    /// 当前 M1 第一块只有一个守卫，但保留 List 是为后续双守卫/多守卫扩展。
    /// 每 tick 由 GameLoop 传入“玩家 + 残影”的目标列表和视线世界。
    /// </summary>
    public sealed class GuardSystem
    {
        private readonly List<GuardBrain> _guards = new List<GuardBrain>();

        public IReadOnlyList<GuardBrain> Guards => _guards;

        public void Add(GuardBrain guard)
        {
            if (guard != null)
            {
                _guards.Add(guard);
            }
        }

        public void Tick(float dt, IReadOnlyList<PerceptionTarget> targets, IVisionWorld vision)
        {
            for (int i = 0; i < _guards.Count; i++)
            {
                _guards[i].Tick(dt, targets, vision);//根据此时残影的数量->Brain的Tick->做感知和追击等等逻辑
            }
        }
    }
}
