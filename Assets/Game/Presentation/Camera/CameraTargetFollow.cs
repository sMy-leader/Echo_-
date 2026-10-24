using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 世界空间相机目标：只跟随玩家位置，不继承玩家旋转。
    ///
    /// 如果直接把 CameraTarget 挂在玩家下面，角色按 A 转身时目标也会旋转，
    /// Cinemachine 会跟着转，表现为“镜头瞬间跳转”。
    /// 这里用 LateUpdate 同步位置，保持相机轨道独立于角色朝向。
    /// </summary>
    public sealed class CameraTargetFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 1.2f, 0f);

        private void LateUpdate()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }
    }
}
