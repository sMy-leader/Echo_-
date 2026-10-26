using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class TimeLockTests
    {
        [Test]
        public void OpensOnlyWhenAllSourcesAreActive()
        {
            TimeLock timeLock = new TimeLock(3);

            timeLock.SetSource(0, true);
            timeLock.SetSource(1, true);
            Assert.IsFalse(timeLock.IsOpen);

            timeLock.SetSource(2, true);
            Assert.IsTrue(timeLock.IsOpen);
            Assert.AreEqual(3, timeLock.ActiveCount);
        }

        [Test]
        public void ClosingOneSourceKeepsLockOpen()
        {
            TimeLock timeLock = new TimeLock(3);
            timeLock.SetSource(0, true);
            timeLock.SetSource(1, true);
            timeLock.SetSource(2, true);

            timeLock.SetSource(1, false);

            Assert.IsTrue(timeLock.IsOpen);
            Assert.AreEqual(2, timeLock.ActiveCount);
        }

        [Test]
        public void ResetClearsAllSources()
        {
            TimeLock timeLock = new TimeLock(3);
            timeLock.SetSource(0, true);
            timeLock.SetSource(1, true);
            timeLock.SetSource(2, true);

            timeLock.Reset();

            Assert.IsFalse(timeLock.IsOpen);
            Assert.AreEqual(0, timeLock.ActiveCount);
        }
    }
}
