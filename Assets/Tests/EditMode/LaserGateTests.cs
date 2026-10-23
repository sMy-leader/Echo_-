using Game.Core;
using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 激光门时间窗口测试：只验证纯 C# 状态，不依赖场景/物理。
    /// </summary>
    public sealed class LaserGateTests
    {
        [Test]
        public void Periodic_OnOffCycle_Works()
        {
            LaserGate gate = new LaserGate(LaserGateMode.Periodic, 2f, 3f);

            Assert.IsTrue(gate.IsActive);

            gate.Tick(1.9f);
            Assert.IsTrue(gate.IsActive);

            gate.Tick(0.2f);
            Assert.IsFalse(gate.IsActive);

            gate.Tick(3f);
            Assert.IsTrue(gate.IsActive);
        }

        [Test]
        public void AlwaysOn_StaysActive()
        {
            LaserGate gate = new LaserGate(LaserGateMode.AlwaysOn, 0.01f, 0f);
            gate.Tick(100f);

            Assert.IsTrue(gate.IsActive);
        }
    }
}
