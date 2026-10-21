# ECHO / 残响 — 代码导读（Code Tour）

> 版本：v1.0（M0 垂直切片）
> 更新日期：2026-10-20
> 适用代码：`Assets/Game/**`、`Assets/Tests/EditMode/**`
>
> 这份文档不是 API 手册，而是“带你读懂系统怎么运转”的路线图。
> 它回答四个问题：
>
> 1. 先读什么、后读什么？
> 2. 每个文件在系统里负责什么？
> 3. 一次输入、一次回溯、一帧画面是怎么串起来的？
> 4. 想改一个功能时，应该动哪些文件？

## 目录

0. 怎么使用这份导读
1. 一分钟理解这个项目
2. 工程结构与程序集
3. 推荐阅读顺序
4. 从 Boot 到第一帧
5. 一帧的生命周期（最核心）
6. 核心数据结构
7. 玩家运动：从输入到位置
8. 时间轴与残影（核心机制）
9. 机关：压力板与门
10. 表现层：状态如何变成画面
11. 物理实现
12. 场景生成器 M0SceneBuilder
13. 测试导读
14. 构建、运行与验证
15. 常见修改指南
16. 调试方法
17. 术语表
18. 后续里程碑如何扩展这份导读
附录 A：代码地图（按文件）
附录 B：维护检查清单

---


## 0. 怎么使用这份导读

### 三种读法

| 场景 | 建议读法 |
|---|---|
| 第一次通读 | 按第 1 → 2 → 3 → 4 → 5 → 6 章顺序读，先把“一帧生命周期”看懂 |
| 查某个文件 | 直接跳到附录 A“代码地图”，按文件查职责和关键方法 |
| 要改功能 | 先看第 6 章理解数据流，再看第 11 章“常见修改指南” |

### 阅读原则

1. **先看数据，再看流程。** 先知道 `InputFrame`、`EchoFrame`、`Vec3` 长什么样，再看它们怎么流动。
2. **先看调用顺序，再看实现细节。** 例如先知道 `GameLoop.Update` 里“先移动、再统计压力板”，再去看 `PlayerMotor` 的胶囊扫掠。
3. **每读一个类，问三个问题：**
   - 它的输入是什么？
   - 它的输出/副作用是什么？
   - 它不负责什么？
4. **对照测试读。** `Assets/Tests/EditMode` 里每个测试都是“这条规则应该怎样”的可执行说明。
5. **对照文档读。** 规则看 `Docs/DesignRules.md`，分层看 `Docs/Architecture.md`，环境看 `Docs/Environment.md`，关键取舍看 `Docs/TechDecisions.md`。

### 维护规则（重要）

新增或修改代码时，**必须同步更新这份导读**，至少检查：

- 附录 A“代码地图”是否新增/删除/改名了文件；
- 第 4 章“一帧生命周期”是否多/少了一步；
- 第 10 章“测试地图”是否新增了测试；
- 第 11 章“常见修改指南”是否出现了新的改法；
- 第 2 章“程序集依赖图”是否变化。

这条规则也写在 `AGENTS.md` 里，属于项目长期约定。

---

## 1. 一分钟理解这个项目

### 1.1 核心玩法

你是一个在时间实验设施里醒来的角色。每次手动回溯，时间回到最近的检查点，
而你上一轮的行动会留下一个**残影**，它精确重演你走过的路线。

残影是实体：

- 它能踩压力板；
- 它能挡住激光；
- 它会被守卫发现并击杀；
- 它不会听你指挥，只会按录制轨迹行动。

所以核心不是“重来一次”，而是“和上一次的自己组队”。

### 1.2 M0 当前实现了什么

M0 只做一条最小闭环：

```text
Boot → Level_00
  → 玩家移动/潜行
  → 踩压力板 → 门开
  → 按 R 回溯
  → 玩家回检查点
  → 残影重演“走到压力板”的轨迹
  → 压力板保持开启
  → 玩家通过门
```

M0 **没有**：守卫、激光、声音、存档、菜单、正式美术、热更。

### 1.3 三个最重要的概念

| 概念 | 含义 | 代码位置 |
|---|---|---|
| 本体 | 当前正在操作的玩家 | `PlayerMotor` |
| 残影 | 上一轮录制的轨迹回放 | `EchoPlayback` / `EchoManager` |
| 检查点 | 回溯时恢复的时间锚点 | `CheckpointService` |

### 1.4 总数据流

```text
Unity Input System
      │  Keyboard / Gamepad
      ▼
UnityInputService.Sample()
      │  InputFrame（量化后的逻辑输入）
      ▼
GameLoop.Update() ──固定 60Hz──> GameSession.Tick()
                                  │
                  ┌───────────────┼────────────────┐
                  ▼               ▼                ▼
           PlayerMotor.Tick   InputRecorder    EchoRecorder
           （移动/重力）       （输入录制）      （轨迹录制）
                  │               │                │
                  └───────────────┴────────────────┘
                                  │
                                  ▼
                         Echoes.Tick()
                         （已有残影前进一帧）
                                  │
                                  ▼
                  PressurePlateView.Contains(位置)
                                  │ 激活源数量
                                  ▼
                  ActivationAggregator.Tick()
                                  │ IsActive
                                  ▼
                  DoorView.SetOpen()
                                  │
                                  ▼
                  PlayerView / EchoView / CameraRig / GameHud
                  （用 alpha 插值渲染）
```

---

## 2. 工程结构与程序集

### 2.1 目录树

```text
Assets/
├── Game/
│   ├── Core/                 # 纯 C# 契约层，不引用 UnityEngine
│   │   ├── Math/Vec3.cs
│   │   ├── Input/InputFrame.cs
│   │   ├── Collision/ICollisionWorld.cs
│   │   ├── Logging/ILogger.cs
│   │   └── GameConstants.cs
│   ├── Domain/               # 纯 C# 玩法规则
│   │   ├── Player/PlayerMotor.cs
│   │   ├── Timeline/TimelineRecorder.cs
│   │   ├── Timeline/EchoRecorder.cs
│   │   ├── Timeline/EchoFrame.cs
│   │   ├── Echo/EchoPlayback.cs
│   │   ├── Echo/EchoManager.cs
│   │   ├── Checkpoint/CheckpointService.cs
│   │   └── Mechanisms/ActivationAggregator.cs
│   ├── Application/          # 用例编排
│   │   └── GameFlow/GameSession.cs
│   ├── Framework/            # AOT Unity 工具
│   │   └── Logging/UnityLogger.cs
│   ├── Infrastructure/       # AOT Unity 实现
│   │   └── Physics/PhysicsCollisionWorld.cs
│   ├── Presentation/         # 表现层 / 组合根
│   │   ├── Bootstrap/GameLoop.cs
│   │   ├── Input/UnityInputService.cs
│   │   ├── Camera/CameraRig.cs
│   │   ├── Views/...
│   │   └── UI/GameHud.cs
│   ├── Bootstrap/            # AOT 启动器
│   │   └── Bootstrapper.cs
│   └── Editor/               # 编辑器工具
│       └── M0SceneBuilder.cs
├── Scenes/
│   ├── Boot.unity
│   ├── MainMenu.unity
│   └── Level_00.unity
└── Tests/
    └── EditMode/
```

### 2.2 程序集依赖图

```text
Game.Core（纯 C#，noEngineReferences）
   ▲
   ├── Game.Domain（纯 C#）
   │      ▲
   │      └── Game.Application（纯 C#）
   │             ▲
   │             └── Game.Presentation（Unity，热更边界）
   │
   ├── Game.Framework（AOT Unity 工具）
   │      ▲
   │      └── Game.Infrastructure（AOT Unity 实现）
   │             ▲
   │             └── Game.Bootstrap（AOT 启动器）
   │
   ├── Game.Editor（Editor-only）
   └── Game.Tests.EditMode（Editor-only）
```

### 2.3 每个程序集的职责

| 程序集 | 类型 | 依赖 | 职责 | 不应该做什么 |
|---|---|---|---|---|
| `Game.Core` | AOT，纯 C# | 无 | `Vec3`、`InputFrame`、接口、常量 | 不引用 UnityEngine |
| `Game.Domain` | 纯 C# | Core | 玩家移动、录制、残影、检查点、机关聚合 | 不引用 UnityEngine |
| `Game.Application` | 纯 C# | Core、Domain | 用例编排 `GameSession` | 不引用 UnityEngine |
| `Game.Framework` | AOT Unity | Core | `UnityLogger` 等 Unity 工具 | 不引用热更程序集 |
| `Game.Infrastructure` | AOT Unity | Core、Framework | `PhysicsCollisionWorld` 等实现 | 不引用 Domain/Application |
| `Game.Presentation` | Unity | Core、Domain、Application、Framework、Infrastructure | 输入、相机、View、HUD、组合根 | 不写玩法规则 |
| `Game.Bootstrap` | AOT Unity | Core、Framework、Infrastructure | 启动器，加载场景 | 不编译期引用热更程序集 |
| `Game.Editor` | Editor-only | 全部 | 场景生成、校验、调试工具 | 不进包 |
| `Game.Tests.EditMode` | Editor-only | Core、Domain、Application | 纯逻辑测试 | 不依赖场景 |

### 2.4 为什么要这样分

- **纯 C# Domain**：玩法规则能秒级测试，不依赖渲染/物理；残影回放也更容易保持可预测。
- **Core 单独一层**：AOT 的 Infrastructure 和纯 C# 的 Domain 都需要 `Vec3`/`InputFrame`/接口，不能让 Domain 反向依赖 Unity。
- **AOT 不引用热更**：`Game.Bootstrap` 只引用 AOT 程序集；热更入口用反射。当前 Mono 不启用热更，但边界保留。
- **Presentation 只读状态**：View 不修改 `GameSession`，只把状态画出来。

> 当前构建是 **Mono**，HybridCLR `enable: 0`。
> 上表的“热更”分类是未来边界，不是当前已启用热更。

---

## 3. 推荐阅读顺序

### 第 0 轮：先看“游戏怎么跑起来”

读：

1. `Assets/Scenes/Boot.unity`（只看看对象：`Bootstrap`）
2. `Assets/Game/Bootstrap/Bootstrapper.cs`
3. `Assets/Game/Presentation/Bootstrap/GameLoop.cs`
4. `Assets/Game/Editor/M0SceneBuilder.cs`（只看布局和引用绑定）

读完能回答：

- 为什么先加载 `Boot` 而不是 `Level_00`？
- `GameLoop` 的字段是谁赋值的？
- `Level_00` 里的 `Game`、`Player`、`PressurePlate`、`Door` 是怎么被创建/关联的？

### 第 1 轮：数据形状

读：

1. `GameConstants.cs`
2. `Vec3.cs`
3. `InputFrame.cs`
4. `EchoFrame.cs`
5. `ICollisionWorld.cs`
6. `ILogger.cs`

读完能回答：

- 为什么 `InputFrame` 用 `sbyte` 而不是 `float`？
- `EchoFrame` 为什么是 16 字节？
- `Position` 是胶囊中心还是脚底？
- `ICollisionWorld` 的 `direction` 有什么要求？

### 第 2 轮：玩家运动

读：

1. `PlayerMotor.cs`
2. `PhysicsCollisionWorld.cs`
3. `PlayerMotorTests.cs`
4. `Docs/DesignRules.md` 的玩家/相机参数

读完能回答：

- 一次 `Tick` 里先做什么、后做什么？
- 撞墙后剩余位移怎么处理？
- 为什么不用 `Rigidbody` / `CharacterController`？
- `SkinWidth` 和 `GroundCheckDistance` 各解决什么问题？

### 第 3 轮：时间轴与残影（核心）

读：

1. `TimelineRecorder.cs`
2. `EchoRecorder.cs`
3. `CheckpointService.cs`
4. `EchoPlayback.cs`
5. `EchoManager.cs`
6. `GameSession.cs`
7. `GameSessionTests.cs` / `EchoManagerTests.cs`

读完能回答：

- 输入录制和轨迹录制分别为了什么？
- 环形缓冲的 `start` 下标怎么算？
- 回溯时为什么先 `Echoes.Create` 再 `Clear`？
- 为什么所有残影要 `ResetAll` 回 t=0？
- 为什么结束的残影要下一 tick 才移除？

### 第 4 轮：表现层

读：

1. `UnityInputService.cs`
2. `GameLoop.cs` 的 `Render` / `SyncEchoViews`
3. `PlayerView.cs` / `EchoView.cs`
4. `CameraRig.cs`
5. `PressurePlateView.cs` / `DoorView.cs`
6. `GameHud.cs`

读完能回答：

- `alpha` 是什么？为什么渲染需要它？
- 残影的 GameObject 什么时候创建、什么时候销毁？
- 为什么压力板用几何 `Contains` 而不是 Trigger？
- 门的碰撞体为什么会跟着门升起？

### 第 5 轮：测试与验证

读：

1. `Assets/Tests/EditMode` 下所有测试
2. `Docs/M0-Verification.md`
3. `Docs/Environment.md` 的构建部分

读完能回答：

- 哪些逻辑有测试、哪些没有？
- 如何新增一个 Domain 测试？
- 当前构建后端是什么？为什么不是 IL2CPP？

---

## 4. 从 Boot 到第一帧

### 4.1 启动链路

```text
Unity 启动
  → 加载 Build Settings 索引 0：Boot.unity
  → Boot 场景里的 Bootstrap 对象
  → Bootstrapper.Start()
  → SceneManager.LoadScene("Level_00")
  → Level_00 场景加载
  → GameLoop.Awake()
      → new UnityInputService()
      → new GameSession(new PhysicsCollisionWorld(), spawn, yaw)
      → GameSession.CaptureCheckpoint()
      → GameHud.Bind(session)
  → 第一帧 Update()
```

### 4.2 场景对象

`M0SceneBuilder` 生成的 `Level_00` 结构：

| 对象 | 作用 | 关键组件 |
|---|---|---|
| `Main Camera` | 固定视角相机 | `Camera`、`CameraRig` |
| `Directional Light` | 主方向光 | `Light` |
| `Global Volume` | URP 后处理 | `Volume` |
| `Floor` / 四面墙 | 灰盒空间 | `BoxCollider`、`SceneColorView` |
| `Wall_DoorLeft/Right` | 门框墙，防止绕过门 | `BoxCollider` |
| `Door` | 红色门，机关开启后升起 | `BoxCollider`、`DoorView` |
| `PressurePlate` | 黄色压力板 | `PressurePlateView`（无 Collider） |
| `Checkpoint` | 蓝色检查点视觉 | `SceneColorView`（无 Collider） |
| `Player` | 橙色胶囊视觉 | `PlayerView`（无 Collider） |
| `PlayerSpawn` | 出生点/初始检查点 | `Transform` |
| `Game` | 组合根 | `GameLoop`、`GameHud` |

### 4.3 为什么用 Editor 工具生成场景

`M0SceneBuilder` 做三件事：

1. 删除自己“拥有”的对象，重新创建（幂等）；
2. 设置相机、灯光、门、压力板、玩家；
3. 把 `GameLoop` 的序列化引用直接连好。

好处：

- 不依赖手工拖拽，场景可重复生成；
- 改布局只改代码，不会出现“某个引用丢了”；
- 面试时能展示“工具化搭场景”的工程习惯。

代价：

- 如果你在场景里手工改这些对象，下次执行菜单会被覆盖；
- 新增对象时，要把它加进 `GeneratedObjectNames`。

---

## 5. 一帧的生命周期（最核心）

### 5.1 总流程

`GameLoop.Update()`：

```text
1. accumulator += min(unscaledDeltaTime, 0.25)
2. while accumulator >= 1/60 且 steps < 8:
     a. accumulator -= 1/60
     b. input = UnityInputService.Sample()
     c. if input 有 Rewind: GameSession.TryRewind()
     d. GameSession.Tick(1/60, input)
     e. if 玩家掉出场景: GameSession.RestoreCheckpoint()
     f. activators = CountPlateActivators()
     g. GameSession.TickPlate(1/60, activators)
3. alpha = accumulator / (1/60)
4. Render(alpha)
```

### 5.2 每一步在做什么

| 步骤 | 调用 | 为什么 |
|---|---|---|
| 累积时间 | `_accumulator += ...` | 逻辑固定 60Hz，渲染可以更高 |
| 限制单帧 | `min(..., 0.25f)` | 切回窗口/卡顿后不要一次补几百 tick |
| 限制补帧 | `steps < 8` | 防止“死亡螺旋” |
| 采样输入 | `UnityInputService.Sample()` | 每 tick 采样，边沿触发只消费一次 |
| 处理回溯 | `TryRewind()` | 回溯后的第一帧要从检查点开始录制 |
| 逻辑 tick | `GameSession.Tick()` | 移动、录制、残影前进 |
| 掉出场景 | `RestoreCheckpoint()` | 失败兜底，不生成残影 |
| 统计压力板 | `CountPlateActivators()` | 玩家/残影都移动完之后再统计 |
| 机关 tick | `TickPlate()` | 0.3s 释放延迟由聚合器处理 |
| 渲染插值 | `Render(alpha)` | 把逻辑状态平滑地画出来 |

### 5.3 时序图

```text
Update
  │
  ├─ Sample InputFrame
  │
  ├─ if Rewind ──> GameSession.TryRewind()
  │                   ├─ Echoes.Create(EchoRecorder)
  │                   ├─ Checkpoint.Restore(Player)
  │                   ├─ Echoes.ResetAll()
  │                   ├─ InputRecorder.Clear()
  │                   ├─ EchoRecorder.Clear()
  │                   └─ Plate.Reset()
  │
  ├─ GameSession.Tick()
  │    ├─ Player.Tick()
  │    │    ├─ MoveWithSlide()
  │    │    └─ ApplyGravity()
  │    ├─ InputRecorder.Record()
  │    ├─ EchoRecorder.Record(EchoFrame)
  │    └─ Echoes.Tick()
  │
  ├─ CountPlateActivators()
  │    ├─ plate.Contains(Player.Position)
  │    └─ plate.Contains(echo.CurrentPosition) × N
  │
  ├─ GameSession.TickPlate()
  │    └─ ActivationAggregator.Tick()
  │
  └─ Render(alpha)
       ├─ PlayerView.Apply()
       ├─ CameraRig.Follow()
       ├─ PressurePlateView.SetActive()
       ├─ DoorView.SetOpen()
       └─ SyncEchoViews()
```

### 5.4 哪些顺序不能换

| 顺序 | 如果换了会怎样 |
|---|---|
| 先记录 `PreviousPosition`，再移动 | 不记录就无法插值，View 会跳变 |
| 先移动，再统计压力板 | 压力板会慢一 tick，边缘体验差 |
| 先 `Echoes.Create`，再 `Clear` 录制 | 残影会拷贝到空录制，生成失败 |
| 先 `ResetAll`，再 `Tick` | 新残影会从旧 cursor 开始，时间轴错位 |
| 先 `Echoes.Tick`，再渲染 | 残影 Current/Previous 才是本帧的数据 |
| 固定 tick + alpha，而不是直接用 `Time.deltaTime` | 逻辑帧率会随机器变化，回放不稳定 |

### 5.5 为什么用 `alpha`

假设渲染 120fps、逻辑 60Hz：

- 第 1 个渲染帧可能没有逻辑 tick；
- 第 2 个渲染帧跑了一次 tick；
- 如果直接把逻辑位置赋给 View，角色会“每两帧跳一次”。

`alpha = accumulator / tickDelta` 表示“当前渲染时刻处在两个逻辑 tick 之间的比例”。
View 用 `Lerp(Previous, Current, alpha)` 就能平滑显示。

---

## 6. 核心数据结构

### 6.1 `Vec3`

文件：`Assets/Game/Core/Math/Vec3.cs`

- `readonly struct`，避免 tick 中堆分配；
- 只做纯数学，不引用 UnityEngine；
- `Normalized` 在长度接近 0 时返回 `Zero`，避免 NaN；
- `ProjectOnPlane` 用于贴墙滑行；
- `Lerp` 用于表现层插值，会 clamp `t` 到 `[0,1]`。

### 6.2 `InputFrame`

文件：`Assets/Game/Core/Input/InputFrame.cs`

```text
MoveX: sbyte    -127..127
MoveY: sbyte    -127..127
Buttons: byte   位标志：Sneak / Interact / Rewind / Timeline / Pause
LookYaw: byte   预留朝向（固定相机下为 0）
```

总大小：**4 字节**；30 秒 1800 帧 ≈ **7.2KB**。`InputFrameTests.InputFrame_Is4Bytes` 会锁定这个布局。

关键点：

- 轴量化到 `sbyte`，避免浮点在录制/回放中产生平台差异；
- 死区 1%，过滤摇杆漂移；
- `Rewind` / `Pause` 由 `UnityInputService` 做边沿检测，只在按下那一 tick 置位；
- `InputFrame` 是逻辑层唯一输入，Domain 不知道键盘/手柄。

### 6.3 `EchoFrame`

文件：`Assets/Game/Domain/Timeline/EchoFrame.cs`

```text
Position: Vec3      12 bytes（脚底中心）
Yaw: ushort          2 bytes（0..65535，约 0.0055° 精度）
State: byte          1 byte（Idle/Walk/Sneak/Interact/Dead）
Flags: byte          1 byte（预留）
总计：16 bytes
```

为什么不用 `float` 存朝向：

- 16 字节 × 1800 帧 ≈ 28.8KB/残影；
- 3 个残影 ≈ 86KB，内存可控；
- 65535 个刻度足够表现。

### 6.4 `CapsuleCastResult` / `ICollisionWorld`

文件：`Assets/Game/Core/Collision/ICollisionWorld.cs`

约定：

- `bottomCenter` 是**脚底中心**，不是胶囊几何中心；
- `direction` 必须归一化；
- `distance` 单位是米；
- 未命中返回 `CapsuleCastResult.None`。

### 6.5 `CheckpointSnapshot`

文件：`Assets/Game/Domain/Checkpoint/CheckpointService.cs`

M0 只存：

- 玩家位置；
- 玩家朝向；
- `IsValid`。

后续会扩展：机关状态、残影列表、守卫状态等。

### 6.6 `GameConstants`

文件：`Assets/Game/Core/GameConstants.cs`

| 常量 | 值 | 含义 |
|---|---|---|
| `TickDelta` | `1/60` | 逻辑 tick |
| `WalkSpeed` | 3.2 | 步行 m/s |
| `SneakSpeed` | 1.2 | 潜行 m/s |
| `Gravity` | 20 | 重力 m/s² |
| `PlayerRadius` | 0.3 | 胶囊半径 m |
| `PlayerHeight` | 1.8 | 胶囊总高 m |
| `SkinWidth` | 0.02 | 碰撞皮肤 m |
| `GroundCheckDistance` | 0.15 | 地面检测距离 m |
| `InteractDistance` | 2.0 | 交互距离 m |
| `MaxEchoes` | 3 | 残影上限 |
| `RecordLimitSeconds` | 30 | 单轮录制上限 |
| `PressurePlateReleaseDelay` | 0.3 | 压力板释放延迟 |
| `RewindCooldown` | 0.5 | 回溯冷却 |
| `RewindInvulnerability` | 0.3 | 回溯后无敌（预留） |

---

## 7. 玩家运动：从输入到位置

文件：`Assets/Game/Domain/Player/PlayerMotor.cs`

### 7.1 `Tick` 的五步

```text
1. PreviousPosition = Position
   PreviousYaw = Yaw

2. wish = (MoveX, 0, MoveY)
   if |wish| > 1: normalize
   speed = Sneak ? 1.2 : 3.2
   delta = wish * speed * dt

3. MoveWithSlide(delta, world)

4. ApplyGravity(dt, world)

5. 更新 Yaw / State
```

注意：

- `Position` 是脚底中心；
- 输入目前是**世界方向**，因为 M0 相机 yaw = 0；
- 如果以后把相机水平旋转 45°，要在 `wish` 上乘相机 yaw 的旋转矩阵，否则 WASD 会和画面对不上。

### 7.2 `MoveWithSlide` 算法

```text
最多迭代 2 次：
  1. distance = |delta|
  2. 沿 delta 方向做 CapsuleCast(distance + SkinWidth)
  3. 未命中：Position += delta，结束
  4. 命中：
       safe = max(0, hit.Distance - SkinWidth)
       Position += direction * safe
       remaining = delta - direction * safe
       delta = ProjectOnPlane(remaining, hit.Normal)
```

为什么要投影：

- 直接 `Position += delta` 会穿墙；
- 直接停在命中点会“贴墙卡死”；
- 把剩余位移投影到墙面，就能沿着墙滑行。

为什么迭代两次：

- 一次处理单面墙；
- 两次处理墙角/两面墙夹角；
- 更多迭代收益很小，M0 不需要。

### 7.3 `ApplyGravity`

```text
VelocityY -= Gravity * dt
dy = VelocityY * dt
沿上/下方向 CapsuleCast(|dy| + SkinWidth)
  命中：停在表面前 SkinWidth，VelocityY = 0，IsGrounded = dy < 0
  未命中：Position += (0, dy, 0)，IsGrounded = false
最后用 CheckGround 兜底
```

为什么还要 `CheckGround`：

- 当 `VelocityY` 刚好为 0 时，向下扫掠距离可能为 0；
- `CheckGround` 用 `SphereCast` 从脚底向上 offset 一个半径再向下打，能稳定判断“是否站在地上”。

### 7.4 为什么不用 `Rigidbody` / `CharacterController`

- `Rigidbody` 的物理解算受求解器顺序/浮点影响，残影回放要求可预测；
- `CharacterController` 的内部实现不保证跨版本一致；
- 自定义胶囊扫掠约 150 行，换来完全可控的移动、贴墙和地面检测。

> 注意：残影**不重跑** `PlayerMotor`。残影只回放 `EchoFrame`。
> 这里的确定性主要服务于本体手感，不是残影回放的唯一真相。

### 7.5 常见调试

| 现象 | 可能原因 |
|---|---|
| 完全不动 | Game 视图没有焦点 / 输入设备未连接 / `UnityInputService` 没读到 |
| 穿过地板 | Floor 没有 `BoxCollider`；或胶囊起点在地面以下 |
| 贴墙卡死 | `SkinWidth` 太小；或 `CapsuleCast` 起点已经在墙里 |
| 角色抖动 | `GroundCheckDistance` 太大/太小；或胶囊尺寸与视觉不一致 |
| 斜着走更快 | 没有对 `wish` 做归一化 |

---

## 8. 时间轴与残影（核心机制）

### 8.1 两份录制

| 录制器 | 元素 | 用途 |
|---|---|---|
| `TimelineRecorder` | `InputFrame` | 输入回放/调试/未来可能的输入重放 |
| `EchoRecorder` | `EchoFrame` | 残影轨迹回放（当前真正使用） |

为什么两份都要：

- `InputFrame` 记录“玩家按了什么”，方便调试和未来做确定性输入重放；
- `EchoFrame` 记录“玩家实际走到哪”，残影回放不需要重跑物理，成本低且绝对确定。

### 8.2 环形缓冲

文件：`TimelineRecorder.cs` / `EchoRecorder.cs`

```text
容量 = 1800（60Hz × 30s）
_head 指向“下一个写入位置”
_count 是当前有效帧数
```

写入：

```text
_frames[_head] = frame
_head = (_head + 1) % capacity
_count = min(_count + 1, capacity)
```

读取逻辑第 `i` 帧：

```text
start = (_head - _count + capacity) % capacity
actual = (start + i) % capacity
```

例子：容量 4，写入 6 帧（0..5）：

```text
写入 4 帧后：_head=0, _count=4
再写 4、5：
  _head=2, _count=4
start = (2 - 4 + 4) % 4 = 2
GetFrame(0) = _frames[2] = 帧 2
GetFrame(3) = _frames[5 % 4] = _frames[1] = 帧 5
```

不变量：`GetFrame(0)` 永远是最旧的可回溯帧。

### 8.3 检查点

文件：`CheckpointService.cs`

- `Capture(position, yaw)` 保存快照；
- `Restore(player)` 把玩家传回快照；
- 没有快照时什么都不做，避免新关卡第一帧把玩家传到原点。

M0 只在 `GameLoop.Awake` 里调用一次 `CaptureCheckpoint()`。
后续加检查点触发区时，在玩家进入区域时再调用 `CaptureCheckpoint()`。

### 8.4 `EchoPlayback`

文件：`EchoPlayback.cs`

创建时：

- 从 `EchoRecorder` 拷贝当前所有帧（**不是引用**）；
- `Previous = Current = frames[0]`；
- `cursor = 0`。

每 tick：

```text
Previous = Current
Current = frames[cursor]
cursor++
if cursor >= frames.Length: IsFinished = true
```

为什么用 `Previous/Current`：

- 表现层用 `Lerp(Previous, Current, alpha)` 插值；
- 即使渲染帧率高于 60Hz，残影也不会跳变。

为什么结束时 `IsFinished` 但不当场删除：

- `EchoManager.Tick` 在**下一 tick 开头**才移除；
- 这样最后一帧至少被渲染一次。

### 8.5 `EchoManager`

文件：`EchoManager.cs`

- `Create(recorder)`：超过 3 个时移除最旧（FIFO）；
- `Tick()`：先移除上一 tick 已结束的残影，再让存活的残影前进；
- `ResetAll()`：回溯时所有残影回到 t=0；
- `Clear()`：清空（测试/重开用）；
- `Contains(echo)`：表现层同步 GameObject 用。

### 8.6 回溯的完整顺序

`GameSession.TryRewind()`：

```text
1. CanRewind?
   - 冷却结束
   - InputRecorder.Count > 0
   - EchoRecorder.Count > 0

2. Echoes.Create(EchoRecorder)
   // 构造 EchoPlayback 时拷贝当前录制帧

3. _checkpoints.Restore(Player)
   // 玩家回检查点

4. Echoes.ResetAll()
   // 新残影 + 旧残影全部回到 t=0

5. InputRecorder.Clear()
   EchoRecorder.Clear()
   // 开始新一轮录制

6. Plate.Reset()
   // 机关清空，下一 tick 按新位置重算

7. _rewindCooldown = 0.5
```

顺序不能换：

- 先 `Create` 再 `Clear`：否则新残影拷贝到空录制；
- 先 `Restore` 再 `ResetAll`：玩家和残影都回到同一时间轴；
- `Plate.Reset` 后下一 tick 重算：避免门保持上一轮状态。

### 8.7 残影生命周期图

```text
录制中
  │ 每 tick: EchoRecorder.Record(EchoFrame)
  ▼
按 R
  │
  ├─ Echoes.Create(EchoRecorder)  → EchoPlayback 拷贝帧，cursor=0
  ├─ Player 回检查点
  ├─ Echoes.ResetAll()           → 所有残影 cursor=0
  └─ 清空录制
        │
        ▼
新一轮 tick
  │ Player 移动 + 录制
  │ Echoes.Tick()
  │   ├─ 上一 tick 已结束的残影移除
  │   └─ 存活残影 Previous/Current 前进
  ▼
残影到达最后一帧
  │ IsFinished = true
  ▼
下一 tick
  └─ EchoManager 移除，GameLoop 销毁 EchoView
```

### 8.8 为什么用轨迹回放，不重跑模拟

| 方案 | 成本 | 确定性 | 世界冲突 |
|---|---|---|---|
| 重跑输入/物理 | 4 个残影 = 4 倍模拟 | 受物理/浮点影响 | 残影会被门/箱子推挤 |
| 回放 `EchoFrame` | 只插值 + 触发器 | 位置是录制值，绝对确定 | 轨迹固定，只读世界 |

设计上禁止残影推箱子，避免回放分叉。
残影可以踩板、拉杆、挡激光、被守卫击杀。

### 8.9 常见修改

| 想改 | 改哪里 | 注意 |
|---|---|---|
| 残影上限 3 → 4 | `GameConstants.MaxEchoes` | 同步 HUD、测试、关卡预算 |
| 录制 30s → 60s | `GameConstants.RecordLimitSeconds` + `TimelineRecorder/EchoRecorder.DefaultCapacity` | 内存翻倍；HUD 的 `/60f` 要改 |
| 残影回放速度 | `EchoPlayback.Tick` 的调用频率 | 建议不要改，会破坏时间轴 |
| 残影死亡 | `EchoPlayback` 增加状态 + `EchoManager` 移除 | 同步 `DesignRules` 的“下次回溯复活”规则 |
| 残影颜色/透明 | `EchoView` | 只改表现，不改 Domain |

---

## 9. 机关：压力板与门

### 9.1 数据流

```text
Player.Position / Echo.CurrentPosition
        │
        ▼
PressurePlateView.Contains(feetPosition)
        │  true/false
        ▼
CountPlateActivators() → int count
        │
        ▼
ActivationAggregator.Tick(dt, count)
        │ IsActive
        ├──> PressurePlateView.SetActive()
        └──> DoorView.SetOpen()
```

### 9.2 `ActivationAggregator`

文件：`ActivationAggregator.cs`

- 每 tick 根据当前激活源数量重新求值；
- 不记录“谁踩上去了”，只关心“现在有几个源”；
- `ReleaseDelay = 0.3s`：离开后延迟关闭；
- `Mode`：`Or` / `And` / `Count`，M0 只用 `Or`。

为什么这样设计：

- 本体 + 多个残影同时踩板，结果天然正确；
- 残影消散时不需要清理事件；
- 机关状态可被快照/恢复，不会出现“门开着但没人踩”。

### 9.3 `PressurePlateView.Contains`

文件：`PressurePlateView.cs`

- 用 `transform.position` 和 `halfExtents` 做 XZ 包含判断；
- Y 方向给较宽松范围（`-halfY .. +halfY+1.5`），因为脚底位置受 SkinWidth 影响；
- 不依赖 Collider/Trigger，残影不需要物理体。

### 9.4 `DoorView`

文件：`DoorView.cs`

- `Awake` 记录关门位置；
- `SetOpen(bool)` 设置目标；
- `Update` 用 `MoveTowards` 在开/关位置之间移动；
- 门的 `BoxCollider` 跟着 Transform 移动，所以升起后玩家可以通过；
- 颜色只是表现，不参与判定。

### 9.5 扩展机关

加拉杆/时间锁时，建议：

1. 在 `Game.Domain/Mechanisms` 新增机关规则类；
2. 仍然用“激活源数量”或“状态集合”每 tick 重算；
3. `GameSession` 增加对应字段/Tick；
4. Presentation 增加 View + 在 `GameLoop.CountPlateActivators` 或新的统计函数里收集激活源；
5. 在 `M0SceneBuilder` 生成对象；
6. 加 EditMode 测试；
7. 更新 `Docs/DesignRules.md` 和本文第 9 章。

---

## 10. 表现层：状态如何变成画面

### 10.1 `GameLoop` 是组合根

`GameLoop` 负责：

- 创建 `UnityInputService`；
- 创建 `PhysicsCollisionWorld`；
- 创建 `GameSession`；
- 捕获初始检查点；
- 驱动固定 tick；
- 把状态同步给 View。

它**不负责**：

- 玩家怎么移动（`PlayerMotor`）；
- 残影怎么回放（`EchoPlayback`）；
- 门什么时候开（`ActivationAggregator`）；
- 机关规则（Domain）。

### 10.2 `UnityInputService`

文件：`UnityInputService.cs`

- 键盘：WASD、Shift、E、R、Tab、Esc；
- 手柄：左摇杆、LB、A、B、十字键上、Start；
- `Rewind` / `Pause` 做边沿检测；
- 输出 `InputFrame`，不把 `Keyboard` 传进 Domain。

当前是直接轮询设备，后续可以换成 `InputActionAsset`，只改这个类。

### 10.3 `PlayerView` / `EchoView`

- `PlayerView.Apply(player, alpha)`：`Lerp(PreviousPosition, Position, alpha)`；
- `EchoView.Apply(alpha)`：`Lerp(PreviousPosition, CurrentPosition, alpha)`；
- 逻辑位置是脚底，显示时 `+ capsuleHalfHeight`；
- 用 `LerpAngle` 处理朝向环绕。

### 10.4 `CameraRig`

- 固定俯角/偏航，不自由旋转；
- `Follow` 用指数平滑：
  `t = 1 - exp(-followSpeed * dt)`；
- 好处：与帧率无关，30/60/120fps 手感一致；
- 初始位置 = `target + offset`，当前 offset `(0,16,-12)`。

### 10.5 残影视图同步

`GameLoop.SyncEchoViews`：

```text
for each echo in session.Echoes:
    if 没有 View: CreateEchoView(echo)
    view.Apply(alpha)

for each view in _echoViews:
    if echo 不在 session.Echoes: 销毁 View
```

`CreateEchoView`：

- `GameObject.CreatePrimitive(Capsule)`；
- 立刻禁用并销毁 CapsuleCollider；
- 加 `EchoView`，绑定 `EchoPlayback`。

为什么销毁 Collider：

- 残影只做视觉/机关判定，不参与物理；
- 有 Collider 会挡住本体的胶囊扫掠。

### 10.6 `GameHud`

- `OnGUI` 调试版；
- 显示残影数、录制时长、回溯可用、压力板、冷却、是否落地、位置；
- 不修改任何玩法状态；
- 后续换 UGUI/UI Toolkit 时，保留 `Bind(GameSession)` 的绑定方式。

---

## 11. 物理实现

文件：`Game.Infrastructure/Physics/PhysicsCollisionWorld.cs`

### 11.1 坐标转换

```text
Domain Vec3（脚底中心）
  → Unity Vector3 bottom
  → point1 = bottom + up * radius
  → point2 = bottom + up * (height - radius)
  → Physics.CapsuleCast(point1, point2, ...)
```

### 11.2 层与 Trigger

- 默认使用 `Physics.DefaultRaycastLayers`；
- 忽略 `Trigger`；
- 玩家/残影视觉没有 Collider，不会打到彼此。

### 11.3 地面检测

```text
origin = bottom + up * radius
Physics.SphereCast(origin, radius, down, distance)
```

这样球体底部刚好在脚底，能稳定检测地面。

### 11.4 注意

- 物理查询只用于本体移动；
- 残影不重跑物理；
- `SkinWidth` 是防止浮点抖动/卡墙的关键；
- 如果以后加箱子/门，需要确认它们所在的 Layer 是否参与扫掠。

---

## 12. 场景生成器 `M0SceneBuilder`

文件：`Assets/Game/Editor/M0SceneBuilder.cs`

### 12.1 使用方式

Unity 菜单：

```text
ECHO → Build M0 Scenes
```

它会：

1. 重建 `Level_00` 的灰盒对象；
2. 重建 `Boot` 的 `Bootstrap`；
3. 设置 Build Settings：Boot(0) / MainMenu(1) / Level_00(2)；
4. 设置默认分辨率/后台运行/产品名；
5. 保存场景和资源。

### 12.2 布局

单位：米。

```text
房间：20 × 20
地面顶面：y = 0
玩家出生/检查点：z = -6
压力板：z = 4
门框/门：z = 8，门洞 x = -2..2
相机：位置 (0,16,-18)，俯角 50°，FOV 45°
```

### 12.3 为什么某些对象没有 Collider

| 对象 | Collider | 原因 |
|---|---|---|
| Player | 无 | 移动由 `PlayerMotor` + `CapsuleCast` 处理 |
| PressurePlate | 无 | 视觉地板，避免挡住玩家 |
| Checkpoint | 无 | 纯视觉，当前在出生点捕获 |
| Echo | 无 | 轨迹回放，不参与物理 |
| Floor/Walls/Door | 有 | 参与胶囊扫掠/阻挡 |

### 12.4 修改场景

- 新增对象：加进 `GeneratedObjectNames`，否则重建时不会被清理；
- 新增引用：在 `BuildLevel` 里 `AddComponent` + 赋值给 `GameLoop` 字段；
- 手工改场景后，如果再次执行菜单，会被覆盖；
- 想保留手工改动，先把它移出 `GeneratedObjectNames` 或改成 Prefab 流程。

---

## 13. 测试导读

### 13.1 测试地图

| 文件 | 验证什么 | 核心不变量 |
|---|---|---|
| `Vec3Tests` | 归一化、投影、插值、clamp | 纯数学正确 |
| `InputFrameTests` | 轴量化、死区、按钮位、朝向、4 字节布局 | 输入压缩不丢关键信息、内存预算 |
| `TimelineRecorderTests` | 环形回绕、最旧帧索引、清空 | `GetFrame(0)` 最旧 |
| `EchoFrameTests` | 16 字节布局、朝向精度 | 内存预算 |
| `EchoManagerTests` | 3 个 FIFO、结束移除 | 第 4 个挤掉最旧 |
| `ActivationAggregatorTests` | OR 激活、0.3s 释放、Reset | 机关状态可重算 |
| `PlayerMotorTests` | 前进移动、传送 | 运动学基本行为 |
| `GameSessionTests` | 回溯条件、检查点恢复、冷却 | 回溯顺序正确 |

当前结果：**23/23 通过**。

### 13.2 测试怎么隔离 Unity

`PlayerMotorTests` / `GameSessionTests` 使用 `FakeCollisionWorld`：

- `CapsuleCast` 永远不命中；
- `CheckGround` 永远返回 true。

这样测试只验证 Domain 逻辑，不依赖场景/物理。

### 13.3 新增测试的步骤

1. 在 `Assets/Tests/EditMode` 新建 `XxxTests.cs`；
2. 命名空间用 `Game.Tests.EditMode`；
3. 引用 `Game.Core` / `Game.Domain` / `Game.Application`；
4. 用 `[Test]` 写断言；
5. 跑 Unity Test Runner → EditMode；
6. 更新本文第 13 章测试地图。

### 13.4 没有测试的部分

- `Presentation` 的 View/场景引用；
- Unity Physics 的实际碰撞；
- 场景生成器的布局；
- 输入设备的真实映射。

这些需要 Play Mode + 手动验证，见第 14 章。

---

## 14. 构建、运行与验证

### 14.1 打开项目

1. Unity Hub 选择 `6000.4.0f1`；
2. 打开 `Assets/Scenes/Boot.unity`；
3. Play。

### 14.2 重新生成场景

菜单：

```text
ECHO → Build M0 Scenes
```

### 14.3 当前构建

- 后端：**Mono2x**
- 产物：`Builds/M0/ECHO.exe`
- 验证：23/23 EditMode 测试；构建成功；启动日志 `Bootstrapper: loading scene Level_00`
- IL2CPP/HybridCLR：可选，当前未启用

### 14.4 Play Mode 检查清单

- [ ] Boot → Level_00
- [ ] WASD 移动
- [ ] Shift 潜行（速度变慢）
- [ ] 踩压力板 → 门升起
- [ ] 按 R → 回检查点，残影出现
- [ ] 残影踩板 → 门保持开启
- [ ] 连续回溯 → 最多 3 个残影
- [ ] 录制到 30s → HUD 显示满
- [ ] 门打开后可以通过

### 14.5 常见问题

| 问题 | 排查 |
|---|---|
| Play 后没有画面 | 当前场景是否是 Level_00；相机是否启用 |
| WASD 不动 | Game 视图焦点；Input System 是否启用；`UnityInputService` 是否读到 |
| 角色穿地 | Floor 是否有 BoxCollider；玩家起点是否在 y=0 以下 |
| 残影不出现 | 是否先录制了至少一帧；`CanRewind` 是否满足 |
| 残影不在压力板上 | 录制轨迹是否包含更早的起点；残影从 t=0 开始重演 |
| 门不开 | 压力板 `Contains` 是否命中；`ActivationAggregator` 是否 tick |
| 门关得太快 | `ReleaseDelay` 是否 0.3s；残影轨迹是否结束 |

---

## 15. 常见修改指南

### 15.1 改移动速度

改：

- `GameConstants.WalkSpeed`
- `GameConstants.SneakSpeed`
- 可能同步 `Docs/DesignRules.md`

测试：

- `PlayerMotorTests` 只验证“有位移”，不锁定具体速度；
- 如果改速度影响关卡时间，更新关卡设计。

### 15.2 改胶囊尺寸

改：

- `GameConstants.PlayerRadius`
- `GameConstants.PlayerHeight`
- `M0SceneBuilder` 里 Player 的 `localScale`
- `PlayerView.capsuleHalfHeight`
- `EchoView.capsuleHalfHeight`

注意：

- 视觉胶囊默认 2m 高、1m 直径；
- scale `(0.6, 0.9, 0.6)` 对应半径 0.3、总高 1.8；
- 改尺寸要同时改视觉和逻辑，否则会“看起来穿墙/悬空”。

### 15.3 改残影数量/录制时长

改：

- `GameConstants.MaxEchoes`
- `GameConstants.RecordLimitSeconds`
- `TimelineRecorder.DefaultCapacity`
- `EchoRecorder.DefaultCapacity`
- `GameHud` 的 `/60f`
- 测试 `EchoManagerTests` 的 3 个上限

### 15.4 加新机关（拉杆/时间锁）

1. `Game.Domain/Mechanisms` 新增规则类；
2. 用“每 tick 重算”的模式，不要用事件直接改门；
3. `GameSession` 增加字段/Tick；
4. `GameLoop` 收集激活源；
5. Presentation 新增 View；
6. `M0SceneBuilder` 生成对象；
7. 加 EditMode 测试；
8. 更新 `Docs/DesignRules.md` 和本文。

### 15.5 加守卫（M1）

预计新增：

- `Game.Core/Perception/IGuardSenses.cs` 或类似接口；
- `Game.Domain/AI/GuardBrain.cs`、`GuardState.cs`；
- `Game.Infrastructure/Physics/GuardVision.cs`（视锥/Linecast）；
- `Game.Presentation/Views/GuardView.cs`；
- `GameLoop` 增加守卫 Tick/View；
- `M0SceneBuilder` 增加守卫对象；
- EditMode 测试感知/状态机；
- 更新本文的“一帧生命周期”和“代码地图”。

关键原则：

- 守卫只对“当前时间线”的目标反应；
- 残影和本体都是可感知目标；
- 感知频率按 tick 对齐（例如每 6 tick 一次），不要用墙钟。

### 15.6 加检查点触发区

1. Presentation 新增 `CheckpointView`（几何 Contains 或 Trigger）；
2. `GameLoop` 每 tick 检查玩家是否进入；
3. 进入时调用 `GameSession.CaptureCheckpoint()`；
4. 规则：进入新检查点清空残影（补 `Echoes.Clear()`）；
5. `M0SceneBuilder` 生成检查点对象；
6. 更新 `DesignRules` / CodeTour。

### 15.7 换相机/改视角

- 只改 `CameraRig` 的 offset/pitch/FOV；
- 如果改了水平 yaw，要同步改 `PlayerMotor` 的输入映射（当前输入是世界方向）；
- 改完检查“出生点、压力板、门是否同屏”。

### 15.8 加 UI

- 当前 `GameHud` 是 `OnGUI` 调试版；
- 换 UGUI/UI Toolkit 时，保留 `Bind(GameSession)`；
- View 只读 `GameSession`，不写玩法状态；
- 可以把 HUD 拆成多个 ViewModel/View。

### 15.9 接 YooAsset（M2）

- `Game.Infrastructure` 新增 `IAssetService` 实现；
- `Bootstrapper` 增加初始化/版本检查；
- 不改变 Domain；
- 先从 `Main / Level_00 / Config` 三个包开始；
- 更新 `Docs/Environment.md` 的包版本/构建说明。

### 15.10 启用 HybridCLR（可选，不推荐现在做）

前提：

- 安装 **Windows Build Support (IL2CPP)**；
- 切到 IL2CPP；
- HybridCLR `enable: 1`；
- 配置热更程序集/AOT 元数据/link.xml。

当前 Mono 路线不需要这些。启用前先读 `Docs/Environment.md` 第 4 节。

---

## 16. 调试方法

### 16.1 HUD

`GameHud` 已显示：

- `Echoes`：残影数量；
- `Record`：本轮录制时长；
- `Rewind`：是否可回溯；
- `Plate`：压力板状态；
- `Cooldown`：回溯冷却；
- `Grounded`：是否落地；
- `Position`：脚底位置。

### 16.2 Unity Test Runner

```text
Window → General → Test Runner → EditMode → Run All
```

### 16.3 Play Mode

- 先看 Game 视图是否有焦点；
- 用 HUD 的 `Position` 判断是否移动；
- 用 `Plate` 判断机关是否激活；
- 用 `Echoes` 判断残影是否生成；
- 如果残影位置不对，先检查录制的起点（残影从 t=0 开始重演）。

### 16.4 日志

- `UnityLogger` 在 `Game.Framework`；
- `Bootstrapper` 启动时输出 `Bootstrapper: loading scene Level_00`；
- 后续可以扩展分级/文件日志，不影响 Domain。

### 16.5 建议增加的可视化（未实现）

- `PlayerMotor` 的胶囊 Gizmo；
- `PressurePlateView` 的范围 Gizmo；
- `EchoPlayback` 的轨迹线；
- `GameSession` 的时间轴调试面板。

这些只影响调试，不影响玩法。

---

## 17. 术语表

| 术语 | 含义 |
|---|---|
| Tick | 逻辑固定步长，当前 1/60s |
| Alpha | 当前渲染时刻在两个 tick 之间的比例 |
| Echo | 残影，回放上一轮轨迹的实体 |
| Checkpoint | 检查点，回溯恢复的时间锚点 |
| Activation Source | 压力板上的激活源（本体/残影） |
| AOT | Ahead-of-Time 编译，Unity 的 IL2CPP 路径 |
| Hotfix | 热更新；当前 Mono 路线不启用 |
| asmdef | Unity Assembly Definition，程序集边界 |
| `noEngineReferences` | asmdef 选项，禁止引用 UnityEngine |
| FIFO | 先进先出；残影超过 3 个时淘汰最旧 |
| Skin Width | 碰撞皮肤，防止贴墙/落地抖动 |

---

## 18. 后续里程碑如何扩展这份导读

### M1：守卫 + 激光 + L1

新增后要更新：

- 第 2 章程序集图（可能新增 `Perception`/`AI` 文件夹）；
- 第 5 章一帧生命周期（守卫感知/攻击 Tick）；
- 第 9 章机关（激光门）；
- 附录 A（GuardBrain/GuardView/GuardVision）；
- 第 13 章测试地图（感知/状态机）；
- 第 15 章修改指南（加守卫）。

### M2：时序 + 升降台 + 3 残影

更新：

- 第 8 章残影（多残影时间差）；
- 第 9 章机关（升降台/时间锁）；
- 第 15 章修改指南（YooAsset/资源管线）。

### M3：声音 + L3 + 存档

更新：

- 第 5 章（声音事件 Tick）；
- 第 10 章（Audio View）；
- 第 13 章（存档迁移测试）；
- 第 15 章（MessagePack/存档）。

### M4：L4 + 菜单/设置/发布

更新：

- 第 4 章启动链路（MainMenu → Level）；
- 第 14 章构建/发布；
- 第 15 章 UI/设置；
- 新增“发布检查清单”。

---

## 附录 A：代码地图（按文件）

> 用途：当你已经理解第 5 章的一帧流程后，用这一章快速定位“某个文件在系统里干什么”。
> 优先级：P0 = 先读；P1 = 核心机制；P2 = 表现/工具；P3 = 可选/占位。

### A.1 Core（纯 C# 契约）

| 文件 | 类型 | 职责 | 关键成员 | 依赖 | 优先级 |
|---|---|---|---|---|---|
| `Assets/Game/Core/GameConstants.cs` | `static class` | 全局数值单一来源 | `TickDelta`、`WalkSpeed`、`PlayerRadius`、`MaxEchoes`、`RecordLimitSeconds` | 无 | P0 |
| `Assets/Game/Core/Math/Vec3.cs` | `readonly struct` | 纯 C# 向量数学 | `Normalized`、`Dot`、`Lerp`、`ProjectOnPlane`、`Approximately` | `System` | P1 |
| `Assets/Game/Core/Input/InputFrame.cs` | `readonly struct` + `enum` | 逻辑输入帧，4 字节 | `FromAxes`、`QuantizeAxis`、`Has`、`InputButtons` | `System` | P0 |
| `Assets/Game/Core/Collision/ICollisionWorld.cs` | `interface` + `struct` | 胶囊扫掠/地面检测契约 | `CapsuleCast`、`CheckGround`、`CapsuleCastResult` | 无 | P1 |
| `Assets/Game/Core/Logging/ILogger.cs` | `interface` | 日志契约 | `Info`、`Warning`、`Error` | 无 | P2 |
| `Assets/Game/Core/Game.Core.asmdef` | asmdef | 纯 C# 程序集边界 | `noEngineReferences: true` | 无 | P0 |

### A.2 Domain（纯 C# 玩法）

| 文件 | 类型 | 职责 | 关键成员 | 依赖 | 优先级 |
|---|---|---|---|---|---|
| `Assets/Game/Domain/Player/PlayerMotor.cs` | `sealed class` | 自定义运动学玩家 | `Tick`、`Teleport`、`MoveWithSlide`、`ApplyGravity` | Core | P0 |
| `Assets/Game/Domain/Timeline/TimelineRecorder.cs` | `sealed class` | 输入环形录制 | `Record`、`GetFrame`、`Clear`、`IsFull` | Core | P1 |
| `Assets/Game/Domain/Timeline/EchoRecorder.cs` | `sealed class` | 残影轨迹环形录制 | `Record`、`GetFrame`、`Clear`、`IsFull` | Core | P1 |
| `Assets/Game/Domain/Timeline/EchoFrame.cs` | `readonly struct` + `enum` | 16 字节残影帧 | `Position`、`Yaw`、`State`、`EchoState` | Core | P1 |
| `Assets/Game/Domain/Echo/EchoPlayback.cs` | `sealed class` | 单残影轨迹回放 | `Tick`、`Reset`、`Previous`、`Current`、`IsFinished` | Core、EchoRecorder | P0 |
| `Assets/Game/Domain/Echo/EchoManager.cs` | `sealed class` | 残影列表/3 个 FIFO | `Create`、`Tick`、`ResetAll`、`Clear`、`Contains` | Core、EchoPlayback | P0 |
| `Assets/Game/Domain/Checkpoint/CheckpointService.cs` | `sealed class` + `struct` | 检查点快照/恢复 | `Capture`、`Restore`、`CheckpointSnapshot` | Core、PlayerMotor | P1 |
| `Assets/Game/Domain/Mechanisms/ActivationAggregator.cs` | `sealed class` + `enum` | 机关激活聚合 | `Tick`、`Reset`、`Mode`、`ReleaseDelay` | Core | P1 |
| `Assets/Game/Domain/Game.Domain.asmdef` | asmdef | 纯 C# 玩法边界 | `noEngineReferences: true` | Core | P0 |

### A.3 Application（用例编排）

| 文件 | 类型 | 职责 | 关键成员 | 依赖 | 优先级 |
|---|---|---|---|---|---|
| `Assets/Game/Application/GameFlow/GameSession.cs` | `sealed class` | M0 用例编排：移动/录制/回溯/机关 | `Tick`、`TickPlate`、`TryRewind`、`CaptureCheckpoint`、`RestoreCheckpoint` | Core、Domain | P0 |
| `Assets/Game/Application/Game.Application.asmdef` | asmdef | 纯 C# 应用层 | `noEngineReferences: true` | Core、Domain | P0 |

### A.4 Framework / Infrastructure（AOT Unity 实现）

| 文件 | 类型 | 职责 | 关键成员 | 依赖 | 优先级 |
|---|---|---|---|---|---|
| `Assets/Game/Framework/Logging/UnityLogger.cs` | `sealed class` | `Game.Core.ILogger` 的 Unity 实现 | `Info`、`Warning`、`Error` | Core、UnityEngine | P2 |
| `Assets/Game/Infrastructure/Physics/PhysicsCollisionWorld.cs` | `sealed class` | `ICollisionWorld` 的 Unity Physics 实现 | `CapsuleCast`、`CheckGround` | Core、UnityEngine | P1 |
| `Assets/Game/Framework/Game.Framework.asmdef` | asmdef | AOT Unity 工具层 | — | Core | P2 |
| `Assets/Game/Infrastructure/Game.Infrastructure.asmdef` | asmdef | AOT Unity 实现层 | — | Core、Framework | P1 |

### A.5 Presentation（表现层 / 组合根）

| 文件 | 类型 | 职责 | 关键成员 | 依赖 | 优先级 |
|---|---|---|---|---|---|
| `Assets/Game/Presentation/Bootstrap/GameLoop.cs` | `MonoBehaviour` | 组合根 + 固定 tick + 视图同步 | `Awake`、`Update`、`Render`、`CountPlateActivators`、`SyncEchoViews` | Application、Domain、Infrastructure、所有 View | P0 |
| `Assets/Game/Presentation/Input/UnityInputService.cs` | `sealed class` | 键鼠/手柄 → `InputFrame` | `Sample` | Core、InputSystem | P1 |
| `Assets/Game/Presentation/Camera/CameraRig.cs` | `MonoBehaviour` | 固定视角 + 平滑跟随 | `Follow` | UnityEngine | P2 |
| `Assets/Game/Presentation/Views/PlayerView.cs` | `MonoBehaviour` | 玩家胶囊插值/颜色 | `Apply` | Domain、UnityEngine | P1 |
| `Assets/Game/Presentation/Views/EchoView.cs` | `MonoBehaviour` | 残影胶囊插值/颜色 | `Bind`、`Apply` | Domain、UnityEngine | P1 |
| `Assets/Game/Presentation/Views/PressurePlateView.cs` | `MonoBehaviour` | 压力板几何包含/颜色 | `Contains`、`SetActive` | Core、UnityEngine | P1 |
| `Assets/Game/Presentation/Views/DoorView.cs` | `MonoBehaviour` | 门开关表现 | `SetOpen` | UnityEngine | P2 |
| `Assets/Game/Presentation/Views/SceneColorView.cs` | `MonoBehaviour` | 灰盒通用上色 | `Awake` | UnityEngine | P3 |
| `Assets/Game/Presentation/UI/GameHud.cs` | `MonoBehaviour` | M0 调试 HUD | `Bind`、`OnGUI` | Application、UnityEngine | P1 |
| `Assets/Game/Presentation/Game.Presentation.asmdef` | asmdef | 表现层/组合根边界 | — | Core、Domain、Application、Framework、Infrastructure | P0 |

### A.6 Bootstrap / Editor / Tests / Scenes

| 文件 | 类型 | 职责 | 关键成员 | 依赖 | 优先级 |
|---|---|---|---|---|---|
| `Assets/Game/Bootstrap/Bootstrapper.cs` | `MonoBehaviour` | 启动并加载 `Level_00` | `Start` | Core、Framework、UnityEngine | P0 |
| `Assets/Game/Bootstrap/Game.Bootstrap.asmdef` | asmdef | AOT 启动器边界 | — | Core、Framework、Infrastructure | P0 |
| `Assets/Game/Editor/M0SceneBuilder.cs` | `static class` | 幂等生成 M0 场景/构建配置 | `BuildM0Scenes`、`BuildLevel`、`CreateCube` | Presentation、Bootstrap、UnityEditor | P1 |
| `Assets/Game/Editor/Game.Editor.asmdef` | asmdef | Editor-only 工具边界 | — | 全部运行时程序集 | P2 |
| `Assets/Tests/EditMode/*.cs` | NUnit 测试 | 纯逻辑回归 | 见第 13 章 | Core、Domain、Application | P1 |
| `Assets/Tests/EditMode/Game.Tests.EditMode.asmdef` | asmdef | 测试程序集 | `UNITY_INCLUDE_TESTS` | Core、Domain、Application | P1 |
| `Assets/Scenes/Boot.unity` | Scene | 启动场景（`Bootstrap`） | — | — | P0 |
| `Assets/Scenes/Level_00.unity` | Scene | M0 灰盒关卡 | — | — | P0 |
| `Assets/Scenes/MainMenu.unity` | Scene | 占位主菜单 | — | — | P3 |

### A.7 按问题查文件

| 我想知道/修改 | 先看 | 再看 |
|---|---|---|
| 游戏怎么启动 | `Bootstrapper.cs` | `GameLoop.Awake`、`M0SceneBuilder` |
| 输入怎么变成移动 | `UnityInputService.Sample` | `InputFrame`、`PlayerMotor.Tick` |
| 为什么角色不穿墙 | `PlayerMotor.MoveWithSlide` | `PhysicsCollisionWorld.CapsuleCast` |
| 为什么角色能贴地 | `PlayerMotor.ApplyGravity` | `PhysicsCollisionWorld.CheckGround` |
| 残影怎么生成 | `GameSession.TryRewind` | `EchoManager.Create`、`EchoPlayback` 构造 |
| 残影怎么回放 | `EchoManager.Tick` | `EchoPlayback.Tick`、`GameLoop.SyncEchoViews` |
| 为什么所有残影回 t=0 | `GameSession.TryRewind` | `EchoManager.ResetAll` |
| 压力板怎么开门 | `GameLoop.CountPlateActivators` | `PressurePlateView.Contains`、`ActivationAggregator`、`DoorView` |
| 画面怎么插值 | `GameLoop.Render` | `PlayerView.Apply`、`EchoView.Apply` |
| 场景怎么生成 | `M0SceneBuilder` | `Docs/CodeTour` 第 12 章 |
| 怎么加测试 | `Assets/Tests/EditMode` | 第 13 章、`FakeCollisionWorld` |
| 怎么改数值 | `GameConstants` | `Docs/DesignRules.md`、相关测试 |

### A.8 关键依赖速查

```text
PlayerMotor
  → Vec3, InputFrame, ICollisionWorld, EchoState, GameConstants

GameSession
  → PlayerMotor, TimelineRecorder, EchoRecorder, EchoManager,
    CheckpointService, ActivationAggregator, ICollisionWorld

EchoPlayback
  → EchoRecorder, EchoFrame, Vec3

EchoManager
  → EchoPlayback, EchoRecorder, GameConstants.MaxEchoes

GameLoop
  → UnityInputService, PhysicsCollisionWorld, GameSession,
    PlayerView, EchoView, CameraRig, PressurePlateView, DoorView, GameHud

PhysicsCollisionWorld
  → ICollisionWorld, UnityEngine.Physics

M0SceneBuilder
  → GameLoop, PlayerView, CameraRig, PressurePlateView, DoorView,
    GameHud, Bootstrapper, UnityEditor
```

### A.9 读代码时的“边界提示”

- 看到 `UnityEngine`：说明已经进入表现层或基础设施层；不要把它反向带进 Domain。
- 看到 `Game.Core`：这是契约层，应该只有纯 C# 类型。
- 看到 `EchoFrame`：这是轨迹数据，不是物理状态；不要在这里加 Collider/Rigidbody。
- 看到 `InputFrame`：这是逻辑输入，不是设备状态；不要在这里读 Keyboard。
- 看到 `GameSession`：这是用例编排，不要把 View/GameObject 塞进来。
- 看到 `GameLoop`：这是组合根；新增服务时优先在这里注入/绑定。

---

## 附录 B：维护检查清单

每次提交前，对照：

- [ ] 新增/删除/改名了代码文件？→ 更新附录 A 代码地图。
- [ ] 改了一帧流程？→ 更新第 5 章时序图。
- [ ] 改了核心规则？→ 先改 `Docs/DesignRules.md`，再改本文。
- [ ] 新增了测试？→ 更新第 13 章测试地图。
- [ ] 新增了修改方式？→ 更新第 15 章。
- [ ] 改了程序集依赖？→ 更新第 2 章依赖图。
- [ ] 改了构建后端/包？→ 更新第 14 章与 `Docs/Environment.md`。
- [ ] 本文中的行号/文件名是否还准确？→ 用 `grep`/IDE 搜索确认。

> 这份导读的价值在于“持续准确”。如果代码变了而导读没变，它会从资产变成负债。
