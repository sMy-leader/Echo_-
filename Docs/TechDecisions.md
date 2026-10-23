# 技术决策记录（ADR 摘要）

> 目的：记录“为什么这样选”，避免后续反复摇摆。
> 新增或推翻决策时，追加一条；不要直接改旧结论，旧结论标记为 Superseded。

## TD-001 美术路线：ProBuilder 模块化 + 少量 Blender

- 状态：Accepted
- 日期：2026-10-21
- 背景：项目是固定视角 3D 工业风 Demo，单人开发，美术不是最强项，但需要可展示的完成度。
- 决策：
  - 结构、房间、门框、管道、平台、灯具等用 ProBuilder 做模块化套件；
  - hero 道具、角色关节、复杂硬表面用 Blender；
  - 正式美术阶段使用 Trim Sheet、Decal、Shader Graph、URP 烘焙光照与后处理；
  - 不做写实高模，不追求精细角色动画。
- 理由：
  - ProBuilder 的强项是快速迭代、blockout、模块化结构，不是高模/角色；
  - 风格化低模工业风与项目范围、个人能力、求职展示最匹配；
  - 模块化套件能显著降低 DrawCall/材质/制作成本。
- 后果：
  - M0–M1 继续灰盒，不做正式美术；
  - M2 开始做 12–20 个模块的套件和第一版光照；
  - M3–M4 再补 hero 资产与最终 art pass；
  - 需要维护 `ArtPipeline` 规范：1m 网格、pivot、命名、材质槽、Lightmap UV、texel density。
- 重新评估触发条件：
  - 需要写实级角色/复杂有机模型；
  - 模块化套件无法满足关卡构图；
  - 美术资产来源/授权出现问题。

## TD-002 VContainer 引入时机：M1 末

- 状态：Accepted
- 日期：2026-10-21
- 背景：VContainer 已安装，但 M0 只有少量服务，手写组合根足够清晰。
- 决策：
  - M0 不引入 VContainer；
  - M1 末引入，先注册 3–5 个服务；
  - M2 扩展到 Root/Level 两级 Scope；
  - Domain/Application 不引用 VContainer，继续构造函数注入；
  - `GameLoop` 继续作为唯一 60Hz tick 驱动，不用 `ITickable` 驱动核心玩法。
- 理由：
  - 当前 DI 收益低，过早引入会增加阅读成本；
  - 守卫/激光/关卡流程会把服务数量和生命周期推上来，那时引入更有说服力；
  - 容器只在组合根，Domain 保持纯 C#，测试可直接 `new`。
- 后果：
  - 需要新增 `GameRootLifetimeScope` / `LevelLifetimeScope`；
  - `Game.Presentation`/`Game.Bootstrap` 的 asmdef 增加 `VContainer` 引用；
  - 需要 PlayMode 集成测试验证 Scope 能解析全部依赖。
- 重新评估触发条件：
  - 服务数量 < 3 且生命周期一致，可继续手写；
  - VContainer 与热更/IL2CPP 出现兼容问题；
  - 团队规模/协作方式变化。

## TD-003 插件策略：当前保留全部，里程碑边界再评估

- 状态：Accepted
- 日期：2026-10-21
- 背景：manifest 中有些模板包（Visual Scripting、Timeline、2D Sprite、Collab Proxy、Multiplayer Center）当前未使用。
- 决策：
  - 当前**不删除**任何包，先保留，避免在功能开发中间引入重导入/构建风险；
  - 在 M1 结束或 M2 开始的里程碑边界统一评估；
  - 评估标准：是否被代码/场景引用、是否影响构建/包体/重导入时间、是否有替代方案；
  - 删除包后必须跑 EditMode 测试 + Mono 构建。
- 理由：
  - 当前包体/构建时间不是瓶颈；
  - 中途删包会增加不确定性，影响开发节奏；
  - 里程碑边界更适合做依赖清理。
- 后果：
  - `Docs/Environment.md` 需要维护包用途/状态表；
  - 下次包审计时逐项决定 Keep/Remove；
  - 删除前确认没有场景/预制体引用。

## TD-004 构建后端：Mono 固定，HybridCLR 可选

- 状态：Accepted
- 日期：2026-10-20
- 背景：Unity `6000.4.0f1` 是外部安装，缺少 Windows Build Support (IL2CPP) 模块；项目是单机 Demo。
- 决策：
  - M0–M4 固定 Mono2x；
  - HybridCLR 保留但 `enable: 0`，不参与日常构建；
  - 代码热更不纳入里程碑门禁；
  - 程序集分层仍按“未来可热更”的边界维护；
  - 以后需要 IL2CPP/HybridCLR 时，再手动安装 TargetSupportInstaller。
- 理由：
  - 单机 Demo 没有线上热更刚需；
  - Mono 已验证可构建、可运行；
  - HybridCLR 会引入 AOT 元数据、裁剪、dll 加载等复杂度，当前收益低。
- 后果：
  - 面试叙事改为“保留边界、按需启用”，而不是“硬上热更”；
  - 资源热更仍可用 YooAsset（Mono 可用）；
  - 如果未来要代码热更，需要重新验证 IL2CPP + HybridCLR。

## TD-005 残影回放：轨迹回放，不重跑物理

- 状态：Accepted
- 日期：2026-10-20
- 背景：残影需要可预测、低开销、能与机关/守卫交互。
- 决策：
  - 残影回放 `EchoFrame`（位置/朝向/状态），不重跑 `PlayerMotor`/物理；
  - `InputFrame` 只用于输入录制/调试/未来可能的输入重放；
  - 残影不推动可推箱子，避免世界状态分叉。
- 理由：
  - 轨迹回放成本低，位置绝对确定；
  - 3 个残影不会带来 3 倍物理模拟；
  - 设计上限制残影交互范围，换取工程可靠性。
- 后果：
  - `EchoFrame` 采用 16 字节布局；
  - `EchoPlayback` 拷贝录制帧，避免下一轮录制覆盖；
  - 残影可踩板/拉杆/挡激光/被击杀，但不能推箱子。

## TD-006 角色视觉：机械构造体低模 + 最小 Mecanim + 程序化副动画

- 状态：Accepted
- 日期：2026-10-22
- 背景：项目是固定视角 3D 工业风 Demo，单人开发，程序/引擎能力优先。
- 决策：
  - 玩家/守卫采用机械构造体低模；
  - Mecanim 只做基础视觉状态（Idle/Walk/Sneak/Interact；守卫 Patrol/Alert 等）；
  - 浮空、倾斜、挤压、头部转动等用程序化副动画；
  - Root Motion 关闭，移动/状态仍由 `PlayerMotor` / `GuardBrain` 决定。
- 理由：
  - 机械体比类人更容易做风格统一和顶部视角可读性；
  - 程序化副动画成本低、辨识度高；
  - Mecanim 保持最小化，避免动画状态机重复玩法状态机。
- 后果：
  - View 驱动 Animator 参数，不驱动玩法；
  - 模型导入后只需要替换 visualRoot/Animator，不需要改 Domain；
  - 残影动画先用幽灵剪影 + 程序化浮动，完整 Mecanim 回放后置。

## TD-007 逻辑胶囊与视觉根节点分离

- 状态：Accepted
- 日期：2026-10-22
- 背景：当前玩家/守卫/残影用胶囊做视觉，后续要替换正式美术。
- 决策：
  - `PlayerMotor` / `GuardBrain` 继续使用逻辑胶囊（Radius/Height）；
  - 角色根节点放在脚底，模型/胶囊放在 `visualRoot` 子节点；
  - 模型不挂 Collider，不驱动移动；
  - 场景生成器负责创建根节点 + visualRoot/forwardIndicator，Prefab 为空时回退胶囊。
- 理由：
  - 玩法逻辑与美术资产解耦；
  - 换模型只改 Prefab/visualRoot，不改 Domain；
  - 顶部视角下守卫需要前向指示器解决胶囊朝向不可读。
- 后果：
  - `PlayerView` / `GuardView` / `EchoView` 增加 visualRoot/Animator 字段；
  - `GameLoop.CreateEchoView` 改为“根节点 + 视觉子节点”；
  - `M0SceneBuilder` 改为生成根节点/子节点，不再把 PlayerView 挂在胶囊中心。

## TD-008 VFX 第一版：代码生成 ParticleSystem，VFX Graph 后置

- 状态：Accepted
- 日期：2026-10-22
- 背景：角色是一体低模，不适合骨骼动画；需要低成本提升悬停/时间能量/警报反馈。
- 决策：
  - 第一版用 `CharacterVfx` 运行时生成 ParticleSystem / TrailRenderer；
  - 使用 URP Particles/Unlit Additive 材质；
  - 接入 Hover / Core / Trail / AlertPulse / Burst 五类效果；
  - VFX Graph、专用 Ghost Shader、粒子对象池后置。
- 理由：
  - 不依赖外部粒子资产，先验证视觉方向；
  - 一体模型用程序化副动画 + VFX 比强行做骨骼更合适；
  - 代码生成的粒子系统容易调参和替换。
- 后果：
  - `CharacterVfx` 只出现在 Presentation；
  - `M0SceneBuilder` 给 Player/Guard 添加 CharacterVfx，`GameLoop` 给 Echo 添加；
  - 残影消散爆发需要后续延迟销毁才能完整播放；
  - 正式版要关注 Overdraw 和粒子数量。
