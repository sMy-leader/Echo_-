using Game.Core;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// Unity 日志实现。放在 Framework（AOT），表现层/启动器通过 ILogger 使用。
    /// 注意：UnityEngine 也有一个 ILogger，所以这里显式写 Game.Core.ILogger 避免歧义。
    /// 当前直接转发到 Debug；后续可以在这里加分级、文件轮转和 Release 开关。
    /// </summary>
    public sealed class UnityLogger : Game.Core.ILogger
    {
        public void Info(string message)
        {
            Debug.Log(message);
        }

        public void Warning(string message)
        {
            Debug.LogWarning(message);
        }

        public void Error(string message)
        {
            Debug.LogError(message);
        }
    }
}
