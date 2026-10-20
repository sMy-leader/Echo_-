//案例4：分步场景加载+平滑进度条
//【应用场景】切换大场景时，解决Unity原生LoadSceneAsync进度跳变严重的问题，通过多阶段拆分+插值平滑进度，大幅提升加载体验。
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading;
using System;

public class SceneLoader : MonoBehaviour
{
    public Slider loadingSlider;
    public Text progressText;

    public void LoadGameScene(string sceneName)
    {
        LoadSceneAsync(sceneName, this.GetCancellationTokenOnDestroy()).Forget();//用getCancellationTokenOnDestroy()确保在对象销毁时取消加载任务，避免内存泄漏
    }

    private async UniTask LoadSceneAsync(string sceneName, CancellationToken cancelToken)
    {
        // 阶段1：场景资源加载（占总进度70%）
        var loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = false; // 禁止自动激活场景

        while (!loadOperation.isDone && !cancelToken.IsCancellationRequested)
        {
            // Unity原生进度到0.9停止，映射到0-70%
            float progress = Mathf.Clamp01(loadOperation.progress / 0.9f) * 0.7f;
            UpdateProgress(progress);
            await UniTask.Yield(cancelToken);
        }

        // 阶段2：预加载场景核心资源（占20%）
        await PreloadSceneResourcesAsync(0.7f, 0.9f, cancelToken);

        // 阶段3：激活场景+初始化（占10%）
        loadOperation.allowSceneActivation = true;
        await UniTask.DelayFrame(2, cancellationToken: cancelToken); // 等待场景初始化
        UpdateProgress(1f);

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancelToken);
        // 隐藏加载界面逻辑
    }

    private async UniTask PreloadSceneResourcesAsync(float startProgress, float endProgress, CancellationToken cancelToken)
    {
        // 示例：预加载多个核心资源，分步更新进度
        var resources = new[] { "Player", "Enemy", "BattleEffect" };
        float step = (endProgress - startProgress) / resources.Length;

        for (int i = 0; i < resources.Length; i++)
        {
            await Resources.LoadAsync<GameObject>(resources[i]).WithCancellation(cancelToken);//withCancellation确保在取消时停止加载
            UpdateProgress(startProgress + step * (i + 1));//分步更新进度条
        }
    }

    private void UpdateProgress(float value)
    {
        loadingSlider.value = value;
        progressText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}

//【核心要点】
//1.	拆分加载阶段，解决Unity场景加载进度跳变的通病
//2.	allowSceneActivation = false精准控制场景激活时机
//3.	全程支持取消，玩家返回大厅时可立即中断加载流程
