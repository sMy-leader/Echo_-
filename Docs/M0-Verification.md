# M0 验证记录

日期：2026-10-20
Unity：`6000.4.0f1`
构建后端：Mono（IL2CPP 模块尚未安装）

## 1. EditMode 测试

```text
Assembly: Game.Tests.EditMode
Result:   Passed
Total:    23
Passed:   23
Failed:   0
Skipped:  0
```

覆盖：

- `Vec3` 数学
- `InputFrame` 量化、按钮位、朝向编码、4 字节布局
- `TimelineRecorder` / `EchoRecorder` 环形缓冲
- `EchoFrame` 16 字节布局
- `EchoManager` 3 个残影 FIFO、结束移除
- `ActivationAggregator` 激活与 0.3s 释放
- `PlayerMotor` 前进移动、传送
- `GameSession` 回溯需要录制、回溯生成残影并恢复检查点、恢复检查点不生成残影、回溯冷却

## 2. 场景

`ECHO/Build M0 Scenes` 可重复生成：

- `Boot.unity`：`Bootstrapper` → 加载 `Level_00`
- `Level_00.unity`：20×20 灰盒房间、门框、红色门、黄色压力板、蓝色检查点、橙色玩家、固定相机
- Build Settings：`Boot(0) / MainMenu(1) / Level_00(2)`

场景对象与组件由 `Assets/Game/Editor/M0SceneBuilder.cs` 生成，避免手工引用丢失。

## 3. 运行时验证

通过 Unity MCP 在 Play Mode 中：

1. 将玩家传送到压力板，录制约 3 秒轨迹。
2. 调用 `TryRewind()`。
3. 观察到：
   - 玩家回到蓝色检查点；
   - 青色残影出现在压力板上；
   - 压力板变绿（`Plate: ON`）；
   - 红色门升起，门洞打开；
   - HUD 显示 `Echoes: 1/3`。

说明核心闭环“踩板 → 回溯 → 残影踩板 → 门保持开启”已跑通。

## 4. Windows 构建

### Mono 构建（已通过）

```text
Target:  StandaloneWindows64
Backend: Mono
Result:  Succeeded
Errors:  0
Size:    115,055,085 bytes
Output:  Builds/M0/ECHO.exe
```

已启动构建并确认：

- D3D12 初始化成功（NVIDIA RTX 5060 Laptop）；
- Input System 初始化成功；
- 玩家日志出现 `Bootstrapper: loading scene Level_00`，确认 `Game.Framework.UnityLogger` 生效；
- Boot → Level_00 加载，无运行时报错；
- 进程可正常退出。

### IL2CPP / HybridCLR（可选，不阻塞 M0）

尝试把 Standalone 后端切到 IL2CPP 后构建失败，实际输出：

```text
Target:  StandaloneWindows64
Backend: IL2CPP
Result:  Failed
Errors:  1
Error:   Currently selected scripting backend (IL2CPP) is not installed.
```

构建失败后，编辑器实际后端回到 **Mono2x**，当前 Mono 构建仍然通过。
结论：**下拉框里能看到 IL2CPP ≠ 模块已安装**。当前 `6000.4.0f1` 是外部安装，
Unity Hub 不提供 “Add modules” 入口；如果以后要启用，手动运行官方 TargetSupportInstaller：

```text
https://download.unity3d.com/download_unity/8cf496087c8f/TargetSupportInstaller/UnitySetup-Windows-IL2CPP-Support-for-Editor-6000.4.0f1.exe
```

安装目录选择 `D:\unity\Editor`。当前网络访问该地址会重定向到 `download.unitychina.cn`，
而该镜像没有这个文件（404），所以需要能访问国际 CDN 的网络。详见 `Docs/Environment.md` 第 4 节。

`Builds/` 已被 `.gitignore` 忽略，不进入仓库。

## 5. 已知缺口（M0 之后）

- 真实键盘手感需要手动在 Game 视图中验证（自动化只验证了数据层和传送闭环）。
- 相机、门、压力板、残影都是灰盒占位，视觉表现后置。
- 没有检查点触发区（当前只在出生点捕获检查点）。
- 没有玩家死亡/守卫/激光/声音。
- 没有热更、YooAsset 初始化、存档、主菜单。
- Windows IL2CPP 模块未安装；IL2CPP/HybridCLR 为可选扩展，不作为当前门禁。当前 Mono2x 构建可用。
