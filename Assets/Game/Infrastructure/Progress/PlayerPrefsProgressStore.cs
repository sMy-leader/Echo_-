using Game.Core;
using UnityEngine;

namespace Game.Infrastructure
{
    /// <summary>
    /// 最小进度存储：PlayerPrefs。仅用于流程骨架，
    /// 后续接正式存档时替换为 JSON/MessagePack 实现。
    /// </summary>
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        private const string Prefix = "echo.progress.";

        public bool IsCompleted(string levelId)
        {
            return PlayerPrefs.GetInt(Prefix + levelId, 0) != 0;
        }

        public void MarkCompleted(string levelId)
        {
            //键->prefix+关卡ID  值：0->未完成 1->完成
            PlayerPrefs.SetInt(Prefix + levelId, 1);
            PlayerPrefs.Save();
        }
    }
}
