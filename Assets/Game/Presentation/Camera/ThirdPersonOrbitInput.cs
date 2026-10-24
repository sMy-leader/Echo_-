using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation
{
    /// <summary>
    /// 第三人称 RPG 环绕输入：直接驱动 CinemachineOrbitalFollow 的水平/垂直轴。
    ///
    /// 为什么不用 CinemachineInputAxisController：
    /// - 项目已有 UnityInputService，鼠标输入不需要再建一套 InputActionAsset；
    /// - 直接写轴值更容易控制俯仰限制和灵敏度；
    /// - 相机逻辑留在 Presentation，不污染 Domain。
    ///
    /// 默认需要按住鼠标右键环绕，避免玩家移动鼠标时镜头乱转。
    /// </summary>
    public sealed class ThirdPersonOrbitInput : MonoBehaviour
    {
        public CinemachineOrbitalFollow orbit;

        [Header("Mouse")]
        public float yawSensitivity = 0.18f;
        public float pitchSensitivity = 0.15f;
        public bool requireRightMouse = true;

        [Header("Pitch limits")]
        public float minPitch = -25f;
        public float maxPitch = 60f;
        public float startPitch = 15f;

        [Header("Orbit")]
        public float radius = 6f;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            if (orbit == null)
            {
                orbit = GetComponent<CinemachineOrbitalFollow>();
            }

            if (orbit == null)
            {
                return;
            }

            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = radius;

            CinemachineOrbitalFollow orbitComponent = orbit;
            InputAxis horizontal = orbitComponent.HorizontalAxis;
            horizontal.Range = new Vector2(-180f, 180f);
            horizontal.Wrap = true;
            horizontal.Recentering.Enabled = false;
            orbitComponent.HorizontalAxis = horizontal;

            InputAxis vertical = orbitComponent.VerticalAxis;
            vertical.Range = new Vector2(minPitch, maxPitch);
            vertical.Center = Mathf.Clamp(startPitch, minPitch, maxPitch);
            vertical.Value = vertical.Center;
            vertical.Wrap = false;
            vertical.Recentering.Enabled = false;
            orbitComponent.VerticalAxis = vertical;

            _yaw = horizontal.Value;
            _pitch = vertical.Value;
        }

        private void Update()
        {
            if (orbit == null || Mouse.current == null)
            {
                return;
            }

            if (requireRightMouse && !Mouse.current.rightButton.isPressed)
            {
                return;
            }

            Vector2 delta = Mouse.current.delta.ReadValue();
            _yaw += delta.x * yawSensitivity;
            _pitch = Mathf.Clamp(
                _pitch - delta.y * pitchSensitivity,
                minPitch,
                maxPitch);

            InputAxis horizontal = orbit.HorizontalAxis;
            horizontal.Value = _yaw;
            orbit.HorizontalAxis = horizontal;

            InputAxis vertical = orbit.VerticalAxis;
            vertical.Value = _pitch;
            orbit.VerticalAxis = vertical;
        }
    }
}
