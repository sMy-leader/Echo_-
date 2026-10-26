using Game.Application;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class ProgressServiceTests
    {
        [Test]
        public void Level01_UnlocksOnlyAfterLevel00()
        {
            FakeStore store = new FakeStore();
            ProgressService progress = new ProgressService(store);

            Assert.IsTrue(progress.IsUnlocked(LevelIds.Level00));
            Assert.IsFalse(progress.IsUnlocked(LevelIds.Level01));

            progress.MarkCompleted(LevelIds.Level00);
            Assert.IsTrue(progress.IsUnlocked(LevelIds.Level01));
        }

        private sealed class FakeStore : IProgressStore
        {
            private readonly System.Collections.Generic.HashSet<string> _completed =
                new System.Collections.Generic.HashSet<string>();

            public bool IsCompleted(string levelId)
            {
                return _completed.Contains(levelId);
            }

            public void MarkCompleted(string levelId)
            {
                _completed.Add(levelId);
            }
        }
    }
}
