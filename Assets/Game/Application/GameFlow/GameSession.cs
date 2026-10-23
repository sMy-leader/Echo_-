using System;
using System.Collections.Generic;
using Game.Core;
using Game.Domain;

namespace Game.Application
{
    /// <summary>
    /// M0 的用例编排：玩家移动、录制、残影回放、压力板、检查点/回溯。
    /// 纯 C#，不引用 UnityEngine；Unity 侧只通过 ICollisionWorld 注入物理查询。
    ///
    /// Tick 顺序（重要）：
    /// 1. 冷却递减；
    /// 2. 玩家移动；
    /// 3. 记录输入帧 + 残影轨迹帧；
    /// 4. 前进已有残影。
    /// 这样“本体这一 tick 的位置”和“残影这一 tick 的帧”来自同一个 tick 编号。
    /// </summary>
    public sealed class GameSession
    {
        private readonly ICollisionWorld _collision;
        private readonly CheckpointService _checkpoints = new CheckpointService();
        private float _rewindCooldown;

        public PlayerMotor Player { get; }
        public TimelineRecorder InputRecorder { get; } = new TimelineRecorder();
        public EchoRecorder EchoRecorder { get; } = new EchoRecorder();
        public EchoManager Echoes { get; } = new EchoManager();
        public ActivationAggregator Plate { get; } = new ActivationAggregator();

        public float RewindCooldown => _rewindCooldown;

        /// <summary>
        /// 可以回溯的条件：冷却结束 + 本轮至少录到一帧输入 + 至少录到一帧残影轨迹。
        /// 两个录制器都检查，避免“刚生成空残影”这种边界情况。
        /// </summary>
        public bool CanRewind =>
            _rewindCooldown <= 0f
            && InputRecorder.Count > 0
            && EchoRecorder.Count > 0;

        public GameSession(ICollisionWorld collision, Vec3 spawn, float yaw)
        {
            _collision = collision ?? throw new ArgumentNullException(nameof(collision));
            Player = new PlayerMotor(
                spawn,
                yaw,
                GameConstants.PlayerRadius,
                GameConstants.PlayerHeight);
        }

        /// <summary>
        /// 在出生点/检查点触发区调用。快照只存玩家位置与朝向。
        /// </summary>
        public void CaptureCheckpoint()
        {
            _checkpoints.Capture(Player.Position, Player.Yaw);
        }

        /// <summary>
        /// 失败/掉出场景时恢复：回检查点，但**不生成残影**。
        /// 这与手动回溯的区别是：失败不是一种资源，不能用来刷残影。
        /// </summary>
        public void RestoreCheckpoint()
        {
            _checkpoints.Restore(Player);
            Echoes.ResetAll();
            InputRecorder.Clear();
            EchoRecorder.Clear();
            Plate.Reset();
        }

        /// <summary>
        /// 手动回溯：
        /// 1. 用当前录制创建一个新残影（构造时拷贝帧）；路线：Create->EchoManager->EchoPlayback->EchoFrame；
        /// 2. 玩家回检查点；
        /// 3. 所有残影回到 t=0；
        /// 4. 清空本轮录制，开始新一轮。
        ///
        /// 顺序不能反：如果先 Clear，EchoPlayback 就拷贝不到本轮轨迹。
        /// </summary>
        public bool TryRewind()
        {
            if (!CanRewind)
            {
                return false;
            }

            Echoes.Create(EchoRecorder);
            _checkpoints.Restore(Player);
            Echoes.ResetAll();
            InputRecorder.Clear();
            EchoRecorder.Clear();
            Plate.Reset();
            _rewindCooldown = GameConstants.RewindCooldown;
            return true;
        }

        /// <summary>
        /// 前进一个逻辑 tick。压力板不在这里更新，因为表现层需要先根据
        /// “移动后的位置”统计激活源数量，再调用 TickPlate。
        /// </summary>
        public void Tick(float dt, in InputFrame input)
        {
            _rewindCooldown = Math.Max(0f, _rewindCooldown - dt);

            Player.Tick(dt, input, _collision);

            //分别记录玩家输入和残影轨迹，注意顺序：先记录玩家本体的这一帧，再记录残影的这一帧。
            InputRecorder.Record(input);
            EchoRecorder.Record(new EchoFrame(Player.Position, Player.Yaw, Player.State));

            Echoes.Tick();
        }

        /// <summary>
        /// 把“玩家 + 残影”整理成守卫可感知的目标列表。
        /// 复用外部 List，避免每 tick 分配；玩家 Id 固定为 0，残影用 EchoPlayback.Id。
        /// </summary>
        public void CollectPerceptionTargets(List<PerceptionTarget> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();
            //将玩家的位置每帧传入进去
            buffer.Add(new PerceptionTarget(0, Player.Position, true));

            IReadOnlyList<EchoPlayback> echoes = Echoes.Echoes;
            for (int i = 0; i < echoes.Count; i++)
            {
                //如果残影已经死亡，就不传入守卫的感知列表中
                if (!echoes[i].IsAlive)
                {
                    continue;
                }

                buffer.Add(new PerceptionTarget(echoes[i].Id, echoes[i].CurrentPosition, false));//传入残影的位置
            }
        }

        /// <summary>
        /// 由表现层传入“当前站在压力板上的本体 + 残影数量”。
        /// Domain 不知道压力板的几何形状，只负责聚合规则。
        /// </summary>
        public void TickPlate(float dt, int activationCount)
        {
            Plate.Tick(dt, activationCount);
        }
    }
}
