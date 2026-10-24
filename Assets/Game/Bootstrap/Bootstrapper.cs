using Game.Core;
using Game.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Bootstrap
{
    /// <summary>
    /// M0 启动器：直接加载 Level_00。
    ///
    /// 为什么先不加载热更：
    /// - 当前 Windows 单机 Demo 使用 Mono 构建，不需要代码热更；
    /// - HybridCLR 是可选扩展，当前保持 enable=0，避免日常构建被 AOT 元数据问题打断。
    /// 如果以后启用热更，这个类会改为：加载 dll → 反射 HotfixEntry → 再由入口加载场景。
    /// </summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private string firstScene = "MainMenu";

        // UnityEngine 也有 ILogger，这里显式写 Game.Core.ILogger。
        private readonly Game.Core.ILogger _logger = new UnityLogger();

        private void Start()
        {
            if (string.IsNullOrEmpty(firstScene))
            {
                _logger.Warning("Bootstrapper: firstScene is empty, nothing to load.");
                return;
            }

            _logger.Info("Bootstrapper: loading scene " + firstScene);
            SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
        }
    }
}
