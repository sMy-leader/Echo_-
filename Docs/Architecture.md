# ECHO / 残响 — 程序架构

> 目标：玩法规则可无引擎测试，表现层可替换，AOT/热更边界从第一天就成立。
> 当前 Mono 构建不启用代码热更；边界保留，未来接 IL2CPP/HybridCLR 时不需要重写分层。

## 1. 分层

```text
┌──────────────────────────────────────────────┐
│ Presentation                                 │
│ View / Input / Camera / UI / VFX / Audio     │
└──────────────────────┬───────────────────────┘
                       │
┌──────────────────────▼───────────────────────┐
│ Application                                  │
│ GameFlow / LevelFlow / RewindUseCase         │
└──────────────────────┬───────────────────────┘
                       │
┌──────────────────────▼───────────────────────┐
│ Domain（纯 C#）                               │
│ PlayerMotor / Timeline / Echo / GuardAI      │
│ Mechanisms / Checkpoint                      │
└──────────────────────┬───────────────────────┘
                       │
┌──────────────────────▼───────────────────────┐
│ Core（纯 C# 契约）                            │
│ Vec3 / InputFrame / ICollisionWorld / ILogger│
└──────────────────────▲───────────────────────┘
                       │
┌──────────────────────┴───────────────────────┐
│ Infrastructure（AOT，Unity 实现）              │
│ PhysicsCollisionWorld / YooAsset / Save      │
└──────────────────────┬───────────────────────┘
                       │
┌──────────────────────▼───────────────────────┐
│ Bootstrapper（AOT） + Unity / URP / Input    │
└──────────────────────────────────────────────┘
```

## 2. 程序集

| 程序集 | 类型 | 热更 | 依赖 | 职责 |
|---|---|---|---|---|
| `Game.Core` | AOT | 否 | 无 | 纯 C# 数学、输入帧、接口契约 |
| `Game.Domain` | 热更 | 是 | `Game.Core` | 确定性玩法规则 |
| `Game.Application` | 热更 | 是 | `Game.Core`, `Game.Domain` | 用例编排、流程状态 |
| `Game.Framework` | AOT | 否 | `Game.Core` | Unity 侧通用工具（日志、事件、池） |
| `Game.Infrastructure` | AOT | 否 | `Game.Core`, `Game.Framework` | 物理查询、资源、存档、配置 |
| `Game.Presentation` | 热更 | 是 | `Game.Core`, `Game.Domain`, `Game.Application`, `Game.Framework`, `Game.Infrastructure`, `Unity.InputSystem`, `UnityEngine.UI`, `Unity.TextMeshPro` | View、Input、Camera、UI、VFX；M0 同时作为组合根 |

> 说明：表中的“热更”列是**未来边界分类**。当前 Mono 构建下，Domain/Application/Presentation
> 都按普通程序集编译；没有启用 HybridCLR，也不需要 IL2CPP。
| `Game.Bootstrap` | AOT | 否 | `Game.Core`, `Game.Framework`, `Game.Infrastructure` | 启动时序、反射入口 |
| `Game.Editor` | Editor | 否 | 全部 | 校验、导出、调试工具 |
| `Game.Tests.EditMode` | Editor | 否 | `Game.Core`, `Game.Domain`, `Game.Application` | 纯逻辑单测 |
| `Game.Tests.PlayMode` | Editor | 否 | 全部 | 场景级集成测试 |

## 3. 硬规则

1. **AOT 不能编译期引用热更程序集。**
   `Game.Bootstrap` 只引用 AOT 程序集；M0 由 `Bootstrapper` 直接加载 `Level_00`，
   由场景里的 `GameLoop` 作为 Presentation 组合根。如果以后接入热更，再改为反射入口
   （`Game.Presentation.GameEntry` 或独立的 `Game.HotfixEntry`）。
2. **Domain 不能引用 UnityEngine。**
   `Game.Domain` 的 asmdef 设置 `noEngineReferences: true`；所有 Unity API 通过
   `Game.Core` 中的接口注入。
3. **依赖方向不可逆。**
   `Presentation → Application → Domain → Core`。
   不允许 Application 反向引用 Presentation；如果入口需要同时创建两者，入口放在
   Presentation（或独立的 `Game.HotfixEntry`）。
4. **不使用全局单例。**
   服务通过组合根注入；M0 可以用手写 CompositionRoot，M1 再切 VContainer。
5. **逻辑与渲染解耦。**
   Domain 只接收 `Tick(dt)` 与 `InputFrame`；渲染插值在 Presentation 做。

## 4. 固定 tick

```text
Update()
  accumulator += min(unscaledDeltaTime, 0.25)
  while accumulator >= 1/60:
      accumulator -= 1/60
      simulation.Tick(1/60)
  presentation.Render(accumulator / (1/60))
```

- 逻辑固定 60Hz；渲染帧率不限。
- `InputService` 在 `Update` 采样，tick 消费缓存，避免一帧多 tick 重复输入。
- 不使用 Rigidbody / CharacterController 驱动玩家；使用 `ICollisionWorld` 做胶囊扫掠。
- 守卫感知等低频逻辑必须按 tick 对齐（例如每 6 tick 一次），不要用墙钟计时。

## 5. 回溯/残影数据流

```text
InputService ──> InputFrame ──> PlayerMotor.Tick
                                   │
                                   ├──> TimelineRecorder（输入录制）
                                   └──> EchoRecorder（位置/朝向/状态录制）
                                             │
CheckpointService.Snapshot ──────────────────┤
                                             ▼
                                     按 R：RewindService
                                             │
                    ┌────────────────────────┼────────────────────────┐
                    ▼                        ▼                        ▼
             恢复检查点快照            生成新 EchoActor         所有残影回到 t=0
                    │                        │                        │
                    └────────────────────────┴────────────────────────┘
                                             ▼
                              EchoActor 按 EchoFrame 轨迹回放
                                             │
                                             ▼
                              ActivationAggregator → 压力板/门
```

### 为什么用轨迹回放

- 残影回放的是录制位置，不重跑物理，天然确定、成本低。
- `InputFrame` 用于录制/调试/未来输入重放，不作为残影回放的唯一真相。
- 设计上禁止残影推动可推箱子，避免世界状态分叉。

## 6. 目录约定

```text
Assets/
├── Game/
│   ├── Core/
│   ├── Domain/
│   ├── Application/
│   ├── Presentation/
│   ├── Infrastructure/
│   ├── Framework/
│   ├── Bootstrap/
│   └── Editor/
├── Scenes/          # Boot / MainMenu / Level_00...
├── Settings/        # URP、Input Actions、Volume
├── Art/             # 模型、材质、贴图
├── Audio/
├── Learning/        # 学习/实验代码，Editor-only，不进发布
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

## 7. M0 范围

M0 只做：

- 固定 60Hz + 输入帧
- 自定义胶囊移动 + 固定 45° 相机
- 检查点快照
- 回溯生成残影
- 残影轨迹回放
- 压力板 + 门

M0 不做：守卫 AI、激光、热更管线、存档、主菜单、正式美术。
