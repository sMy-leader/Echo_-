using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 固定视角相机：俯角/偏航固定，只做轻微平滑跟随与震动预留。
    /// 不自由旋转，保证解谜构图稳定。
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0f, 16f, -12f);
        public float pitch = 50f;
        public float yaw = 0f;
        public float followSpeed = 3.5f;

        private void Awake()
        {
            // 固定旋转在 Awake 设一次，之后 Follow 只改位置。
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        /// <summary>
        /// 指数平滑跟随：t = 1 - exp(-speed * dt)，
        /// 与帧率无关，不会因为 30/60/120fps 改变跟随手感。
        /// </summary>
        public void Follow(Vector3 targetPosition, float deltaTime)
        {
            if (followSpeed <= 0f || deltaTime <= 0f)
            {
                return;
            }

            Vector3 desired = targetPosition + offset;
            float t = 1f - Mathf.Exp(-followSpeed * deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }
    }
}
