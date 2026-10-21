using System.Runtime.InteropServices;
using Game.Core;
using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EchoFrame 测试：16 字节内存布局与朝向量化精度。
    /// </summary>
    public sealed class EchoFrameTests
    {
        [Test]
        public void EchoFrame_Is16Bytes()
        {
            Assert.AreEqual(16, Marshal.SizeOf<EchoFrame>());
        }

        [Test]
        public void YawEncoding_RoundTripsWithinOneStep()
        {
            EchoFrame frame = new EchoFrame(Vec3.Zero, 45f, EchoState.Idle);

            Assert.AreEqual(45f, frame.YawDegrees, 0.01f);
            Assert.AreEqual(EchoState.Idle, frame.EchoState);
        }
    }
}
