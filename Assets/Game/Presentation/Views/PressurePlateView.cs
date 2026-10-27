using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 压力板表现 + 几何包含判断。
    ///
    /// 为什么用 Contains 而不是 OnTriggerEnter：
    /// - 残影是轨迹回放，不一定有 Collider；
    /// - Domain 每 tick 重算激活源，几何判断更直接、可预测；
    /// - 避免物理回调顺序影响机关结果。
    /// </summary>
    public sealed class PressurePlateView : MonoBehaviour
    {
        public Vector3 halfExtents = new Vector3(1f, 0.5f, 1f);
        public Color activeColor = new Color(0.2f, 1f, 0.6f);
        public Color inactiveColor = new Color(0.9f, 0.75f, 0.2f);

        [Header("Optional feedback (assets can be assigned later)")]
        public AudioSource audioSource;
        public AudioClip activateClip;
        public AudioClip deactivateClip;
        public ParticleSystem activateVfx;
        public ParticleSystem deactivateVfx;

        private Renderer _renderer;
        private bool _active;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            ApplyColor(false);
        }

        /// <summary>
        /// feetPosition 是胶囊脚底中心。Y 方向给较宽松的范围，
        /// 因为玩家/残影的 Y 会受 SkinWidth 和地面高度影响。
        /// </summary>
        public bool Contains(Vec3 feetPosition)
        {
            Vector3 center = transform.position;

            return Mathf.Abs(feetPosition.X - center.x) <= halfExtents.x
                && Mathf.Abs(feetPosition.Z - center.z) <= halfExtents.z
                && feetPosition.Y >= center.y - halfExtents.y
                && feetPosition.Y <= center.y + halfExtents.y + 1.5f;
        }

        public void SetActive(bool active)
        {
            if (_active == active)
            {
                return;
            }

            _active = active;
            ApplyColor(active);
            PlayFeedback(active);
        }

        /// <summary>
        /// 可选反馈：素材未导入时所有引用都是 null，直接跳过。
        /// 以后在 Inspector 里拖 AudioClip/ParticleSystem 即可，不需要改代码。
        /// </summary>
        private void PlayFeedback(bool active)
        {
            AudioClip clip = active ? activateClip : deactivateClip;
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }

            ParticleSystem vfx = active ? activateVfx : deactivateVfx;
            if (vfx != null)
            {
                vfx.Play();
            }
        }

        private void ApplyColor(bool active)
        {
            if (_renderer != null)
            {
                Color color = active ? activeColor : inactiveColor;
                _renderer.material.color = color;
                // URP Lit 实际使用 _BaseColor；只改 color 会保持白色。
                _renderer.material.SetColor("_BaseColor", color);
            }
        }
    }
}
