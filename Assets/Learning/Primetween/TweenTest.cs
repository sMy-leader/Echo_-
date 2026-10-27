using System;
using PrimeTween;
using Unity.VisualScripting;
using UnityEngine;

public class TweenTest : MonoBehaviour
{
    [SerializeField]public TweenSettings<float> settings;
    public SpriteRenderer imgRender;
    private SpriteRenderer btnImage;
    private Tween moveTween;

    public void Case01_MoveY()
    {
        Tween.PositionY(transform, settings);
    }

    private void Start()
    {
        Case01_MoveY();
        Case03_ShakeCamera();
        Case06_Breath();
    }

    // 案例 2：UI Image 透明度 1 -> 0
    public void Case02_FadeUI()
    {
        Tween.Alpha(imgRender, endValue: 0f, duration: 2f);
    }

    // 案例 3：点击按钮时摄像头做一次强度 0.5 的震动
    public void Case03_ShakeCamera()
    {
        Tween.ShakeCamera(Camera.main,strengthFactor: 0.5f);
    }

    // 案例 4：受击时局部坐标沿 transform.up 方向 Punch
    public void Case04_PunchLocalPosition()
    {
        Tween.PunchLocalPosition(transform, strength: transform.up * 0.6f, duration: 0.4f, frequency: 10);
    }

    //案例 5：缩放完成后销毁自己，零分配写法
    //需求：物体缩放到 0 后销毁自身。
    public void Case05a_DestroyAfterScale_ALLOCATING()
    {
        Tween.Scale(transform, endValue: 0f, duration: 1f)
            .OnComplete(() => Destroy(gameObject));  // 捕获 this/gameObject -> 每次分配闭包
    }

    public void Case05b_DestroyAfterScale_ZERO_ALLOC()
    {
        Tween.Scale(transform, endValue: 0f, duration: 1f)
            .OnComplete(transform, t => Destroy(t.gameObject)); // 零分配：用参数 t，不捕获外部变量
    }

    // 案例 6：无限 Yoyo 缩放（呼吸），单程 1 秒（一个来回 2 秒）
    public void Case06_Breath()
    {
        Tween.Scale(transform, endValue: 1.2f, duration: 1f, cycles: -1, cycleMode: CycleMode.Yoyo, ease: Ease.InOutSine);
    }

    // 案例 7：按下 -> 缩小 -> 延迟 -> 弹回(+并行淡入) -> 变色
    //需求模拟：按下按钮后先缩小到 0.9（0.2s），停一下（0.1s），再弹回 1.0（0.3s），弹回的同时做一点透明度变化，最后把颜色变成红色。

    public void Case07_SequenceChain()
    {
        PrimeTween.Sequence.Create()
            .Chain(Tween.Scale(transform, endValue: 0.9f, duration: 0.2f, ease: Ease.InSine))
            .ChainDelay(0.1f)
            .Chain(Tween.Scale(transform, endValue: 1f, duration: 0.3f, ease: Ease.OutBack))
            .Group(Tween.Alpha(btnImage, endValue: 0.8f, duration: 0.3f))
            .Chain(Tween.Color(btnImage, endValue: Color.red, duration: 0.2f));
            
    }
    // 案例 8：位移序列 + 第 0.5 秒插入独立镜头震动
    public void Case08_Insert()
    {
        PrimeTween.Sequence.Create()
            .Chain(Tween.LocalPosition(transform, endValue: new Vector3(0, 3, 0),
                                       duration: 1.2f, ease: Ease.InOutSine))
            // 在序列的第 0.5 秒插入镜头震动（与位移重叠并行）
            .Insert(atTime: 0.5f, Tween.ShakeCamera(Camera.main, strengthFactor: 0.3f, duration: 0.4f));
    }

    // 案例 9：异步依次 await 位移 -> 延迟 -> 旋转（简单过场）
    public async void Case09_AsyncCutscene()
    {
        await Tween.LocalPosition(transform, endValue: new Vector3(0, 3, 0), duration: 1f, ease: Ease.OutCubic);
        await Tween.Delay(0.5f);                                     // 停留
                                                                     // 指定 startValue: Vector3.zero 表示"先从 0 度开始转"；不写则从当前角度接着转
        await Tween.EulerAngles(transform, startValue: Vector3.zero,
                                endValue: new Vector3(0, 90, 0), duration: 0.5f);
        Debug.Log("过场结束");
    }

    // 案例 10a：先调用它启动一段 3 秒的位移，把句柄存进字段 moveTween
    public void Case10_ControlTween()
    {
        moveTween = Tween.LocalPosition(transform, endValue: new Vector3(5, 0, 0), duration: 3f);
    }
    // 案例 10b：与上面的方法一起放进同一个类，由按键控制生命周期
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)&&moveTween.isAlive == true)
        {
            moveTween.isPaused = !moveTween.isPaused;//控制暂停
        }

        if(Input.GetKeyDown(KeyCode.S))
        {
            moveTween.Stop();//停在当前值，但不会触发OnComplete回调
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            moveTween.Complete();//结束，直接到endvalue的值
        }
    }

    ///- **`TweenSettings`** **/** **`TweenSettings<T>`**：可**序列化**的动画设置结构体（startValue、endValue、duration、ease/自定义曲线、cycles、延迟等）。
    ///你可以把它 `[SerializeField]` 到 MonoBehaviour/ScriptableObject，在 Inspector 里调好后直接传给对应 `Tween.` 方法：

    //public class InspectorTween : MonoBehaviour
    //{
    //    [SerializeField] TweenSettings<float> yPositionTweenSettings; // Inspector 里调
    //    [SerializeField] TweenSettings<Vector3> rotationTweenSettings;

    //    void Start()
    //    {
    //        Tween.PositionY(transform, yPositionTweenSettings);  // 直接传入设置
    //        Tween.Rotation(transform, rotationTweenSettings);
    //    }
    //}

    //进阶自定义补间动画
    float floatField; // 类字段

    void CustomExample()
    {
        // 把 floatField 从 0 补到 10，1 秒
        // 注意：lambda 访问了实例字段，会隐式捕获 this -> 每次调用分配一个闭包
        Tween.Custom(startValue: 0, endValue: 10, duration: 1f,
                     onValueChange: newVal => floatField = newVal);

        // 零分配版：把 target 传入，用参数写字段
        Tween.Custom(this, 0, 10, 1f, (target, newVal) => target.floatField = newVal);
    }
    void TimescaleExample(Tween tween)
    {
        // 单个补间的时间倍率（立即生效）
        tween.timeScale = 2f;                                // 2 倍速
                                                             // 平滑地把某个补间/序列的时间倍率动画到目标值（1 秒内到 0.2 倍速）
        Tween.TweenTimeScale(tween, endValue: 0.2f, duration: 1f);
        // 平滑地修改全局 Time.timeScale：1 秒慢动作到 0.2（适合慢镜头/结算）
        Tween.GlobalTimeScale(endValue: 0.2f, duration: 1f);
    }


    //---------------///坑点和需要注意的地方///----------------//
    //### 1. 补间完成后还持有 / 复用 Tween 句柄

    //    PrimeTween** 不可复用**：补间 `isAlive==false` 后就废了，没有 restart / PlayBackwards 这类方法。

    //```
    //错误：保存 `tween`，完成后再 `tween.???(重启)` —— PrimeTween 没有重启方法，句柄已死。
    //正确：想再播一遍就重新 `Tween.xxx(...)` 新启动一个（启动零分配，无需缓存老句柄）。
    //```

    //### 2. 在热路径（帧更新 / 大量对象）里用 async/await 或匿名闭包

    //PrimeTween 本身零分配，但** async/await 状态机**和** 捕获闭包**都会分配。

    //```
    //错误：每帧或每个怪物都 `await Tween.xxx()`，或大量把 `this` 捕获进 OnComplete 闭包。
    //正确：高频路径用 Sequence 编排或直接控制已启动的补间；非要回调就用 `OnComplete(target, t=>...)` 这类非捕获重载；
    //      非要用 await 就接受它的少量分配，只在低频事件（过场、结算、UI 弹窗）上用。
    //```

    //### 3. 对同一属性重复启动补间而不处理覆盖

    //新的补间会“无缝接管”旧值（从当前值接着走），并把该目标上的旧补间覆盖掉，**一般没问题**。但如果你的代码可能在** 每一帧**都创建新的补间，会堆积大量重复动画。

    //```
    //错误：每帧都 `Tween.PositionX(...)`，不考虑之前的。
    //正确：要么预先 `oldTween.Stop()`（或 `Tween.StopAll(onTarget: target)`）再做新的；
    //      要么明确“新补间覆盖旧补间”是想要的语义（例如窗口开合）。
    //```

    //### 4. OnComplete 里直接捕获 this / gameObject，破坏零分配

    //```
    //错误：Tween.xxx(...).OnComplete(() => Destroy(gameObject));   // 捕获闭包 -> 分配
    //    正确：Tween.xxx(...).OnComplete(transform, t => Destroy(t.gameObject));  // 传 target，用参数
    //    或 Tween.xxx(...).OnComplete(this, me => me.SomeMethod());
    //```

    //（注意：PrimeTween 没有 DOTween 的 `SetOnComplete`；它用的是这里的 `OnComplete(target, callback)` 重载。若你的版本没有 `OnComplete<T>` 重载，请以 XML 文档为准，可改用无捕获的委托/静态方法。）

    //### 5. 误以为 `Tween.StopAll()` / `CompleteAll()` 是廉价的

    //`StopAll`/`CompleteAll` 需要** 遍历所有运行中动画**（带了 `onTarget` 还要逐个匹配 target），**复杂度是 O(n)**，n 为当前动画总数。

    //```
    //错误：每帧对大量对象 `Tween.StopAll(onTarget: x)`，性能反而下降。
    //正确：单个/少数已知句柄直接用 `tween.Stop()` / `tween.Complete()`；
    //      批量静态调用只在低频事件（如切场景清理）使用。
    //```

    //### 6. 在回调里给动画加 cycles 会弄错行为

    //给 `Delay` 这类“延迟”设 cycles，**不会重复执行其 OnComplete 回调**；实际效果是把这段延迟按 cycles 拉长，而 OnComplete 仍只在全部结束后调用一次。要“每循环都执行一次回调”，请放进 `Sequence.Create(cycles: n)` 里。

    //```
    //错误：Tween.Delay(1).SetRemainingCycles(3) 以为回调会跑 3 次 —— 实际是延迟变成 3 秒、回调只在全部结束后调用一次。
    //正确：Sequence.Create(cycles: 3, ...).ChainCallback(...) ... 让每轮循环都能回调。
    //```

    //### 7. 直接对结构体句柄做极端 seek

    //`progress`/`elapsedTime` 是 seek，但** 在补间自身的回调（递归调用）里改写会让数据错乱**，会走断言报错。

    //```
    //错误：在 tween.OnUpdate 里又对同一个 tween 设 progress。
    //正确：避免在补间正在 update 时修改它自己的进度；如需“暂停/恢复/清空”用专门的控制 API。
    //```

    //### 8. 用 DOTween 的思维“判空 / 缓存复用”

    //`Tween` 是** 结构体**，不存在 `null`，也不能像 DOTween 那样 `SetAutoKill(false)` 后一直复用。

    //```
    //错误：if (tween != null) tween.Kill();        // struct 不能与 null 比较，编译不通过
    //     缓存的 tween 播完后再次 Play / Restart。 // 没有这些方法，句柄已死
    //正确：if (tween.isAlive) tween.Stop();         // 用 isAlive 判断是否还活着
    //     要重播就重新调用 `Tween.xxx(...)`。



    //    ----------------------------### 概念讲解：零 GC 与闭包分配——关键中的关键-----------------------------------

    //- **PrimeTween 本身永不分配**：启动补间、跑补间都不产生托管堆垃圾。
    //- **但你的回调可能破坏它**：C# 的 lambda 只要**捕获了外部变量**（例如捕获 `this`、`gameObject`、局部变量），编译器就会生成一个**闭包类**；
    //每次执行到创建该 lambda 的那行代码时，会在堆上 `new` 一个闭包对象并缓存到委托里。补间完成时“调用回调”本身不再分配，但**每启动一次这样的补间就有一次分配**。

    //例如下面这行 `OnComplete(() => SomeMethod())`，因为 `SomeMethod()` 是实例方法，lambda 隐式捕获了 `this`，**每次调用这个方法启动补间都会分配一个闭包对象**：

    // 反例：有分配
    //Tween.Position(transform, new Vector3(10, 0, 0), 1f)
    //.OnComplete(() => SomeMethod()); // lambda 捕获 this -> 每次启动补间分配一个闭包


    //**怎么做到零分配？** 使用带 `target` 参数的“非捕获”重载：把 `this`（或某个 UnityEngine.Object）作为 `target` 传进去，
    //回调签名变成 `target => ...`，这样 lambda 只引用参数变量、不捕获外部变量，就不会产生闭包分配。

    // 正例：零分配。target 显式传入，回调用参数 '_this' 而不是捕获 this
    //Tween.Position(transform, new Vector3(10, 0, 0), 1f)
    //.OnComplete(target: this, target => target.SomeMethod());

}
