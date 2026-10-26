using System;
using Game.Core;

namespace Game.Application
{
    /// <summary>
    /// 关卡解锁规则：L0 默认开放；后续关卡要求前一关完成。
    /// 存储层只记录 completed，不记录复杂进度结构，方便以后替换成存档系统。
    /// </summary>
    public sealed class ProgressService
    {
        private static readonly string[] Order =
        {
            LevelIds.Level00,
            LevelIds.Level01,
            LevelIds.Level02,
            LevelIds.Level03
        };

        private readonly IProgressStore _store;

        public ProgressService(IProgressStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool IsCompleted(string levelId)
        {
            return !string.IsNullOrEmpty(levelId) && _store.IsCompleted(levelId);
        }

        public bool IsUnlocked(string levelId)
        {
            int index = Array.IndexOf(Order, levelId);
            if (index <= 0)
            {
                return index == 0;
            }

            return IsCompleted(Order[index - 1]);
        }

        public void MarkCompleted(string levelId)
        {
            if (!string.IsNullOrEmpty(levelId))
            {
                _store.MarkCompleted(levelId);
            }
        }
    }
}
