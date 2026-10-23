# ECHO / 残响

固定视角 3D 时间回溯潜行解谜 Demo。每次回溯都会留下一个会重演你上一轮行动的「残影」；你要和自己过去的影子配合，踩板、拉杆、引开守卫。

## 当前状态

**M0 垂直切片 + M1 守卫感知 + M1.5 视觉基础已可运行（灰盒）**：Boot → Level_00 → 踩板/回溯/残影，守卫可看见玩家并进入 Suspicious/Investigate/Alert；角色已有 visualRoot/Animator 接口，换正式模型不需要改 Domain。

- 35 个 EditMode 测试通过
- Windows Mono 构建成功并启动
- 第一版角色 VFX（悬停光圈 / 能量核心 / 拖尾 / Alert 脉冲 / 残影爆发）已接入
- L1 ProBuilder 灰盒基础布局已生成（`ECHO/Build L1 Scene`）
- 验证记录：[Docs/M0-Verification.md](Docs/M0-Verification.md) · [Docs/M1-Verification.md](Docs/M1-Verification.md) · [Docs/M1.5-Verification.md](Docs/M1.5-Verification.md) · [Docs/VFX-Verification.md](Docs/VFX-Verification.md)

## 文档

- [环境基线](Docs/Environment.md)
- [程序架构](Docs/Architecture.md)
- [核心规则](Docs/DesignRules.md)
- [代码导读](Docs/CodeTour.md)
- [技术决策记录](Docs/TechDecisions.md)
- [美术资产管线](Docs/ArtPipeline.md)
- [代码注释规范](Docs/CodingStyle.md)
- [里程碑](Docs/Milestones.md)
- [M0 验证记录](Docs/M0-Verification.md)
- [M1 验证记录](Docs/M1-Verification.md)
- [M1.5 验证记录](Docs/M1.5-Verification.md)
- [VFX 验证记录](Docs/VFX-Verification.md)

## 注意

- 当前构建后端为 **Mono2x**，M0–M4 可全程使用 Mono。
  IL2CPP / HybridCLR 是可选扩展，当前不启用；如需启用，安装步骤见 `Docs/Environment.md` 第 4 节。
- `Assets/Learning/` 是学习/实验代码，Editor-only，不参与发布；发布前可整体删除。
- 任何 `Resources/` 文件夹都会被强制进包（包括 `Assets/Learning/UniTask/Resources`），业务资源不要放在这里。
- `Assets/**/*.meta` 必须提交。
