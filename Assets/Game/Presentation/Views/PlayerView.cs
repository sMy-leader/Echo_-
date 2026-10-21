using Game.Core;
using Game.Domain;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 玩家胶囊的表现层。只读 PlayerMotor 的 Previous/Current，做插值。
    /// 逻辑位置是脚底中心，所以显示时要抬高半个胶囊。
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private float capsuleHalfHeight = 0.9f;
        [SerializeField] private Color playerColor = new Color(1f, 0.55f, 0.15f);

        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _renderer.material.color = playerColor;
            }
        }

        /// <summary>
        /// alpha=0 显示上一 tick，alpha=1 显示当前 tick。
        /// 用 LerpAngle 处理 359°→0° 的环绕。
        /// </summary>
        public void Apply(PlayerMotor player, float alpha)
        {
            Vec3 position = Vec3.Lerp(player.PreviousPosition, player.Position, alpha);
            transform.position = new Vector3(position.X, position.Y + capsuleHalfHeight, position.Z);

            float yaw = Mathf.LerpAngle(player.PreviousYaw, player.Yaw, alpha);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
