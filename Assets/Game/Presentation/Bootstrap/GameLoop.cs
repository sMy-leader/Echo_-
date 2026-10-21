using System.Collections.Generic;
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
        public GameHud hud;

        // EchoPlayback 是引用类型，直接作为字典键，避免额外 Id 映射表。
        private readonly Dictionary<EchoPlayback, EchoView> _echoViews =
            new Dictionary<EchoPlayback, EchoView>();

        // 遍历字典时不能直接删除，先收集再删。
        private readonly List<EchoPlayback> _removeBuffer = new List<EchoPlayback>();

        private UnityInputService _input;
        private GameSession _session;
        private float _accumulator;// 累积真实时间，驱动固定 tick 循环。

        /// <summary>
        /// 在 Awake 建 Session，保证第一帧 Update 之前已经有检查点。
        /// </summary>
        private void Awake()
        {
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

            if (hud != null)
            {
                hud.Bind(_session);
            }
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

                // 先处理回溯，再 Tick；这样回溯后的第一帧会从检查点开始录制。
                if (input.Has(InputButtons.Rewind))
                {
                    _session.TryRewind();
                }

                _session.Tick(GameConstants.TickDelta, input);

                // 掉出场景的兜底：M0 没有死亡系统，直接回检查点且不生成残影。
                if (_session.Player.Position.Y < -10f)
                {
                    _session.RestoreCheckpoint();
                }

                // 玩家和残影都移动完之后再统计，压力板看到的才是本 tick 的位置。
                _session.TickPlate(GameConstants.TickDelta, CountPlateActivators());
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

            if (cameraRig != null)
            {
                Vec3 playerPosition = _session.Player.Position;
                cameraRig.Follow(
                    new Vector3(playerPosition.X, playerPosition.Y, playerPosition.Z),
                    Time.unscaledDeltaTime);
            }

            if (pressurePlate != null)
            {
                pressurePlate.SetActive(_session.Plate.IsActive);
            }

            if (door != null)
            {
                door.SetOpen(_session.Plate.IsActive);
            }

            SyncEchoViews(alpha);
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
                        Destroy(view.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// 残影视图是纯表现对象，不参与物理：
        /// - 用 Capsule 只是借用默认网格；
        /// - 立刻禁用并销毁 Collider，避免它挡住本体的胶囊扫掠。
        /// </summary>
        private static EchoView CreateEchoView(EchoPlayback echo)
        {
            GameObject echoObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            echoObject.name = "Echo_" + echo.Id;
            echoObject.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

            Collider collider = echoObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            EchoView view = echoObject.AddComponent<EchoView>();
            view.Bind(echo);
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
