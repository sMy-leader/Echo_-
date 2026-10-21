

//新手避坑指南：6 个高频错误与解决方案
//UniTask 用法简单，但新手容易因忽视细节导致问题，以下是最常见的坑：

//错误 1：忘记调用 Forget ()
//问题：调用无返回值的 UniTask 时未加Forget()，控制台报 “未处理的 UniTask” 警告。解决方案：所有不需要等待结果的 UniTask，必须添加Forget()（如TestAsync().Forget()）。

//错误 2：异步方法返回 void 
//问题：定义异步方法时用async void，导致异常无法捕获、资源无法释放。解决方案：异步方法返回值优先用UniTask，仅在 Unity 事件回调（如 UI 按钮点击）中使用async void。

//错误 3：在非主线程操作 Unity API
//问题：用UniTask.RunOnThreadPool开启后台线程后，直接修改 Transform、UI 等 Unity 组件，导致崩溃。解决方案：Unity API 只能在主线程执行，后台线程执行完成后需切回主线程：


//// 正确写法：后台线程执行计算，主线程更新UI
//async UniTask TestBackgroundTask()
//{
//    // 后台线程执行耗时计算
//    var result = await UniTask.RunOnThreadPool(() => 
//    {
//        return HeavyCalculation(); // 耗时操作（无Unity API）
//    });

//// 自动切回主线程，可安全操作UI
//resultText.text = "计算结果：" + result;
//}


//错误 4：未处理取消逻辑
//问题：异步任务（如资源加载）未绑定CancellationToken，对象销毁后任务仍在执行，导致内存泄漏。解决方案：所有长时间运行的任务，必须传入CancellationToken，并在OnDestroy中取消。

//错误 5：滥用 UniTask.RunOnThreadPool
//问题：将简单逻辑放入后台线程，导致线程切换开销大于执行逻辑本身。解决方案：仅将 “CPU 密集型任务”（如大量计算、数据解析）放入后台线程，UI 交互、短延迟任务直接在主线程执行。

//错误 6：忽略异常捕获
//问题：异步任务中发生异常（如资源加载失败），未捕获导致程序崩溃。解决方案：用try-catch捕获异常，尤其注意OperationCanceledException（取消异常）和NullReferenceException（空引用）：

//async UniTask LoadResourceSafe()
//{
//    try
//    {
//        var sprite = await Resources.LoadAsync<Sprite>("MissingIcon").ToUniTask();
//if (sprite == null) throw new Exception("资源加载失败");
//    }
//    catch (OperationCanceledException)
//    {
//    // 取消异常
//}
//    catch (Exception e)
//    {
//        Debug.LogError("加载异常：" + e.Message);
//    }
//}
