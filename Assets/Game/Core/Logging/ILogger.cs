namespace Game.Core
{
    /// <summary>
    /// 日志契约。Domain / Application 只依赖这个接口，不直接调用 UnityEngine.Debug。
    /// 这样纯 C# 层可以在测试里注入 NullLogger / 记录器，而不需要 Unity 运行时。
    /// </summary>
    public interface ILogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}
