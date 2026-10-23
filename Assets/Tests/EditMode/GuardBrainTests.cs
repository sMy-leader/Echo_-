using System;
using System.Collections.Generic;
using Game.Core;
using Game.Domain;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 守卫第一块测试：视锥、遮挡、警戒值、状态迁移。
    /// 用 FakeVisionWorld 隔离 Unity Physics，只验证 Domain 规则。
    /// </summary>
    public sealed class GuardBrainTests
    {
        private const float Step = 0.1f;

        [Test]
        public void Patrol_MovesTowardWaypoint()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>();
            FakeVisionWorld vision = new FakeVisionWorld();

            Tick(guard, targets, vision, 0.5f);

            Assert.Greater(guard.Position.Z, 0f);
            Assert.AreEqual(GuardState.Patrol, guard.State);
        }

        [Test]
        public void TargetInCone_EntersSuspicious()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>
            {
                new PerceptionTarget(0, new Vec3(0f, 0f, 5f), true)
            };
            FakeVisionWorld vision = new FakeVisionWorld();

            Tick(guard, targets, vision, Step);

            Assert.IsTrue(guard.HasVisibleTarget);
            Assert.Greater(guard.Suspicion, 0f);
            Assert.AreEqual(GuardState.Suspicious, guard.State);
        }

        [Test]
        public void TargetBehindGuard_IsNotVisible()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>
            {
                new PerceptionTarget(0, new Vec3(0f, 0f, -5f), true)
            };
            FakeVisionWorld vision = new FakeVisionWorld();

            Tick(guard, targets, vision, Step);

            Assert.IsFalse(guard.HasVisibleTarget);
            Assert.AreEqual(0f, guard.Suspicion, 1e-5f);
            Assert.AreEqual(GuardState.Patrol, guard.State);
        }

        [Test]
        public void CloseTargetBehindGuard_IsVisibleBecauseOfCloseAwareness()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>
            {
                new PerceptionTarget(0, new Vec3(0f, 0f, -1.5f), true)
            };
            FakeVisionWorld vision = new FakeVisionWorld();

            Tick(guard, targets, vision, Step);

            Assert.IsTrue(guard.HasVisibleTarget);
            Assert.AreEqual(GuardState.Suspicious, guard.State);
        }

        [Test]
        public void TargetBehindWall_IsNotVisible()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>
            {
                new PerceptionTarget(0, new Vec3(0f, 0f, 5f), true)
            };
            FakeVisionWorld vision = new FakeVisionWorld { Visible = false };

            Tick(guard, targets, vision, Step);

            Assert.IsFalse(guard.HasVisibleTarget);
            Assert.AreEqual(GuardState.Patrol, guard.State);
        }

        [Test]
        public void Suspicion60_EntersInvestigate()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>
            {
                new PerceptionTarget(0, new Vec3(0f, 0f, 5f), true)
            };
            FakeVisionWorld vision = new FakeVisionWorld();

            Tick(guard, targets, vision, 1.6f);

            Assert.GreaterOrEqual(guard.Suspicion, 60f);
            Assert.AreEqual(GuardState.Investigate, guard.State);
        }

        [Test]
        public void Suspicion100_EntersAlert()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>
            {
                new PerceptionTarget(0, new Vec3(0f, 0f, 5f), true)
            };
            FakeVisionWorld vision = new FakeVisionWorld();

            Tick(guard, targets, vision, 2.6f);

            Assert.GreaterOrEqual(guard.Suspicion, 100f);
            Assert.AreEqual(GuardState.Alert, guard.State);
        }

        [Test]
        public void AlertLosesSight_EntersSearch()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>
            {
                new PerceptionTarget(0, new Vec3(0f, 0f, 5f), true)
            };
            FakeVisionWorld vision = new FakeVisionWorld();

            Tick(guard, targets, vision, 2.6f);
            Assert.AreEqual(GuardState.Alert, guard.State);

            vision.Visible = false;
            Tick(guard, targets, vision, 6.1f);

            Assert.AreEqual(GuardState.Search, guard.State);
        }

        [Test]
        public void SearchTimesOut_EntersReturn()
        {
            GuardBrain guard = CreateGuard(new[] { new Vec3(0f, 0f, 5f) });
            List<PerceptionTarget> targets = new List<PerceptionTarget>
            {
                new PerceptionTarget(0, new Vec3(0f, 0f, 5f), true)
            };
            FakeVisionWorld vision = new FakeVisionWorld();

            Tick(guard, targets, vision, 2.6f);
            vision.Visible = false;
            Tick(guard, targets, vision, 6.1f);
            Assert.AreEqual(GuardState.Search, guard.State);

            Tick(guard, targets, vision, 8.1f);

            Assert.AreEqual(GuardState.Return, guard.State);
        }

        private static GuardBrain CreateGuard(IReadOnlyList<Vec3> patrolPoints)
        {
            return new GuardBrain(
                1,
                Vec3.Zero,
                0f,
                patrolPoints,
                new GuardConfig());
        }

        private static void Tick(
            GuardBrain guard,
            IReadOnlyList<PerceptionTarget> targets,
            IVisionWorld vision,
            float seconds)
        {
            int steps = (int)Math.Round(seconds / Step);
            for (int i = 0; i < steps; i++)
            {
                guard.Tick(Step, targets, vision);
            }
        }

        private sealed class FakeVisionWorld : IVisionWorld
        {
            public bool Visible = true;

            bool IVisionWorld.HasLineOfSight(Vec3 from, Vec3 to)
            {
                return Visible;
            }
        }
    }
}
