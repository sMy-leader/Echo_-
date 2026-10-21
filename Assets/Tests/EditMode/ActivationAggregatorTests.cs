using Game.Core;
using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 机关聚合测试：OR 激活、0.3s 释放延迟、Reset 立即关闭。
    /// </summary>
    public sealed class ActivationAggregatorTests
    {
        [Test]
        public void Or_ActivatesWithOneSource()
        {
            ActivationAggregator aggregator = new ActivationAggregator();

            aggregator.Tick(GameConstants.TickDelta, 1);

            Assert.IsTrue(aggregator.IsActive);
        }

        [Test]
        public void ReleaseDelay_KeepsActiveThenReleases()
        {
            ActivationAggregator aggregator = new ActivationAggregator
            {
                ReleaseDelay = 0.3f
            };

            aggregator.Tick(0.1f, 1);
            aggregator.Tick(0.1f, 0);
            Assert.IsTrue(aggregator.IsActive);

            aggregator.Tick(0.1f, 0);
            Assert.IsTrue(aggregator.IsActive);

            aggregator.Tick(0.1f, 0);
            Assert.IsFalse(aggregator.IsActive);
        }

        [Test]
        public void Reset_DeactivatesImmediately()
        {
            ActivationAggregator aggregator = new ActivationAggregator();
            aggregator.Tick(0.1f, 1);

            aggregator.Reset();

            Assert.IsFalse(aggregator.IsActive);
        }
    }
}
