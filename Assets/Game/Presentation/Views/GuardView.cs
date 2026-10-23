using System.Collections.Generic;
using Game.Core;
using Game.Domain;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 守卫的表现层。
    ///
    /// 约定与 PlayerView 一致：
    /// - 本 GameObject 在脚底中心，和 GuardBrain.Position 一致；
    /// - 模型/胶囊放在 visualRoot 子节点；
    /// - forwardIndicator 是朝前的视觉标记，解决胶囊从任何方向看都一样的问题。
    ///
    /// 有 Animator 时驱动参数；没有 Animator 时用程序化副动画占位。
    /// </summary>
    public sealed class GuardView : MonoBehaviour
    {
        [Header("Visual")]
        public Transform visualRoot;
        public Animator animator;
        public CharacterVfx vfx;

        [Header("Forward readability")]
        public Transform forwardIndicator;
        [SerializeField] private float viewDistance = 8f;
        [SerializeField] private float viewAngle = 60f;

        [Header("State colors")]
        public bool applyStateColorToVisual = true;
        [SerializeField] private Color patrolColor = new Color(0.45f, 0.55f, 0.65f);
        [SerializeField] private Color suspiciousColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color investigateColor = new Color(1f, 0.55f, 0.15f);
        [SerializeField] private Color alertColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private Color searchColor = new Color(0.75f, 0.35f, 1f);
        [SerializeField] private Color returnColor = new Color(0.2f, 0.85f, 0.5f);

        [Header("Patrol")]
        public Transform[] patrolPoints;

        [Header("Procedural fallback")]
        [SerializeField] private float bobAmplitude = 0.02f;
        [SerializeField] private float bobFrequency = 4f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        private Renderer _visualRenderer;
        private Renderer _forwardRenderer;
        private Vector3 _visualBasePosition;
        private Quaternion _visualBaseRotation;
        private bool _hasSeparateVisualRoot;
        private GuardState _lastState = GuardState.Patrol;
        private bool _wasAttacking;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _hasSeparateVisualRoot = visualRoot != transform;
            _visualBasePosition = visualRoot.localPosition;
            _visualBaseRotation = visualRoot.localRotation;
            _visualRenderer = visualRoot.GetComponentInChildren<Renderer>();

            if (animator == null)
            {
                animator = visualRoot.GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                // 玩法位置由 GuardBrain 决定，Animator 只做表现。
                animator.applyRootMotion = false;
            }
            _forwardRenderer = forwardIndicator != null
                ? forwardIndicator.GetComponentInChildren<Renderer>()
                : null;

            if (vfx == null)
            {
                vfx = GetComponent<CharacterVfx>();
            }

            ApplyStateColor(GuardState.Patrol);
        }

        /// <summary>
        /// 把巡逻点从 Unity Transform 转成 Domain 的 Vec3 列表。
        /// 路点 Transform 约定放在脚底高度；GuardBrain.Position 也是脚底中心。
        /// </summary>
        public void CollectPatrolPositions(List<Vec3> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                Vector3 feet = transform.position;
                buffer.Add(new Vec3(feet.x, feet.y, feet.z));
                return;
            }

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                {
                    continue;
                }

                Vector3 point = patrolPoints[i].position;
                buffer.Add(new Vec3(point.x, point.y, point.z));
            }
        }

        /// <summary>
        /// 根节点在脚底；visualRoot 承载模型；forwardIndicator 随根节点旋转。
        /// </summary>
        public void Apply(GuardBrain guard, float alpha)
        {
            if (guard == null)
            {
                return;
            }

            Vec3 feet = Vec3.Lerp(guard.PreviousPosition, guard.Position, alpha);
            transform.position = new Vector3(feet.X, feet.Y, feet.Z);
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(guard.PreviousYaw, guard.Yaw, alpha), 0f);

            if (vfx != null)
            {
                vfx.SetMoving(IsMoving(guard.State));
                vfx.SetAlert(guard.State == GuardState.Alert);
            }

            if (animator != null)
            {
                // Locomotion 只用 Speed：0 = Idle，1 = Jog；
                // Alert 保持 Idle，靠 VFX/颜色表达警戒，不单独做 6 个动画状态。
                animator.SetFloat(SpeedHash, IsMoving(guard.State) ? 1f : 0f);

                // Attack 用 Trigger：只在 IsAttacking 的上升沿触发一次，
                // 避免 0.4s 蓄力结束后 bool 变 false 把 punch 动画截断。
                if (guard.IsAttacking && !_wasAttacking)
                {
                    animator.SetTrigger(AttackHash);
                }

                _wasAttacking = guard.IsAttacking;
            }
            else if (_hasSeparateVisualRoot)
            {
                // 机械体占位：巡逻/调查时轻微上下浮动；Alert 时前倾一点。
                float bob = IsMoving(guard.State)
                    ? Mathf.Sin(Time.time * bobFrequency) * bobAmplitude
                    : 0f;
                float lean = guard.State == GuardState.Alert ? 6f : 0f;

                visualRoot.localPosition = _visualBasePosition + new Vector3(0f, bob, 0f);
                visualRoot.localRotation = _visualBaseRotation * Quaternion.Euler(lean, 0f, 0f);
            }

            if (_lastState != guard.State)
            {
                _lastState = guard.State;
                ApplyStateColor(guard.State);
            }
        }

        private static bool IsMoving(GuardState state)
        {
            // Alert 用 Idle + VFX/颜色表达；只有真正位移的状态才播 Jog。
            return state == GuardState.Patrol
                || state == GuardState.Investigate
                || state == GuardState.Return;
        }

        private void ApplyStateColor(GuardState state)
        {
            Color color = GetStateColor(state);

            if (applyStateColorToVisual && _visualRenderer != null)
            {
                _visualRenderer.material.color = color;
            }

            // 前向指示器始终跟随状态变色，保证顶部视角能读出朝向 + 警戒等级。
            if (_forwardRenderer != null)
            {
                _forwardRenderer.material.color = color;
            }
        }

        private Color GetStateColor(GuardState state)
        {
            switch (state)
            {
                case GuardState.Suspicious:
                    return suspiciousColor;
                case GuardState.Investigate:
                    return investigateColor;
                case GuardState.Alert:
                    return alertColor;
                case GuardState.Search:
                    return searchColor;
                case GuardState.Return:
                    return returnColor;
                default:
                    return patrolColor;
            }
        }

        /// <summary>
        /// Scene 视图调试：画出眼睛位置、前向线、视锥边界。
        /// 只在选中 Guard 时显示，不影响运行时画面。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            const float eyeHeight = 1.6f;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            float halfAngle = viewAngle * 0.5f;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(eye, 0.08f);

            Vector3 left = Quaternion.Euler(0f, -halfAngle, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, halfAngle, 0f) * transform.forward;

            Gizmos.DrawLine(eye, eye + left * viewDistance);
            Gizmos.DrawLine(eye, eye + right * viewDistance);
            Gizmos.DrawLine(eye + left * viewDistance, eye + right * viewDistance);
        }
    }
}
