using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 室外固定俯视相机：俯角/偏航固定，只做跟随、滚轮缩放和潜行轻微拉近。
    ///
    /// 为什么不用 Cinemachine：
    /// - 室外俯视只需要稳定的构图，不需要多目标/复杂轨道；
    /// - 自定义 100 行即可，便于先验证玩法；
    /// - 室内第三人称原型时再引入 Cinemachine ThirdPersonFollow/Deoccluder。
    ///
    /// 逻辑位置由 PlayerMotor 决定，相机只读 Transform，不参与玩法判定。
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [Header("Framing")]
        public float distance = 16f;
        public float minDistance = 10f;
        public float maxDistance = 22f;
        public float pitch = 50f;
        public float yaw = 0f;
        public float followSpeed = 3.5f;

        [Header("Zoom / Sneak")]
        public float zoomSpeed = 8f;
        public float zoomStep = 1.5f;
        public float fieldOfView = 45f;
        public float sneakDistanceScale = 0.85f;
        public float sneakFieldOfView = 43f;

        [Header("Shake")]
        public float shakeFrequency = 26f;

        private Camera _camera;
        private float _currentDistance;
        private float _targetDistance;
        private float _currentFov;
        private bool _sneaking;

        private float _shakeDuration;
        private float _shakeTime;
        private float _shakeMagnitude;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _currentDistance = distance;
            _targetDistance = distance;
            _currentFov = fieldOfView;

            // 固定俯角/偏航，只有位置和 FOV 会变化。
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (_camera != null)
            {
                _camera.fieldOfView = fieldOfView;
            }
        }

        /// <summary>
        /// 鼠标滚轮缩放：只改变距离，不改变俯角，保证解谜构图稳定。
        /// delta 由 InputSystem 的 scroll.y 传入，已在调用方乘灵敏度。
        /// </summary>
        public void Zoom(float delta)
        {
            if (Mathf.Abs(delta) < 1e-4f)
            {
                return;
            }

            _targetDistance = Mathf.Clamp(_targetDistance + delta, minDistance, maxDistance);
        }

        /// <summary>
        /// 潜行时轻微拉近 + 收窄 FOV；不改变任何玩法判定。
        /// 由 GameLoop 根据 PlayerMotor.State 设置，离开潜行后平滑恢复。
        /// </summary>
        public void SetSneaking(bool sneaking)
        {
            _sneaking = sneaking;
        }

        /// <summary>
        /// 触发一次短震动。失败回溯/守卫扑击时由 GameLoop 调用；
        /// 幅度按剩余时间线性衰减，避免相机永久偏移。
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeTime = _shakeDuration;
            _shakeMagnitude = Mathf.Max(0f, magnitude);
        }

        /// <summary>
        /// 指数平滑跟随：t = 1 - exp(-speed * dt)，
        /// 与帧率无关，不会因为 30/60/120fps 改变跟随手感。
        /// </summary>
        public void Follow(Vector3 targetPosition, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }
            //潜行状态和走路状态分开用不同的距离和FOV，潜行状态下拉近相机距离和收窄FOV（此处收距离）
            float desiredDistance = _sneaking
                ? _targetDistance * sneakDistanceScale
                : _targetDistance;

            _currentDistance = Mathf.Lerp(
                _currentDistance,
                desiredDistance,
                1f - Mathf.Exp(-zoomSpeed * deltaTime));

            //同上判断，潜行状态下收窄FOV
            float desiredFov = _sneaking ? sneakFieldOfView : fieldOfView;
            _currentFov = Mathf.Lerp(
                _currentFov,
                desiredFov,
                1f - Mathf.Exp(-zoomSpeed * deltaTime));

            if (_camera != null)
            {
                _camera.fieldOfView = _currentFov;
            }

            // 从固定俯角/偏航反推相机相对目标的位置；distance 越大，画面越远。
            Vector3 offset = Quaternion.Euler(pitch, yaw, 0f) * (Vector3.back * _currentDistance);
            Vector3 desired = targetPosition + offset;

            float t = 1f - Mathf.Exp(-followSpeed * deltaTime);
            Vector3 next = Vector3.Lerp(transform.position, desired, t);

            if (_shakeTime > 0f)
            {
                _shakeTime = Mathf.Max(0f, _shakeTime - deltaTime);
                float falloff = _shakeTime / _shakeDuration;
                float wave = Mathf.Sin(Time.time * shakeFrequency) * _shakeMagnitude * falloff;
                next += new Vector3(wave, -wave * 0.5f, 0f);
            }

            transform.position = next;
        }
    }
}
