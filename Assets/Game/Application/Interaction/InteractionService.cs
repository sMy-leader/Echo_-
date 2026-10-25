using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Application
{
    /// <summary>
    /// 交互查询：玩家按 E 时找最近的可交互物。
    ///
    /// 当前只处理玩家；残影交互后续通过 EchoState.Interact 接入，
    /// 接口本身不依赖具体 Actor 类型。
    /// </summary>
    public sealed class InteractionService
    {
        private readonly List<IInteractable> _interactables = new List<IInteractable>();

        public void Add(IInteractable interactable)
        {
            if (interactable != null && !_interactables.Contains(interactable))
            {
                _interactables.Add(interactable);
            }
        }

        public bool TryInteract(PerceptionTarget actor, bool pressed)
        {
            if (!pressed)
            {
                return false;
            }

            IInteractable best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < _interactables.Count; i++)
            {
                IInteractable candidate = _interactables[i];
                if (!candidate.CanInteract(actor.Id))
                {
                    continue;
                }

                Vec3 delta = candidate.Position - actor.Position;
                delta = new Vec3(delta.X, 0f, delta.Z);
                float sqr = delta.SqrMagnitude;
                if (sqr > candidate.Radius * candidate.Radius)
                {
                    continue;
                }

                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            if (best == null)
            {
                return false;
            }

            best.Interact(actor.Id);
            return true;
        }
    }
}
