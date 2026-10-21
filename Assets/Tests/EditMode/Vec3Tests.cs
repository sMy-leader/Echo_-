using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Vec3 纯数学回归测试：归一化、投影、插值与边界 clamp。
    /// </summary>
    public sealed class Vec3Tests
    {
        [Test]
        public void Normalized_ReturnsUnitVector()
        {
            Vec3 normalized = new Vec3(3f, 4f, 0f).Normalized;

            Assert.AreEqual(0.6f, normalized.X, 1e-5f);
            Assert.AreEqual(0.8f, normalized.Y, 1e-5f);
            Assert.AreEqual(1f, normalized.Magnitude, 1e-5f);
        }

        [Test]
        public void ProjectOnPlane_RemovesNormalComponent()
        {
            Vec3 projected = Vec3.ProjectOnPlane(new Vec3(1f, 2f, 3f), Vec3.Up);

            Assert.IsTrue(projected.Approximately(new Vec3(1f, 0f, 3f)));
        }

        [Test]
        public void Lerp_ClampsAndInterpolates()
        {
            Vec3 a = new Vec3(0f, 0f, 0f);
            Vec3 b = new Vec3(10f, 0f, 0f);

            Assert.IsTrue(Vec3.Lerp(a, b, 0.5f).Approximately(new Vec3(5f, 0f, 0f)));
            Assert.IsTrue(Vec3.Lerp(a, b, -1f).Approximately(a));
            Assert.IsTrue(Vec3.Lerp(a, b, 2f).Approximately(b));
        }
    }
}
