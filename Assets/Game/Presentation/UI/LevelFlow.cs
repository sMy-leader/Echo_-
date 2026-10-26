using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Game.Application;
using Game.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Game.Presentation
{
    /// <summary>
    /// 轻量关卡流程：异步加载场景 + 简单黑屏过渡。
    ///
    /// 这是 M1 骨架：
    /// - 不做 YooAsset；
    /// - 不做存档/解锁；
    /// - 场景名直接走 Build Settings；
    /// - DontDestroyOnLoad，保证跨场景持续存在。
    ///
    /// 后续接 YooAsset 时，只需要把 LoadSceneAsync 内部替换成
    /// IAssetService/YooAsset 场景加载，外部 HubMenu/LevelPortal 不改。
    /// </summary>
    public sealed class LevelFlow : MonoBehaviour
    {
        public static LevelFlow Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("LevelFlow");
                    _instance = go.AddComponent<LevelFlow>();
                }

                return _instance;
            }
        }

        private static LevelFlow _instance;
        private bool _isLoading;
        private float _fade;
        private ProgressService _progress;//拿流程的对象

        public bool IsLoading => _isLoading;

        /// <summary>
        /// 进度服务按需创建；先 PlayerPrefs，后续替换为正式存档实现。
        /// </summary>
        public ProgressService Progress =>
            _progress ??= new ProgressService(new PlayerPrefsProgressStore());

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // 场景加载入口：推荐返回 UniTask，外部可选择 await 调用；若要完全兼容原void调用，可改为 async void
        public async UniTask LoadScene(string sceneName)
        {
            if (_isLoading || string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            // 替代原 StartCoroutine，直接 await 异步方法
            await LoadRoutine(sceneName);
        }

        /// <summary>
        /// 关卡完成时写进度；传送门在加载下一关前调用。
        /// </summary>
        public void MarkCompleted(string levelId)
        {
            Progress.MarkCompleted(levelId);
        }

        // 核心加载逻辑：协程 → async UniTask 异步方法
        private async UniTask LoadRoutine(string sceneName)
        {
            _isLoading = true;
            _fade = 0f;

            // 先把画面压黑再切换，避免场景加载瞬间露出空白
            // 对应原 WaitForSecondsRealtime：ignoreTimeScale = true 表示不受时间缩放影响
            await UniTask.Delay(TimeSpan.FromSeconds(1f), ignoreTimeScale: true);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                _isLoading = false;
                Debug.LogError("LevelFlow: failed to load scene " + sceneName);
                return;
            }

            // 等价于原 while (!op.isDone) { yield return null; }
            // UniTask 原生支持直接 await AsyncOperation，自动等待加载完成
            await op;

            _isLoading = false;
        }

        private void OnGUI()
        {
            if (!_isLoading)
            {
                return;
            }

            // 简单黑屏：不依赖 Canvas/UGUI，先保证流程骨架可用。
            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(_fade));
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
