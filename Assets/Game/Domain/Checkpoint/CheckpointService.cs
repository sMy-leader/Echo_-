using Game.Core;

namespace Game.Domain
{
    /// <summary>
    /// 检查点快照。M0 只存玩家位置/朝向；后续会把机关状态、残影列表等加进来。
    /// </summary>
    public readonly struct CheckpointSnapshot
    {
        public readonly Vec3 Position;
        public readonly float Yaw;
        public readonly bool IsValid;

        public CheckpointSnapshot(Vec3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
            IsValid = true;
        }

        public static CheckpointSnapshot Empty => default;
    }

    /// <summary>
    /// 检查点服务。回溯/失败恢复都从这里取快照。
    /// 只保留最近一个检查点，降低内存与实现复杂度。
    /// </summary>
    public sealed class CheckpointService
    {
        private CheckpointSnapshot _snapshot;

        public bool HasCheckpoint => _snapshot.IsValid;
        public CheckpointSnapshot Current => _snapshot;

        public void Capture(Vec3 position, float yaw)
        {
            _snapshot = new CheckpointSnapshot(position, yaw);
        }

        /// <summary>
        /// 把玩家传送回快照位置。没有快照时什么都不做，避免新关卡第一帧把玩家传到原点。
        /// </summary>
        public void Restore(PlayerMotor player)
        {
            if (!_snapshot.IsValid || player == null)
            {
                return;
            }

            player.Teleport(_snapshot.Position, _snapshot.Yaw);
        }
    }
}
