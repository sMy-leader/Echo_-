using Game.Core;
using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 残影管理测试：3 个上限 FIFO、结束帧保留一 tick 再移除。
    /// </summary>
    public sealed class EchoManagerTests
    {
        [Test]
        public void Create_EnforcesMaxThreeFifo()
        {
            EchoManager manager = new EchoManager();

            for (int i = 0; i < 4; i++)
            {
                EchoRecorder recorder = new EchoRecorder(2);
                recorder.Record(new EchoFrame(new Vec3(i, 0f, 0f), 0f, EchoState.Walk));
                recorder.Record(new EchoFrame(new Vec3(i, 0f, 1f), 0f, EchoState.Walk));
                manager.Create(recorder);
            }

            Assert.AreEqual(3, manager.Count);
            Assert.AreEqual(2, manager.Echoes[0].Id);
            Assert.AreEqual(4, manager.Echoes[2].Id);
        }

        [Test]
        public void Tick_RemovesFinishedEchoOnNextTick()
        {
            EchoManager manager = new EchoManager();
            EchoRecorder recorder = new EchoRecorder(1);
            recorder.Record(new EchoFrame(Vec3.Zero, 0f, EchoState.Idle));
            manager.Create(recorder);

            manager.Tick();
            Assert.AreEqual(1, manager.Count);
            Assert.IsTrue(manager.Echoes[0].IsFinished);

            manager.Tick();
            Assert.AreEqual(0, manager.Count);
        }
        [Test]
        public void TryKill_MarksDeadAndRemovesOnNextTick()
        {
            EchoManager manager = new EchoManager();
            EchoRecorder recorder = new EchoRecorder(2);
            recorder.Record(new EchoFrame(Vec3.Zero, 0f, EchoState.Walk));
            recorder.Record(new EchoFrame(new Vec3(0f, 0f, 1f), 0f, EchoState.Walk));
            EchoPlayback echo = manager.Create(recorder);

            Assert.IsTrue(manager.TryKill(echo.Id));
            Assert.IsFalse(echo.IsAlive);
            Assert.IsFalse(manager.TryKill(echo.Id));

            manager.Tick();
            Assert.AreEqual(0, manager.Count);
        }
    }
}
