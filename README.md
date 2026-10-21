# ECHO / 残响

固定视角 3D 时间回溯潜行解谜 Demo。每次回溯都会留下一个会重演你上一轮行动的「残影」；你要和自己过去的影子配合，踩板、拉杆、引开守卫。

## 当前状态

**M0 垂直切片已可运行（灰盒）**：Boot → Level_00 → 踩板 → 回溯 → 残影踩板 → 门开。

- 23 个 EditMode 测试通过
- Windows Mono 构建成功并启动
- 验证记录：[Docs/M0-Verification.md](Docs/M0-Verification.md)

## 文档

- [环境基线](Docs/Environment.md)
- [程序架构](Docs/Architecture.md)
- [核心规则](Docs/DesignRules.md)
- [代码导读](Docs/CodeTour.md)
- [技术决策记录](Docs/TechDecisions.md)
- [代码注释规范](Docs/CodingStyle.md)
- [里程碑](Docs/Milestones.md)
- [M0 验证记录](Docs/M0-Verification.md)

## 注意

- 当前构建后端为 **Mono2x**，M0–M4 可全程使用 Mono。
  IL2CPP / HybridCLR 是可选扩展，当前不启用；如需启用，安装步骤见 `Docs/Environment.md` 第 4 节。
- `Assets/Learning/` 是学习/实验代码，Editor-only，不参与发布；发布前可整体删除。
- 任何 `Resources/` 文件夹都会被强制进包（包括 `Assets/Learning/UniTask/Resources`），业务资源不要放在这里。
- `Assets/**/*.meta` 必须提交。
