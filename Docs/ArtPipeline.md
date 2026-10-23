# 美术资产与角色管线

> 目标：程序/玩法与美术资产解耦；换模型不重写 Domain；资源可优化、可迁移到 YooAsset。

## 1. 目录约定

当前目录（可继续扩展）：

```text
Assets/Game/Art/
├── Animation/        Animator Controller / AnimationClip
├── Materials/        角色与场景材质
├── Model/            源模型 FBX
├── Prefabs/          运行时 Prefab
├── Textures/         贴图
├── UI/               UI 图集/图标
├── VFX/              粒子、VFX Graph、材质
└── Videos/           演示/过场视频（可选）
```

建议后续按“角色/环境/道具”再分层：

```text
Art/Characters/Player/
Art/Characters/Guard/
Art/Characters/Echo/
Art/Environment/
Art/Props/
```

当前三套角色资产已放在 `Model/`、`Materials/`、`Prefabs/`，规模还小，不需要立刻重组；当角色数量超过 3–4 个时再按角色分目录。

## 2. 命名

| 类型 | 前缀 | 示例 |
|---|---|---|
| 模型 FBX | `SM_` | `SM_Player` |
| Prefab | `PF_` | `PF_Player` |
| 材质 | `M_` | `M_Player` |
| 贴图 | `T_` | `T_Player_BaseColor` |
| Animator Controller | `AC_` | `AC_Player` |
| 动画剪辑 | `A_` | `A_Player_Idle` |
| VFX | `VFX_` | `VFX_Echo_Dissolve` |

现有 `Player_robot` / `Enemy_robot` / `Echo_robot` 可以继续用；新增资产按上表统一。

## 3. 角色 Prefab 约定

```text
Player / Guard / Echo 根节点（脚底）
  ├── Visual（模型/胶囊，visualRoot）
  │     ├── Mesh / SkinnedMeshRenderer
  │     ├── VFX_Hover / VFX_Core / VFX_Trail
  │     └── Animator（可选）
  └── ForwardIndicator（守卫专用）
```

硬规则：

- 根节点在脚底，和 `PlayerMotor.Position` / `GuardBrain.Position` 一致；
- 模型 pivot 在脚底；如果 pivot 在胯部，给 `visualRoot` 加偏移；
- 模型/视觉体不挂 Collider；
- 逻辑胶囊尺寸在 `GameConstants` / `GuardConfig` 调整；
- Animator 的 Root Motion 关闭；
- Prefab 上的 Tag 不用于资产查找；用序列化引用或 `IAssetService`。

## 4. 模型预算

| 项 | 当前 | 建议 |
|---|---|---|
| 玩家三角面 | ~50k | 20–30k |
| 守卫三角面 | ~50k | 20–30k |
| 残影三角面 | ~50k | 可复用玩家模型 |
| 材质槽 | 1 | 1–2 |
| 贴图 | 2048 | 1024（normal 1024，metallic/rough 512） |
| DrawCall | 1/角色 | 保持 1–2 |

优化手段：

- Blender Decimate / 重新拓扑；
- 合并小零件、减少材质槽；
- 贴图压缩（BC7/BC5）、关闭不需要的 Read/Write；
- LOD Group（远景用 50% 面数）；
- 静态角色可开 Static Batching；动态角色用 GPU Instancing/SRP Batcher；
- 用 Frame Debugger / Profiler 看实际收益。

## 5. 贴图导入

| 贴图 | sRGB | Max Size | 压缩 |
|---|---|---|---|
| BaseColor | ✅ | 1024 | BC7/High Quality |
| Normal | ❌ | 1024 | BC5/Normal |
| Metallic | ❌ | 512 | BC4/BC7 |
| Roughness | ❌ | 512 | BC4/BC7 |
| Emissive | ✅ | 1024 | BC7 |

- 关闭 Read/Write；
- 开启 Mip Maps（角色会远离相机）；
- `NPOT` 用 ToNearest；
- 源文件放 LFS；构建只包含压缩后纹理。

## 6. 动画策略

当前模型已经带骨骼/Animator，M1 采用**最小 Mecanim**：

### 玩家

- 参数：`Speed` Float；
- 结构：`Locomotion` 1D BlendTree：
  - `Speed = 0` → Idle
  - `Speed = 0.5` → Sneak Walk
  - `Speed = 1` → Walk
- 代码 `PlayerView` 只设置 `Speed`，并兼容旧 Controller 的 `Sneaking` 参数；
- Root Motion 关闭，位置由 `PlayerMotor` 决定。

### 守卫

- 参数：`Speed` Float，`Attack` Trigger；
- 结构：`Locomotion`（Idle/Jog）+ `Attack`（punch，one-shot）；
- 代码映射：
  - Patrol / Investigate / Return → `Speed = 1`（Jog）
  - Suspicious / Alert / Search → `Speed = 0`（Idle）
  - `GuardBrain.IsAttacking` 上升沿 → `SetTrigger("Attack")`
- `Alert` 不单独做动画状态，用 VFX/颜色表达；以后要 AlertIdle 再加参数。
- Root Motion 关闭；`jog-inplace` 必须 Loop Time；punch 不循环。

### 旧方案

如果坚持 `idle / walk / sneak` 三个独立状态：

- 需要用 `Sneaking` bool 做优先级；
- `Speed=0.5` 且 `Sneaking=true` 时，`walk` 与 `sneak` 条件可能同时成立；
- 不如 BlendTree 稳定，建议只作为过渡方案。

## 7. VFX 建议

当前已实现 `Game.Presentation.CharacterVfx`：

- Hover：脚底 ParticleSystem 环形粒子；
- Core：胸口 ParticleSystem 球形粒子；
- Trail：TrailRenderer；
- AlertPulse：守卫 Alert 红色扩散环；
- Burst：残影生成/消散爆发；
- 材质：`Assets/Game/Art/VFX/M_Vfx_Additive.mat`。

这是第一版代码生成占位；正式版可以按下面升级：

| 效果 | 实现 |
|---|---|
| 悬停光圈 | 地面 Decal / Mesh Ring / VFX Graph |
| 能量核心 | 模型胸口发光材质 + 点光/粒子 |
| 移动拖尾 | TrailRenderer / VFX Graph |
| 残影消散 | 专用 Ghost Shader + 碎片粒子 + 上升粒子 |
| 回溯演出 | 全屏后处理 + 相机震动 + 音频倒放 |
| 警报 | 守卫前向指示器变红 + 脉冲粒子 + 音效 |

优先用 `ParticleSystem` 做占位；VFX Graph 留给回溯/残影消散等 1–2 个重点效果。注意 Overdraw 和粒子数量。

## 8. 守卫朝向可读性

顶部视角下，胶囊/对称模型无法表达朝向。三层解决：

1. **模型层**：单眼/面罩/胸甲发光条，前后轮廓不对称；
2. **指示器层**：`forwardIndicator` 小方块/发光条，跟随根节点旋转，按状态变色；
3. **调试层**：`OnDrawGizmosSelected` 画视锥，调巡逻路线用，不进正式画面。

正式关卡不建议一直显示完整视野锥；可以在 `Suspicious/Alert` 时短暂显示地面扇形/Decal，作为强反馈。

## 9. YooAsset 接入时机

当前 M1.5：

- 直接序列化 Prefab 引用 + `Instantiate`；
- 不接 YooAsset；
- 不接 Addressables。

M2/M3 再接：

- `IAssetService` 抽象：`LoadAssetAsync<T>` / `InstantiateAsync` / `Release`；
- YooAsset 包：`Main` / `Level_XX` / `Config`；
- 按更新单元分包，不按文件类型；
- 用 `UniTask` + `CancellationToken`；
- 残影/特效对象池，避免频繁 Instantiate/Destroy；
- 资源句柄必须释放，避免泄漏。

## 10. 提交前检查

- [ ] 模型 pivot 在脚底；
- [ ] 视觉体没有 Collider；
- [ ] Prefab 引用的是正式资产，不是场景临时对象；
- [ ] 贴图 Max Size / sRGB / 压缩正确；
- [ ] 没有把 2048 源图直接打进构建；
- [ ] 没有用 `FindWithTag` / `Resources.Load` 找资产；
- [ ] 更新了 `Docs/CodeTour.md` 第 10.3 / 15.2.1 节。
