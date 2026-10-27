using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class LiftTests
    {
        [Test]
        public void MovesToTopAndStops()
        {
            Lift lift = new Lift(0f, 4f, 2f);

            lift.Tick(1f, true);
            Assert.AreEqual(2f, lift.Height, 0.001f);

            lift.Tick(2f, true);
            Assert.AreEqual(4f, lift.Height, 0.001f);
            Assert.IsTrue(lift.IsAtTop);
        }

        [Test]
        public void MovesDownWhenInactive()
        {
            Lift lift = new Lift(0f, 4f, 2f);
            lift.Tick(3f, true);

            lift.Tick(1f, false);

            Assert.AreEqual(2f, lift.Height, 0.001f);
            Assert.IsFalse(lift.IsAtBottom);
        }

        [Test]
        public void ResetReturnsToBottom()
        {
            Lift lift = new Lift(0f, 4f, 2f);
            lift.Tick(3f, true);

            lift.Reset();

            Assert.AreEqual(0f, lift.Height, 0.001f);
            Assert.IsTrue(lift.IsAtBottom);
        }
    }
}
