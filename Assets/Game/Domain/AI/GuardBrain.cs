using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Domain
{
    /// <summary>
    /// 单个守卫的感知 + 状态机。
    ///
    /// 设计边界：
    /// - 纯 C#，不引用 UnityEngine；视线遮挡通过 IVisionWorld 注入；
    /// - 只负责“看到/怀疑/追/搜/回巡逻”，不负责动画、音效、UI；
    /// - M1 第一块直接用直线移动验证感知/状态机；L1 布局时再接入碰撞/寻路。
    ///
    /// 状态迁移（与 Docs/DesignRules.md 对应）：
    /// Patrol --看到目标--> Suspicious
    /// Suspicious --警戒>=60--> Investigate
    /// Suspicious --警戒>=100--> Alert
    /// Investigate --警戒>=100--> Alert
    /// Investigate --到达最后已知点--> Search
    /// Alert --丢失目标 6s--> Search
    /// Search --警戒>=100--> Alert
    /// Search --搜索 8s--> Return
    /// Return --到达最近巡逻点--> Patrol
    /// </summary>
    public sealed class GuardBrain
    {
        // 角度转换常量，避免每帧重复计算。
        private const float DegToRad = (float)(Math.PI / 180.0);
        private const float RadToDeg = (float)(180.0 / Math.PI);

        private readonly GuardConfig _config;
        private readonly Vec3[] _patrolPoints;

        private int _patrolIndex;
        private float _waypointWaitTimer;
        private float _visionTimer;
        private float _stateTimer;
        private float _lostSightTimer;
        private float _suspicion;

        private bool _hasVisibleTarget;
        private int _visibleTargetId = -1;
        private bool _visibleTargetIsPlayer;
        private Vec3 _lastKnownPosition;
        private int _lastKnownTargetId = -1;

        // 攻击状态：Alert 下进入范围后蓄力，蓄力结束产生一次 AttackHit。
        private bool _isAttacking;
        private float _attackTimer;
        private float _attackCooldown;
        private int _attackTargetId = -1;
        private bool _attackTargetIsPlayer;

        public int Id { get; }
        public Vec3 Position { get; private set; }
        public Vec3 PreviousPosition { get; private set; }
        public float Yaw { get; private set; }
        public float PreviousYaw { get; private set; }
        public GuardState State { get; private set; }

        public float Suspicion => _suspicion;
        public bool HasVisibleTarget => _hasVisibleTarget;
        public int VisibleTargetId => _visibleTargetId;
        public Vec3 LastKnownPosition => _lastKnownPosition;
        public int LastKnownTargetId => _lastKnownTargetId;
        public GuardConfig Config => _config;

        public bool IsAttacking => _isAttacking;
        public bool AttackHit { get; private set; }
        public int AttackTargetId => _attackTargetId;
        public bool AttackTargetIsPlayer => _attackTargetIsPlayer;

        public GuardBrain(
            int id,
            Vec3 spawn,
            float yaw,
            IReadOnlyList<Vec3> patrolPoints,
            GuardConfig config = null)
        {
            Id = id;
            Position = spawn;
            PreviousPosition = spawn;
            Yaw = NormalizeYaw(yaw);
            PreviousYaw = Yaw;
            _config = config ?? new GuardConfig();
            _lastKnownPosition = spawn;

            // 拷贝路点，避免外部 List 后续被修改导致守卫巡逻路线漂移。
            if (patrolPoints == null || patrolPoints.Count == 0)
            {
                _patrolPoints = new[] { spawn };
            }
            else
            {
                _patrolPoints = new Vec3[patrolPoints.Count];
                for (int i = 0; i < patrolPoints.Count; i++)
                {
                    _patrolPoints[i] = patrolPoints[i];
                }
            }

            State = GuardState.Patrol;// 初始状态为巡逻
        }

        /// <summary>
        /// 前进一个逻辑 tick。
        /// 顺序：记录上一帧 → 10Hz 视觉采样 → 警戒值 → 状态行为。
        /// 先更新感知再跑状态，保证本 tick 的状态判断使用的是最新可见性。
        /// </summary>
        public void Tick(float dt, IReadOnlyList<PerceptionTarget> targets, IVisionWorld vision)
        {
            //如果 dt <= 0，说明时间没有流逝，不需要更新状态机。
            if (dt <= 0f)
            {
                return;
            }

            PreviousPosition = Position;
            PreviousYaw = Yaw;
            AttackHit = false;

            if (_attackCooldown > 0f)
            {
                _attackCooldown = Math.Max(0f, _attackCooldown - dt);
            }

            UpdateVision(dt, targets, vision);
            UpdateSuspicion(dt);

            if (_isAttacking)
            {
                TickAttack(dt, targets);
                return;
            }

            // Alert 且目标在攻击范围：先蓄力，不立即扣血/失败。
            if (State == GuardState.Alert
                && _attackCooldown <= 0f
                && _hasVisibleTarget
                && (LastKnownPosition - Position).SqrMagnitude
                    <= _config.AttackRange * _config.AttackRange)
            {
                BeginAttack();
                TickAttack(dt, targets);
                return;
            }

            switch (State)
            {
                case GuardState.Patrol:
                    TickPatrol(dt);
                    break;
                case GuardState.Suspicious:
                    TickSuspicious(dt);
                    break;
                case GuardState.Investigate:
                    TickInvestigate(dt);
                    break;
                case GuardState.Alert:
                    TickAlert(dt);
                    break;
                case GuardState.Search:
                    TickSearch(dt);
                    break;
                case GuardState.Return:
                    TickReturn(dt);
                    break;
            }
        }

        /// <summary>
        /// 10Hz 视觉采样：
        /// 1. 距离；
        /// 2. 水平角；
        /// 3. 垂直角；
        /// 4. 视线遮挡；
        /// 5. 选最近的有效目标。
        ///
        /// 为什么不是每 tick 做：视锥/Linecast 比纯数学贵，10Hz 足够，
        /// 且警戒值仍然每 tick 累积，不会改变手感。
        /// </summary>
        private void UpdateVision(
            float dt,
            IReadOnlyList<PerceptionTarget> targets,
            IVisionWorld vision)
        {
            _visionTimer += dt;
            //如果 _visionTimer 小于采样间隔，说明还没到采样时间，不需要更新视觉信息。
            if (_visionTimer < _config.VisionSampleInterval)
            {
                return;
            }

            _visionTimer = 0f;
            _hasVisibleTarget = false;
            _visibleTargetId = -1;

            if (targets == null)
            {
                return;
            }

            Vec3 eye = Position + Vec3.Up * _config.EyeHeight;
            Vec3 forward = ForwardFromYaw(Yaw);
            float bestDistance = float.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                PerceptionTarget target = targets[i];//存入玩家的位置和状态信息
                if (!target.IsAlive)
                {
                    continue;
                }

                Vec3 aimPoint = target.Position + Vec3.Up * _config.TargetAimHeight;
                Vec3 toTarget = aimPoint - eye;
                Vec3 flat = new Vec3(toTarget.X, 0f, toTarget.Z);
                float flatDistance = flat.Magnitude;

                if (flatDistance > _config.ViewDistance)
                {
                    continue;
                }

                // 近身警觉：目标离得很近时，不要求它在视锥中心。
                // 这解决“守卫已经贴到玩家身上，反而因为垂直角度过大而看不见”的边界。
                bool closeRange = flatDistance <= _config.CloseAwarenessDistance;

                // 水平视锥：用 XZ 平面上的夹角，避免目标高度影响水平判断。
                if (!closeRange && flatDistance > 1e-4f)
                {
                    float horizontalAngle = (float)Math.Acos(
                        Clamp(Vec3.Dot(flat.Normalized, forward), -1f, 1f)) * RadToDeg;

                    if (horizontalAngle > _config.ViewHorizontalAngle * 0.5f)
                    {
                        continue;
                    }
                }

                // 垂直视锥：目标在眼睛上方/下方的角度。
                if (!closeRange)
                {
                    float verticalAngle = (float)Math.Abs(
                        Math.Atan2(toTarget.Y, Math.Max(flatDistance, 1e-4f))) * RadToDeg;

                    if (verticalAngle > _config.ViewVerticalAngle * 0.5f)
                    {
                        continue;
                    }
                }

                // 遮挡：from 眼睛，to 目标胸口。注意目标本身没有 Collider，
                // 所以 Linecast 不会打到目标自己。
                if (vision != null && !vision.HasLineOfSight(eye, aimPoint))
                {
                    continue;
                }

                if (flatDistance < bestDistance)
                {
                    bestDistance = flatDistance;
                    _hasVisibleTarget = true;
                    _visibleTargetId = target.Id;
                    _visibleTargetIsPlayer = target.IsPlayer;
                    _lastKnownPosition = target.Position;
                    _lastKnownTargetId = target.Id;
                }
            }

            if (_hasVisibleTarget)
            {
                _lostSightTimer = 0f;
            }
        }

        private void UpdateSuspicion(float dt)
        {
            // 警戒值每秒增加/减少，取决于是否看到目标。Clamp 保证在 [0, 100]。
            float delta = (_hasVisibleTarget
                ? _config.SuspicionRisePerSecond
                : -_config.SuspicionDecayPerSecond) * dt;

            _suspicion = Clamp(_suspicion + delta, 0f, 100f);
        }

        /// <summary>
        /// 开始一次蓄力攻击。锁定当前可见目标，蓄力期间不再移动。
        /// </summary>
        private void BeginAttack()
        {
            _isAttacking = true;
            _attackTimer = 0f;
            _attackTargetId = _visibleTargetId;
            _attackTargetIsPlayer = _visibleTargetIsPlayer;
        }

        /// <summary>
        /// 蓄力结束后只产生一次 AttackHit；Application 层据此决定
        /// “本体失败回溯”还是“残影死亡”。命中距离带 0.5m 容差，
        /// 避免目标刚好在边缘时视觉上打到、逻辑上却没中。
        /// </summary>
        private void TickAttack(float dt, IReadOnlyList<PerceptionTarget> targets)
        {
            _attackTimer += dt;
            FaceTowards(_lastKnownPosition);

            if (_attackTimer < _config.AttackWindupSeconds)
            {
                return;
            }

            bool hit = false;
            if (targets != null)
            {
                float range = _config.AttackRange + _config.AttackHitDistanceBuffer;
                float rangeSqr = range * range;

                for (int i = 0; i < targets.Count; i++)
                {
                    PerceptionTarget target = targets[i];
                    if (target.Id != _attackTargetId || !target.IsAlive)
                    {
                        continue;
                    }

                    if ((target.Position - Position).SqrMagnitude <= rangeSqr)
                    {
                        hit = true;
                        _lastKnownPosition = target.Position;
                    }

                    break;
                }
            }

            AttackHit = hit;
            _isAttacking = false;
            _attackCooldown = _config.AttackCooldownSeconds;
        }

        private void TickPatrol(float dt)
        {
            if (_hasVisibleTarget)
            {
                SetState(GuardState.Suspicious);
                return;
            }

            if (_patrolPoints.Length == 0)
            {
                return;
            }

            if (MoveTowards(_patrolPoints[_patrolIndex], _config.PatrolSpeed, dt))
            {
                _waypointWaitTimer += dt;
                if (_waypointWaitTimer >= _config.WaypointWaitSeconds)
                {
                    _waypointWaitTimer = 0f;
                    _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
                }
            }
            else
            {
                _waypointWaitTimer = 0f;
            }
        }

        private void TickSuspicious(float dt)
        {
            if (_hasVisibleTarget)
            {
                FaceTowards(_lastKnownPosition);
            }

            if (_suspicion >= _config.SuspicionAlertThreshold)
            {
                SetState(GuardState.Alert);
                return;
            }

            if (_suspicion >= _config.SuspicionInvestigateThreshold)
            {
                SetState(GuardState.Investigate);
                return;
            }

            if (!_hasVisibleTarget && _suspicion <= 0f)
            {
                SetState(GuardState.Patrol);
            }
        }

        private void TickInvestigate(float dt)
        {
            if (_suspicion >= _config.SuspicionAlertThreshold)
            {
                SetState(GuardState.Alert);
                return;
            }

            // 到达最后已知点后转入搜索；如果目标仍然可见，UpdateVision 会持续更新最后已知点。
            if (MoveTowards(_lastKnownPosition, _config.InvestigateSpeed, dt))
            {
                SetState(GuardState.Search);
            }
        }

        private void TickAlert(float dt)
        {
            if (_hasVisibleTarget)
            {
                _lostSightTimer = 0f;
            }
            else
            {
                _lostSightTimer += dt;
                if (_lostSightTimer >= _config.AlertLostSightSeconds)
                {
                    SetState(GuardState.Search);
                    return;
                }
            }

            MoveTowards(_lastKnownPosition, _config.AlertSpeed, dt, _config.MinApproachDistance);
        }

        private void TickSearch(float dt)
        {
            if (_suspicion >= _config.SuspicionAlertThreshold)
            {
                SetState(GuardState.Alert);
                return;
            }

            _stateTimer += dt;

            // 原地缓慢转头搜索；表现层会读出 Yaw 做旋转。
            Yaw = NormalizeYaw(Yaw + _config.SearchTurnSpeedDegrees * dt);

            if (_stateTimer >= _config.SearchDurationSeconds)
            {
                SetState(GuardState.Return);
            }
        }

        private void TickReturn(float dt)
        {
            if (_hasVisibleTarget)
            {
                SetState(GuardState.Suspicious);
                return;
            }

            int nearestIndex = FindNearestPatrolIndex();
            if (MoveTowards(_patrolPoints[nearestIndex], _config.ReturnSpeed, dt))
            {
                _patrolIndex = nearestIndex;
                SetState(GuardState.Patrol);
            }
        }

        /// <summary>
        /// 移动到目标点（只走 XZ 平面），到达 stopDistance 返回 true。
        /// stopDistance < 0 时使用 WaypointReachDistance。
        /// M1 第一块是直线移动；L1 布局时替换为胶囊碰撞/寻路，不影响状态机。
        /// </summary>
        private bool MoveTowards(Vec3 target, float speed, float dt, float stopDistance = -1f)
        {
            float stop = stopDistance >= 0f ? stopDistance : _config.WaypointReachDistance;
            Vec3 delta = target - Position;
            delta = new Vec3(delta.X, 0f, delta.Z);
            float distance = delta.Magnitude;

            if (distance <= stop)
            {
                FaceTowards(target);
                return true;
            }

            Vec3 direction = delta / distance;
            float step = Math.Min(speed * dt, distance - stop);
            Position += direction * step;
            FaceTowards(target);

            return distance - step <= stop;
        }

        private void FaceTowards(Vec3 target)
        {
            Vec3 delta = target - Position;
            if (Math.Abs(delta.X) < 1e-6f && Math.Abs(delta.Z) < 1e-6f)
            {
                return;
            }

            Yaw = NormalizeYaw((float)Math.Atan2(delta.X, delta.Z) * RadToDeg);
        }

        private int FindNearestPatrolIndex()
        {
            int bestIndex = 0;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                Vec3 delta = _patrolPoints[i] - Position;
                delta = new Vec3(delta.X, 0f, delta.Z);
                float sqr = delta.SqrMagnitude;

                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void SetState(GuardState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            _stateTimer = 0f;
            _lostSightTimer = 0f;

            if (next != GuardState.Alert)
            {
                _isAttacking = false;
            }

            if (next == GuardState.Patrol)
            {
                _waypointWaitTimer = 0f;
            }
        }

        private static Vec3 ForwardFromYaw(float yawDegrees)
        {
            float radians = yawDegrees * DegToRad;
            return new Vec3(
                (float)Math.Sin(radians),
                0f,
                (float)Math.Cos(radians));
        }

        private static float NormalizeYaw(float yawDegrees)
        {
            float normalized = yawDegrees % 360f;
            return normalized < 0f ? normalized + 360f : normalized;
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
