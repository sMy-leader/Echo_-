> **《ECHO / 残响》—— Unity 单机时间回溯潜行解谜 Demo 开发计划书**
>
> | 项目 | 内容 |
> |---|---|
> | 文档版本 | v1.0 |
> | 编制日期 | 2026-10-17 |
> | 开发者画像 | 单人独立开发，具备 Unity 基础；在校，目标是 PC/主机独立游戏方向 |
> | 时间预算 | 总量 ~430 人时，节奏不固定，按里程碑门禁推进 |
> | 基线范围 | 教学关 + 4 个正式关 + 1 个挑战关，18–25 分钟完整流程，PC 平台 |
> | 技术基线 | Unity 6 LTS（6000.0.x）+ URP 17 + YooAsset 2.x + HybridCLR |
> | 交付物 | Windows 可执行构建、完整源码、技术拆解文档、3 分钟演示视频、性能报告 |

---

# 1. 项目概述

## 1.1 推荐的游戏品类与核心玩法一句话介绍

**品类：固定视角 3D 时间回溯 · 潜行解谜（Time-Rewind Stealth Puzzle）**

**一句话玩法：** 你被困在一座时间实验设施中，每次按下回溯，时间会倒流回最近的检查点，而你上一轮的行动会留下一个「残影」——它精确重演你做过的每一个动作。你必须和自己过去的影子配合：一个踩住压力板，另一个穿过激光门；一个引开守卫的视线，另一个从背后溜过去。

**暂定名：** 《ECHO / 残响》（英文名 `ECHO`，中文名《残响》）

**核心动词：** 移动（Move）、潜行（Sneak）、交互（Interact）、回溯（Rewind）、协同（Coordinate）。

## 1.2 品类推荐理由

推荐不是在四个维度上"都还行"，而是**这套机制在四个维度上互相加成**：机制越特殊，技术实现越有深度；技术越有深度，作品集越有辨识度；而它的内容量又天然很小。

| 维度 | 评估 | 具体说明 |
|---|---|---|
| **开发周期** | ★★★★★ | 核心机制只有 1 个（回溯 + 残影），场景是室内小空间，资产复用率极高。不需要敌人种类、不需要技能树、不需要随机地图。4 个关卡是"设计"出来的，不是"生产"出来的。 |
| **创新性** | ★★★★☆ | 时间回溯本身不新（Braid / Outer Wilds / The Sexy Brutale / Deathloop），但「残影是**可被 AI 感知、可触发机关、可被摧毁的实体**，且与本体同时存在于同一空间」这一组合，在小型独立作品中很少见。它带来的不是"新按钮"，而是**新的空间推理方式**。 |
| **技术表现力** | ★★★★★ | 确定性输入重放、状态快照与回滚、多实体时间轴同步、守卫多通道感知、RenderGraph 自定义后处理、半透明残影 Shader、多残影性能优化——每一个都是能讲 20 分钟的技术点，且都**不是模板代码**。 |
| **作品集价值** | ★★★★★ | 面试官看 10 秒视频就能理解"这个人在解决什么非模板问题"。而"做过一个 FPS 控制器"需要 5 分钟解释才勉强算加分。 |

### 被否决的品类及理由

| 品类 | 否决理由 |
|---|---|
| FPS / TPS 射击 | 手感调优依赖大量动画与音频资产，单人 3 个月内做不出商业级手感；且模板化严重，面试官默认你会。 |
| 2D 平台跳跃 | 关卡设计工作量被严重低估（一个"好玩"的平台关卡需要几十次迭代）；同类作品集泛滥。 |
| Roguelike 割草 / 幸存者类 | 内容量陷阱：需要大量敌人、武器、词条、数值，2–3 个月只能做出"能跑"的版本，完成度上不去。 |
| 塔防 / 卡牌构筑 | 系统架构漂亮，但玩法创新弱、UI 与内容量巨大，且"数据驱动"在面试中已被讲烂。 |
| 开放世界 / 生存建造 | 单人业余周期内不可能达到可展示的完成度。 |

## 1.3 核心创新点与差异化亮点

| # | 创新点 | 玩家可感知的体验 | 对应技术支撑 |
|---|---|---|---|
| 1 | **残影是实体，不是录像** | 残影能踩板、能挡激光、能被守卫发现并"杀掉"，但它不会听你指挥 | 残影交互代理 + 触发器系统 + 统一感知接口 |
| 2 | **残影数量即解谜预算** | 最多 3 个残影，第 4 次回溯会挤掉最老的那个，逼你规划"我要留下哪三个自己" | FIFO 时间线管理 + 残影生命周期 + UI 时间轴 |
| 3 | **守卫只对"当前时间线"反应** | 守卫看见残影会去追，本体可以趁机绕后——残影是你的诱饵，也是你的替死鬼 | 多目标感知 + 视线/听觉双通道 + 状态机 |
| 4 | **声音是第二套视野** | 跑动会发出 12 米噪音，潜行只有 3 米；残影消散也会发出声音 | 声音事件系统 + 距离衰减 + 材质脚步声 |
| 5 | **失败是演出，不是惩罚** | 被抓后 0.8 秒的"时间撕裂"演出，直接回到可操作状态，节奏不断 | 后处理时间扭曲 + 音频倒放 + 状态快照恢复 |
| 6 | **世界是一台可被反复改写的机器** | 同一个房间，你会用三种完全不同的时间线组合通过它 | 检查点快照 + 机关激活聚合器 + 关卡状态机 |

**一句话差异化：** 大多数时间回溯游戏让你"重来一次"，这个游戏让你"和上一次的自己组队"。

## 1.4 目标体验时长

| 层级 | 时长 | 说明 |
|---|---|---|
| 单次解谜循环 | 20–60 秒 | 一次"侦察 → 规划 → 执行 → 回溯 → 协同"的完整小循环 |
| 单关 | 3–5 分钟 | 含 1–3 个检查点、2–3 个核心谜题 |
| **核心可体验流程** | **10–15 分钟** | 教学关 + 第 1 关 + 第 2 关，足以让面试官理解全部核心机制 |
| 完整 Demo | 18–25 分钟 | 教学关 + 4 个正式关 + 1 个挑战关 |
| 全收集 / 全解法 | 30–40 分钟 | 每关隐藏一条"优雅解法"（用更少残影通关） |

## 1.5 作品集价值说明

这份 Demo 的价值不在"我又做了一个游戏"，而在于它能证明四件事：

1. **你能设计一个非模板的机制闭环**——从核心动词到关卡教学到难度曲线，全部自洽。
2. **你能把玩法翻译成可靠的技术系统**——确定性重放、快照回滚、AI 感知，这些是"引擎深度"的直接证据。
3. **你理解商业项目的工程边界**——程序集划分、热更管线、存档兼容、性能预算，这些是"能进团队干活"的证据。
4. **你能交付一个完成品**——有开始、有结束、有菜单、有存档、有设置、有性能报告，而不是一个永远在"开发中"的工程。

**最终作品集形态（四件套）：**

| 交付物 | 形式 | 用途 |
|---|---|---|
| 可执行构建 | Windows x64 zip，双击即玩 | 面试现场 / 邮件附件 |
| 演示视频 | 3 分钟，含机制讲解字幕 | 简历链接 / B 站 / YouTube |
| 技术拆解文档 | 8–12 页 PDF + 本 Plan.md | 面试深聊的提纲 |
| 源码仓库 | GitHub，含 README、架构图、提交历史 | 证明代码是你写的 |

## 1.6 关键决策：为什么选 3D 固定视角，而不是 2D

你在 2D 与 3D 之间犹豫，这是本项目**第一个必须锁死的决策**，因为它决定美术管线、渲染方案和性能优化的全部抓手。结论：**做 3D，但用固定视角把 3D 的成本压到接近 2D。**

### 决策对比

| 维度 | 3D 固定视角（推荐） | 2D 俯视 / 侧视 |
|---|---|---|
| 技术展示面 | URP、烘焙光照、阴影、后处理、Shader Graph、LOD、遮挡剔除、SRP Batcher——**全都能用上** | 2D Light、Sprite Atlas、2D Shader，深度有限 |
| 美术成本 | **可控**：全场景用几何体 + 统一 PBR 材质 + 三平面贴图；角色是机械构造体，**不需要骨骼动画** | 看似低，实则高：高质量像素图需要严格调色板与像素级一致性，AI 生成极难稳定；手绘成本更高 |
| 风格统一难度 | 低：统一材质 + 后处理调色 + 烘焙光照，天然统一 | 高：每张图都要对齐风格、透视、光照方向 |
| 求职匹配度（PC/主机独立） | 高：3D 是主流，URP 是硬通货 | 中：2D 独立游戏岗位更少，且更看美术 |
| 玩法可读性 | 固定 45° 斜俯视，与 2D 俯视几乎等价 | 好 |
| 性能优化空间 | 大：DrawCall、LOD、遮挡剔除、阴影级联、GPU Instancing 都有真实抓手 | 小：主要只有图集与合批 |
| 风险 | 3D 建模/动画门槛 | 美术风格不一致 |
| 风险缓解 | 几何体 + ProBuilder 灰盒 + AI 生成 PBR 贴图 + 程序化动画（浮动/倾斜/挤压）+ 粒子 | 需要稳定画师或高质量素材包 |

### 结论与执行约束

- **采用 3D 固定视角**（斜 45°，可选 30°–60° 微调），相机只做轻微跟随与震动，**不做自由旋转**。这既保证了"3D 的技术纵深"，又保留了"2D 的构图可控"。
- **逻辑层完全不依赖 3D**：`Game.Domain` 是纯 C# 程序集，位置用自定义 `Vec3` 或 `UnityEngine.Vector3`（仅在表现层转换）。如果 3 周内灰盒手感不达标，**可只替换表现层切到 2D 俯视，玩法代码一行不改**。这是本方案最重要的风险对冲。
- **美术风格锁定为「冷峻工业 + 时间能量」**：混凝土灰、锈铁、青色时间光、琥珀色警报。低多边形（每个道具 < 2k 三角面），统一材质球不超过 12 个，靠灯光与后处理出质感。

---

# 2. 核心设计文档

## 2.1 核心玩法循环完整拆解

### 三层循环

```
┌─────────────────────────────────────────────────────────────┐
│  关卡级循环（3–5 分钟）                                      │
│  进入房间 → 观察全局 → 识别机关与巡逻路线 → 构造多时间线解法  │
│  → 通过 → 到达检查点 → 下一房间                              │
└─────────────────────────────────────────────────────────────┘
              ▲                                    │
              │                                    ▼
┌─────────────────────────────────────────────────────────────┐
│  回溯级循环（20–60 秒）                                      │
│  ① 侦察 Observe → ② 规划 Plan → ③ 执行 Perform               │
│  → ④ 回溯 Rewind（生成残影）→ ⑤ 协同 Coordinate              │
│  → ⑥ 突破 Breakthrough / 失败 Fail-Fast                      │
└─────────────────────────────────────────────────────────────┘
              ▲                                    │
              │                                    ▼
┌─────────────────────────────────────────────────────────────┐
│  操作级循环（0.2–2 秒）                                      │
│  看（守卫视野/机关状态）→ 走（移动/潜行）→ 做（交互）→ 反馈   │
└─────────────────────────────────────────────────────────────┘
```

### 回溯级循环的六步拆解

| 步骤 | 玩家行为 | 系统响应 | 设计要点 |
|---|---|---|---|
| ① 侦察 | 停下，观察守卫巡逻、机关周期、目标位置 | 守卫视锥可见、机关状态灯、可交互物高亮 | **必须给足观察时间**，房间入口设"安全区" |
| ② 规划 | 在心里排演"这一轮我要走到哪、做什么" | 时间轴 UI 显示当前残影数量与已录制时长 | 规划成本低，才能鼓励试错 |
| ③ 执行 | 移动、潜行、踩板、拉杆、吸引守卫 | 录制输入帧，实时显示录制指示 | 录制对玩家透明，不增加操作负担 |
| ④ 回溯 | 按 R | 0.6 秒时间撕裂演出 → 本体回到检查点 → 残影生成并开始重演 | **回溯必须快**，超过 1 秒就会打断心流 |
| ⑤ 协同 | 与残影同时行动，利用它触发机关/吸引守卫 | 残影按录制轨迹移动，与机关/守卫实时交互 | 这是全部乐趣的来源，必须保证残影行为**可预测** |
| ⑥ 突破 | 通过激光门 / 到达出口 / 拿到目标 | 机关反馈、音乐推进、检查点保存 | 成功必须有强反馈（画面 + 音效 + 短暂慢镜） |

### 失败快循环（Fail-Fast）

```
被发现 → 警戒值满 → 守卫扑击（0.4s）→ 时间撕裂（0.4s）→ 回到检查点
                                                    ↓
                                        残影保留，玩家立即恢复操作
```

**核心设计原则：失败的成本必须低于"思考下一步"的成本。** 如果玩家失败一次要等 3 秒加载、看 5 秒动画，他就会开始厌烦；如果失败只花 0.8 秒，他会把失败当成"试错的一部分"，这正是时间回溯题材应有的体验。

## 2.2 核心机制详细说明与规则设计

### 2.2.1 时间模型

| 规则 | 数值 / 定义 | 理由 |
|---|---|---|
| 逻辑帧率 | 固定 60 Hz（`tick = 1/60s`） | 确定性重放的前提；与渲染帧率解耦 |
| 渲染帧率 | 不限制（目标 60/120 fps） | 逻辑与表现分离，低端机也不影响判定 |
| 回溯目标 | 最近激活的**检查点快照** | 只存 1 份快照，内存与实现复杂度都极低 |
| 检查点数量 | 每关 2–3 个 | 控制单段解谜长度在 60–90 秒内 |
| 回溯表现时长 | 0.6 秒（可跳过） | 足够传达"时间倒流"，又不打断节奏 |
| 回溯后无敌时间 | 0.3 秒 | 避免刚恢复就被守卫连锁抓住 |
| 自动锚定 | 进入检查点、通过关卡时自动保存 | 玩家不需要理解"锚点"概念 |
| 手动重开 | 长按 R 1 秒 | 清空所有残影，回到关卡起点 |

**为什么用"检查点快照"而不是"任意时刻回溯"？**

任意时刻回溯（如 `Braid` 的按住倒带）需要每帧保存世界状态，内存与 CPU 成本高，且会让谜题失去"结构"。检查点方案把谜题切成一段段"可排练的乐章"，每一段的目标清晰、残影数量可控，**更适合解谜而非动作**。这是典型的"用设计约束换工程可靠性"的决策。

### 2.2.2 残影（Echo）规则

| 规则项 | 定义 | 设计意图 |
|---|---|---|
| 生成时机 | 每次**手动回溯**生成 1 个残影 | 让"回溯"从惩罚变成资源 |
| 生成位置 | 从检查点位置开始，重放上一轮从检查点起的全部输入 | 与本体起点一致，便于协同 |
| 数量上限 | 3 个（FIFO，最旧的消散） | 形成"保留哪三个自己"的规划压力 |
| 轨迹确定性 | 100% 按录制输入重放，不受物理推挤、不被守卫阻挡 | 玩家必须能预测残影走位，否则解谜变成赌博 |
| 可交互对象 | 压力板、拉杆、可拾取道具、激光/守卫的伤害判定 | 让残影"有用" |
| 不可交互对象 | 可推动箱子、门把手、需要长按的交互 | 避免回放时世界状态分叉（见 4.1.3 难点 3） |
| 感知属性 | 有视觉体积、有脚步声（强度 = 本体的 80%）、有"生命信号" | 让残影能当诱饵 |
| 死亡条件 | 被守卫扑击 / 被激光命中 / 被环境击杀 | 残影是消耗品，失败有代价但不致命 |
| 死亡后果 | 该残影立即消散 + 发出声音事件（半径 8m） | 死亡也可能是一种"策略"（用消散声引开守卫） |
| 视觉表现 | 半透明（alpha 0.35）+ 边缘 Fresnel 光 + 时间刻度粒子 + 轻微色散 | 一眼可辨"这是残影不是本体" |
| 年龄编码 | 颜色从青（新）渐变到紫（旧），消散前 5 秒开始闪烁 | 玩家能读出"哪个快没了" |
| 录制上限 | 单轮最长 30 秒，超出后自动截断并提示 | 防止无限录制导致数据与平衡失控 |

### 2.2.3 守卫 AI

**状态机：**

```
Patrol ──(视觉/听觉触发)──> Suspicious ──(警戒值≥60)──> Investigate
   ▲                            │                          │
   │                            │(警戒值衰减到 0)          │(到达最后已知位置)
   │                            ▼                          ▼
   └──────────────────────── Search <──(警戒值≥100)── Alert
                                 │
                                 └──(搜索 8s 无果)──> Return ──> Patrol
```

| 状态 | 行为 | 参数 |
|---|---|---|
| Patrol | 沿路点巡逻，停留 2s 环视 | 移速 1.6 m/s，转头速度 90°/s |
| Suspicious | 停下，转向可疑点，警戒值上升 | 持续时间 ≤ 3s |
| Investigate | 走向可疑点，到达后环视 3s | 移速 2.2 m/s |
| Alert | 锁定目标，直线追击 | 移速 3.6 m/s，持续 6s |
| Search | 在最后已知位置周围随机搜索 | 半径 6m，持续 8s |
| Return | 返回巡逻路线 | 移速 1.8 m/s |

**感知系统：**

| 通道 | 参数 | 实现 |
|---|---|---|
| 视觉 | 水平 60°、垂直 40°、距离 8m、遮挡 LayerMask | 视锥内 + `Physics.Linecast` 遮挡检测；每 0.1s 检测一次（10Hz，分帧） |
| 听觉 | 半径 12m，按声音强度线性衰减 | 事件驱动：`SoundEvent(position, radius, intensity)`，守卫收到后比较距离 |
| 警戒值 | 0–100；视觉 +40/s，听觉 +25/次；脱离视野 -15/s | 每 tick 更新，UI 显示为头顶弧形条 |
| 记忆 | 最后已知位置保留 8s | 用于 Investigate / Search |

**关键设计：守卫只对"当前时间线"的目标反应。** 它不知道残影是"过去的你"，它只知道"那里有个人"。这让残影成为天然的诱饵，也制造了本作最有趣的画面：三个半透明的你从守卫眼前跑过，守卫追向其中一个，而真正的你从背后溜走。

**攻击规则：** 守卫进入 1.2m 内且目标在视锥内 → 0.4 秒扑击动画 → 命中则目标死亡（本体 = 关卡失败回溯；残影 = 消散）。

### 2.2.4 机关系统

| 机关 | 行为 | 可被残影触发 | 备注 |
|---|---|---|---|
| 压力板（Pressure Plate） | 有物体站立时激活，离开 0.3s 后关闭 | ✅ | 最基础的协同工具 |
| 拉杆（Lever） | 交互一次切换状态 | ✅ | 需要"按住 0.5s"，残影可执行 |
| 激光门（Laser Gate） | 周期开关或常闭（需供电） | ❌（但可被残影阻挡） | 残影挡激光会消散 |
| 升降台（Lift） | 激活后移动到目标高度 | ✅ | 可制造"时间差"谜题 |
| 时间锁门（Chrono Lock） | 需要 N 个激活源**同时**存在 | ✅ | 多残影协同的核心 |
| 可推动箱子 | 本体可推，残影不可推 | ❌ | 避免回放分叉 |
| 检查点（Checkpoint） | 进入即保存快照 | ✅（仅本体） | 设计上放在安全区 |

**激活聚合器（ActivationAggregator）：** 所有机关的状态由"当前所有激活源"每 tick 计算一次，而不是由"谁踩上去"直接改状态。这样做的好处是：

- 本体和 3 个残影同时踩板时，状态天然正确（OR / AND / COUNT 三种模式）。
- 残影消散时，机关自动重新计算，不需要额外的"清理"逻辑。
- 机关状态可被完整快照与恢复，不会出现"门开着但没人踩"的脏状态。

### 2.2.5 资源与约束

| 资源 | 规则 | 目的 |
|---|---|---|
| 残影槽位 | 3 个，FIFO | 核心解谜预算 |
| 回溯冷却 | 0.5 秒 | 防止连按导致状态机抖动 |
| 录制上限 | 30 秒 / 轮 | 防止数据无限增长 |
| 时间能量（P1 可选） | 上限 100，回溯消耗 25，每秒回 3 | 给"无脑试错"加一点成本；P0 不做 |
| 关卡时限 | 无 | 解谜游戏不应有硬性倒计时 |

### 2.2.6 失败、重试与容错

| 情况 | 处理 | 玩家感知 |
|---|---|---|
| 本体被守卫抓住 | 0.8s 演出 → 恢复到检查点，**不生成残影** | 快速、无惩罚 |
| 本体被激光击杀 | 同上 | 同上 |
| 残影被击杀 | 残影消散 + 声音事件，本体不受影响 | "牺牲一个自己" |
| 卡关超过 3 分钟 | 右上角出现"提示"按钮（可关闭） | 不强制、不打断 |
| 存档损坏 | 自动回退到 `.bak`，再失败则新建存档 | 不崩溃、不丢全部进度 |

### 2.2.7 输入映射

| 动作 | 键鼠 | 手柄 | 备注 |
|---|---|---|---|
| 移动 | WASD | 左摇杆 | 相机固定，移动方向 = 屏幕方向 |
| 潜行 | Shift（按住） | LB（按住） | 移速 1.2 m/s，噪音 3m |
| 交互 | E / 鼠标左键 | A | 长按类交互显示进度环 |
| 回溯 | R | B | 单击触发，长按 1s 重开关卡 |
| 时间轴 | Tab（按住） | 十字键上 | 显示残影轨迹与剩余时间 |
| 暂停 | Esc | Start | 设置 / 重开 / 退出 |

**输入系统设计：** 使用 Unity Input System 的 Action Map（`Gameplay` / `UI` / `Debug`），所有输入读取统一走 `InputService`，**逻辑层只接收抽象后的 `InputFrame` 结构体**——这既是热更友好的解耦，也是录制回放的前提。

## 2.3 Demo 版本关卡 / 内容规划

### 关卡总表

| 关卡 | 名称 | 时长 | 核心教学目标 | 引入机制 | 谜题数 | 检查点 |
|---|---|---|---|---|---|---|
| L0 | 苏醒（教学） | 2–3 min | 学会移动、交互、回溯、残影踩板 | 回溯、残影、压力板 | 2 | 1 |
| L1 | 视线 | 4–5 min | 学会用残影当诱饵 | 守卫视觉、激光门 | 3 | 2 |
| L2 | 时序 | 4–5 min | 学会制造"时间差" | 升降台、周期激光、3 残影 | 3 | 2 |
| L3 | 回声 | 4–5 min | 学会管理噪音 | 听觉感知、双守卫、材质地面 | 3 | 2 |
| L4 | 残响 | 5–6 min | 综合运用全部机制 | 时间锁门、多房间、最终逃脱 | 4 | 3 |
| LX | 挑战室（可选） | 3 min | 用 ≤1 个残影通关 | 全机制 | 1 | 1 |

### 关卡设计细则

**L0 · 苏醒**

- 场景：一个狭长的实验舱，尽头是一扇需要压力板供能的门。
- 教学节奏（不弹窗，用环境引导）：
  1. 玩家醒来 → 地面有发光的脚印指向控制器 → 交互后门锁亮起。
  2. 压力板在门右侧，玩家踩上去门开，离开门关——第一次"意识到一个人不够"。
  3. 屏幕角落提示 `[R] 回溯`，按下后生成残影，残影自动重演"走过去踩板"。
  4. 门保持开启（残影仍在板上），玩家通过。
- 验收：首次游玩 3 分钟内通过；不需要文字说明也能理解。

**L1 · 视线**

- 场景：一条走廊 + 两个侧室，1 名守卫巡逻，1 道常闭激光门。
- 谜题 1：压力板在守卫巡逻路径上，直接踩会被发现 → 用残影踩板，本体躲进侧室。
- 谜题 2：激光门需要"供电"（拉杆），拉杆在守卫视野死角 → 残影拉杆，本体通过。
- 谜题 3（隐藏）：不生成任何残影，利用守卫转身的 3 秒窗口跑过去（奖励"优雅解法"）。
- 验收：至少有 2 种解法；玩家平均尝试 3–5 次。

**L2 · 时序**

- 场景：垂直竖井 + 两层平台 + 周期激光（开 2s / 关 3s）。
- 谜题 1：升降台需要持续按压，本体无法同时乘坐 → 残影压台，本体上升。
- 谜题 2：上层有两个时间锁，需要 2 个激活源同时存在 → 2 个残影 + 本体。
- 谜题 3：第 3 个残影用来挡住一束激光（牺牲），为最后一段路争取 3 秒。
- 验收：玩家必须理解"残影会在固定时间到达固定位置"，开始做"时间编排"。

**L3 · 回声**

- 场景：图书馆/档案室，木地板（噪音 1.5×）与金属格栅（噪音 0.6×）交替，2 名守卫。
- 谜题 1：跑过木地板会引来守卫 → 学会潜行与绕路。
- 谜题 2：残影在金属格栅上跑动的声音更小，可以当"安静诱饵"。
- 谜题 3：让残影在远处被击杀（消散声 8m），把两名守卫都引过去。
- 验收：玩家能主动使用"声音"而非只躲避视线。

**L4 · 残响**

- 场景：三层环形控制室 + 中央时间核心，3 名守卫，时间锁门，最终出口。
- 结构：三个子区域，每个区域需要一个特定组合（踩板 / 拉杆 / 诱饵），最后在中央汇合。
- 最终演出：出口开启时，所有残影同时转向玩家（叙事暗示：它们也是你）。
- 验收：全流程 6 分钟内可完成；至少 1 条"零死亡"路线。

**设计原则（重要）：**

1. **一个房间只教一件事**，教完立刻用一次，再用一次变体。
2. **每个谜题必须有"安全观察点"**，玩家可以在不被发现的情况下看完全部巡逻周期。
3. **每关至少一条隐藏优雅解法**（更少残影 / 更快 / 不杀残影），给重玩与视频展示留素材。
4. **不做随机**：所有巡逻、激光周期固定，解谜游戏的可预测性优先于重玩性。

## 2.4 玩家反馈与难度曲线设计

### 反馈层级

| 层级 | 触发 | 表现 | 时长 |
|---|---|---|---|
| 微反馈 | 移动、脚步 | 角色轻微上下浮动、倾斜、脚步粒子 + 材质音 | 持续 |
| 中反馈 | 踩板、拉杆、开门 | 机关状态灯变色、机械音、轻微相机震动、门体动画 | 0.3–0.8s |
| 强反馈 | 回溯 | 全屏径向模糊 + 色差 + 色调反转 + 音频倒放 + 时间粒子内爆 | 0.6s |
| 强反馈 | 残影生成 | 从玩家位置"剥离"出半透明复制体 + 冲击波 + 低频音 | 0.4s |
| 强反馈 | 残影消散 | 碎片化溶解 + 上升粒子 + 8m 声音事件 | 0.5s |
| 强反馈 | 被发现 | 屏幕边缘红色脉冲 + 警报音 + 守卫视锥变红 + 手柄震动 | 持续到脱离 |
| 强反馈 | 通过关卡 | 短暂慢镜（0.3× 速度 0.5s）+ 音乐上扬 + 检查点白光 | 1.5s |
| UI 反馈 | 残影槽位 | 3 个圆形槽，生成/消散有缩放与颜色动画 | 0.2s |

### 难度曲线

```
难度
 ▲
 │                                        ╭── L4 综合
 │                          ╭── L3 声音 ─╯
 │              ╭── L2 时序 ─╯
 │      ╭─ L1 视线 ─╯
 │  ╭─ L0 教学 ─╯
 └──┴──────┴──────┴──────┴──────┴──────► 时间
    0–3    3–8    8–13   13–18  18–25 (min)
```

| 阶段 | 关卡 | 认知负荷 | 失败惩罚 | 设计目标 |
|---|---|---|---|---|
| 学习期 | L0–L1 | 低 | 极低（几乎不会死） | 建立"回溯 = 资源"的心智模型 |
| 应用期 | L2–L3 | 中 | 低（0.8s 重试） | 学会"编排时间线"而非"即时反应" |
| 精通期 | L4 | 高 | 低 | 自主组合机制，产生"我很聪明"的爽感 |

**节奏控制：** 每个紧张段落之后必须有一个"安全屋"（无守卫、有观察窗、有环境叙事），让玩家喘息并规划。**连续高压超过 90 秒就会让解谜变成动作游戏**，这是本作最需要避免的失败模式。

### 辅助功能（低成本、高印象分）

| 功能 | 实现 | 成本 |
|---|---|---|
| 高对比度模式 | 后处理 LUT 切换 + 守卫视锥加粗 | 0.5 天 |
| 色盲友好 | 机关状态用"形状 + 颜色"双编码 | 1 天 |
| 延长警戒时间 | 全局参数倍率 1.5× | 0.2 天 |
| 字幕 / 音量分离 | 配置驱动 | 0.5 天 |
| 手柄震动强度 | 设置项 | 0.2 天 |

---

# 3. 技术栈选型与整体架构

## 3.1 引擎版本与选型理由

**选择：Unity 6 LTS（`6000.0.x` LTS 分支）+ URP 17.x**

| 理由 | 说明 |
|---|---|
| LTS 支持周期 | Unity 6 LTS 提供约 2 年官方支持，补丁稳定，适合"做完一个能长期展示的项目"。 |
| RenderGraph 成熟 | URP 17 的 RenderGraph 是 Unity 6 的默认渲染框架，自定义 Pass 的写法与商业项目一致，面试可讲。 |
| 新特性可用 | GPU Resident Drawer、STP（空间时间后处理）、改进的烘焙光照，都能成为优化章节的实证。 |
| 生态兼容 | YooAsset 2.x、HybridCLR v7+、Input System、Cinemachine 3.x 均已适配 Unity 6。 |
| 招聘关键词 | 2026 年 PC/主机独立团队普遍已迁移到 Unity 6，写在简历上是有效信号。 |

**版本锁定策略（商业项目规范）：**

- `ProjectSettings/ProjectVersion.txt` 锁定到具体补丁版本（例如 `6000.0.5xf1`），团队/个人都**禁止中途升级 minor 版本**。
- `Packages/manifest.json` 中所有第三方包使用**精确版本号**，不使用 `^` 范围。
- 升级只在里程碑间隙做，且必须跑完整回归 + 性能基线对比。
- 记录一份 `Docs/Environment.md`：Unity 版本、包版本、构建机配置、SDK 版本。

**备选与降级：** 若某个关键插件（如特定 Shader 工具）不兼容 Unity 6，退回 **2022.3 LTS + URP 14**。代价是失去 RenderGraph 与 GPU Resident Drawer，但核心玩法与热更方案完全不受影响。

**平台范围：** 首发 Windows x64；架构上保留 macOS 与移动端的适配点（输入抽象、画质分级、分辨率适配），但**不在本期实现**，避免范围蔓延。

## 3.2 整体程序架构图与设计思路

### 分层架构

```
┌──────────────────────────────────────────────────────────────────┐
│ 表现层 Presentation                                               │
│  UI View / ViewModel · Camera · Renderer Feature · VFX · Audio    │
│  职责：把状态画出来、把输入收进来、播放反馈                        │
└───────────────────────────┬──────────────────────────────────────┘
                            │ 单向依赖（表现层知道领域层，反之不行）
┌───────────────────────────▼──────────────────────────────────────┐
│ 应用层 Application                                                │
│  GameFlow · LevelFlow · CheckpointService · InputService          │
│  职责：编排用例（开始关卡、回溯、存档、暂停），不含具体规则         │
└───────────────────────────┬──────────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────────┐
│ 领域层 Domain（纯 C#，noEngineReferences）                         │
│  Timeline · Echo · PlayerMotor · GuardAI · Mechanism · LevelRules │
│  职责：确定性规则、状态、事件；可单元测试、可无引擎运行             │
└───────────────────────────┬──────────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────────┐
│ 基础设施 Infrastructure                                           │
│  AssetService(YooAsset) · SaveService · ConfigService · Logger     │
│  EventBus · ObjectPool · HotfixLoader                             │
└───────────────────────────┬──────────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────────┐
│ 引擎与第三方 Unity · URP · Input System · HybridCLR · MessagePack │
└──────────────────────────────────────────────────────────────────┘
```

### 设计思路（为什么这么分）

| 决策 | 理由 | 商业项目对应 |
|---|---|---|
| **领域层纯 C#** | 玩法规则不依赖 `MonoBehaviour`，可以用 NUnit 秒级跑几千次回归；确定性重放必须能在无渲染环境下验证 | 很多商业项目的 `Logic` 层就是纯 C#，便于热更与多端复用 |
| **表现层用 MVVM** | UI 只做"绑定 + 动画"，不写业务判断；ViewModel 可单测 | 手游/端游的 UI 框架主流做法 |
| **玩法层用"数据驱动的系统"而非完整 ECS** | 本作实体数量 < 100，ECS 是过度设计；但用 `struct` 状态 + 系统函数，保留确定性 | 商业项目也常在"小规模高确定性"场景用类似结构 |
| **不用全局单例** | 所有服务通过 VContainer 注入，生命周期明确；只有 `Bootstrap` 是入口 | 避免 `FindObjectOfType` 与初始化顺序地狱 |
| **事件总线而非直接引用** | 领域层发出 `EchoDiedEvent`，表现层订阅播特效；领域层不知道特效存在 | 解耦、可测试、可回放 |

### 目录结构（约定）

```
Assets/
├── Game/
│   ├── Bootstrap/                 # 启动场景、入口、热更加载
│   ├── Framework/                 # AOT：日志、事件、池、工具
│   ├── Domain/                    # 热更：纯 C# 玩法规则
│   │   ├── Timeline/
│   │   ├── Echo/
│   │   ├── AI/
│   │   ├── Mechanisms/
│   │   └── Level/
│   ├── Application/               # 热更：用例编排
│   ├── Presentation/              # 热更：View、ViewModels、VFX、Audio
│   │   ├── UI/
│   │   ├── Rendering/
│   │   └── Feedback/
│   ├── Infrastructure/            # AOT：YooAsset 封装、存档、配置
│   └── Editor/                    # 编辑器工具（不打包）
├── ThirdParty/
├── Art/
│   ├── Models/  Materials/  Textures/  VFX/
├── Audio/
├── Settings/                      # URP Asset、Volume Profile、Input Actions
└── Tests/
    ├── EditMode/                  # Domain 单元测试
    └── PlayMode/                  # 集成测试
```

## 3.3 程序集划分（含热更边界）

| 程序集 | 类型 | 是否热更 | 依赖 | 职责 | 备注 |
|---|---|---|---|---|---|
| `Game.Bootstrap` | AOT | ❌ | Framework, Infrastructure, HybridCLR | 入口、启动时序、加载热更 dll | 必须最先运行 |
| `Game.Framework` | AOT | ❌ | 无（仅 UnityEngine） | 日志、事件总线、对象池、扩展方法 | 热更代码可调用 |
| `Game.Infrastructure` | AOT | ❌ | Framework, YooAsset, MessagePack | 资源、存档、配置、热更加载器 | 含第三方封装 |
| `Game.ThirdParty` | AOT | ❌ | 第三方 dll | MessagePack、UniTask 等 | 用 asmdef 隔离 |
| `Game.Domain` | 热更 | ✅ | Framework | 玩法规则、确定性模拟 | `noEngineReferences: true` |
| `Game.Application` | 热更 | ✅ | Domain, Framework | 用例编排、流程状态机 | |
| `Game.Presentation` | 热更 | ✅ | Domain, Application, Framework, UniTask | UI、渲染、音效、输入表现 | |
| `Game.Editor` | Editor | ❌ | 全部 | 关卡校验、配置导出、时间轴调试 | 仅编辑器 |
| `Game.Tests.EditMode` | Editor | ❌ | Domain | 纯逻辑单测 | 不打包 |
| `Game.Tests.PlayMode` | Editor | ❌ | 全部 | 场景级集成测试 | 不打包 |

**热更边界的设计原则：**

1. **AOT 只放"启动器 + 基础设施 + 第三方"**，因为这些代码变动频率低，且 HybridCLR 对 AOT 泛型的补充成本高。
2. **所有业务逻辑（Domain / Application / Presentation）全部热更**，这样"改玩法不用重新出包"的收益最大化。
3. **`Game.Domain` 不引用 UnityEngine**，用 asmdef 的 `noEngineReferences: true` 强制约束。这样即使有人误写 `Debug.Log`，编译期就会报错，逼你用 `ILogger` 抽象。
4. **依赖方向由 asmdef 强制**：`Presentation → Application → Domain → Framework`，反向引用会在编译期失败。CI 里再加一条脚本检查 asmdef 依赖图，防止循环。

**为什么不用一个巨大的 Assembly-CSharp？**

- 编译速度：改一行 UI 代码只重编 `Game.Presentation`，而不是整个工程。
- 热更粒度：可以只更新 `Presentation.dll`，不必重下全部代码。
- 架构约束：程序集边界是"廉价而有效"的架构护栏，比口头约定可靠。

## 3.4 YooAsset + HybridCLR 热更新完整架构设计

### 3.4.1 单机项目为什么仍然要做热更

先说清楚"不做"的理由：单机游戏没有服务器，理论上"发一个版本就完事"。但本项目的目标是**工程能力训练 + 作品集**，热更在这里的真实价值是：

| 价值 | 说明 |
|---|---|
| **程序集边界治理** | 强制你区分 AOT / 热更、稳定 / 易变，这是商业项目最核心的工程能力之一。 |
| **内容与代码解耦** | 关卡、配置、美术资源可以独立更新；发布后修一个数值 bug 不需要重新出 300MB 的包。 |
| **发布管线训练** | 版本号、资源清单、增量下载、哈希校验、回滚——这套流程在任何商业项目里都一样。 |
| **面试叙事** | "我在单机项目里跑通了完整的 YooAsset + HybridCLR 管线，并量化了启动开销"比"我会用 Addressables"有说服力得多。 |

**同时必须诚实说明成本：**

- 构建复杂度上升：需要额外的 dll 打包、AOT 元数据生成、资源清单构建步骤。
- 启动时序变长：初始化 YooAsset → 检查版本 → 加载 dll → 反射入口，冷启动增加约 0.5–1.5 秒。
- 调试成本上升：热更代码的断点、堆栈、异常需要额外处理。
- 包体增加：HybridCLR 运行时 + AOT 元数据约增加 5–10MB。

**结论：** 本项目做，但把它做成**可开关的管线**（`UseHotfix = true/false`），并在文档里写清楚"什么规模的游戏不该上这套方案"——这本身就是工程判断力的体现。

### 3.4.2 启动流程（完整时序）

```
[0] Unity 引擎启动，加载 Bootstrap 场景
     │
[1] 初始化日志 / 异常捕获 / 平台信息
     │
[2] YooAssets.Initialize()
     │
[3] 创建资源包 "Main"（PlayMode 由环境决定）
     │
[4] RequestPackageVersionAsync()  ──► 得到远端版本号（如 1.0.3）
     │
[5] 对比本地版本
     ├── 相同 ──► 跳到 [8]
     └── 不同 ──► UpdatePackageManifestAsync(version)
                   │
[6] CreateResourceDownloader() → 计算差异文件
     │
[7] 下载（并发 8，重试 3 次，断点续传，进度 UI）
     │
[8] 加载热更程序集清单（HotfixManifest）
     ├── 读取 AOT 元数据 dll 列表
     ├── RuntimeApi.LoadMetadataForAOTAssemblies()
     └── 逐个 Assembly.Load(热更 dll bytes)
     │
[9] 反射找到入口类（约定：Game.Application.GameEntry）
     │
[10] 调用 GameEntry.StartAsync() → 加载主菜单场景
```

**关键实现点：**

```csharp
// 伪代码：启动器核心时序
public sealed class Bootstrapper : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var yoo = new YooAssetService();
        await yoo.InitializeAsync(playMode);              // EditorSimulate / Host / Offline
        var version = await yoo.RequestVersionAsync();
        if (version != yoo.LocalVersion)
        {
            await yoo.UpdateManifestAsync(version);
            var downloader = yoo.CreateDownloader();
            await downloader.DownloadAsync(onProgress);  // 断点续传由 YooAsset 处理
        }

        var hotfix = await yoo.LoadRawFileAsync("HotfixManifest");
        var manifest = HotfixManifest.Parse(hotfix);

        // 1) 先补充 AOT 泛型元数据，再加载热更 dll（顺序不能反）
        foreach (var aotDll in manifest.AotMetadataList)
            RuntimeApi.LoadMetadataForAOTAssembly(
                await yoo.LoadRawFileAsync(aotDll), HomologousImageMode.Consistent);

        // 2) 加载热更程序集（注意依赖顺序）
        foreach (var dllName in manifest.HotfixAssemblyList)
            Assembly.Load(await yoo.LoadRawFileAsync(dllName));

        // 3) 反射入口
        var entryType = System.Type.GetType("Game.Application.GameEntry, Game.Application");
        entryType.GetMethod("StartAsync").Invoke(null, null);
    }
}
```

### 3.4.3 YooAsset 资源分包策略

**包（Package）划分：**

| 资源包 | 内容 | 更新频率 | 大小目标 |
|---|---|---|---|
| `Main` | 框架 UI、公共材质、公共 Shader、字体、通用音效 | 低 | < 40 MB |
| `Level_00` | 教学关场景、光照贴图、专属资产 | 中 | < 20 MB |
| `Level_01` | 第 1 关 | 中 | < 25 MB |
| `Level_02` | 第 2 关 | 中 | < 25 MB |
| `Level_03` | 第 3 关 | 中 | < 25 MB |
| `Level_04` | 第 4 关 | 中 | < 30 MB |
| `Hotfix` | 热更 dll + AOT 元数据 + 配置表 | 高 | < 8 MB |
| `Audio_Extra`（可选） | 高质量音乐，可单独下载 | 低 | < 50 MB |

**分包原则（重要，面试常问）：**

1. **按"更新单元"分包，不按"文件类型"分包。** 一关的所有资产（场景、光照、音频、预制体）放同一个包，这样更新一关只下一个包；如果按类型分（所有贴图一个包），改一关的一张图会导致整包重下。
2. **共享资源必须下沉到 `Main`。** 守卫预制体、门、压力板等跨关卡复用的资产放 `Main`，避免每个关卡包各存一份（YooAsset 的依赖分析会报重复资源）。
3. **Shader 单独收集并常驻。** Shader 变体集与材质分离，便于做变体剥离；Shader 包永不卸载。
4. **包体控制在 30MB 以内。** 单个 AB 过大导致加载峰值与内存抖动；过小导致文件句柄与依赖管理开销。30MB 是本项目经验值。
5. **配置表与代码同包（`Hotfix`）。** 保证"代码版本 ↔ 配置版本"原子一致，避免出现"新代码读旧配置"的经典事故。

**资源标签（Tag）用于运行时批量操作：**

| 标签 | 用途 |
|---|---|
| `preload` | 启动时预加载（UI 图集、通用音效、Shader） |
| `level` | 关卡切换时统一卸载 |
| `ui` | UI 打开/关闭时按引用计数加载/卸载 |
| `vfx` | 特效池预热 |
| `audio` | 音频组管理 |

### 3.4.4 资源加载与卸载管理

**核心设计：一切资源访问都经过 `IAssetService`，禁止业务代码直接 `Resources.Load` 或 `AssetDatabase`。**

```csharp
public interface IAssetService
{
    UniTask<T> LoadAssetAsync<T>(string location, CancellationToken ct = default) where T : UnityEngine.Object;
    IAssetHandle<T> LoadAssetHandle<T>(string location) where T : UnityEngine.Object;
    UniTask<GameObject> InstantiateAsync(string location, Transform parent);
    void Release(IAssetHandle handle);
    UniTask PreloadAsync(IReadOnlyList<string> locations, IProgress<float> progress);
    void UnloadUnused();
}
```

**引用计数与生命周期：**

- 每个 `IAssetHandle` 持有 YooAsset 的 `AssetHandle`，`Dispose` 时归还。
- 场景级资源由 `LevelScope` 统一持有：进入关卡时 `Load`，离开时 `Dispose + UnloadUnused`。
- UI 资源按"打开次数"计数，关闭时减一，归零后延迟 5 秒卸载（避免快速开关抖动）。
- **每 30 秒或在场景切换后**调用一次 `Resources.UnloadUnusedAssets()`，并用 Profiler 验证没有意外驻留。

**异步加载规范：**

- 所有加载返回 `UniTask`，支持 `CancellationToken`（切场景时取消未完成请求）。
- 加载句柄必须有超时（10 秒）与失败重试（2 次），失败时回退到占位资源并记录日志。
- **禁止在 `Update` 里同步 `WaitForCompletion`**，这是移动端卡顿的头号原因。

### 3.4.5 版本控制与回滚机制

| 机制 | 实现 |
|---|---|
| 版本号 | 语义化 `Major.Minor.Patch`，存于 `PackageVersion` 文件与远端清单 |
| 资源清单 | YooAsset 生成的 `PackageManifest`（含文件 hash、大小、依赖） |
| 完整性校验 | 下载后校验文件 CRC；清单本身用 SHA256 校验 |
| 增量更新 | 只下载 hash 不同的文件；YooAsset 内置 diff |
| 断点续传 | YooAsset 下载器支持；记录已下载临时文件 |
| 回滚 | 本地保留上一版本清单与文件（`cache/backup`），启动失败时切回 |
| 灰度（可选） | 本地配置文件模拟"灰度比例"，用于演示发布流程 |
| 强制更新 | 版本号 Major 不同则不允许跳过 |

**单机落地形态：**

- **开发期**：`HostPlayMode` + 本地 HTTP 静态服务器（`python -m http.server` 或 Node 静态服务）指向 `Bundles/` 目录，完整模拟 CDN 增量更新。这一步是"练管线"的关键，不要跳过。
- **发布期**：`OfflinePlayMode`（资源全部内置）或"内置首包 + 本地可写目录"的混合模式。首发版推荐 **OfflinePlayMode**，保证玩家下载即玩；同时保留热更代码路径，用于演示与后续补丁。
- **更新演示**：录制一段"修改一张贴图 → 构建 → 启动 → 只下载 2MB"的视频，作为作品集素材。

### 3.4.6 HybridCLR 程序集划分与 AOT 泛型补充

**热更程序集清单：**

| dll | 内容 | 依赖 |
|---|---|---|
| `Game.Domain.dll` | 玩法规则、时间轴、AI、机关 | `Game.Framework.dll` |
| `Game.Application.dll` | 流程、用例、存档编排 | Domain, Framework |
| `Game.Presentation.dll` | UI、渲染、音效、输入表现 | Domain, Application, UniTask |

**AOT 侧必须保留的：** `Game.Bootstrap`、`Game.Framework`、`Game.Infrastructure`、`Game.ThirdParty`。

**AOT 泛型补充（HybridCLR 的核心坑）：**

- 热更代码里如果出现 `List<MyHotfixStruct>`、`Dictionary<string, MyHotfixClass>` 这类**以热更类型为泛型参数**的实例化，AOT 侧没有对应代码，运行时会 `MissingMethodException`。
- 解决方案：
  1. 运行 `HybridCLR/Generate/AOTGenericReferences`，生成 `AOTGenericReferences.cs`。
  2. 把其中列出的 AOT dll 加入 `AotMetadataList`，启动时 `LoadMetadataForAOTAssembly`。
  3. 对确实无法覆盖的泛型，写一个 `AOTGenericHelper` 显式实例化（"泛型补全"）。
- **工程纪律：** 热更代码中尽量使用 `List<T>` / `Dictionary<K,V>` 的常见闭包类型；避免在热更层定义泛型委托并跨程序集传递。

**裁剪（IL2CPP）：**

- `link.xml` 保留：热更入口类、反射用到的类型、MessagePack 生成的 resolver、Unity 生命周期类。
- Managed Stripping Level 设为 `Low`（本项目体量小，优先稳定）；发布前用 `Medium` 跑一次完整回归。
- **每次构建后必须验证**：真机/打包版启动、加载热更 dll、跑通一关。这是 HybridCLR 项目最容易翻车的地方。

**热更代码加载顺序（必须遵守）：**

```
1. LoadMetadataForAOTAssemblies(所有 AOT 补充 dll)
2. Assembly.Load(Game.Framework 对应的 AOT 已在包内，无需加载)
3. Assembly.Load(Game.Domain.dll)
4. Assembly.Load(Game.Application.dll)
5. Assembly.Load(Game.Presentation.dll)
6. 反射调用入口
```

顺序错误的典型症状：`TypeLoadException`、`MissingMethodException`、`NullReferenceException` 出现在看似无关的地方。

### 3.4.7 热更调试方案

| 场景 | 方案 |
|---|---|
| 编辑器内开发 | HybridCLR 的 Editor 模式：热更程序集直接以源码编译，断点、Inspector、热重载全部可用 |
| 编辑器内验证热更加载 | 关闭 Editor 模式，走完整的"构建 dll → 加载"流程，验证时序 |
| 真机/打包版日志 | 日志写入 `persistentDataPath/logs/`，分级 + 轮转（保留 5 个文件），启动时输出到 `Player.log` 同目录 |
| 异常堆栈 | 热更 dll 保留调试符号（`pdb`/`mdb`），异常捕获时输出原始堆栈 + 翻译后的行号 |
| 版本比对 | 启动时打印 `AppVersion / ResourceVersion / HotfixHash`，便于确认加载的是哪一版 |
| 远程调试 | 可选：内置一个只读的 HTTP 状态页（端口 8080），显示当前版本、资源列表、最近日志 |

### 3.4.8 方案落地价值总结（面试话术）

> "这个项目是单机，但热更不是摆设。我用 YooAsset 做了按关卡分包和增量更新，用 HybridCLR 做了三层热更程序集划分。真正的收益是：**程序集边界倒逼我把 AOT 和热更分开，把玩法逻辑做成纯 C#，把资源访问收敛到一个服务里**。我也量化了成本：冷启动多 1.2 秒，包体多 8MB。如果项目只有两周，我不会上这套方案——这是工程判断，不是技术炫技。"

## 3.5 数据持久化方案选型与设计

### 3.5.1 主流方案对比

| 方案 | 可读性 | 体积 | 读写性能 | 版本兼容 | 跨平台 | AOT/IL2CPP | 适用场景 |
|---|---|---|---|---|---|---|---|
| PlayerPrefs | 中（注册表/plist） | 极小 | 快 | 差（无结构） | 好 | 好 | 仅存音量、语言等**极小键值** |
| JSON | 高 | 大（文本 1.5–3×） | 慢（解析） | 好（字段可缺省） | 好 | 好 | 配置表、设置、调试数据 |
| Protobuf | 低 | 小 | 快 | 中（需 .proto 演进规则） | 好 | 好 | 网络协议、跨语言数据 |
| MessagePack | 低 | 小 | 很快 | 中（需 schema 管理） | 好 | **需预生成 resolver** | **存档**、结构化二进制 |
| SQLite | 中（可用工具查看） | 中（库 ~1MB） | 中（有 IO/事务开销） | 好（SQL 迁移） | 好 | 好 | 大量结构化数据、查询、排行榜 |
| 自定义二进制 | 低 | 最小 | 最快 | 差（全靠自己） | 好 | 好 | 高频内部数据（如回放帧） |
| MemoryPack | 低 | 最小 | 最快 | 中 | 好 | 需 source generator | 极致性能场景 |
| BinaryFormatter | — | — | — | — | — | — | **已废弃，禁用** |

### 3.5.2 选型结论（分层，不搞"一刀切"）

| 数据类型 | 选型 | 理由 |
|---|---|---|
| **玩家存档** | **MessagePack-CSharp + LZ4** | 体积小、读写快、C# 对象直接序列化、支持版本容错；比 JSON 小 60%，比 SQLite 简单一个数量级 |
| **游戏配置** | **源表 CSV/Excel → 导出 JSON（紧凑）→ 运行时转只读结构** | 策划/自己可编辑、可 diff、可被 YooAsset 单独热更；运行时反序列化一次并建索引，之后零分配查询 |
| **偏好设置** | **JSON 明文** | 需要可读、可手改、跨版本容错；体积极小（< 4KB），性能无所谓 |
| **时间轴回放数据** | **自定义二进制（内存 + 可选落盘）** | 每帧 8 字节，高频写入，绝不能走反射序列化；仅用于录制/调试，不参与存档 |

**为什么不用 SQLite？**

- 本作存档只有"进度 + 统计 + 解锁"三类，总量 < 100KB，没有复杂查询需求。
- SQLite 引入 ~1MB 原生库、线程/事务模型、移动端 IO 差异，**收益远小于复杂度**。
- 如果未来做"玩家行为分析"或"排行榜"，再引入也不迟——这是可扩展性，不是现在就要付的成本。

**为什么不用 PlayerPrefs 存存档？**

- 无结构、无版本、无校验；Windows 下写注册表，卸载不清理；大字符串性能差。
- 只保留三个键：`last_version`、`language`、`first_launch`。

### 3.5.3 存档设计

**数据模型：**

```csharp
[MessagePackObject]
public sealed class SaveData
{
    [Key(0)] public int SchemaVersion;          // 存档结构版本，迁移用
    [Key(1)] public string GameVersion;         // 产生该存档的游戏版本
    [Key(2)] public long SavedAtUnix;           // 时间戳，用于冲突提示
    [Key(3)] public ProgressData Progress;      // 关卡进度、检查点、解锁
    [Key(4)] public StatsData Stats;            // 死亡次数、回溯次数、最速通关
    [Key(5)] public SettingsSnapshot Settings;  // 设置快照（与 settings.json 双写）
}

[MessagePackObject]
public sealed class ProgressData
{
    [Key(0)] public int HighestLevel;
    [Key(1)] public Dictionary<int, LevelRecord> Levels; // 关卡ID → 记录
}
```

**文件封装（防损坏的核心）：**

```
┌────────────┬──────────────┬─────────────┬──────────┬──────────────┬─────────┐
│ Magic "ECHO"│ SchemaVersion│ PayloadLen  │ CRC32    │ Timestamp    │ Payload │
│  4 bytes   │   2 bytes    │  4 bytes    │ 4 bytes  │  8 bytes     │  N bytes│
└────────────┴──────────────┴─────────────┴──────────┴──────────────┴─────────┘
```

**原子写入流程（断电安全）：**

```
1. 序列化 payload → 计算 CRC32 → 拼装 envelope
2. 写入 save.tmp（FileStream + Flush(true) 强制落盘）
3. 校验 tmp 文件（重新读取并比对 CRC）
4. 若 save.dat 存在 → 重命名为 save.bak
5. 将 save.tmp 重命名为 save.dat
6. 删除多余临时文件
```

**读取流程（三级容错）：**

```
读取 save.dat → 校验 Magic/CRC → 成功则用
   │ 失败
   ▼
读取 save.bak → 校验 → 成功则用并告警
   │ 失败
   ▼
创建新存档（保留损坏文件为 save.corrupt.<时间戳>）
```

**版本兼容与迁移：**

```csharp
public interface ISaveMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    void Migrate(SaveData data);
}

// SaveService 内部按版本链依次执行：1→2→3
// 迁移失败时：备份原档 + 提示"存档来自更新版本"，绝不静默丢弃
```

**校验与防篡改：**

- CRC32 用于**防损坏**（必需）；SHA256 用于**完整性**（可选）。
- AES-256 加密：单机项目防的是"玩家手改数值"，不是"破解"。**在文档里明确写清这一点**，比假装能防破解更专业。
- 密钥不硬编码在明文里：用 `[SerializeField]` 的 `ScriptableObject` + 构建时注入，或简单混淆（成本可控即可）。

**自动存档时机：**

| 时机 | 说明 |
|---|---|
| 通过检查点 | 写进度（最频繁，必须快，< 5ms） |
| 通过关卡 | 写进度 + 统计 |
| 修改设置 | 写 settings.json（防抖 0.5s） |
| 退出游戏 | 写一次完整存档 |

**性能约束：** 存档写入必须在后台线程完成（MessagePack 序列化 + 文件 IO），主线程只做"拷贝数据快照"，避免卡顿。

### 3.5.4 配置系统设计

```
Excel/CSV (源表，可 diff)
    │  Editor 工具导出（校验主键/外键/范围）
    ▼
JSON (紧凑，随 Hotfix 包分发)
    │  运行时反序列化一次
    ▼
只读配置表 + 字典索引（O(1) 查询，零运行时分配）
```

| 表 | 字段示例 | 用途 |
|---|---|---|
| `LevelConfig` | id, sceneName, checkpointIds, timeLimit | 关卡元数据 |
| `GuardConfig` | id, speed, viewAngle, viewRange, hearRange, alertRise | 守卫参数 |
| `MechanismConfig` | id, type, activationMode, requiredCount, duration | 机关参数 |
| `EchoConfig` | maxCount, recordLimit, fadeTime, footstepLoudness | 残影参数 |
| `AudioConfig` | id, clip, volume, maxDistance, mixerGroup | 音频参数 |

**Editor 校验规则（导出时强制）：**

- 主键唯一、非空。
- 外键必须存在（如 `LevelConfig.checkpointIds` 必须能在场景中找到）。
- 数值范围合法（视野角度 0–180，速度 0–10）。
- 所有字段必须有注释（导出的 JSON 带 `_comment` 字段）。

## 3.6 各模块技术选型与选型理由

| 领域 | 选型 | 选型理由 | 替代方案 | 风险 / 备注 |
|---|---|---|---|---|
| 渲染管线 | **URP 17** | 性能可控、Shader Graph 友好、自定义 Renderer Feature 成熟、PC/移动通吃 | HDRP（过重）、Built-in（已淘汰） | 需注意 RenderGraph API 与旧版不同 |
| 输入 | **Input System 1.11+** | 设备无关、Action Map 可配置、支持回放与重绑定 | 旧 Input Manager（无法重绑定） | 学习曲线略高 |
| 相机 | **Cinemachine 3.x**（仅用固定跟随 + 震动） | 震动/推拉/淡入淡出开箱即用 | 自写 CameraRig（更轻） | 若只用固定视角，自写 80 行也够，可按需取舍 |
| 补间动画 | **PrimeTween** | 零 GC、API 简洁、性能优于 DOTween 的多数场景 | DOTween（生态大但 GC 与授权）、自写 | 社区较小，需读源码 |
| 异步 | **UniTask** | 零分配的 async/await、深度集成 Unity 生命周期、支持 CancellationToken | C# Task（GC 高）、协程（不可组合） | 团队需统一规范，避免 `async void` |
| DI | **VContainer** | 轻量、IL2CPP 友好、性能好、支持热更程序集 | Zenject（重、反射多）、手写 ServiceLocator | 需要理解生命周期（Singleton/Scoped/Transient） |
| 序列化（存档） | **MessagePack-CSharp** | 快、小、C# 友好、支持 AOT resolver | Protobuf（需 .proto）、MemoryPack（更新但生态小） | **必须预生成 resolver**，否则 IL2CPP 下反射失败 |
| 序列化（配置/设置） | **Newtonsoft.Json** | 容错好、可读、Unity 生态成熟 | System.Text.Json（Unity 支持较新） | 解析大表时注意分配，一次性加载可接受 |
| 日志 | **自研 Logger** | 分级、文件轮转、Release 可关闭、零分配格式化 | ZLogger / NLog（依赖重） | 必须支持"热更代码调用 AOT 日志" |
| 事件 | **自研 EventBus** | 类型安全、可观测、易调试、避免字符串事件 | UniRx（已停更）、MessagePipe（优秀但多一层依赖） | 注意订阅泄漏，统一在 `IDisposable` 中释放 |
| 对象池 | **自研 Pool + Unity ObjectPool** | 残影/特效/UI 元素高频复用，必须零分配 | 第三方池（功能过剩） | 池化对象必须实现 `IPoolable` 重置 |
| 测试 | **Unity Test Framework** | EditMode 测 Domain、PlayMode 测集成，CI 可跑 | NUnit 裸用（无 Unity 集成） | Domain 纯 C# 是前提 |
| 版本管理 | **Git + Git LFS** | 美术资源走 LFS，代码走普通 Git | Perforce（单人过重） | 需配置 `.gitattributes` |
| CI（可选） | **GitHub Actions** | 自动跑 EditMode 测试 + 构建 + 性能基线 | 本地脚本 | Unity License 配置略繁琐 |
| 编辑器工具 | **自研（UIElements/IMGUI）** | 关卡校验器 + 配置导出器 + 时间轴调试器 | Odin（收费） | 见第 4 章 |

**明确不引入的技术（及理由）：**

| 技术 | 不用的理由 |
|---|---|
| DOTS / ECS | 实体数量 < 100，收益为负；但可借鉴其"数据与行为分离"的思想 |
| Addressables | 与 YooAsset 功能重叠，二选一；YooAsset 的 API 更轻、更适合学习底层 |
| Timeline | 本作的"时间轴"是**玩法逻辑时间轴**，不是过场动画；引入会混淆概念 |
| FairyGUI | UGUI 足够，且热更/图集/工作流更可控 |
| Odin Inspector | 收费；自研 2 个工具更有作品集价值 |
| DOTween Pro | 收费；PrimeTween 免费且性能更好 |

---

# 4. 核心模块拆分与功能详情

> 每个模块统一按 **功能边界 → 实现逻辑 → 技术难点与解决方案 → 能力提升点** 展开。

## 4.1 核心玩法模块

### 4.1.1 功能边界

**负责：**

- 固定 tick 逻辑循环（60Hz）与渲染解耦。
- 输入录制、回放、时间轴管理。
- 玩家移动、潜行、交互。
- 残影生成、重放、交互代理、生命周期。
- 守卫感知、状态机、寻路、攻击。
- 机关状态求值与聚合。
- 检查点快照的采集与恢复。
- 关卡流程（进入、通过、失败、重开）。

**不负责：**

- 任何渲染、特效、音频播放（通过事件通知表现层）。
- UI 布局与动画。
- 资源加载（通过 `IAssetService`）。
- 存档落盘（通过 `ISaveService`）。

### 4.1.2 实现逻辑

#### （1）固定 tick 循环

```csharp
// Bootstrap 中每帧驱动，逻辑与渲染解耦
public sealed class GameLoop : MonoBehaviour
{
    private const float TickDelta = 1f / 60f;
    private float _accumulator;

    private void Update()
    {
        _accumulator += Mathf.Min(Time.unscaledDeltaTime, 0.25f); // 防止卡顿后追帧爆炸
        while (_accumulator >= TickDelta)
        {
            _accumulator -= TickDelta;
            _simulation.Tick(TickDelta);   // 纯 C#，无 UnityEngine 依赖
        }
        _presentation.Render(_accumulator / TickDelta); // 插值渲染
    }
}
```

**要点：**

- 逻辑固定 60Hz，渲染可 30/60/120Hz，用 `alpha` 插值位置，避免视觉抖动。
- `Physics.simulationMode = SimulationMode.Script`，在 tick 内手动 `Physics.Simulate(TickDelta)`，或者**干脆不用 Rigidbody**（推荐）。
- 输入在 `Update` 中采样并缓存，在 tick 中消费，保证"一帧多 tick"时输入不重复。

#### （2）玩家移动：不用 Rigidbody

```csharp
// 自定义运动学：胶囊体 Sweep + 地面吸附
public void Tick(float dt, in InputFrame input, LevelGeometry geometry)
{
    var wishDir = new Vec3(input.MoveX, 0, input.MoveY).Normalized();
    var speed = input.Sneak ? SneakSpeed : WalkSpeed;
    var delta = wishDir * speed * dt;

    // 水平移动：CapsuleCast 检测，滑动沿墙
    MoveWithSlide(delta, geometry);
    // 垂直：重力 + 地面检测（SphereCast 向下 0.1m）
    ApplyGravity(dt, geometry);
}
```

**为什么不用 `Rigidbody` / `CharacterController`？**

- `CharacterController` 的内部实现不保证跨版本确定性，且与自定义 tick 的配合别扭。
- `Rigidbody` 的物理解算在固定步长下仍受浮点与求解器顺序影响，且会引入"被箱子推着走"的不可控因素。
- 自定义运动学代码量约 300 行，换来**完全可预测**——这对"残影必须精确重演"是刚需。

#### （3）输入录制与回放

```csharp
// 8 字节/帧，30 秒 = 1800 帧 = 14.4KB
public readonly struct InputFrame
{
    public readonly sbyte MoveX;    // -127..127
    public readonly sbyte MoveY;
    public readonly byte Buttons;   // bit0 潜行, bit1 交互, bit2 回溯...
    public readonly byte LookYaw;   // 预留（固定相机下为 0）
}

public sealed class TimelineRecorder
{
    private readonly InputFrame[] _buffer = new InputFrame[1800];
    private int _head;
    private int _count;

    public void Record(in InputFrame frame) { _buffer[_head] = frame; _head = (_head + 1) % _buffer.Length; _count = Math.Min(_count + 1, _buffer.Length); }
    public ReadOnlySpan<InputFrame> GetFrames() { /* 处理环形回绕 */ }
    public void Clear() { _head = 0; _count = 0; }
}
```

#### （4）残影重放：不重跑模拟，只重放轨迹

这是本方案最关键的性能决策。

```
❌ 方案 A：为每个残影跑一份完整的物理/角色模拟（成本 ×4，且容易与世界状态冲突）
✅ 方案 B：录制"位置 + 朝向 + 状态"的关键帧，回放时按 tick 插值驱动一个"交互代理"
```

**方案 B 的实现：**

```csharp
public struct EchoFrame
{
    public Vec3 Position;
    public float Yaw;
    public byte State;      // Idle / Walk / Sneak / Interact
}   // 16 字节/帧，30 秒 = 43KB/残影

public sealed class EchoActor
{
    private readonly EchoFrame[] _frames;
    private int _cursor;

    public void Tick()
    {
        var f = _frames[_cursor++];
        _transform.SetPositionAndRotation(f.Position, Quaternion.Euler(0, f.Yaw, 0));
        _animator.SetState(f.State);       // 表现层：程序化动画
        _interactionProxy.SetActive(true); // 触发器：压力板 / 拉杆 / 激光
    }
}
```

**为什么方案 B 更好？**

| 维度 | 方案 A（重跑模拟） | 方案 B（轨迹回放） |
|---|---|---|
| CPU | 4 个残影 = 4 倍模拟成本 | 仅插值 + 触发器，成本可忽略 |
| 确定性 | 依赖物理/浮点，跨平台有风险 | 位置是录制值，**绝对确定** |
| 与世界冲突 | 残影会被门/箱子推挤，轨迹变形 | 轨迹固定，只读世界 |
| 实现复杂度 | 需要完整的"无副作用模拟" | 简单直接 |
| 缺点 | 理论上更"真实" | 残影不能推箱子（已在设计上规避） |

**结论：** 用轨迹回放，并在设计上明确"残影不能推物理物体"。这是**用设计约束换工程可靠性**的典型案例，面试时非常加分。

#### （5）守卫 AI

```csharp
public sealed class GuardBrain
{
    private GuardState _state;
    private float _alert;          // 0..100
    private Vec3 _lastKnownPos;
    private readonly GuardConfig _cfg;

    public void Tick(float dt, in GuardSenses senses)
    {
        // 1) 感知（视觉 10Hz 分帧，听觉事件驱动）
        var target = senses.FindBestTarget();       // 本体优先？还是最近？→ 规则：最近可见目标

        // 2) 警戒值
        if (senses.CanSee(target)) _alert += _cfg.VisualRise * dt;
        else _alert -= _cfg.AlertDecay * dt;
        _alert = Math.Clamp(_alert, 0, 100);

        // 3) 状态迁移
        _state = _state switch
        {
            GuardState.Patrol when _alert > 20 => GuardState.Suspicious,
            GuardState.Suspicious when _alert >= 60 => GuardState.Investigate,
            GuardState.Investigate when _alert >= 100 => GuardState.Alert,
            GuardState.Alert when _alert < 30 => GuardState.Search,
            GuardState.Search when _alert <= 0 => GuardState.Return,
            _ => _state
        };

        // 4) 行为执行（移动、转向、攻击）
        Execute(_state, dt);
    }
}
```

**视觉检测实现：**

```csharp
public bool CanSee(in TargetInfo t)
{
    var to = t.Position - EyePosition;
    var dist = to.Length();
    if (dist > _cfg.ViewRange) return false;
    if (Vec3.Dot(Forward, to / dist) < CosHalfAngle) return false;   // 视锥
    if (Physics.Linecast(EyePosition, t.EyePosition, _occluderMask)) return false; // 遮挡
    return true;
}
```

**分帧策略：** 视觉检测对每个守卫每 6 个 tick 执行一次（10Hz），并把不同守卫的检测错开到不同 tick，避免同一帧集中计算。3 个守卫 × 1 次射线 = 每帧 0.5 次，成本可忽略。

#### （6）机关聚合

```csharp
public enum ActivationMode { Any, All, Count }

public sealed class ActivationAggregator
{
    private readonly List<IActivator> _activators = new();

    public bool Evaluate(ActivationMode mode, int required)
    {
        int active = 0;
        foreach (var a in _activators) if (a.IsActive) active++;
        return mode switch
        {
            ActivationMode.Any => active > 0,
            ActivationMode.All => active == _activators.Count,
            ActivationMode.Count => active >= required,
            _ => false
        };
    }
}
```

**关键：** 机关状态每 tick 全量重算，而不是"踩上 +1 / 离开 -1"。后者在残影消散、回溯、对象禁用时会漏减，产生"门永远开着"的 bug。全量重算的成本是 O(n)，n < 10，完全可接受。

#### （7）检查点快照

```csharp
public struct WorldSnapshot
{
    public PlayerState Player;
    public GuardState[] Guards;       // 3 个 × 32B
    public MechanismState[] Mechanisms; // 20 个 × 16B
    public float[] AlertValues;
}   // 约 1–2 KB

// 保存：进入检查点时 DeepCopy
// 恢复：逐字段写回，并强制所有"瞬时状态"（动画、粒子、音效）由表现层重置
```

**为什么快照只有 1–2KB？** 因为我们**不保存残影**（残影是独立的时间线，回溯后继续存在），也不保存"世界已死对象"（关卡内没有可破坏物）。这是设计约束带来的巨大简化。

### 4.1.3 技术难点与解决方案

| # | 难点 | 具体表现 | 解决方案 |
|---|---|---|---|
| 1 | **确定性** | 同一段输入两次回放，残影位置略有偏移，导致踩板失败 | ① 逻辑固定 60Hz；② 不用 Rigidbody；③ 浮点运算顺序固定（避免 LINQ/字典遍历顺序影响）；④ 录制的是**位置**而非输入重算（方案 B）；⑤ 单元测试：同一段输入跑 1000 次，位置误差 = 0 |
| 2 | **快照与残影的语义冲突** | 回溯后，残影还在踩板，但门的快照是"关"的 | 明确规则：**快照只恢复"世界 + 本体"，残影不参与快照**；机关每 tick 由聚合器重算，所以残影一站上去门立刻开。语义统一在"聚合器"一处 |
| 3 | **残影与世界状态分叉** | 残影推开箱子后，回放时箱子位置不同，后续轨迹全错 | 设计规避：残影**不能推动物理物体**；所有可交互物分为"触发器类"（可被残影影响）与"物理类"（不可）。用 `IEchoInteractable` 标记接口显式约束 |
| 4 | **多残影同时交互的性能** | 4 个角色 × 每秒 60 次触发器检测 | 残影用**离散位置采样 + 触发器**，不用 `CharacterController`；压力板用 `OnTriggerEnter/Exit` 而非每帧 Overlap；激光用射线与胶囊体求交，每 2 tick 一次 |
| 5 | **回溯手感** | 演出太长打断节奏；太短没有"时间感" | 0.6 秒演出 + 0.3 秒无敌 + 可跳过（按任意键）；演出期间输入缓冲，结束后立即生效 |
| 6 | **守卫在残影与本体间选择目标** | 守卫反复切换目标，行为抽搐 | 规则：**锁定最近可见目标 2 秒**，期间不切换；目标消失后 0.5 秒才重新选择；用 `TargetLockTimer` 实现 |
| 7 | **回溯后表现层残留** | 残影生成了，但上一轮的特效/音效还在播 | 表现层订阅 `RewindEvent`，统一 `StopAll + Reset`；所有短生命周期表现对象必须走对象池并在重置时清理 |
| 8 | **检查点误触发** | 残影踩到检查点，导致快照被覆盖 | 检查点只响应本体（`IEchoInteractable` 不实现于检查点），且只保存一次（幂等） |

### 4.1.4 能力提升点

- **固定步长模拟与渲染解耦**：理解游戏循环的本质，而不是"在 Update 里写逻辑"。
- **确定性系统的设计**：浮点、执行顺序、状态管理——这是网络同步/回放/录像系统的基础。
- **用设计约束换工程可靠性**：残影不能推箱子、只存检查点快照，都是"少做一点，换来稳定"的成熟判断。
- **AI 状态机与感知系统**：视野锥、遮挡、听觉、警戒值、目标锁定，一套完整的潜行 AI。
- **数据与表现分离**：领域层纯 C#、事件驱动表现，为热更与测试打基础。

## 4.2 资源管理与热更新模块

### 4.2.1 功能边界

- YooAsset 初始化、包管理、版本检查、清单更新。
- 资源下载（进度、断点续传、重试、校验）。
- 资源加载/实例化/释放（引用计数、句柄、超时）。
- 场景加载（异步、additive、卸载）。
- 热更 dll 与 AOT 元数据加载。
- 资源泄漏检测与卸载策略。

**不负责：** 具体业务资源的语义（由各模块决定加载什么）。

### 4.2.2 实现逻辑

```csharp
public sealed class YooAssetService : IAssetService, IDisposable
{
    private readonly Dictionary<string, AssetHandle> _handles = new();
    private ResourcePackage _package;

    public async UniTask InitializeAsync(PlayMode mode)
    {
        YooAssets.Initialize();
        _package = YooAssets.CreatePackage("Main");
        InitializeParameters p = mode switch
        {
            PlayMode.EditorSimulate => new EditorSimulateModeParameters
            {
                SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild("Main")
            },
            PlayMode.Offline => new OfflinePlayModeParameters(),
            PlayMode.Host => new HostPlayModeParameters
            {
                BuildinQueryServices = new BuildinQueryServices(),
                RemoteServices = new LocalHttpRemoteService("http://127.0.0.1:8080/bundles/")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        await _package.InitializeAsync(p);
    }

    public async UniTask<string> RequestVersionAsync()
    {
        var op = _package.RequestPackageVersionAsync();
        await op.ToUniTask();
        return op.PackageVersion;
    }
}
```

**加载策略：**

| 场景 | 策略 |
|---|---|
| 启动预加载 | `preload` 标签资源并行加载，进度条按"文件数"而非"字节"（更平滑） |
| 关卡加载 | `LoadSceneAsync(Additive)` + 依赖资源预加载 + 淡入淡出遮罩 |
| UI 加载 | 首次打开时异步加载，缓存句柄，关闭时延迟卸载 |
| 运行时实例化 | 对象池优先，池空才 `InstantiateAsync` |

### 4.2.3 技术难点与解决方案

| # | 难点 | 解决方案 |
|---|---|---|
| 1 | 资源重复打包（同一贴图进多个包） | 构建后用 YooAsset 的依赖报告 + 自研 Editor 工具扫描重复资源，共享资源强制下沉 `Main` |
| 2 | 引用计数泄漏 | 所有句柄由 `LevelScope` / `UIScope` 持有，作用域结束时统一释放；Debug 模式下每 10 秒打印未释放句柄的调用栈 |
| 3 | 下载中断与网络抖动 | 重试 3 次（指数退避）、超时 30 秒、断点续传、失败后提示"重试/跳过（跳过会导致资源缺失，仅调试可用）" |
| 4 | 版本不一致导致加载失败 | 启动时强校验清单 hash；进入关卡前校验该关卡包完整性；失败则回退上一版本 |
| 5 | 编辑器模拟模式与真机行为不一致 | 每周至少一次真机（打包）验证；CI 跑一次 `OfflinePlayMode` 冒烟测试 |
| 6 | 热更 dll 加载顺序错误 | 在 `HotfixManifest` 中显式声明依赖拓扑序，加载前做一次断言校验 |

### 4.2.4 能力提升点

- **理解 AssetBundle 的依赖、冗余、生命周期**，而不是只会 `Resources.Load`。
- **掌握一套可落地的热更发布流程**：版本、清单、增量、校验、回滚。
- **资源引用计数与作用域管理**：这是商业项目里最容易出事故、也最能体现经验的地方。
- **异步与取消**：`UniTask` + `CancellationToken` 处理场景切换时的悬挂请求。

## 4.3 数据持久化模块

### 4.3.1 功能边界

- 存档的序列化、校验、原子写入、备份、迁移、加密。
- 配置表的加载、索引、校验、热更。
- 设置项的读写、默认值、防抖保存。
- 存档版本与游戏版本的一致性检查。

### 4.3.2 实现逻辑

```csharp
public interface ISaveService
{
    UniTask<SaveData> LoadAsync();
    UniTask SaveAsync(SaveData data);          // 内部走后台线程 + 原子写入
    UniTask<SaveData> CreateNewAsync();
    event Action<SaveData> OnSaved;
}

public sealed class SaveService : ISaveService
{
    private readonly string _dir = Application.persistentDataPath;

    public async UniTask SaveAsync(SaveData data)
    {
        var payload = MessagePackSerializer.Serialize(data, SaveResolver.Options);
        var envelope = SaveEnvelope.Pack(payload);        // Magic + Version + CRC + Timestamp
        await FileIO.WriteAtomicAsync(Path.Combine(_dir, "save.dat"), envelope);
        OnSaved?.Invoke(data);
    }
}
```

**MessagePack 的 AOT 配置（必须）：**

```csharp
// 用 mpc 工具预生成 resolver，避免 IL2CPP 下的动态代码生成
[Preserve]
public static class SaveResolver
{
    public static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard
            .WithResolver(StaticCompositeResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4Block);
}
```

- 在 `link.xml` 中保留 `SaveData`、`ProgressData`、`StatsData` 及其字段。
- 把 `MessagePack.Resolvers.GeneratedResolver` 加入 `StaticCompositeResolver`。
- **每次新增存档字段，都要重新生成 resolver 并跑一次打包版验证**。

### 4.3.3 技术难点与解决方案

| # | 难点 | 解决方案 |
|---|---|---|
| 1 | 断电/崩溃导致存档损坏 | 原子写入（tmp → 校验 → 替换）+ `.bak` 备份 + 三级读取容错 |
| 2 | 版本升级后旧档读不了 | `SchemaVersion` + 迁移链；新增字段给默认值；删除字段保留 `[Key]` 不复用 |
| 3 | IL2CPP 下 MessagePack 反射失败 | 预生成 resolver + `link.xml` + 打包版回归测试 |
| 4 | 存档写入卡顿 | 后台线程序列化 + 异步 IO；主线程只拷贝数据快照 |
| 5 | 玩家手改存档 | CRC + 轻量加密（明确"防手改不防破解"的定位） |
| 6 | 配置表热更后与代码不匹配 | 代码与配置同包（`Hotfix`）；配置表带 `minCodeVersion` 字段，启动时校验 |

### 4.3.4 能力提升点

- **序列化的取舍**：文本 vs 二进制、反射 vs 源生成、压缩 vs 速度。
- **数据完整性设计**：校验、备份、迁移——这是"能上线"和"会写 Demo"的分水岭。
- **AOT 约束下的工程处理**：resolver、link.xml、裁剪，这些是 Unity 高级岗位的常见考点。

## 4.4 UI 交互模块

### 4.4.1 功能边界

- UI 框架：层级管理、栈式导航、异步加载、缓存。
- MVVM 数据绑定：ViewModel 暴露状态，View 只负责渲染与动画。
- HUD：残影槽位、警戒指示、交互提示、录制指示。
- 菜单：主菜单、暂停、设置、关卡选择、结算。
- 多分辨率适配、手柄导航、本地化（可选）。

**不负责：** 业务规则（ViewModel 调用 Application 层用例）。

### 4.4.2 实现逻辑

**为什么用 UGUI 而不是 UI Toolkit？**

| 维度 | UGUI | UI Toolkit |
|---|---|---|
| 运行时成熟度 | 极高 | 中（Unity 6 已可用，但复杂动效/第三方适配仍有坑） |
| 美术工作流 | 成熟（PS/Sprite/九宫格） | 偏 Web（USS/UXML），独立开发者学习成本高 |
| 动效 | PrimeTween/DOTween 直接操作 | 需自写过渡 |
| 热更友好 | 预制体 + 图集，YooAsset 直接加载 | 需额外处理资源引用 |
| 结论 | **选 UGUI** | 未来可迁移 |

**UIManager：**

```csharp
public sealed class UIManager : IAsyncDisposable
{
    private readonly Stack<UIView> _stack = new();
    private readonly Dictionary<Type, UIView> _cache = new();

    public async UniTask<T> OpenAsync<T>(object args = null) where T : UIView
    {
        var view = await GetOrCreateAsync<T>();
        view.Bind(args);                 // ViewModel 注入
        await view.ShowAsync();          // 动画
        _stack.Push(view);
        return view;
    }

    public async UniTask CloseAsync(UIView view)
    {
        await view.HideAsync();
        _stack.Pop();
        if (view.CachePolicy == CachePolicy.Destroy) { _cache.Remove(view.GetType()); await view.DisposeAsync(); }
    }
}
```

**MVVM 绑定（轻量实现，不引入框架）：**

```csharp
public abstract class ViewModel : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();
    protected void Subscribe<T>(IEventBus bus, Action<T> handler) => _subscriptions.Add(bus.Subscribe(handler));
    public virtual void Dispose() { foreach (var s in _subscriptions) s.Dispose(); }
}

public sealed class HudViewModel : ViewModel
{
    public ReactiveProperty<int> EchoCount { get; } = new(0);
    public ReactiveProperty<float> AlertLevel { get; } = new(0f);
    public ReactiveProperty<bool> IsRecording { get; } = new(false);
}
```

**HUD 设计要点：**

- 残影槽位：3 个圆环，显示"存活 / 录制中 / 消散中"三种状态。
- 警戒指示：屏幕边缘的红色渐变 + 守卫头顶弧形条（双通道，不依赖单一颜色）。
- 交互提示：世界空间图标 + 距离淡出，不用文字也能懂。
- 录制指示：底部时间条，显示本轮已录制时长（上限 30 秒）。

### 4.4.3 技术难点与解决方案

| # | 难点 | 解决方案 |
|---|---|---|
| 1 | UI 与逻辑耦合 | ViewModel 只依赖 Application 接口；View 不引用 Domain 类型 |
| 2 | 异步打开导致顺序错乱 | 打开请求入队，同一时刻只处理一个；用 `AsyncLock` 或串行队列 |
| 3 | 手柄导航 | Input System 的 `UI Input Module` + 显式设置 `Navigation`；每次打开面板时聚焦第一个可交互元素 |
| 4 | 多分辨率 | `CanvasScaler` 的 `Scale With Screen Size`（1920×1080 基准）+ `SafeArea` 组件 |
| 5 | 订阅泄漏 | 所有 ViewModel 在 `Dispose` 时释放订阅；Debug 模式统计事件订阅数 |
| 6 | UI 动画与逻辑冲突 | 动画只操作表现层（CanvasGroup/Transform），不修改逻辑状态；关闭时先禁用交互再播动画 |

### 4.4.4 能力提升点

- **UI 架构能力**：MVVM、栈式导航、异步生命周期——面试常问"你的 UI 框架怎么设计的"。
- **输入设备适配**：键鼠 + 手柄 + UI 导航，是主机/PC 方向的加分项。
- **表现与逻辑分离**：UI 不改数据，只反映数据。

## 4.5 渲染与特效模块

### 4.5.1 功能边界

- URP Asset 与 Quality 分级配置。
- 自定义 Renderer Feature（时间回溯后处理、警戒屏幕效果）。
- 自定义 Shader（残影、时间能量、扫描光）。
- 光照方案（烘焙 GI + 实时局部光 + Light Probe）。
- 后处理 Volume（Bloom、Color Grading、Vignette、自定义）。
- VFX Graph / Particle System 特效。
- 性能预算与降级策略。

### 4.5.2 三个核心技术效果

#### 效果 1：时间回溯全屏后处理（自定义 Renderer Feature）

**表现：** 按下 R 的 0.6 秒内，画面出现径向模糊、色差、色调分离、扫描线，并伴随音频倒放。

**实现路径：**

```
ScriptableRendererFeature
  └── RewindPass : ScriptableRenderPass
        ├── 输入：CameraColorTarget
        ├── 材质：Hidden/ECHO/Rewind（Shader Graph 或手写 HLSL）
        ├── 参数：_Intensity（0→1→0）、_Center（屏幕中心）、_Time
        └── 输出：Blit 回 CameraColorTarget
```

**Shader 关键算法：**

```hlsl
// 径向模糊：沿屏幕中心方向多次采样
float2 dir = uv - _Center;
float3 blur = 0;
for (int i = 0; i < 8; i++)
{
    float t = (float)i / 8.0;
    blur += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - dir * t * _Intensity * 0.08).rgb;
}
blur /= 8;

// 色差：R/G/B 三通道沿径向偏移
float2 offset = dir * _Intensity * 0.004;
float r = SAMPLE(_MainTex, uv + offset).r;
float g = SAMPLE(_MainTex, uv).g;
float b = SAMPLE(_MainTex, uv - offset).b;

// 色调分离 + 扫描线
float3 col = float3(r, g, b) * _Tint;
col = floor(col * 12) / 12;                       // 12 级色阶
col *= 1 - 0.15 * step(0.5, frac(uv.y * 120 + _Time * 4)); // 扫描线
```

**技术深度点：**

- RenderGraph API（Unity 6 的 `RecordRenderGraph`）与旧版 `Execute` 的区别。
- `RTHandle` 的生命周期与临时 RT 的申请/释放。
- 后处理的性能预算：全屏 Pass 在 1080p 下的 bandwidth 成本。

#### 效果 2：残影 Shader（半透明 + 边缘光 + 溶解）

**需求：** 一眼看出"这是残影"，同时不能遮挡本体视线。

**实现（Shader Graph + 自定义 HLSL 节点）：**

| 特性 | 实现 |
|---|---|
| 半透明 | `Surface Type: Transparent`，`Blend: SrcAlpha OneMinusSrcAlpha`，alpha 0.35 |
| 边缘光 | `Fresnel = pow(1 - saturate(dot(N, V)), 3)`，乘以青色 |
| 深度偏移 | `Offset -1, -1` 或 `ZWrite Off` + 按距离排序，避免与本体 z-fighting |
| 溶解 | `Dither = frac(sin(dot(screenPos.xy, float2(12.9898,78.233))) * 43758.5453)`，与 `_Dissolve` 比较 |
| 脉冲 | `sin(_Time.y * 3) * 0.05` 加到 alpha，模拟"不稳定" |
| 年龄编码 | `_Age` 参数从 0→1，颜色从青 `#3FE0FF` 渐变到紫 `#9B5CFF` |
| 遮挡 | 被墙挡住时用 `Depth Texture` 做软边缘（可选，P1） |

**技术深度点：**

- 透明物体的排序问题（`Queue = Transparent`、`SortingFudge`）。
- 为何不能写深度（会挡住后面的透明物体），以及如何用 `Stencil` 做局部处理。
- Shader Graph 与手写 HLSL 混用时的 `Custom Function` 节点。

#### 效果 3：守卫视锥与警戒反馈

**表现：** 守卫前方有一个半透明的锥形光，被照射时屏幕边缘变红。

**实现路径：**

- 视锥几何：`Mesh` 程序化生成（扇形 + 轻微抖动），材质为加法混合 + 边缘衰减。
- 遮挡：在 Shader 中用 `Depth Texture` 采样，被墙挡住的部分淡出（软粒子思路）。
- 警戒后处理：全屏 `Vignette` + 红色 tint，强度由 `AlertLevel` 驱动（Volume 参数插值）。
- 扫描光：一个旋转的 `Spot Light` + 体积光粒子（低成本方案）。

**技术深度点：**

- 如何用 `Depth Texture` 做"被遮挡的视锥"（而不是穿墙）。
- Volume 参数的运行时插值与性能（避免每帧 `volume.profile` 修改）。

### 4.5.3 光照方案

| 光源类型 | 方案 | 理由 |
|---|---|---|
| 环境光 | **烘焙 GI（Lightmap）** | 场景静态、相机固定，烘焙质量最高、运行时零成本 |
| 主光 | 1 个方向光（实时，仅阴影） | 提供动态物体的阴影 |
| 局部光 | 少量实时点光/聚光（≤ 8 个） | 机关指示灯、时间能量、守卫扫描光 |
| 动态物体 | **Light Probe + Reflection Probe** | 让移动的角色融入烘焙环境 |
| 雾 | URP 内置雾 / 自定义高度雾 | 增强空间纵深与氛围 |

**为什么不做全实时？** 固定相机 + 静态场景 = 烘焙的最优场景。全实时会在 1050Ti 上直接掉帧，而且没有视觉收益。这是"用场景约束换性能"的典型决策。

### 4.5.4 技术难点与解决方案

| # | 难点 | 解决方案 |
|---|---|---|
| 1 | 透明残影的排序与穿墙 | `ZWrite Off` + 按距离排序 + 深度纹理软边缘；数量 ≤ 3，排序成本可忽略 |
| 2 | 后处理在低端机的开销 | 提供"低"画质档：关闭径向模糊与色差，只保留色调；分辨率缩放 0.85 |
| 3 | RenderGraph API 变更 | 锁定 URP 版本；封装 `BlitPass` 基类；升级前先跑渲染回归 |
| 4 | 烘焙光照与动态残影不匹配 | 残影用 `Light Probe` 采样，且材质以自发光为主（减少对环境的依赖） |
| 5 | Shader 变体爆炸 | 用 `Shader Variant Collection` + 构建时剥离；自定义 Shader 用 `multi_compile` 严格控制关键字 |
| 6 | 视锥穿墙 | 深度纹理采样 + 距离衰减；低成本方案是"射线采样 8 个点"近似 |

### 4.5.5 能力提升点

- **URP 渲染管线**：Renderer Feature、RenderGraph、RTHandle、后处理链。
- **Shader 编写**：从 Shader Graph 到 HLSL，理解透明、深度、混合、变体。
- **光照工程**：烘焙 vs 实时、Light Probe、阴影级联、性能预算。
- **性能与画面的取舍**：知道什么时候该关效果，而不是"全开再说"。

---

# 5. 工作量与里程碑规划

## 5.1 总周期评估

本项目按**人时**估算，不按自然周——因为你的时间不固定，按周排期只会制造焦虑。所有排期都以"可验证的交付物"为门禁，而不是日历。

| 项目 | 数值 |
|---|---|
| 净开发工时 | **430 人时** |
| 缓冲（20%，用于返工、学习、踩坑） | 86 人时 |
| 总预算 | **516 人时** |
| 每周 10h 节奏 | 约 43–52 周（10–12 个月） |
| 每周 15h 节奏 | 约 29–34 周（7–8 个月） |
| 每周 25h 节奏 | 约 17–21 周（4–5 个月） |
| 每周 35h 节奏（假期冲刺） | 约 12–15 周（3–4 个月） |

**建议节奏：** 学期内每周 12–15h（工作日晚上 2h × 3 + 周末 6h），假期每周 30h+。这样总周期约 6–7 个月，符合"离毕业还有一段时间"的现实。

**关键原则：**

1. **每个里程碑结束必须有可运行的构建**，哪怕是灰盒。不能出现"三个月没东西可看"的阶段。
2. **每周至少一次"可玩构建"**（本地打包或编辑器录制 30 秒视频），用于自我复盘与 devlog。
3. **M0 必须先做技术风险最高的部分**（确定性回放、HybridCLR 真机跑通），而不是先做美术。

## 5.2 分阶段里程碑

| 里程碑 | 名称 | 工时 | 核心交付物（可验证） | 验收标准（门禁） |
|---|---|---|---|---|
| **M0** | 技术预研 Spike | 40h | ① 一个灰盒房间，玩家可移动、回溯、生成 1 个残影并踩板开门；② HybridCLR + YooAsset 在打包版跑通；③ 确定性测试用例 1000 次无偏差 | 打包版能玩通"残影踩板"；`dotnet test` 确定性用例全绿；冷启动 < 5s |
| **M1** | 核心原型（灰盒） | 65h | L0 + L1 两关灰盒；守卫 AI（巡逻/视觉/追击）；机关（板/门/拉杆）；检查点快照；失败重试 | 不看代码，陌生人能在 8 分钟内玩通 L0+L1；无阻塞性 bug |
| **M2** | 玩法系统完整 | 75h | 残影上限/FIFO/消散；听觉感知；时间锁门；升降台；3 个残影协同；关卡流程状态机；输入重绑定 | 3 个残影同屏时逻辑帧 < 2ms；L2 关卡可通关；所有机制有单元测试 |
| **M3** | 工程架构落地 | 70h | 程序集划分完成；YooAsset 分包 + 增量更新；HybridCLR 三层热更；存档系统（含迁移/校验）；配置表管线；Logger/EventBus/DI | 改一行热更代码 → 构建 → 只下载 < 2MB → 生效；存档跨版本迁移测试通过；损坏存档可自动恢复 |
| **M4** | UI 与渲染表现 | 50h | UGUI 框架 + MVVM；HUD/主菜单/暂停/设置/结算；3 个核心渲染效果；烘焙光照；音频系统 | 手柄可完整操作全部 UI；1080p 60fps；时间回溯效果可开关 |
| **M5** | 关卡内容与叙事 | 55h | L3 + L4 + 挑战关；环境叙事；教学引导；辅助功能；本地化（中/英） | 全流程 18–25 分钟可通关；至少 3 名外部试玩者完成，无卡关超过 5 分钟 |
| **M6** | 性能优化专项 | 35h | 性能基线报告；CPU/GPU/内存/加载四项优化；性能监控 Overlay；自动化性能测试 | 达到 6.7 节全部量化目标；连续 10 分钟运行无 GC 峰值、无内存增长 |
| **M7** | 打磨与发布 | 40h | 音效/音乐终混；过场与演出；Bug 修复；构建签名；演示视频；技术文档；GitHub 整理 | 可执行包双击即玩；视频 3 分钟；README 含架构图与性能数据 |

**里程碑依赖关系：**

```
M0 ──► M1 ──► M2 ──────────► M5 ──► M6 ──► M7
        │      │              ▲
        └──────┴──► M3 ───────┤
                      │       │
                      └──► M4 ┘
```

- M3 可以在 M2 后期并行开始（程序集划分不依赖具体玩法）。
- M4 的 UI 框架可以在 M2 期间用占位 UI 先搭。
- **M5 必须等 M2 完成**，否则关卡会反复重做。

## 5.3 各模块开发时长评估

| 模块 | 子项 | 工时 | 备注 |
|---|---|---|---|
| 核心玩法 | 固定 tick + 玩家运动学 | 20h | 含碰撞滑动、手感调优 |
| | 输入录制/回放/时间轴 | 25h | 本项目的技术核心 |
| | 残影系统（生成/交互/消散） | 20h | 含表现层对接 |
| | 守卫 AI（状态机/视觉/听觉） | 30h | 含调试工具 |
| | 机关系统 + 聚合器 | 15h | 数据驱动 |
| | 检查点与关卡流程 | 15h | |
| 资源/热更 | YooAsset 接入 + 分包 | 25h | 含构建脚本 |
| | 下载/版本/回滚 | 15h | |
| | HybridCLR 接入 + AOT 泛型 | 25h | 踩坑预留 |
| | 资源句柄与生命周期 | 10h | |
| 数据 | 存档（序列化/校验/迁移） | 20h | |
| | 配置表管线 + 导出工具 | 15h | |
| | 设置系统 | 5h | |
| UI | 框架 + MVVM | 20h | |
| | HUD/菜单/设置/结算 | 25h | |
| | 手柄导航/适配 | 8h | |
| 渲染 | URP 配置 + 光照烘焙 | 12h | |
| | 时间回溯后处理 | 15h | |
| | 残影 Shader | 10h | |
| | 视锥/警戒效果 | 10h | |
| | 特效与氛围 | 13h | |
| 内容 | 5 个关卡设计+灰盒+打磨 | 55h | 每关约 10–12h |
| | 音频（音效/音乐/混音） | 12h | 可用素材库 |
| | 叙事包装/教学引导 | 8h | |
| 工程 | 编辑器工具（3 个） | 18h | 关卡校验/配置导出/时间轴调试 |
| | 测试（单测+集成） | 15h | |
| | 性能优化 | 35h | |
| | 发布/文档/视频 | 25h | |
| **合计** | | **~430h** | 不含 20% 缓冲 |

## 5.4 核心必做 / 可选扩展

### P0 · 必须完成（缺一不可，构成作品集底线）

| 功能 | 理由 |
|---|---|
| 回溯 + 残影 + 3 个槽位 | 核心机制，没有它就不是这个游戏 |
| 守卫视觉感知 + 状态机 | 潜行的骨架 |
| 压力板 / 拉杆 / 激光门 / 时间锁 | 解谜的工具箱 |
| 检查点快照与失败重试 | 体验流畅度的底线 |
| 教学关 + L1 + L2（10–15 分钟） | 作品集最小可体验闭环 |
| 存档（进度 + 设置） | "完成品"的标志 |
| UGUI 主菜单 / 暂停 / 设置 / 结算 | 同上 |
| YooAsset 资源加载 + 分包 | 工程能力证明 |
| HybridCLR 热更跑通 | 工程能力证明 |
| 时间回溯后处理 + 残影 Shader | 视觉辨识度 |
| 性能基线 + Overlay | 优化能力的证据 |
| 可执行构建 + README | 交付物 |

### P1 · 应该完成（决定"良好"与"优秀"的差距）

| 功能 | 价值 | 工时 |
|---|---|---|
| L3 + L4 关卡 | 完整体验与难度曲线 | 25h |
| 听觉感知系统 | 机制深度 + AI 展示 | 10h |
| 编辑器工具 3 件套 | 工程化能力 | 18h |
| 3 个残影同屏优化 | 性能专项实证 | 8h |
| 手柄支持 + 辅助功能 | 主机/PC 方向加分 | 8h |
| 中英本地化 | 展示本地化架构 | 6h |
| 自动化性能测试 | 数据化能力 | 8h |
| 演示视频 + 技术文档 | 作品集传播 | 12h |

### P2 · 可选扩展（时间充裕再做，不影响主线）

| 功能 | 价值 | 风险 |
|---|---|---|
| 时间能量资源系统 | 增加策略深度 | 可能破坏解谜节奏，需试玩验证 |
| 残影交换（Echo Swap） | 机制再进化 | 实现复杂度高，容易失控 |
| 挑战模式（限时/限残影） | 重玩价值 | 需要额外关卡设计 |
| 拍照模式 | 展示渲染 | 与核心无关 |
| Steam 成就/云存档 | 平台经验 | 需要 Steamworks 接入 |
| 关卡编辑器 | 工具能力 | 工作量巨大，建议只做内部版 |
| 移动端适配 | 跨平台经验 | 触屏操作与性能差异大，明确不做 |

**砍功能顺序（当进度落后时）：** P2 全砍 → P1 中本地化/挑战模式 → P1 中 L4 降级为"简化版" → P0 中第二套渲染效果 → **绝不动核心三件套（回溯/残影/守卫）**。

## 5.5 推进机制与节奏建议

| 机制 | 做法 | 目的 |
|---|---|---|
| 每周可玩构建 | 每周日打包一次，录制 30 秒 | 防止"三个月没东西" |
| Devlog | 每两周写一篇（图文），发 B 站/知乎/小红书 | 外部监督 + 作品集素材 |
| 任务粒度 | 每个任务 ≤ 4 小时，可独立验证 | 避免"一周一个大任务"的失控 |
| 每日站会（自我） | 3 分钟：昨天做了什么 / 今天做什么 / 有什么阻塞 | 保持方向 |
| 里程碑复盘 | 每个 M 结束写 1 页：达成/未达成/原因/下阶段调整 | 工程复盘能力 |
| 技术预研先行 | 任何"没做过"的技术，先做 2 小时 spike | 避免在主线里踩坑 |
| 代码提交规范 | `feat/fix/refactor/docs` + 中文描述 | 提交历史也是作品集 |

## 5.6 关键路径与并行建议

**关键路径（不能压缩）：**

```
确定性回放验证 (M0) → 残影系统 (M1) → 守卫 AI (M2) → 关卡设计 (M5) → 性能优化 (M6) → 发布 (M7)
```

**可并行（利用碎片时间）：**

- 美术资源收集/AI 生成贴图 → 贯穿全程，每天 30 分钟。
- 音频素材筛选 → M1 开始，随关卡推进。
- 文档/Devlog → 每周固定 1 小时。
- 编辑器工具 → 在"被某个重复劳动烦到"时顺手做，而不是专门安排。

---

# 6. 性能优化专项方案

> 性能优化不是最后"跑个 Profiler 调一调"，而是**从第一天就建立基线、每次提交都可回归**的工程习惯。本章的目标是让你在面试中能拿出**数字**，而不是"我做过优化"。

## 6.1 性能基线与目标硬件

| 项目 | 定义 |
|---|---|
| 目标平台 | Windows x64 |
| 最低配置 | i5-8400 / GTX 1050 Ti 4GB / 16GB RAM / SSD |
| 推荐配置 | i7-10700 / RTX 3060 / 16GB RAM |
| 分辨率 | 1920×1080，窗口/全屏 |
| 基准场景 | L4（3 守卫 + 3 残影 + 全部特效） |
| 测量工具 | Unity Profiler（Development Build）+ 自研 Overlay + Frame Debugger |

## 6.2 CPU 优化

### 6.2.1 代码架构层面

| 手段 | 具体做法 | 预期收益 |
|---|---|---|
| 逻辑与渲染解耦 | 固定 60Hz tick + 渲染插值；逻辑不读 `Time.deltaTime` | 高刷屏不增加逻辑成本；低端机可降逻辑帧 |
| 纯 C# 领域层 | 无 `MonoBehaviour`、无 `GetComponent`、无 `Find` | 逻辑帧 < 2ms，且可单测 |
| 数据与表现分离 | 残影只回放位置，不跑模拟 | 3 残影成本 < 0.3ms |
| 事件驱动而非轮询 | 机关/感知用事件，避免每帧 `FindObjectsOfType` | 消除 O(n²) 隐患 |
| 分帧与错峰 | 视觉检测 10Hz、不同守卫错开 tick、AI 决策 5Hz | 峰值摊平 |
| 避免热路径虚调用 | 用 `sealed`、`struct`、`readonly`；避免接口装箱 | 减少 GC 与调用开销 |

### 6.2.2 GC 控制（目标：稳态 0 B/frame）

| 来源 | 问题 | 解决方案 |
|---|---|---|
| 字符串拼接 | `Debug.Log("hp:" + hp)` 每帧分配 | 用 `ZString`/自研 `Log` 的结构化参数；Release 关闭日志 |
| `foreach` 装箱 | 对非泛型集合或旧版 Unity 的 `foreach` | 用 `for` + `List<T>`；Unity 现代版本已优化，但仍避免热路径 |
| 闭包 | `list.ForEach(x => ...)`、事件匿名函数 | 缓存委托到字段；用静态方法 + 参数 |
| LINQ | `Where/Select/OrderBy` 每次分配迭代器 | 热路径禁止 LINQ；用预分配数组 + 手写循环 |
| `GetComponent` | 每帧调用 | Awake 缓存；用 `TryGetComponent` |
| `Camera.main` | 内部 `FindGameObjectWithTag` | 缓存引用 |
| 装箱 | `object` 参数、枚举作为字典键（旧版） | 泛型化；用 `int` 键 |
| 数组/列表扩容 | `new List<T>()` 无容量 | 预设容量；复用缓冲区 |
| UnityEvent | 反射调用 + 分配 | 用 C# event 或自研 EventBus |
| 协程 | `yield return new WaitForSeconds` 每次分配 | 用 UniTask 或缓存 `WaitForSeconds` |
| 物理查询 | `Physics.OverlapSphere` 返回新数组 | 用 `OverlapSphereNonAlloc` |

**验证方法：** Profiler 的 `GC Alloc` 列在稳态下必须为 0；用 `PerformanceOverlay` 显示 `GC.Alloc/frame` 实时值。

### 6.2.3 对象池

| 池化对象 | 数量级 | 说明 |
|---|---|---|
| 残影表现体 | ≤ 3 | 复用预制体，避免 Instantiate 峰值 |
| 特效（粒子/Decal） | 20–50 | 时间回溯、消散、脚步 |
| UI 列表项 | 20–50 | 关卡选择、设置项 |
| 音频源 | 16–24 | 统一 AudioSource 池，避免动态添加组件 |
| 临时列表/缓冲区 | 10+ | 用 `ArrayPool<T>` 或自研 |

**池的规范：** 所有池化对象实现 `IPoolable.OnSpawn()/OnDespawn()`，在 `OnDespawn` 中重置所有状态（位置、旋转、粒子、动画、事件订阅）。**池化对象的状态泄漏是最常见的隐蔽 bug。**

### 6.2.4 逻辑帧率与物理

- 逻辑固定 60Hz；低画质档可降到 30Hz（需重新验证手感与 AI 判定）。
- `Physics.simulationMode = Script`，手动 `Simulate`，避免引擎在渲染帧里插入物理步进。
- 玩家与守卫**不使用 Rigidbody**；机关中的可推动箱子才用，且限制数量 ≤ 3。
- 碰撞矩阵：只配置必要的层（Player / Echo / Guard / Environment / Trigger / Interactable），减少无意义检测。
- 触发器用 `OnTriggerEnter/Exit`（离散），不用每帧 `Overlap`。

### 6.2.5 可选进阶（P2）

- **Burst + Jobs** 批量做视锥检测（> 10 个守卫才有意义，本项目不需要）。
- **SIMD** 批量计算残影轨迹（无必要）。
- 明确写"不做"的理由，比盲目上技术更有说服力。

## 6.3 GPU 优化

### 6.3.1 DrawCall 与合批

| 手段 | 具体做法 | 目标 |
|---|---|---|
| SRP Batcher | 统一使用少量 Shader 变体；材质属性放在 `cbuffer` | 让大部分不透明物体走 SRP Batcher |
| GPU Instancing | 重复道具（管道、灯、栏杆）开启 Instancing | 相同材质的多实例合为 1 次 |
| 静态合批 | 场景装饰（墙体、地板）标记 Static | 减少 DrawCall |
| 图集 | UI 1–2 张 2048 图集；道具贴图 1024 图集 | 减少材质切换 |
| 材质数量 | 全场景 ≤ 12 个材质球 | 控制 SetPass |
| Shader 变体 | 自定义 Shader 只用 2–3 个 keyword；构建时剥离 | 减少加载与内存 |

**验收：** Frame Debugger 中 DrawCall < 120，SetPass < 70（1080p，L4 场景）。

### 6.3.2 LOD 与剔除

| 手段 | 做法 |
|---|---|
| LOD Group | 大型道具 2 级（100% / 40% 面数），距离 15m |
| 视锥剔除 | 默认开启；固定相机下可用手工剔除组（按房间分组） |
| 遮挡剔除 | 烘焙 Occlusion Culling；室内场景收益显著 |
| 距离剔除 | 小物件（粒子、装饰）超过 25m 关闭 |
| 相机 Culling Mask | 分层渲染，UI/特效单独相机（谨慎使用，会增加 DrawCall） |

### 6.3.3 阴影与光照

| 项 | 配置 | 理由 |
|---|---|---|
| 方向光阴影 | 1 个，2 级级联，距离 30m，分辨率 1024 | 室内场景阴影范围小 |
| 局部光阴影 | 关闭（除主光外） | 实时点光阴影成本高、收益低 |
| 烘焙 GI | 全部静态物体 | 运行时零成本 |
| Light Probe | 动态角色/残影 | 融入烘焙环境 |
| 反射探针 | 1 个全局 + 2 个局部 | 金属材质需要 |
| 雾 | 线性雾或高度雾 | 增强纵深，成本低 |

### 6.3.4 后处理与 Shader

| 效果 | 成本控制 |
|---|---|
| Bloom | 降分辨率（1/4）+ 2 次迭代 |
| 时间回溯径向模糊 | 8 次采样，仅触发时启用，1080p 下单 Pass < 0.5ms |
| 色差/色调分离 | 合并到同一个 Pass |
| SSAO | **不做**（烘焙已足够，实时 AO 收益低） |
| 抗锯齿 | SMAA（中档）/ FXAA（低档）；TAA 会与残影半透明冲突 |
| Volume 参数 | 用 `VolumeProfile` 预设 + 权重插值，避免每帧改材质 |

### 6.3.5 Overdraw 控制

- 残影半透明面积控制在屏幕 15% 以内；残影不接收阴影、不投射阴影。
- 粒子使用 `Soft Particles` + 低分辨率渲染（URP 的 Particle 降分辨率选项）。
- 视锥体积光用单个半透明 Mesh，不用后处理全屏 raymarch。

## 6.4 内存优化

| 类别 | 手段 | 目标 |
|---|---|---|
| 纹理 | ASTC 6x6（PC 用 BC7）；UI 关闭 Mipmap；世界贴图开启 Mipmap + Streaming | 纹理总量 < 350MB |
| 模型 | 顶点数控制（道具 < 2k，角色 < 8k）；合并同材质 | 模型总量 < 60MB |
| 音频 | 音乐 Vorbis 流式（`Load Type: Streaming`）；短音效 PCM/ADPCM | 音频 < 40MB 常驻 |
| 动画 | 无骨骼动画（程序化），仅少量道具动画 | 几乎为 0 |
| 托管堆 | 稳态 < 50MB；避免大数组长期驻留 | GC 峰值可控 |
| 资源句柄 | 作用域管理 + 引用计数 + 定时 `UnloadUnusedAssets` | 场景切换后回落到基线 ±10% |
| 残影数据 | 43KB/残影 × 3 = 129KB | 可忽略 |
| 快照 | 1–2KB × 1 | 可忽略 |

**泄漏排查流程：**

1. 进入关卡 → 记录 Memory Profiler 快照 A。
2. 玩 5 分钟（反复回溯、开关 UI、切场景）→ 快照 B。
3. 对比 A/B：托管堆、Texture、Mesh、AudioClip、GameObject 数量。
4. 若某类持续增长 → 用"引用链"定位持有者。
5. 常见原因：事件未退订、静态字典缓存、协程未停止、`AssetHandle` 未释放、对象池未清理。

**自研工具：** `AssetLeakDetector` 在 Debug 模式下每 30 秒打印未释放句柄的 `location + 调用栈`。

## 6.5 加载优化

| 场景 | 手段 | 目标 |
|---|---|---|
| 冷启动 | 首包最小化（Main 包 < 40MB）；热更检查异步化；Logo 期间预加载 | < 5s（含版本检查） |
| 关卡切换 | `LoadSceneAsync(Additive)` + 异步预加载依赖 + 淡入淡出 | < 3s（SSD） |
| 资源加载 | 按标签批量预加载；并行度 8；进度按文件数平滑 | 无明显卡顿 |
| Shader 预热 | `ShaderVariantCollection.WarmUp()` 在加载界面执行 | 消除首次出现的编译卡顿 |
| 对象池预热 | 特效/UI 常用对象在加载界面预实例化 | 首次触发不掉帧 |
| 纹理流式 | 大贴图开启 `Mipmap Streaming`，按相机距离加载 | 显存峰值降低 30% |
| 内存回收 | 场景卸载后 `UnloadUnusedAssets` + `GC.Collect`（仅加载界面） | 峰值可控 |

**加载界面设计：** 真实进度 + 最小展示 0.5 秒（避免闪烁）+ 可跳过（若已加载完）+ 加载中的玩法提示（残影机制小贴士）。

## 6.6 性能监控与调试工具链

| 工具 | 用途 | 使用频率 |
|---|---|---|
| Unity Profiler | CPU/GPU/内存/渲染总览 | 每次提交前 |
| Profiler (Deep Profile) | 定位具体函数耗时 | 遇到瓶颈时 |
| Memory Profiler | 快照对比、引用链 | 每周 |
| Frame Debugger | DrawCall / SetPass / 批次分析 | 美术资源合入后 |
| Rendering Debugger | 光照/材质/Overdraw 可视化 | 渲染调试 |
| 自研 `PerformanceOverlay` | F1 显示 FPS/CPU/GC/DrawCall/内存/残影数 | 全程开启（Development） |
| 自研 `PerfTestRunner` | 自动跑固定路线 60 秒，输出 CSV | M6 与每次大改后 |
| Unity Test Framework | 确定性/存档/配置的单测 | CI |
| `AssetLeakDetector` | 未释放句柄告警 | Debug 构建常开 |

**PerformanceOverlay 显示项：**

```
FPS: 62  (1% low: 54)
CPU: 6.8ms  [Logic 1.4 | Render 3.2 | Script 1.1 | GC 0.0]
GC Alloc: 0 B/frame   Managed: 42.3 MB
DrawCall: 98   SetPass: 61   Tris: 420k
VRAM: 612 MB   Texture: 348 MB   Mesh: 52 MB
Echoes: 3/3   Guards: 3   Active VFX: 12
```

## 6.7 量化优化目标

| 指标 | 目标值 | 测量条件 |
|---|---|---|
| 平均帧率 | **60 fps** | 1080p，L4 场景，3 残影 + 3 守卫 |
| 1% low 帧率 | **≥ 50 fps** | 同上，含回溯演出 |
| CPU 主线程 | **≤ 8 ms** | 同上 |
| 逻辑帧耗时 | **≤ 2 ms** | 60Hz tick，3 残影 |
| 单残影增量 | **≤ 0.1 ms** | 相对无残影 |
| GC Alloc（稳态） | **0 B/frame** | 游戏中无 UI 打开 |
| 托管堆 | **≤ 50 MB** | 稳态 |
| DrawCall | **≤ 120** | L4 场景 |
| SetPass Call | **≤ 70** | 同上 |
| 三角形数 | **≤ 500k** | 同上 |
| 显存 | **≤ 800 MB** | 同上 |
| 进程内存峰值 | **≤ 1.2 GB** | 含 10 分钟游玩 |
| 场景加载 | **≤ 3 s** | SSD，L1→L2 |
| 冷启动 | **≤ 5 s** | 含热更检查（无更新） |
| 包体 | **≤ 250 MB** | Windows x64 压缩后 |
| 连续运行 30 分钟 | 内存增长 **≤ 5%** | 无泄漏 |

## 6.8 优化流程（先测量，后优化）

```
① 建立基线（M1 结束）
   └─ 固定场景 + 固定路线 + 自动跑 60s → 记录 CSV
② 找瓶颈（Profiler / Frame Debugger）
   └─ 按"CPU 逻辑 > CPU 渲染提交 > GPU > 内存 > 加载"排序
③ 单点优化（一次只改一个变量）
   └─ 记录改动前后的数字
④ 回归验证（同一路线重跑）
   └─ 确认没有把瓶颈转移到别处
⑤ 写进文档（优化日志）
   └─ "问题 → 手段 → 前后数据 → 结论"
```

**面试时的价值：** 你不说"我做了对象池"，而是说"L4 场景 DrawCall 从 210 降到 98，原因是把 37 个重复管道合并为 Instancing，并用 SRP Batcher 统一了材质；CPU 逻辑帧从 2.8ms 降到 1.4ms，主要是把视觉检测从每帧改成 10Hz 分帧"。**数字 + 因果**才是优化能力的证明。

---

# 7. 作品集亮点设计

## 7.1 Demo 整体展示的核心亮点

| # | 亮点 | 一句话展示 | 支撑证据 |
|---|---|---|---|
| 1 | 非模板的核心机制 | "你可以和上一秒的自己组队" | 3 分钟视频的前 20 秒 |
| 2 | 技术纵深 | "残影不是录像，是确定性轨迹回放 + 交互代理" | 技术文档 + 代码片段 |
| 3 | 工程完整度 | "有热更、有存档、有性能报告" | 构建产物 + 报告 |
| 4 | 完成度 | "有开始、有结束、有菜单、有设置" | 完整流程视频 |
| 5 | 视觉辨识度 | "时间撕裂"后处理 + 半透明残影 | 封面图 / GIF |
| 6 | 工程判断力 | "我知道什么时候不该用热更" | 面试问答 |

## 7.2 每个模块对应体现的求职能力点

| 模块 | 面试官看到的能力 | 具体证据 |
|---|---|---|
| 核心玩法 | 玩法系统设计、状态管理、确定性模拟 | 时间轴数据结构、残影规则、单测 |
| 守卫 AI | 状态机、感知系统、行为调试 | 状态迁移图、视锥检测代码、调试可视化 |
| 资源/热更 | AssetBundle、依赖管理、发布管线 | 分包表、增量更新视频、回滚流程 |
| HybridCLR | AOT/热更边界、IL2CPP、泛型补充 | 程序集依赖图、AOTGenericReferences、踩坑记录 |
| 数据持久化 | 序列化、版本迁移、数据完整性 | 存档格式图、迁移链、损坏恢复测试 |
| UI | 框架设计、MVVM、多设备适配 | UIManager 代码、手柄演示 |
| 渲染 | URP、RenderGraph、Shader、光照 | 后处理代码、残影 Shader、性能预算 |
| 性能优化 | 量化分析、系统性优化 | CSV 报告、前后对比、Overlay |
| 工程规范 | 程序集划分、DI、日志、测试 | 目录结构、asmdef 图、CI |
| 编辑器工具 | 工具链意识、生产力 | 三个工具的截图与说明 |

## 7.3 面试中可以深入讲解的技术深度点

1. **为什么用固定 tick，而不是 `Update` + `deltaTime`？**
   —— 确定性、可回放、逻辑与渲染解耦；顺带讲"一帧多 tick"与输入采样。

2. **残影为什么不重跑模拟，而是回放位置？**
   —— 成本、确定性、世界状态冲突；以及"用设计约束（不能推箱子）换可靠性"的判断。

3. **检查点快照为什么只有 1–2KB？**
   —— 不保存残影、不保存可破坏物；快照的边界就是设计的边界。

4. **机关为什么每 tick 全量重算，而不是加减计数？**
   —— 残影消散/回溯/对象禁用会导致计数泄漏；全量重算 O(n) 且 n 很小。

5. **YooAsset 为什么按关卡分包，而不是按资源类型？**
   —— 更新单元 = 业务单元；按类型分会导致"改一张图重下整个包"。

6. **HybridCLR 的 AOT 泛型坑是什么？怎么发现的？**
   —— `List<HotfixStruct>` 在 IL2CPP 下缺方法；`AOTGenericReferences` + `LoadMetadataForAOTAssembly` + 打包版回归。

7. **存档为什么要原子写入 + 备份 + 迁移？**
   —— 断电、版本升级、玩家手改；三级容错流程。

8. **RenderGraph 与旧版 Renderer Feature 的区别？**
   —— `RecordRenderGraph`、RTHandle、Pass 合并、资源生命周期。

9. **透明残影的排序与深度怎么处理？**
   —— `ZWrite Off`、排序、深度纹理软边缘、Stencil 的取舍。

10. **你的性能优化最有价值的一次是什么？**
    —— 用数字回答：问题、手段、前后对比、结论。

11. **如果让你把这个 Demo 扩展成商业项目，你会先做什么？**
    —— 内容管线（关卡编辑器）、存档云同步、更多机制组合、可访问性；以及"哪些现在的设计会阻碍扩展"。

12. **为什么单机项目也要热更？什么时候不该用？**
    —— 工程训练价值 + 成本量化 + 判断边界。**这题最能体现成熟度。**

## 7.4 简历与视频的表达方式

### 简历项目描述（STAR 结构，3–4 行）

> **《ECHO / 残响》— Unity 单机时间回溯潜行解谜（个人项目）**
> - 设计并实现"回溯生成残影、与过去自我协同"的核心机制，基于 60Hz 固定 tick + 确定性轨迹回放，3 残影同屏逻辑开销 < 0.3ms。
> - 搭建 YooAsset + HybridCLR 热更管线：按关卡分包、增量更新、AOT 泛型补充，热更包体 < 8MB，冷启动 < 5s。
> - 实现存档系统（MessagePack + CRC + 原子写入 + 版本迁移）与 UGUI MVVM 框架，支持键鼠/手柄。
> - 性能优化：L4 场景 DrawCall 210→98、逻辑帧 2.8ms→1.4ms、稳态 GC Alloc 0 B/frame。

### 3 分钟演示视频脚本

| 时间 | 内容 | 目的 |
|---|---|---|
| 0:00–0:15 | 黑屏 → "如果你能和上一秒的自己组队？" → 游戏画面 | 钩子 |
| 0:15–0:45 | 教学关：踩板 → 回溯 → 残影踩板 → 通过 | 讲清核心机制 |
| 0:45–1:20 | L1：残影引开守卫，本体绕后 | 展示 AI 与策略 |
| 1:20–1:50 | L2：3 个残影协同，时间差通过激光 | 展示机制深度 |
| 1:50–2:10 | 时间回溯后处理特写 + 残影 Shader 特写 | 展示渲染 |
| 2:10–2:35 | 技术信息图：架构分层、热更流程、性能数据 | 展示工程 |
| 2:35–2:50 | 失败/重试的快节奏剪辑 | 展示手感与完成度 |
| 2:50–3:00 | 标题 + "Made with Unity 6 / URP / YooAsset / HybridCLR" | 关键词 |

### GitHub README 结构

```
1. 一句话介绍 + GIF
2. 核心机制说明（3 张图）
3. 技术栈与架构图
4. 关键技术点（链接到 Docs/）
5. 性能数据（表格）
6. 如何运行（构建/源码）
7. 开发日志（里程碑）
8. 致谢与素材授权
```

---

# 8. 风险与应对方案

## 8.1 风险矩阵

| 风险 | 概率 | 影响 | 等级 |
|---|---|---|---|
| 确定性回放出现难以复现的偏差 | 中 | 高 | 🔴 |
| 范围蔓延（想做的越来越多） | 高 | 高 | 🔴 |
| 关卡设计不好玩 / 玩家卡关 | 中 | 高 | 🔴 |
| 美术资源风格不统一 | 中 | 中 | 🟠 |
| HybridCLR 在打包版/IL2CPP 下踩坑 | 中 | 中 | 🟠 |
| 性能不达标（尤其残影 + 后处理） | 中 | 中 | 🟠 |
| 时间不固定导致长期停摆 | 中 | 高 | 🟠 |
| YooAsset 学习曲线导致进度拖延 | 低 | 中 | 🟡 |
| 存档迁移/损坏 | 低 | 中 | 🟡 |
| 动力下降 / 完美主义 | 中 | 高 | 🟠 |

## 8.2 技术风险与应对

| 风险 | 触发信号 | 应对方案 | 降级方案 |
|---|---|---|---|
| **确定性偏差** | 同一段输入两次回放位置不同；残影偶尔踩不到板 | ① M0 就写 1000 次回放测试；② 位置回放而非模拟重跑；③ 禁用 Rigidbody；④ 固定执行顺序；⑤ 时间轴调试器可视化对比 | 若仍有偏差：改为"残影位置直接录制关键帧 + 贝塞尔插值"，彻底放弃模拟重算（本方案已是此路线） |
| **HybridCLR 打包版失败** | 编辑器正常，打包后 `MissingMethodException` | ① M0 先跑通最小热更 demo；② 每次构建跑冒烟测试；③ `link.xml` + AOT 泛型清单；④ 保留"关闭热更"的构建开关 | 关闭代码热更，只保留 YooAsset 资源热更；文档中如实说明取舍 |
| **YooAsset 分包/依赖问题** | 资源重复、加载失败、包体异常 | ① 先跑官方示例；② 用依赖报告扫描重复；③ 共享资源下沉 `Main`；④ 每周真机验证 | 退回单包（不分包），仍保留版本与增量能力 |
| **性能不达标** | L4 场景 < 50fps | ① M1 就建基线；② 后处理提供低画质档；③ 残影不投影/不接收阴影；④ 降低阴影与粒子 | 关闭时间回溯径向模糊，仅保留色调；逻辑帧降 30Hz |
| **透明排序/穿墙** | 残影在墙后可见、闪烁 | 深度纹理软边缘 + 排序 + 限制同屏残影数 | 残影改为"描边 + 低透明度实体"，不做半透明 |
| **存档损坏** | 玩家反馈进度丢失 | 原子写入 + 备份 + 校验 + 迁移测试 | 提供"导出/导入存档"文本功能 |
| **编辑器工具耗时** | 工具开发超过 20h | 只做"关卡校验器"和"配置导出器"，时间轴调试器用 Gizmos 代替 | 全部用 Gizmos + 自定义 Inspector 替代 |

## 8.3 进度风险与应对

| 风险 | 表现 | 应对 |
|---|---|---|
| **范围蔓延** | "再加一个机制吧"、"要不做成 8 关" | ① 本 Plan 的 P0/P1/P2 是合同，改动需写明理由；② 任何新功能必须替换一个旧功能，不能净增；③ 每周复盘时检查"本周新增了几个 P2" |
| **长期停摆** | 两周没提交 | ① 设定"最小启动动作"（打开工程跑一次构建）；② 用 devlog 制造外部承诺；③ 把大任务拆到 2 小时粒度 |
| **完美主义** | 一个 Shader 调三天 | ① 时间盒：每个视觉任务 ≤ 4h；② "够用即冻结"，记录到"以后再说"清单；③ 先完成再完美 |
| **关卡难产** | 设计不出有趣的谜题 | ① 用"机制组合表"系统性穷举（板×门×守卫×残影数）；② 先做灰盒试玩再包装；③ 参考《The Sexy Brutale》《Braid》的关卡教学法 |
| **美术卡壳** | 找不到合适资源 | ① 锁定"几何体 + 统一材质"风格；② AI 生成贴图只做 PBR 基础色/法线；③ 用 Quixel/Fab 免费资源做点缀 |
| **学习成本超预期** | YooAsset/HybridCLR 卡住 | ① 每天限定 2h 学习，超出就切换到"能推进的任务"；② 找官方示例/最小可跑 demo；③ 记录踩坑笔记，形成文档 |

## 8.4 功能降级方案（按优先级）

### 降级 Level 1：时间不足（剩余 < 150h）

- 关卡：L0 + L1 + L2（保留 10–15 分钟核心体验）。
- 热更：只做 YooAsset 资源热更，关闭 HybridCLR 代码热更（文档说明原因）。
- 渲染：保留时间回溯后处理 + 残影 Shader，砍掉视锥体积光。
- UI：主菜单 + 暂停 + 结算，砍掉关卡选择与本地化。
- 性能：只保证 1080p 60fps，不做自动化报告。

### 降级 Level 2：严重不足（剩余 < 80h）

- 关卡：L0 + L1（8–10 分钟）。
- 只保留 1 个残影（不做 FIFO 与 3 残影协同）。
- 保留存档（进度 + 设置），配置表用 JSON 直读。
- 热更：只保留 YooAsset 的加载能力，不做下载流程。
- 渲染：只保留残影 Shader，后处理用 URP 内置 Volume。
- **仍然保留**：确定性回放、守卫 AI、完整 UI 流程、可执行构建。

### 降级 Level 3：转为 2D（手感/美术无法达标时）

- 保留全部 Domain 代码，只替换 Presentation 层（2D 俯视 + Sprite）。
- 渲染效果改为 2D：Sprite 发光、2D 后处理、Tilemap 关卡。
- 预计额外成本 20–30h（重做美术与相机），但**技术叙事不变**。

### 明确"不做"清单（防止反复动摇）

- ❌ 联机 / 排行榜 / 云存档
- ❌ 移动端
- ❌ 关卡编辑器（只做内部校验工具）
- ❌ 多语言超过中/英
- ❌ 语音配音
- ❌ 程序化关卡生成
- ❌ 战斗系统 / 血量 / 武器

## 8.5 决策规则（遇到纠结时的判断标准）

当你不确定"要不要做某个功能"时，按顺序问四个问题：

1. **它是否直接强化"回溯 + 残影"的核心体验？** 否 → 不做。
2. **它能否在 8 小时内做出可验证的原型？** 否 → 拆小或不做。
3. **它是否能被面试官在 30 秒内理解？** 否 → 降级为文档说明。
4. **去掉它，Demo 是否仍然完整？** 是 → 放到 P2。

**最后一条原则：完成比完美重要，但这个项目的"完成"标准是——一个陌生人能双击打开、玩 10 分钟、看懂你在做什么，并且觉得"这人会做游戏"。**

---

> **文档结束。**
>
> 下一步建议：从 **M0 技术预研**开始，先写一个 300 行的"固定 tick + 输入录制 + 残影回放"最小原型，跑通后再接入 YooAsset / HybridCLR。不要先做美术，也不要先做 UI。
