using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Application;
using Game.Core;
using Game.Domain;
using Game.Infrastructure;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// M0 的表现层组合根：
    /// - 创建 UnityInputService / PhysicsCollisionWorld / GameSession；
    /// - 驱动固定 60Hz 逻辑；
    /// - 把 Domain 状态同步到相机、玩家、残影、压力板、门和 HUD。
    ///
    /// 这里只做“读状态 + 画出来”，不写玩法规则。
    /// </summary>
    public sealed class GameLoop : MonoBehaviour
    {
        [Header("Scene references")]
        public Transform playerSpawn;
        public PlayerView playerView;
        public CameraRig cameraRig;
        public PressurePlateView pressurePlate;
        public DoorView door;
        public PressurePlateView[] timeLockPlates;//时间锁的压力版
        public DoorView timeLockDoor;
        public LiftView[] lifts;
        public GuardView guardView;
        public GameObject echoVisualPrefab;
        public Material echoVfxMaterial;
        public LaserGateView laserGate;
        public LevelPortalView levelPortal;
        public LeverView lever;
        public GameHud hud;

        [Header("Camera movement")]
        public bool cameraRelativeMovement;

        // EchoPlayback 是引用类型，直接作为字典键，避免额外 Id 映射表。
        private readonly Dictionary<EchoPlayback, EchoView> _echoViews =
            new Dictionary<EchoPlayback, EchoView>();

        // 遍历字典时不能直接删除，先收集再删。
        private readonly List<EchoPlayback> _removeBuffer = new List<EchoPlayback>();

        private UnityInputService _input;
        private GameSession _session;
        private GuardSystem _guards;
        private PhysicsVisionWorld _vision;
        private InteractionService _interactions;
        private bool _interactHeld;
        private readonly Dictionary<int, bool> _echoInteractHeld = new Dictionary<int, bool>();
        private readonly List<PerceptionTarget> _perceptionTargets = new List<PerceptionTarget>();
        private float _accumulator;// 累积真实时间，驱动固定 tick 循环。
        private float _failureCooldown;
        private Transform _cameraTransform;
        private bool _portalTriggered;//传送门触发

        /// <summary>
        /// 在 Awake 建 Session，保证第一帧 Update 之前已经有检查点。
        /// 同时提供 Start/Update 兜底：某些场景（例如编辑器新建后直接 Play）
        /// 可能错过 Awake 时序，幂等初始化可以避免空引用。
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_session != null)
            {
                return;
            }

            _input = new UnityInputService();

            //确定玩家生成点
            Vector3 spawn = playerSpawn != null
                ? playerSpawn.position
                : new Vector3(0f, 0.05f, -6f);
            float spawnYaw = playerSpawn != null ? playerSpawn.eulerAngles.y : 0f;

            _session = new GameSession(
                new PhysicsCollisionWorld(),
                new Vec3(spawn.x, spawn.y, spawn.z),
                spawnYaw);
            _session.CaptureCheckpoint();

            // 守卫：场景里的 GuardView 提供出生点/巡逻点；Domain 只拿到纯数据。
            _vision = new PhysicsVisionWorld();
            _guards = new GuardSystem();

            if (guardView != null)
            {
                List<Vec3> patrolPoints = new List<Vec3>();
                guardView.CollectPatrolPositions(patrolPoints);//开始收集巡逻点

                // GuardView 根节点已经在脚底，GuardBrain 直接使用根节点位置。
                Vector3 feet = guardView.transform.position;

                //将 GuardView 的 Transform 位置和旋转、巡逻点信息等转换为 GuardBrain 的数据
                GuardConfig config = new GuardConfig
                {
                    MinApproachDistance = guardView.minApproachDistance,
                    AttackRange = Mathf.Max(guardView.attackRange, guardView.minApproachDistance + 0.1f),
                    AttackHitDistanceBuffer = guardView.attackHitDistanceBuffer
                };

                _guards.Add(new GuardBrain(
                    1,
                    new Vec3(feet.x, feet.y, feet.z),
                    guardView.transform.eulerAngles.y,
                    patrolPoints,
                    config));
            }

            if (hud != null)
            {
                hud.Bind(_session);
                hud.BindGuards(_guards);
                hud.BindTimeLock(
                    timeLockPlates != null && timeLockPlates.Length > 0
                        ? _session.TimeLock
                        : null);
            }

            _interactions = new InteractionService();
            if (lever == null)
            {
                lever = Object.FindAnyObjectByType<LeverView>();
            }

            if (lever != null)
            {
                _interactions.Add(lever);
            }

            //设置摄像机的位置保证固定
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindAnyObjectByType<Camera>();
            }

            _cameraTransform = mainCamera != null ? mainCamera.transform : null;
        }

        /// <summary>
        /// 固定 tick 循环：
        /// - accumulator 累积真实时间；
        /// - 每满 1/60s 跑一次逻辑；
        /// - 最多补 8 步，防止卡顿后“追帧爆炸”；
        /// - 剩余 accumulator 转成 alpha，交给表现层插值。
        /// </summary>
        private void Update()
        {
            if (_session == null)
            {
                EnsureInitialized();
                if (_session == null)
                {
                    return;
                }
            }

            // 滚轮每帧只消费一次；按方向固定步进，兼容 ±1 与 ±120 两种平台值。
            if (cameraRig != null)
            {
                float scroll = _input.ReadScroll();
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    cameraRig.Zoom(Mathf.Sign(scroll) * cameraRig.zoomStep);
                }
            }

            // 卡顿时单帧最多累积 0.25s，避免切回窗口后一次补几百个 tick。
            _accumulator += Mathf.Min(Time.unscaledDeltaTime, 0.25f);

            int steps = 0;

            // 每个 tick 16.6667ms，最多补 8 步，避免卡顿后“追帧爆炸”。
            while (_accumulator >= GameConstants.TickDelta && steps < 8)
            {
                _accumulator -= GameConstants.TickDelta;
                steps++;

                // 每个 tick 采样一次，保证边沿触发的 Rewind 只被消费一次。
                InputFrame input = _input.Sample();

                // 第三人称：把键盘/手柄的屏幕轴转换成相机在地面的前后左右。
                if (cameraRelativeMovement)
                {
                    input = ToCameraRelative(input);
                }

                if (hud != null)
                {
                    hud.SetTimelineVisible(input.Has(InputButtons.Timeline));
                }

                // E 是边沿交互：按住不会重复 toggle。
                //通过这里进行是否按下E交互->触发HandleEchoInteract来同步残影行为
                bool interactPressed = input.Has(InputButtons.Interact);
                if (interactPressed && !_interactHeld && _interactions != null)
                {
                    //尝试交互->找最近的交互物->进入交互接口
                    _interactions.TryInteract(
                        new PerceptionTarget(0, _session.Player.Position, true),
                        true);
                }

                _interactHeld = interactPressed;

                // 先处理回溯，再 Tick；这样回溯后的第一帧会从检查点开始录制。
                if (input.Has(InputButtons.Rewind))
                {
                    if (_session.TryRewind())
                    {
                        ResetLifts();
                    }
                }

                _session.Tick(GameConstants.TickDelta, input);//开始记录玩家和残影的移动—（打断点进去看）

                // 残影的 Interact 状态是轨迹回放的一部分；用边沿检测避免每帧重复 toggle。
                HandleEchoInteractions();
                UpdateInteractionPrompt();//更新交互提示词

                // 玩家/残影移动完之后再让守卫感知，保证目标位置是本 tick 的。
                _session.CollectPerceptionTargets(_perceptionTargets);
                _guards.Tick(GameConstants.TickDelta, _perceptionTargets, _vision);//不断加入列表刷新帧，让守卫感知到玩家和残影的位置->System

                // 守卫蓄力结束的 AttackHit 只在这一 tick 为 true。
                HandleGuardAttacks();

                // 激光命中在守卫之后判定，避免同一 tick 的失败流程重复触发。
                CheckLaserHits();

                // 传送门只触发一次；玩家没有 Collider，所以用几何包含判断。（需要保证压力板正在处于被激活状态）
                if (!_portalTriggered
                    && levelPortal != null
                    && levelPortal.Contains(_session.Player.Position)
                    && (!levelPortal.requirePlateActive || _session.Plate.IsActive)
                    && (levelPortal.requiredLever == null || levelPortal.requiredLever.IsOn))
                {
                    _portalTriggered = true;
                    if (!string.IsNullOrEmpty(levelPortal.completedLevelId))
                    {
                        LevelFlow.Instance.MarkCompleted(levelPortal.completedLevelId);//根据关卡具体哪个传送门的ID->标记为完成此关卡
                    }

                    LevelFlow.Instance.LoadScene(levelPortal.nextScene).Forget();
                }

                // 掉出场景的兜底：通过统一的失败流程回到检查点。
                if (_session.Player.Position.Y < -10f)
                {
                    TriggerFailure();
                }

                // 失败冷却计时，避免同一 tick 多个守卫重复触发。
                if (_failureCooldown > 0f)
                {
                    _failureCooldown = Mathf.Max(0f, _failureCooldown - GameConstants.TickDelta);
                }

                // 玩家和残影都移动完之后再统计，压力板看到的才是本 tick 的位置。
                UpdateLifts(GameConstants.TickDelta);
                _session.TickPlate(GameConstants.TickDelta, CountPlateActivators());
                UpdateTimeLock();
            }

            // 如果 8 步还不够，丢弃剩余时间，宁可慢一点也不要继续追帧。
            if (steps >= 8)
            {
                _accumulator = 0f;
            }

            float alpha = Mathf.Clamp01(_accumulator / GameConstants.TickDelta);
            Render(alpha);
        }

        /// <summary>
        /// 玩法状态 → 视图状态的同步：压力板、门、玩家、残影、相机。
        /// </summary>
        /// <param name="alpha"></param>
        private void Render(float alpha)
        {
            if (playerView != null)
            {
                playerView.Apply(_session.Player, alpha);
            }

            if (guardView != null && _guards.Guards.Count > 0)
            {
                guardView.Apply(_guards.Guards[0], alpha);
            }

            //设置为潜行状态并且跟随玩家位置—>业务层在“CameraRig”里（做拉近拉远的逻辑）
            if (cameraRig != null)
            {
                Vec3 playerPosition = _session.Player.Position;
                cameraRig.SetSneaking(_session.Player.State == EchoState.Sneak);
                cameraRig.Follow(
                    new Vector3(playerPosition.X, playerPosition.Y, playerPosition.Z),
                    Time.unscaledDeltaTime);
            }

            if (pressurePlate != null)
            {
                pressurePlate.SetActive(_session.Plate.IsActive);
            }

            if (laserGate != null)
            {
                laserGate.SetActive(_session.Laser.IsActive);
            }

            if (door != null)
            {
                bool leverReady = lever == null || lever.IsOn;
                door.SetOpen(_session.Plate.IsActive && leverReady);
            }

            if (lever != null)
            {
                lever.SetPowered(_session.Plate.IsActive);
            }

            SyncEchoViews(alpha);
        }

        /// <summary>
        /// 激光命中：只有 IsActive 时才判定。
        /// - 本体命中 → 统一失败回溯；
        /// - 残影命中 → 只击杀残影，本体不受影响。
        /// 命中点取胸口而不是脚底，避免角色站在光束下方被误判/漏判。
        /// </summary>
        private void CheckLaserHits()
        {
            if (laserGate == null || !_session.Laser.IsActive)
            {
                return;
            }
            //获取玩家中心位置
            Vec3 playerCenter = _session.Player.Position + Vec3.Up * 1.0f;
            if (laserGate.Intersects(
                new Vector3(playerCenter.X, playerCenter.Y, playerCenter.Z),
                0.3f))
            {
                TriggerFailure();
                return;
            }

            IReadOnlyList<EchoPlayback> echoes = _session.Echoes.Echoes;
            for (int i = 0; i < echoes.Count; i++)
            {
                if (!echoes[i].IsAlive)
                {
                    continue;
                }

                //如果是残影的话，执行残影逻辑->获取中心点->Trykill，从存储残影的列表中移除
                Vec3 echoCenter = echoes[i].CurrentPosition + Vec3.Up * 0.9f;
                if (laserGate.Intersects(
                    new Vector3(echoCenter.X, echoCenter.Y, echoCenter.Z),
                    0.25f))
                {
                    _session.Echoes.TryKill(echoes[i].Id);
                }
            }
        }

        /// <summary>
        /// 把输入轴从“屏幕/世界方向”转换成相机相对方向。
        /// 只在第三人称场景启用；室外固定俯视仍用世界轴，保证解谜方向稳定。
        ///
        /// 相机 forward/right 投影到 XZ 平面，避免俯仰导致移动速度变化。
        /// </summary>
        private InputFrame ToCameraRelative(InputFrame input)
        {
            if (_cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                _cameraTransform = mainCamera != null ? mainCamera.transform : null;
            }

            if (_cameraTransform == null)
            {
                return input;
            }

            Vector3 forward = _cameraTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            Vector3 right = _cameraTransform.right;
            right.y = 0f;
            if (right.sqrMagnitude < 1e-6f)
            {
                right = Vector3.right;
            }
            right.Normalize();

            Vector3 wish = forward * input.MoveYFloat + right * input.MoveXFloat;
            return InputFrame.FromAxes(
                wish.x,
                wish.z,
                (InputButtons)input.Buttons,
                input.YawDegrees);
        }

        /// <summary>
        /// 残影进入 Interact 状态时触发拉杆；只触发一次，避免按住 E 时反复开关。
        /// </summary>
        private void HandleEchoInteractions()
        {
            if (_interactions == null)
            {
                return;
            }

            IReadOnlyList<EchoPlayback> echoes = _session.Echoes.Echoes;
            for (int i = 0; i < echoes.Count; i++)
            {
                EchoPlayback echo = echoes[i];
                //残影是否在交互->PlayerMotor里的tick->通过玩家是否点按交互来同步残影的行为
                bool pressed = echo.IsAlive && echo.Current.EchoState == EchoState.Interact;
                bool wasPressed = _echoInteractHeld.TryGetValue(echo.Id, out bool held) && held;

                if (pressed && !wasPressed)
                {
                    _interactions.TryInteract(
                        new PerceptionTarget(echo.Id, echo.CurrentPosition, false),
                        true);
                }

                _echoInteractHeld[echo.Id] = pressed;
            }
        }

        /// <summary>
        /// 玩家靠近拉杆等交互物时更新 HUD 提示；每 tick 只读位置，不改玩法状态。
        /// </summary>
        private void UpdateInteractionPrompt()
        {
            if (hud == null || _interactions == null)
            {
                return;
            }
            //确定当前玩家位置->找最近的交互物->显示提示
            PerceptionTarget player = new PerceptionTarget(0, _session.Player.Position, true);
            IInteractable nearest = _interactions.FindNearest(player);
            hud.SetInteractionPrompt(nearest != null ? nearest.Prompt : null);
        }

        /// <summary>
        /// 守卫攻击结算：
        /// - 命中本体 → 统一失败流程（回检查点，不生成残影）；
        /// - 命中残影 → 只击杀残影，本体不受影响。
        /// AttackHit 是 GuardBrain 的一次性事件，处理完下一 tick 自动清掉。
        /// </summary>
        private void HandleGuardAttacks()
        {
            IReadOnlyList<GuardBrain> guards = _guards.Guards;
            for (int i = 0; i < guards.Count; i++)
            {
                GuardBrain guard = guards[i];
                if (!guard.AttackHit)
                {
                    continue;
                }
                //如果守卫攻击的目标是玩家，则触发失败（回溯到检查点）
                if (guard.AttackTargetIsPlayer)
                {
                    TriggerFailure();
                }
                else
                {
                    _session.Echoes.TryKill(guard.AttackTargetId);
                }
            }
        }

        /// <summary>
        /// 失败演出入口：先恢复检查点，再触发相机震动/HUD 提示。
        /// 这里没有延迟 0.8s 再恢复，因为 M1 优先保证“失败后立刻可操作”；
        /// 正式的 0.4s 扑击 + 0.8s 时间撕裂会在动画/后处理接入后补上。
        /// 冷却避免同一 tick 多个守卫重复触发。
        /// </summary>
        private void TriggerFailure()
        {
            if (_failureCooldown > 0f)
            {
                return;
            }

            _failureCooldown = 1.2f;
            _session.RestoreCheckpoint();
            ResetLifts();

            if (cameraRig != null)
            {
                cameraRig.Shake(0.35f, 0.35f);
            }

            if (hud != null)
            {
                hud.ShowStatus("被发现 — 时间回溯", 0.9f);
            }
        }

        /// <summary>
        /// 升降台：站在平台上时自动上升；离开后下降。
        /// 平台本 tick 的位移会同步给玩家，保证玩家跟着平台走，
        /// 但不改变水平碰撞/残影回放逻辑。
        /// </summary>
        private void UpdateLifts(float dt)
        {
            if (lifts == null)
            {
                return;
            }

            for (int i = 0; i < lifts.Length; i++)
            {
                LiftView lift = lifts[i];
                if (lift == null)
                {
                    continue;
                }

                bool onLift = lift.Contains(_session.Player.Position);
                bool active = lift.autoActivateOnStand && onLift;
                lift.Tick(dt, active);

                if (onLift)
                {
                    // 不累加 delta，直接把脚底对齐到平台顶面；
                    // 否则移动平台 + 重力会让玩家慢慢落在平台顶面下方，卡腿/抽搐。
                    float correction = lift.transform.position.y - _session.Player.Position.Y;
                    _session.Player.MoveWithPlatform(Vec3.Up * correction);
                }
            }
        }

        /// <summary>
        /// 回溯/失败时升降台必须回到底层，避免玩家回到检查点后
        /// 平台还停在上层，形成不可重复的状态。
        /// </summary>
        private void ResetLifts()
        {
            if (lifts == null)
            {
                return;
            }

            for (int i = 0; i < lifts.Length; i++)
            {
                if (lifts[i] != null)
                {
                    lifts[i].Reset();
                }
            }
        }

        /// <summary>
        /// 更新 L3 时间锁：每块板独立统计“本体 + 所有存活残影”，
        /// 然后写入固定槽位。这样做的好处是残影死亡/消散后，
        /// 下一 tick 自动把槽位置 false，不需要额外的清理回调。
        /// </summary>
        private void UpdateTimeLock()
        {
            if (timeLockPlates == null || timeLockPlates.Length == 0)
            {
                return;
            }

            int slotCount = _session.TimeLock.RequiredCount;
            for (int i = 0; i < timeLockPlates.Length && i < slotCount; i++)
            {
                PressurePlateView plate = timeLockPlates[i];
                if (plate == null)
                {
                    continue;
                }

                bool active = plate.Contains(_session.Player.Position);
                if (!active)
                {
                    for (int e = 0; e < _session.Echoes.Echoes.Count; e++)
                    {
                        EchoPlayback echo = _session.Echoes.Echoes[e];
                        if (echo.IsAlive && plate.Contains(echo.CurrentPosition))
                        {
                            active = true;
                            break;
                        }
                    }
                }

                plate.SetActive(active);
                _session.TimeLock.SetSource(i, active);//将踩到的某一个压力板传入Timelock中->加入到RequiredCount
            }

            //三源同开->开门
            if (timeLockDoor != null)
            {
                timeLockDoor.SetOpen(_session.TimeLock.IsOpen);
            }
        }

        /// <summary>
        /// 统计压力板上的激活源：本体 + 所有残影。
        /// 这里用几何包含判断，而不是 Unity Trigger，保证结果和 Domain 的
        /// “每 tick 重算”一致，也不依赖物理回调顺序。
        /// </summary>
        private int CountPlateActivators()
        {
            if (pressurePlate == null)
            {
                return 0;
            }

            int count = pressurePlate.Contains(_session.Player.Position) ? 1 : 0;

            //将残影列表从 Domain 传到 Presentation 层，避免直接访问 Domain 内部状态。
            IReadOnlyList<EchoPlayback> echoes = _session.Echoes.Echoes;

            for (int i = 0; i < echoes.Count; i++)
            {
                if (!echoes[i].IsAlive)
                {
                    continue;
                }

                if (pressurePlate.Contains(echoes[i].CurrentPosition))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 让 GameObject 列表跟随 Domain 的残影列表：
        /// - 新残影 → 创建 EchoView；
        /// - 现有残影 → 更新插值；
        /// - 被 FIFO 淘汰/轨迹结束的残影 → 销毁视图。
        /// </summary>
        private void SyncEchoViews(float alpha)
        {
            IReadOnlyList<EchoPlayback> echoes = _session.Echoes.Echoes;

            for (int i = 0; i < echoes.Count; i++)
            {
                EchoPlayback echo = echoes[i];
                // 没有残影，创建残影视图
                if (!_echoViews.TryGetValue(echo, out EchoView view))
                {
                    view = CreateEchoView(echo);
                    _echoViews.Add(echo, view);
                }

                view.Apply(alpha);
            }

            _removeBuffer.Clear();
            foreach (KeyValuePair<EchoPlayback, EchoView> pair in _echoViews)
            {
                // 现有残影不在 Domain 列表里，说明被淘汰了，收集起来等会儿删。
                if (!_session.Echoes.Contains(pair.Key))
                {
                    _removeBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < _removeBuffer.Count; i++)
            {
                EchoPlayback echo = _removeBuffer[i];
                if (_echoViews.TryGetValue(echo, out EchoView view))
                {
                    _echoViews.Remove(echo);
                    if (view != null)
                    {
                        // 先让消散爆发播完，再销毁根节点；否则粒子会随对象立刻消失。
                        view.PlayDespawnBurst();
                        Destroy(view.gameObject, 0.35f);
                    }
                }
            }
        }

        /// <summary>
        /// 残影视图是纯表现对象，不参与物理：
        /// - 根节点在脚底；
        /// - 有 echoVisualPrefab 时实例化正式的残影模型；没有时回退胶囊；
        /// - 模型/胶囊都作为子节点，EchoView 只同步根节点；
        /// - 移除视觉上的 Collider，避免挡住本体的胶囊扫掠。
        ///
        /// 注意：不要用 FindWithTag 去“找预制体”。Prefab 是资产，不在场景里；
        /// 而且多个残影会抢同一个场景对象。这里用序列化引用 + Instantiate。
        /// </summary>
        private EchoView CreateEchoView(EchoPlayback echo)
        {
            GameObject echoRoot = new GameObject("Echo_" + echo.Id);
            echoRoot.SetActive(false);

            EchoView view = echoRoot.AddComponent<EchoView>();
            CharacterVfx vfx = echoRoot.AddComponent<CharacterVfx>();

            GameObject visual;
            //实例化残影模型或回退胶囊（Prefab）
            if (echoVisualPrefab != null)
            {
                visual = Instantiate(echoVisualPrefab, echoRoot.transform, false);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(echoRoot.transform, false);
                visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            }

            // 视觉体不参与物理；模型自带 Collider 时也一并移除。
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
                Destroy(colliders[i]);
            }

            vfx.visualRoot = visual.transform;
            vfx.particleMaterial = echoVfxMaterial;
            vfx.primaryColor = new Color(0.15f, 0.85f, 1f);
            vfx.coreHeight = 0.95f;

            view.vfx = vfx;
            view.Initialize(visual.transform, visual.GetComponentInChildren<Animator>());
            view.Bind(echo);

            echoRoot.SetActive(true);
            return view;
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<EchoPlayback, EchoView> pair in _echoViews)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            _echoViews.Clear();
        }
    }
}
