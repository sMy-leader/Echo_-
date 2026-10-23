using System.Collections.Generic;
using Game.Application;
using Game.Core;
using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 回溯用例测试：录制要求、检查点恢复、残影生成、冷却、失败恢复不生成残影。
    /// </summary>
    public sealed class GameSessionTests
    {
        private static GameSession CreateSession()
        {
            GameSession session = new GameSession(
                new FakeCollisionWorld(),
                Vec3.Zero,
                0f);
            session.CaptureCheckpoint();
            return session;
        }

        private static void Tick(GameSession session, int frames, float moveY = 1f)
        {
            InputFrame input = InputFrame.FromAxes(0f, moveY);

            for (int i = 0; i < frames; i++)
            {
                session.Tick(GameConstants.TickDelta, input);
            }
        }

        [Test]
        public void TryRewind_RequiresRecordedFrames()
        {
            GameSession session = CreateSession();

            Assert.IsFalse(session.TryRewind());
            Assert.AreEqual(0, session.Echoes.Count);
        }

        [Test]
        public void TryRewind_CreatesEchoAndRestoresCheckpoint()
        {
            GameSession session = CreateSession();
            Tick(session, 10);

            Assert.IsTrue(session.CanRewind);
            Assert.IsTrue(session.Player.Position.Z > 0f);

            bool rewound = session.TryRewind();

            Assert.IsTrue(rewound);
            Assert.AreEqual(1, session.Echoes.Count);
            Assert.AreEqual(10, session.Echoes.Echoes[0].FrameCount);
            Assert.IsTrue(session.Player.Position.Approximately(Vec3.Zero));
            Assert.AreEqual(0, session.InputRecorder.Count);
            Assert.AreEqual(0, session.EchoRecorder.Count);
        }

        [Test]
        public void RestoreCheckpoint_DoesNotCreateEcho()
        {
            GameSession session = CreateSession();
            Tick(session, 10);

            session.RestoreCheckpoint();

            Assert.AreEqual(0, session.Echoes.Count);
            Assert.IsTrue(session.Player.Position.Approximately(Vec3.Zero));
            Assert.AreEqual(0, session.InputRecorder.Count);
        }

        [Test]
        public void CollectPerceptionTargets_IncludesPlayerAndEchoes()
        {
            GameSession session = CreateSession();
            Tick(session, 10);
            Assert.IsTrue(session.TryRewind());
            Tick(session, 1);

            List<PerceptionTarget> targets = new List<PerceptionTarget>();
            session.CollectPerceptionTargets(targets);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(0, targets[0].Id);
            Assert.IsTrue(targets[0].IsPlayer);
            Assert.IsTrue(targets[0].IsAlive);
            Assert.IsFalse(targets[1].IsPlayer);
            Assert.Greater(targets[1].Id, 0);
            Assert.IsTrue(targets[1].IsAlive);
        }

        [Test]
        public void RewindCooldown_BlocksImmediateSecondRewind()
        {
            GameSession session = CreateSession();
            Tick(session, 10);

            Assert.IsTrue(session.TryRewind());
            Assert.IsFalse(session.TryRewind());
        }
    }
}
