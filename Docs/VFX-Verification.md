# VFX 第一版验证记录

日期：2026-10-22
Unity：`6000.4.0f1`
构建后端：Mono2x

## 1. 本版范围

新增 `Game.Presentation.CharacterVfx`，运行时生成：

- **Hover**：脚底悬停光圈（ParticleSystem 环形粒子）；
- **Core**：胸口能量核心（ParticleSystem 球形粒子）；
- **Trail**：移动拖尾（TrailRenderer）；
- **AlertPulse**：守卫进入 Alert 时的红色扩散环；
- **Burst**：残影生成/消散的爆发粒子。

所有 VFX 只做表现，不参与玩法；挂在角色根节点/`visualRoot` 下。

## 2. 接入方式

- `PlayerView`：移动时 `vfx.SetMoving(true)`；
- `GuardView`：状态移动时 `SetMoving`，进入 Alert 时 `SetAlert`；
- `EchoView`：移动状态驱动 Trail，首次 Apply 播放生成爆发；
- `GameLoop.CreateEchoView`：运行时创建 `CharacterVfx`，设置青色 + 残影材质；
- `M0SceneBuilder`：给 Player/Guard 根节点添加 `CharacterVfx`，设置颜色/材质/coreHeight；
- `Assets/Game/Art/VFX/M_Vfx_Additive.mat`：URP Particles/Unlit Additive 材质。

## 3. 验证

```text
EditMode Tests: 33/33 passed
Mono Build:     Succeeded
Build Size:     155,833,300 bytes
Output:         Builds/M0/ECHO.exe
Console:        无 error / warning
```

Play Mode 观察：

- 玩家/守卫脚底有悬停光圈；
- 守卫进入 Alert 时出现红色扩散环；
- 回溯后 Echo_robot 以半透明青色出现在压力板上，脚底有青色光圈；
- 无 NullReference，无材质丢失；
- 构建启动日志 `Bootstrapper: loading scene Level_00`。

## 4. 已知边界

- 当前是代码生成的基础 ParticleSystem，不是最终 VFX Graph；
- 残影“消散”爆发还没有在销毁前播放（需要延迟销毁/独立 VFX 对象）；
- 粒子没有对象池；3 个角色规模下可接受；
- 颜色/尺寸/频率是首版占位，需要按正式模型调整；
- 悬停光圈/能量核心在远景下偏淡，正式版可换成专用 Shader/Mesh/VFX Graph。

## 5. 下一步

1. 为 Echo 做专用 Ghost Shader（Fresnel + dissolve）；
2. 残影消散时延迟销毁 0.3–0.5s 并播放爆发；
3. 用 VFX Graph 做回溯/残影消散两个重点效果；
4. 粒子对象池；
5. 优化贴图/模型后，再统一调 VFX 亮度与颜色。
