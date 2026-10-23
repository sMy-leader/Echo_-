using Game.Core;
using Game.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Presentation
{
    /// <summary>
    /// 单个残影的表现层。
    ///
    /// 约定与 PlayerView 一致：根节点在脚底，模型/胶囊放在 visualRoot 子节点。
    /// 这样未来可以把胶囊换成玩家模型的“幽灵版本”，只改 Prefab，不改 Domain。
    ///
    /// M1/M2 默认走程序化副动画；如果以后要完整 Mecanim 回放，
    /// 需要把 Animator 状态/时间写进 EchoFrame，当前 16 字节帧没有这些数据。
    /// </summary>
    public sealed class EchoView : MonoBehaviour
    {
        [Header("Visual")]
        public Transform visualRoot;
        public Animator animator;
        public CharacterVfx vfx;

        [Header("Ghost color")]
        public bool applyColorToVisual = true;
        [SerializeField] private Color echoColor = new Color(0.15f, 0.85f, 1f);
        [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.35f;

        [Header("Procedural fallback")]
        [SerializeField] private float bobAmplitude = 0.025f;
        [SerializeField] private float bobFrequency = 5f;
        [SerializeField] private float tiltAmplitude = 1.5f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int StateHash = Animator.StringToHash("State");

        private EchoPlayback _echo;
        private Vector3 _visualBasePosition;
        private Quaternion _visualBaseRotation;
        private bool _hasSeparateVisualRoot;
        private bool _spawnBurstPlayed;

        public EchoPlayback Echo => _echo;

        private void Awake()
        {
            ResolveVisual();
        }

        /// <summary>
        /// 运行时创建残影时，GameLoop 在 AddComponent 之后调用，
        /// 把程序化生成的胶囊/加载的幽灵 Prefab 设为 visualRoot。
        /// </summary>
        public void Initialize(Transform root, Animator animator)
        {
            visualRoot = root;
            this.animator = animator;
            ResolveVisual();
        }

        public void Bind(EchoPlayback echo)
        {
            _echo = echo;
        }

        /// <summary>
        /// 残影被击杀/轨迹结束、视图即将销毁前调用。
        /// 爆发粒子挂在根节点下，先 Emit 再延迟销毁才能看到消散。
        /// </summary>
        public void PlayDespawnBurst()
        {
            if (vfx != null)
            {
                vfx.SetMoving(false);
                vfx.PlayDespawnBurst();
            }
        }

        private void ResolveVisual()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _hasSeparateVisualRoot = visualRoot != transform;
            _visualBasePosition = visualRoot.localPosition;
            _visualBaseRotation = visualRoot.localRotation;

            if (applyColorToVisual)
            {
                ApplyGhostMaterial();
            }
        }

        /// <summary>
        /// 把 URP Lit 材质切到透明 Alpha Blend，并把整体染成青色。
        /// 这是 M1.5 的运行时占位；正式美术可以换成专用 Ghost Shader。
        /// </summary>
        private void ApplyGhostMaterial()
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>();
            Color color = echoColor;
            color.a = ghostAlpha;

            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].material;
                if (material == null)
                {
                    continue;
                }

                material.SetColor("_BaseColor", color);
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
        }

        /// <summary>
        /// 根节点在脚底；视觉子节点做轻微浮动/倾斜，表现“不稳定残影”。
        /// </summary>
        public void Apply(float alpha)
        {
            if (_echo == null)
            {
                return;
            }

            Vec3 feet = Vec3.Lerp(_echo.PreviousPosition, _echo.CurrentPosition, alpha);
            transform.position = new Vector3(feet.X, feet.Y, feet.Z);
            transform.rotation = Quaternion.Euler(
                0f,
                Mathf.LerpAngle(_echo.PreviousYaw, _echo.CurrentYaw, alpha),
                0f);

            EchoState state = _echo.Current.EchoState;

            if (vfx != null)
            {
                vfx.SetMoving(state == EchoState.Walk || state == EchoState.Sneak);

                if (!_spawnBurstPlayed)
                {
                    vfx.PlaySpawnBurst();
                    _spawnBurstPlayed = true;
                }
            }

            if (animator != null)
            {
                animator.SetFloat(SpeedHash, state == EchoState.Walk ? 1f : (state == EchoState.Sneak ? 0.5f : 0f));
                animator.SetInteger(StateHash, (int)state);
                return;
            }

            if (!_hasSeparateVisualRoot)
            {
                return;
            }

            float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            float tilt = Mathf.Sin(Time.time * bobFrequency * 0.5f) * tiltAmplitude;

            visualRoot.localPosition = _visualBasePosition + new Vector3(0f, bob, 0f);
            visualRoot.localRotation = _visualBaseRotation * Quaternion.Euler(tilt, 0f, 0f);
        }
    }
}
