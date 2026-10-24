using System.Collections;
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

        public bool IsLoading => _isLoading;

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

        /// <summary>
        /// 异步加载场景。重复点击会被 IsLoading 挡掉，避免并发加载。
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading || string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            _isLoading = true;
            _fade = 0f;

            // 先把画面压黑再切换，避免场景加载瞬间露出空白。
            yield return new WaitForSecondsRealtime(0.15f);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                _isLoading = false;
                Debug.LogError("LevelFlow: failed to load scene " + sceneName);
                yield break;
            }

            while (!op.isDone)
            {
                yield return null;
            }

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
