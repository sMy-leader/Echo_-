namespace Game.Core
{
    /// <summary>
    /// 进度存储契约。Application 只依赖它；Unity PlayerPrefs/JSON 实现在 Infrastructure。
    /// </summary>
    public interface IProgressStore
    {
        bool IsCompleted(string levelId);
        void MarkCompleted(string levelId);
    }
}
