using System.Runtime.InteropServices;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 输入帧测试：轴量化、死区、按钮位、朝向编码、4 字节内存布局。
    /// </summary>
    public sealed class InputFrameTests
    {
        [Test]
        public void InputFrame_Is4Bytes()
        {
            // 这份布局直接决定 30 秒录制缓冲的大小；改字段时要同步更新。
            Assert.AreEqual(4, Marshal.SizeOf<InputFrame>());
        }

        [Test]
        public void QuantizeAxis_ClampsAndZeroesDeadZone()
        {
            Assert.AreEqual((sbyte)127, InputFrame.QuantizeAxis(1.5f));
            Assert.AreEqual((sbyte)-127, InputFrame.QuantizeAxis(-1.5f));
            Assert.AreEqual((sbyte)0, InputFrame.QuantizeAxis(0.005f));
            Assert.AreEqual((sbyte)64, InputFrame.QuantizeAxis(0.5f));
        }

        [Test]
        public void Buttons_AreBitFlags()
        {
            InputFrame frame = InputFrame.FromAxes(
                0f,
                0f,
                InputButtons.Sneak | InputButtons.Interact);

            Assert.IsTrue(frame.Has(InputButtons.Sneak));
            Assert.IsTrue(frame.Has(InputButtons.Interact));
            Assert.IsFalse(frame.Has(InputButtons.Rewind));
            Assert.IsTrue(frame.IsSneaking);
        }

        [Test]
        public void Yaw_RoundTripsWithinOneStep()
        {
            InputFrame frame = InputFrame.FromAxes(0f, 0f, yawDegrees: 90f);

            Assert.AreEqual(90f, frame.YawDegrees, 1f);
        }
    }
}
