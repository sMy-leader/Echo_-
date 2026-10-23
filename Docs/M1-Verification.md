# M1 验证记录 — 守卫感知与状态机（第一块）

日期：2026-10-21
Unity：`6000.4.0f1`
构建后端：Mono2x

## 1. 本块范围

已实现：

- 守卫视觉感知：水平 60°、垂直 40°、8m、10Hz 采样、Linecast 遮挡；
- 近身警觉：2m 内不要求目标在视锥中心；
- 警戒值：0–100，视觉内 +40/s，脱离 -15/s；
- 状态机：Patrol / Suspicious / Investigate / Alert / Search / Return；
- Alert 保持 2.2m，避免守卫与玩家胶囊/模型重叠；
- 玩家 + 残影统一为 `PerceptionTarget`；
- HUD 显示守卫状态/警戒值/目标；
- 场景生成器生成 Guard + 四个巡逻点。

未实现（后续）：

- 守卫攻击/抓捕/失败处理；
- 听觉；
- 残影死亡；
- 寻路/胶囊碰撞（当前直线移动）；
- L1 关卡布局。

## 2. EditMode 测试

```text
Assembly: Game.Tests.EditMode
Result:   Passed
Total:    33
Passed:   33
Failed:   0
Skipped:  0
```

新增 `GuardBrainTests` 覆盖：

- Patrol 向路点移动；
- 视锥内目标 → Suspicious；
- 背后远距离目标不可见；
- 背后近距离目标因近身警觉可见；
- 有墙遮挡不可见；
- 警戒 60 → Investigate；
- 警戒 100 → Alert；
- Alert 丢失目标 6s → Search；
- Search 8s → Return；
- `GameSession.CollectPerceptionTargets` 包含玩家 + 残影。

原有 M0 测试仍全部通过。

## 3. Play Mode 验证

场景：`ECHO → Build M0 Scenes` 重建后，从 `Boot` 启动。

观察结果：

```text
Guard: Alert   Suspicion: 100   Target: 0
```

- 守卫颜色变红（Alert）；
- 守卫从出生点看到玩家，警戒值上升；
- 到达 2.2m 后停住并面向玩家，不再与玩家胶囊/模型重叠；
- 玩家在检查点，守卫在正前方；
- 无 Console 错误。

## 4. Windows 构建

```text
Target:  StandaloneWindows64
Backend: Mono2x
Result:  Succeeded
Errors:  0
Size:    117,267,076 bytes
Output:  Builds/M0/ECHO.exe
```

已启动构建并确认：

- D3D12 初始化成功；
- Input System 初始化成功；
- 日志出现 `Bootstrapper: loading scene Level_00`；
- 无运行时报错。

## 5. 已知边界

- 守卫是直线移动，L1 布局时需要接入胶囊碰撞/寻路。
- 没有攻击/抓捕；Alert 只追到 2.2m。
- 没有听觉；声音事件在 M3。
- 残影目前全部视为存活目标；残影死亡在后续接入。
- 守卫参数是 `GuardConfig` 纯 C# 默认值；后续由 ScriptableObject/关卡配置注入。
