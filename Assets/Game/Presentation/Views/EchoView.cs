using Game.Core;
using Game.Domain;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 单个残影的表现层。数据来自 EchoPlayback，不参与物理/机关判定。
    /// 残影的位置与朝向都来自录制帧，所以它不会推箱子、不会被门挤变形。
    /// </summary>
    public sealed class EchoView : MonoBehaviour
    {
        [SerializeField] private float capsuleHalfHeight = 0.9f;
        [SerializeField] private Color echoColor = new Color(0.15f, 0.85f, 1f);

        private EchoPlayback _echo;
        private Renderer _renderer;

        public EchoPlayback Echo => _echo;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _renderer.material.color = echoColor;
            }
        }
        //绑定当前的残影数据
        public void Bind(EchoPlayback echo)
        {
            _echo = echo;
        }

        /// <summary>
        /// 在上一帧/当前帧之间插值。EchoPlayback 的 Previous/Current 已经
        /// 是逻辑 tick 的采样点，表现层不需要再采样录制器。
        /// </summary>
        public void Apply(float alpha)
        {
            if (_echo == null)
            {
                return;
            }

            Vec3 position = Vec3.Lerp(_echo.PreviousPosition, _echo.CurrentPosition, alpha);
            transform.position = new Vector3(position.X, position.Y + capsuleHalfHeight, position.Z);

            float yaw = Mathf.LerpAngle(_echo.PreviousYaw, _echo.CurrentYaw, alpha);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
