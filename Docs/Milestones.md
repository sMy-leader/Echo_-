# ECHO / 残响 — 里程碑

> 总预算约 430 人时。里程碑按“可验收的能力”划分，不按“写完多少代码”划分。

## 总览

| 里程碑 | 目标 | 预计 | 核心验收 |
|---|---|---|---|
| M0 | 工程骨架 + 回溯/残影垂直切片 | 4–6 天 | 踩板 → 回溯 → 残影开门 → 本体通过 |
| M1 | 守卫感知 + 激光门 + L1 灰盒 | 2–3 周 | 残影当诱饵，本体绕后；至少 2 种解法 |
| M2 | 时序谜题 + 升降台 + 3 残影 | 2–3 周 | 时间差谜题可解；3 残影 FIFO 稳定 |
| M3 | 声音系统 + L3 + 资源管线 | 2–3 周 | 声音诱饵有效；YooAsset 资源包跑通（代码热更可选） |
| M4 | L4 综合关 + 菜单/存档/设置 + 发布 | 3–4 周 | Windows 构建、完整流程、性能报告 |

## M0：工程骨架 + 垂直切片

### 目标

从 `Boot` 启动，进入 `Level_00` 灰盒，完成：

```text
移动/潜行 → 踩压力板 → 门开
按 R → 回检查点 → 残影重演踩板 → 门保持开启 → 本体通过
```

### 步骤

| 步骤 | 内容 | 产出 |
|---|---|---|
| 0 | 环境基线、asmdef、场景/构建配置、学习资产隔离 | 工程可编译、构建列表正确 |
| 1 | `GameLoop` 固定 60Hz、`InputService`、`InputFrame` | 输入可录制、逻辑与渲染解耦 |
| 2 | `PlayerMotor`、`ICollisionWorld`、`CameraRig`、灰盒场景 | WASD/Shift 移动、不穿墙、固定 45° 相机 |
| 3 | `TimelineRecorder`、`CheckpointService`、`RewindService`、`EchoActor`、`PressurePlate` | 核心闭环可玩 |
| 4 | HUD、回溯反馈、Debug 面板 | 状态可读 |

### Definition of Done

1. 从 `Boot` 启动 → `Level_00` → 可移动、潜行、踩板。
2. 按 R 回溯后残影重演踩板，门保持开启，本体通过。
3. 连续回溯生成多个残影，3 个上限，FIFO 淘汰最旧。
4. 逻辑稳定 60Hz；EditMode 测试全绿。
5. Windows exe 可构建并运行（Mono 已验证；IL2CPP 为可选补验）。

### 明确不做

- 守卫 AI、激光、听觉系统
- 热更管线、YooAsset 初始化
- 存档、主菜单、设置
- 正式美术、音频、后处理

## M1：视线

- 守卫状态机（Patrol / Suspicious / Investigate / Alert / Search / Return）
- 视觉感知：60°/40°/8m，10Hz 分帧
- 激光门（常闭/周期）
- 残影被守卫感知、被击杀
- L1 灰盒 + 2 种解法
- `IAssetService` 的最小实现预研（可选，Mono 可用）

## M2：时序

- 升降台
- 周期激光
- 3 残影协同
- 时间锁门
- RenderGraph 后处理原型（回溯演出）
- YooAsset 最小分包（Main / Level_00 / Config）

## M3：回声

- 声音事件系统、距离衰减、材质脚步
- 双守卫
- L3 灰盒
- 残影消散声作为诱饵
- 存档基础版（MessagePack + CRC + 原子写入）

## M4：残响

- L4 综合关
- 主菜单、设置、暂停
- 可访问性基础项
- 性能报告、3 分钟演示视频、技术拆解文档
- Windows 发布构建

## 当前状态

M0 垂直切片已可运行（灰盒）。验证记录见 [M0-Verification.md](M0-Verification.md)。
M1 守卫感知第一块已完成，见 [M1-Verification.md](M1-Verification.md)。

- [x] Step 0.1 基线文档
- [x] Step 0.2 asmdef 骨架
- [x] Step 0.3 场景/构建配置
- [x] Step 0.4 EditMode 测试
- [x] Step 1 固定 tick + 输入
- [x] Step 2 玩家移动 + 灰盒
- [x] Step 3 回溯/残影闭环
- [x] Step 4 HUD/反馈
- [x] M1 守卫感知与状态机第一块（33/33 测试）
- [x] M1.5 视觉基础（visualRoot / Animator 接口 / 前向指示器）
- [x] VFX 第一版（悬停光圈 / 能量核心 / 拖尾 / Alert 脉冲 / 残影爆发）
- [ ] 手动键盘手感验证（WASD/Shift/R 实际操作）
- [ ] M1 守卫攻击/抓捕 + 激光门 + L1 布局
- [ ] （可选）安装 Windows IL2CPP 模块并验证 HybridCLR；不阻塞当前 Mono 路线
