using Game.Application;
using Game.Core;
using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 玩家运动学测试：前进移动、状态切换、传送重置插值基准。
    /// 使用 FakeCollisionWorld 隔离 Unity Physics。
    /// </summary>
    public sealed class PlayerMotorTests
    {
        [Test]
        public void Tick_WithForwardInput_MovesAlongZ()
        {
            PlayerMotor player = new PlayerMotor(
                Vec3.Zero,
                0f,
                GameConstants.PlayerRadius,
                GameConstants.PlayerHeight);
            FakeCollisionWorld world = new FakeCollisionWorld();

            player.Tick(
                GameConstants.TickDelta,
                InputFrame.FromAxes(0f, 1f),
                world);

            Assert.Greater(player.Position.Z, 0f);
            Assert.AreEqual(EchoState.Walk, player.State);
            Assert.IsTrue(player.IsGrounded);
        }

        [Test]
        public void Teleport_ResetsPreviousPosition()
        {
            PlayerMotor player = new PlayerMotor(
                Vec3.Zero,
                0f,
                GameConstants.PlayerRadius,
                GameConstants.PlayerHeight);
            player.Tick(
                GameConstants.TickDelta,
                InputFrame.FromAxes(0f, 1f),
                new FakeCollisionWorld());

            player.Teleport(new Vec3(5f, 0f, 5f), 90f);

            Assert.IsTrue(player.Position.Approximately(new Vec3(5f, 0f, 5f)));
            Assert.IsTrue(player.PreviousPosition.Approximately(new Vec3(5f, 0f, 5f)));
            Assert.AreEqual(90f, player.Yaw, 1e-4f);
        }
    }

    internal sealed class FakeCollisionWorld : ICollisionWorld
    {
        public CapsuleCastResult CapsuleCast(
            Vec3 bottomCenter,
            float radius,
            float height,
            Vec3 direction,
            float distance)
        {
            return CapsuleCastResult.None;
        }

        public bool CheckGround(Vec3 bottomCenter, float radius, float distance, out Vec3 normal)
        {
            normal = Vec3.Up;
            return true;
        }
    }
}
