using Game.Core;
using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 录制缓冲测试：环形回绕、最旧帧索引、清空、残影录制。
    /// </summary>
    public sealed class TimelineRecorderTests
    {
        [Test]
        public void RingBuffer_KeepsNewestFrames()
        {
            TimelineRecorder recorder = new TimelineRecorder(4);

            for (int i = 0; i < 6; i++)
            {
                recorder.Record(new InputFrame((sbyte)i, 0, 0));
            }

            Assert.AreEqual(4, recorder.Count);
            Assert.IsTrue(recorder.IsFull);
            Assert.AreEqual((sbyte)2, recorder.GetFrame(0).MoveX);
            Assert.AreEqual((sbyte)5, recorder.GetFrame(3).MoveX);
        }

        [Test]
        public void Clear_ResetsCountAndFrames()
        {
            TimelineRecorder recorder = new TimelineRecorder(2);
            recorder.Record(new InputFrame(1, 0, 0));
            recorder.Record(new InputFrame(2, 0, 0));

            recorder.Clear();

            Assert.AreEqual(0, recorder.Count);
            Assert.IsFalse(recorder.IsFull);
        }

        [Test]
        public void EchoRecorder_ReplaysOldestRetainedFrame()
        {
            EchoRecorder recorder = new EchoRecorder(3);

            for (int i = 0; i < 4; i++)
            {
                recorder.Record(new EchoFrame(new Vec3(i, 0f, 0f), 0f, EchoState.Walk));
            }

            Assert.AreEqual(3, recorder.Count);
            Assert.AreEqual(1f, recorder.GetFrame(0).Position.X, 1e-5f);
            Assert.AreEqual(3f, recorder.GetFrame(2).Position.X, 1e-5f);
        }
    }
}
