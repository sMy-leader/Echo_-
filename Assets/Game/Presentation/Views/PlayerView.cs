using Game.Core;
using Game.Domain;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 玩家的表现层。
    ///
    /// 重要约定：
    /// - 本 GameObject 的 Transform 在“脚底中心”，和 PlayerMotor.Position 一致；
    /// - 模型/胶囊放在 visualRoot 子节点下，pivot 也应在脚底；
    /// - 逻辑胶囊尺寸由 PlayerMotor.Radius/Height 决定，视觉模型只负责好看。
    ///
    /// 这样换美术时只换 visualRoot 下的模型和 Animator，不改 PlayerMotor。
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        [Header("Visual")]
        public Transform visualRoot;
        public Animator animator;
        public CharacterVfx vfx;

        [Header("Fallback color")]
        public bool applyColorToVisual = true;
        [SerializeField] private Color playerColor = new Color(1f, 0.55f, 0.15f);

        [Header("Procedural fallback")]
        [SerializeField] private float bobAmplitude = 0.03f;
        [SerializeField] private float bobFrequency = 6f;
        [SerializeField] private float tiltAmplitude = 2f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int SneakingHash = Animator.StringToHash("Sneaking");

        private Renderer _renderer;
        private Vector3 _visualBasePosition;
        private Quaternion _visualBaseRotation;
        private bool _hasSeparateVisualRoot;
        private bool _hasSneakingParameter;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _hasSeparateVisualRoot = visualRoot != transform;
            _visualBasePosition = visualRoot.localPosition;
            _visualBaseRotation = visualRoot.localRotation;
            _renderer = visualRoot.GetComponentInChildren<Renderer>();

            if (animator == null)
            {
                animator = visualRoot.GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                // 玩法位置由 PlayerMotor 决定，Animator 只做表现。
                animator.applyRootMotion = false;
                // 兼容当前“离散状态”Controller；换成 1D BlendTree 后自动跳过。
                _hasSneakingParameter = HasParameter(animator, SneakingHash);
            }

            if (vfx == null)
            {
                vfx = GetComponent<CharacterVfx>();
            }

            if (applyColorToVisual && _renderer != null)
            {
                _renderer.material.color = playerColor;
            }
        }

        /// <summary>
        /// 检查 Animator 是否真的存在某个参数。
        /// 用于兼容旧离散状态机（Sneaking）和新 1D BlendTree（只吃 Speed），
        /// 避免给不存在的参数 SetBool 刷 Console 错误。
        /// </summary>
        private static bool HasParameter(Animator animator, int nameHash)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == nameHash)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 逻辑状态 → 视觉状态。
        /// 有 Animator 时驱动参数；没有 Animator 时用程序化浮动/倾斜做占位。
        /// </summary>
        public void Apply(PlayerMotor player, float alpha)
        {
            Vec3 feet = Vec3.Lerp(player.PreviousPosition, player.Position, alpha);

            // 根节点永远在脚底，模型作为子节点。
            transform.position = new Vector3(feet.X, feet.Y, feet.Z);
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(player.PreviousYaw, player.Yaw, alpha), 0f);

            bool moving = player.State == EchoState.Walk || player.State == EchoState.Sneak;

            if (vfx != null)
            {
                vfx.SetMoving(moving);
            }

            if (animator != null)
            {
                // 1D BlendTree：0 = Idle，0.5 = Sneak，1 = Walk。
                animator.SetFloat(SpeedHash, player.State == EchoState.Walk ? 1f : (player.State == EchoState.Sneak ? 0.5f : 0f));

                // 过渡期兼容旧的离散状态机；新 BlendTree 不需要 Sneaking。
                if (_hasSneakingParameter)
                {
                    animator.SetBool(SneakingHash, player.State == EchoState.Sneak);
                }

                return;
            }

            if (!_hasSeparateVisualRoot)
            {
                return;
            }

            // 无 Animator 的灰盒/机械体：轻微上下浮动 + 前倾。
            float bob = moving ? Mathf.Sin(Time.time * bobFrequency) * bobAmplitude : 0f;
            float tilt = moving ? Mathf.Sin(Time.time * bobFrequency * 0.5f) * tiltAmplitude : 0f;

            visualRoot.localPosition = _visualBasePosition + new Vector3(0f, bob, 0f);
            visualRoot.localRotation = _visualBaseRotation * Quaternion.Euler(tilt, 0f, 0f);
        }
    }
}
