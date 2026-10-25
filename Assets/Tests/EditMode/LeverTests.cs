using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class LeverTests
    {
        [Test]
        public void Toggle_FlipsState()
        {
            Lever lever = new Lever();

            Assert.IsFalse(lever.IsOn);
            lever.Toggle();
            Assert.IsTrue(lever.IsOn);
            lever.Toggle();
            Assert.IsFalse(lever.IsOn);
        }
    }
}
