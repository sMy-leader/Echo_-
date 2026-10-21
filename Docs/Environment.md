# ECHO / 残响 — 环境基线

> 本文件是工程的“可复现基线”。任何版本升级、构建模块变更，都必须先更新这里，再改工程。

## 1. 基线信息

| 项目 | 值 |
|---|---|
| 更新日期 | 2026-10-20 |
| Unity 版本 | `6000.4.0f1` |
| Unity 安装路径 | `D:\unity\Editor\Unity.exe` |
| 渲染管线 | URP `17.4.0`（`Assets/Settings/PC_RPAsset.asset`） |
| 构建目标 | Windows x64 |
| 当前构建后端 | **Mono2x**（已验证可构建、可运行）；IL2CPP / HybridCLR 为可选扩展，当前不启用 |
| 颜色空间 | Linear |
| 输入系统 | Input System `1.19.0`，Active Input Handling = New only |
| API Compatibility | `apiCompatibilityLevel: 6`（构建前需与 HybridCLR 要求复核） |
| 版本控制 | Git + Git LFS |

## 2. 包版本

| 包 | 版本 / 来源 | 备注 |
|---|---|---|
| `com.unity.render-pipelines.universal` | `17.4.0` | 与 Unity 6000.4 配套 |
| `com.unity.inputsystem` | `1.19.0` | 新输入系统 |
| `com.tuyoogame.yooasset` | `3.0.6` | **Plan.md 写的是 2.x，代码必须按 3.x API 写** |
| `com.code-philosophy.hybridclr` | Git（lock hash `e48a7beb...`） | 可选扩展；当前 `enable: 0`，不参与 M0–M4 门禁 |
| `com.cysharp.unitask` | Git（lock hash `a77a1b4a...`） | 异步基础设施 |
| `com.kyrylokuzyk.primetween` | `1.5.0` | 补间动画 |
| `com.unity.nuget.newtonsoft-json` | `3.2.2` | 配置/设置序列化 |
| `com.unity.test-framework` | `1.6.0` | EditMode / PlayMode 测试 |
| `com.unity.test-framework.performance` | `3.5.0` | 性能基线 |
| `com.coplaydev.unity-mcp` | Git `#main`（lock hash `81542a7e...`） | 仅开发工具；发布前固定 commit 或移除 |

## 3. 与 Plan.md 的偏差（已确认）

| Plan.md | 实际工程 | 处理 |
|---|---|---|
| Unity `6000.0.x` LTS | Unity `6000.4.0f1` | **保留实际版本**；项目期间不升级 minor；确认发布通道后更新 Plan.md |
| YooAsset `2.x` | YooAsset `3.0.6` | 保留 3.0.6；所有示例代码以 3.x 文档为准 |
| HybridCLR / IL2CPP | 当前 Mono2x 可构建；IL2CPP 模块未安装 | **不阻塞当前 Demo**；保留为可选扩展，未来需要时再装 |
| `Game.Domain → Game.Framework` | `Game.Framework` 会依赖 UnityEngine | 新增纯 C# 的 `Game.Core` 作为 Domain 的依赖 |
| 入口 `Game.Application.GameEntry` | Application 不能反向引用 Presentation | 入口放在最外层；M0 由场景里的 `GameLoop` 作为组合根 |

## 4. 可选扩展：IL2CPP / HybridCLR

> 当前 M0–M4 可以全程使用 Mono，这一节不阻塞开发。
> 如果以后要做代码热更、IL2CPP 发布或 HybridCLR 验证，再按下面的步骤安装模块。

Player Settings 的下拉里出现 IL2CPP，**不代表模块已安装**；真正构建时还需要目标平台模块。

当前 `D:\unity\Editor\Data\PlaybackEngines\WindowsStandaloneSupport\Variations` 只有：

- `mono`
- `win32/win64/win_arm64` 的 mono player 变体

没有 `win64_il2cpp`。已实际测试：脚本后端切到 IL2CPP 后构建失败，错误为：

```text
Error building Player: Currently selected scripting backend (IL2CPP) is not installed.
```

修复步骤（外部安装的编辑器无法用 Hub 加模块）：

**方案 A：手动安装官方 TargetSupportInstaller（推荐，保留 6000.4.0f1）**

官方模块下载地址：

```text
https://download.unity3d.com/download_unity/8cf496087c8f/TargetSupportInstaller/UnitySetup-Windows-IL2CPP-Support-for-Editor-6000.4.0f1.exe
```

- 下载大小约 260 MB，安装后约 903 MB。
- 运行安装器，安装目录选择 **`D:\unity\Editor`**（即包含 `Unity.exe` 和 `Data` 的那一层）。
- 安装完成后确认 `Variations` 下出现 `il2cpp` 相关变体（通常包括
  `il2cpp`、`win64_player_development_il2cpp`、`win64_player_nondevelopment_il2cpp`，以实际安装为准）。

- 重新打开 Unity，Player Settings → Scripting Backend 保持 IL2CPP，构建 `Builds/M0-IL2CPP/ECHO.exe`。

**网络注意：** 当前环境访问 `download.unity3d.com` 会被重定向到
`download.unitychina.cn`，而该镜像目前没有这个版本的文件（返回 404）。
因此方案 A 需要能访问国际 CDN 的网络（VPN/代理/其他网络）。
如果安装器没有自动识别编辑器，手动浏览到 `D:\unity\Editor`。

**方案 B：用 Hub 管理的 LTS（备选）**

如果拿不到 6000.4.0f1 的模块，可以安装一个 Hub 能管理、且带 Windows IL2CPP 的
Unity LTS（例如 6000.0 LTS），用该版本打开项目。代价是可能需要重导入 Library、
调整 URP/包版本；这是降级方案，不是首选。

**方案 C：暂时继续 Mono**

在模块安装前，日常开发/Play Mode 不受影响；如果需要出包，把
Player Settings → Scripting Backend 临时切回 Mono。HybridCLR 相关验证必须等 IL2CPP。

## 5. 验证命令

```text
# EditMode 测试（Unity Test Runner）
Window > General > Test Runner > EditMode > Run All
```

M0 的测试与构建结果见 [M0-Verification.md](M0-Verification.md)。

M0 使用 Mono 构建验证通过（可运行）。IL2CPP 构建为可选尝试，因缺少
**Windows Build Support (IL2CPP)** 模块失败；这不影响当前 Demo 的开发与发布。

## 6. 版本纪律

- 不中途升级 Unity minor 版本；升级只在里程碑间隙做。
- 第三方包使用精确版本或固定 commit；不使用浮动分支（`#main`）。
- 每次升级必须跑完整 EditMode/PlayMode 测试，并记录性能基线。
- `Assets/**/*.meta` 必须提交；`Library/`、`Temp/`、`Logs/`、`UserSettings/` 不提交。
