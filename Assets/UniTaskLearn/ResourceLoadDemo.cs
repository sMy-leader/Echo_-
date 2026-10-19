using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System;

public class ResourceLoadDemo : MonoBehaviour
{
    public Button loadBtn;
    public Button cancelBtn;
    public Text progressText;
    public Image resultImg;

    private CancellationTokenSource _loadcts;

    private void Start()
    {
        loadBtn.onClick.AddListener(StartLoad);
        cancelBtn.onClick.AddListener(CancelLoad);
        cancelBtn.interactable = false;// 初始禁用取消按钮
    }


    private void StartLoad()
    {
        _loadcts = new CancellationTokenSource();

        //更新UI状态
        loadBtn.interactable = false;
        cancelBtn.interactable = true;
        progressText.text = "加载中....%0";
        resultImg.sprite = null;

        LoadResourceWithProgress(_loadcts.Token).Forget();
    }

    async UniTask LoadResourceWithProgress(CancellationToken ct)
    {

        try
        {
            ResourceRequest request = Resources.LoadAsync<Sprite>("TargetImg");

            // 轮询进度，直到加载完成
            while (!request.isDone)
            {
                ct.ThrowIfCancellationRequested(); // 支持取消
                progressText.text = $"加载中...{Mathf.Round(request.progress * 100)}%";
                await UniTask.Yield(ct);
            }

            // 等待加载完成
            Sprite sprite = request.asset as Sprite;
            progressText.text = "加载完成！";
            resultImg.sprite = sprite;
        }

        catch (OperationCanceledException)
        {
            progressText.text = "加载已取消";
        }
        catch (Exception e)
        {
            progressText.text = "加载失败：" + e.Message;
        }
        finally
        {
            // 恢复UI状态（无论成功/失败/取消）
            loadBtn.interactable = true;
            cancelBtn.interactable = false;
            _loadcts?.Dispose();
        }
    }


    // 取消加载
    void CancelLoad()
    {
        if (_loadcts != null && !_loadcts.IsCancellationRequested)
        {
            _loadcts.Cancel();
        }
    }

    void OnDestroy()
    {
        CancelLoad();
    }
}
