# 代码注释规范

> 目标：代码能运行，也能被你自己在三个月后读懂，还能在面试时讲清楚“为什么”。

## 1. 注释层级

### 类型级

每个 `public` 类型都要有 `<summary>`，说明它在架构中的位置和职责。

```csharp
/// <summary>
/// 纯 C# 三维向量，供 Domain 使用，不依赖 UnityEngine。
/// Position 约定：玩家胶囊的“脚底中心”，不是几何中心。
/// </summary>
public readonly struct Vec3
```

### 方法级

`public` 方法写清输入、输出、副作用；复杂 `private` 方法写清算法思路。

```csharp
/// <summary>
/// 从胶囊底部中心沿 direction 扫掠。
/// direction 必须归一化；distance 为世界单位。
/// 返回命中点、法线和距离；未命中返回 CapsuleCastResult.None。
/// </summary>
CapsuleCastResult CapsuleCast(...);
```

### 行内级

只在“读者会问为什么”的地方写：

```csharp
// 先创建残影再清空录制：EchoPlayback 构造时会拷贝当前录制帧。
Echoes.Create(EchoRecorder);
InputRecorder.Clear();
EchoRecorder.Clear();
```

不要写：

```csharp
// 清空录制
InputRecorder.Clear();
```

## 2. 必须解释的内容

| 内容 | 示例 |
|---|---|
| 坐标约定 | “Position 是脚底中心，不是胶囊中心” |
| 单位 | “速度 m/s，距离 m，时间 s” |
| 执行顺序 | “先移动再记录，保证残影和本体同一 tick” |
| 不变量 | “GetFrame(0) 永远是最旧的可回溯帧” |
| 边界条件 | “distance <= 0 直接返回未命中” |
| 为什么不用常见方案 | “不用 Rigidbody：残影回放要求可预测轨迹” |
| 失败语义 | “冷却中返回 false，不生成残影” |

## 3. 算法注释模板

```csharp
// 思路：
// 1. 先沿 wish 方向做一次胶囊扫掠；
// 2. 命中后把剩余位移投影到碰撞平面上，得到贴墙滑行方向；
// 3. 最多迭代两次，避免墙角反复弹跳。
// 不变量：Position 始终在碰撞体外部，且不会穿过薄墙。
```

## 4. Unity 生命周期注释

在 `Awake` / `Start` / `Update` / `LateUpdate` 中，如果顺序影响结果，必须注明：

```csharp
// Awake 中建 Session，保证第一帧 Update 之前已有检查点。
private void Awake()
```

## 5. 不要做的注释

- 不要逐行翻译代码。
- 不要写“TODO”却不写清触发条件。
- 不要保留与实现相反的旧注释。
- 不要把设计文档整段复制进代码；代码里只留关键决策和指针。
