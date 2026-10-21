using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UnitTaskCancel : MonoBehaviour
{
    // 取消令牌源（管理取消逻辑）
    private CancellationTokenSource _cts;

    void Start()
    {
        _cts = new CancellationTokenSource();

        // 传入取消令牌，关联任务生命周期
        TestCancelableTask(_cts.Token).Forget();
    }


    // 支持取消的异步任务
    async UniTask TestCancelableTask(CancellationToken ct)
    {
        try
        {
            Debug.Log("开始长时间任务（模拟资源加载）");
            // 传入ct，任务可被外部取消
            await UniTask.Delay(5000, cancellationToken: ct);
            Debug.Log("任务正常完成");
        }
        catch (OperationCanceledException)
        {
            // 捕获取消异常，避免报错
            Debug.Log("任务被取消");
        }
    }



    // 手动取消（如按钮点击调用）
    public void CancelTask()
    {
        //取消令牌不为空 且没有其他取消正在被请求
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel(); // 触发取消
            _cts.Dispose(); // 释放资源
        }
    }

    // 关键：对象销毁时取消任务
    void OnDestroy()
    {
        CancelTask();
    }
}
