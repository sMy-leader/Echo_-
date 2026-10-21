# ECHO / 残响 — 项目约定

> 这些约定对所有后续代码生效。代码首先是给人读的，其次才是给编译器读的。

## 1. 代码注释（强约束）

- 新增或修改的核心代码必须有关键注释。
- 注释解释 **为什么这样做、边界条件、单位、执行顺序、不变量**，不要复述代码表面动作。
- 所有 `public` 类型、`public` 方法必须有 XML `<summary>`。
- 复杂 `private` 方法必须有块注释，写清输入、输出和失败条件。
- 涉及算法的地方必须说明思路，例如：
  - 碰撞扫掠为什么用胶囊而不是 Rigidbody；
  - 环形缓冲的 `start` 下标怎么算；
  - 回溯时为什么先创建残影再清空录制；
  - 残影为什么在结束后的下一 tick 才移除。
- 注释使用中文；类型名、方法名、字段名、Unity API 保持英文原文。
- 修改代码时同步更新注释；过期注释比没有注释更糟。
- 代码审查把“缺少关键注释”视为缺陷。

## 2. 架构边界

- `Game.Domain` 是纯 C#，不引用 UnityEngine。
- `Game.Core` 是纯 C# 契约层，`noEngineReferences: true`。
- `Game.Application` 只做用例编排，不引用 UnityEngine。
- AOT 程序集（Bootstrap / Framework / Infrastructure）不能编译期引用热更程序集。
- Unity API 只能在 `Game.Presentation` / `Game.Infrastructure` / `Game.Bootstrap` 中使用。
- 逻辑固定 60Hz；表现层只做插值和反馈。

## 3. 核心规则

- 回溯时所有残影统一回到 t=0。
- 残影死亡只在当前时间线有效，下次回溯复活。
- 进入新检查点清空残影。
- 残影轨迹结束立即消散，压力板 0.3s 后释放。
- 残影按 `EchoFrame` 轨迹回放，不重跑物理。
- 详细规则见 `Docs/DesignRules.md`。

## 4. 验证

- 修改 Domain/Core 后必须跑 EditMode 测试。
- 修改场景生成逻辑后执行菜单 `ECHO/Build M0 Scenes`，再进 Play Mode 验证。
- 修改 Presentation 后至少验证：Boot → Level_00、移动、回溯、残影踩板、门开启。
- 构建后端：当前固定 **Mono2x**，M0–M4 可以全程使用 Mono。
  IL2CPP / HybridCLR 是可选扩展，不纳入当前里程碑门禁；如果以后要启用，需先安装
  **Windows Build Support (IL2CPP)**，地址与步骤见 `Docs/Environment.md` 第 4 节。
- HybridCLR 包当前保留但 `enable: 0`，不参与日常构建；程序集分层仍按“未来可热更”的边界维护。
- 验证记录写入 `Docs/M0-Verification.md` 或对应里程碑文档。

## 5. 代码导读维护

- `Docs/CodeTour.md` 是项目的学习入口，不是可选的附属文档。
- 新增/修改代码时，必须同步更新 CodeTour 的相关章节：
  - 新增文件 → 附录 A“代码地图”；
  - 改一帧流程 → 第 5 章“一帧的生命周期”；
  - 改核心规则 → 先改 `Docs/DesignRules.md`，再改 CodeTour；
  - 新增测试 → 第 13 章“测试导读”；
  - 新增改法 → 第 15 章“常见修改指南”；
  - 改程序集依赖 → 第 2 章“工程结构与程序集”。
- CodeTour 要解释“为什么/怎么联系”，不要写成 API 列表。
- 提交前按 CodeTour 附录 B“维护检查清单”过一遍。

## 6. 技术决策记录

- 关键架构/工具/美术决策记录在 `Docs/TechDecisions.md`。
- 新增或推翻决策时追加一条，不要直接改旧结论；旧结论标记 `Superseded`。
- 每条决策至少写：背景、决策、理由、后果、重新评估触发条件。
- 代码/文档与决策冲突时，先确认决策是否已过期，再改代码。
