//【应用场景】游戏中同时触发多个弹窗（奖励、公告、活动）时，按优先级串行弹出，同一时间只显示一个，避免界面重叠混乱。
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    private readonly Queue<PopupInfo> _popupQueue = new Queue<PopupInfo>();//新建一个队列—先进先出
    private bool _isShowing;
    private CancellationTokenSource _destroyCts;//取消令牌

    private void Awake()
    {
        Instance = this;
        _destroyCts = new CancellationTokenSource();
    }

    //生命周期结束后自动销毁最旧的页面
    private void OnDestroy()
    {
        _destroyCts.Cancel();
        _destroyCts.Dispose();
    }

    /// <summary>
    /// 请求显示弹窗，自动进入队列排队
    /// </summary>
    public void ShowPopup(string title, string content)
    {
        _popupQueue.Enqueue(new PopupInfo(title, content));
        ProcessQueueAsync(_destroyCts.Token).Forget();
    }

    private async UniTask ProcessQueueAsync(CancellationToken cancelToken)
    {
        // 正在显示则退出，由当前流程继续处理队列
        if (_isShowing)
            return;
        _isShowing = true;

        try
        {
            while (_popupQueue.Count > 0 && !cancelToken.IsCancellationRequested)
            {
                var popup = _popupQueue.Dequeue();
                await ShowSinglePopupAsync(popup, cancelToken);
            }
        }
        finally
        {
            _isShowing = false;
        }
    }

    private async UniTask ShowSinglePopupAsync(PopupInfo info, CancellationToken cancelToken)
    {
        // 实际项目替换为预制体加载逻辑
        var popupObj = new GameObject("CommonPopup");
        var popup = popupObj.AddComponent<CommonPopup>();
        popup.SetContent(info.Title, info.Content);

        // 异步等待用户点击关闭
        await popup.WaitForCloseAsync(cancelToken);
        Destroy(popupObj);
    }
}

// 弹窗数据类
public class PopupInfo
{
    public string Title;
    public string Content;
    public PopupInfo(string title, string content)
    {
        Title = title;
        Content = content;
    }
}

// 弹窗组件基类
public class CommonPopup : MonoBehaviour
{
    private UniTaskCompletionSource _closeTcs;

    public void SetContent(string title, string content)
    {
        // 文本赋值逻辑
    }

    /// <summary>
    /// 将回调式关闭转为可等待异步逻辑
    /// </summary>
    public UniTask WaitForCloseAsync(CancellationToken cancelToken)
    {
        _closeTcs = new UniTaskCompletionSource();
        cancelToken.Register(() => _closeTcs.TrySetCanceled());
        return _closeTcs.Task;
    }

    // 关闭按钮点击事件绑定此方法
    public void OnCloseClick()
    {
        _closeTcs.TrySetResult();
    }
}
//【核心要点】
//1.	用UniTaskCompletionSource将传统回调封装为可等待的异步任务
//2.	队列+状态机保证弹窗严格串行，支持任意时刻新增弹窗
//3.	统一生命周期令牌，场景销毁时自动中断所有弹窗流程
