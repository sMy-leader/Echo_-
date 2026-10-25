using Game.Core;
using Game.Domain;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 拉杆表现 + IInteractable 适配层。
    /// 位置/颜色是 Unity 数据；开关状态放在 Domain.Lever。
    /// </summary>
    public sealed class LeverView : MonoBehaviour, IInteractable
    {
        public DoorView targetDoor;
        public float radius = 2.2f;
        public Color offColor = new Color(0.55f, 0.6f, 0.7f);
        public Color onColor = new Color(0.2f, 1f, 0.55f);

        private readonly Lever _lever = new Lever();
        private Renderer _renderer;
        private bool _isPowered;

        public bool IsOn => _lever.IsOn;
        public bool IsPowered => _isPowered;

        public Vec3 Position
        {
            get
            {
                Vector3 p = transform.position;
                return new Vec3(p.x, p.y, p.z);
            }
        }

        public float Radius => radius;

        public string Prompt
        {
            get
            {
                if (!_isPowered)
                {
                    return "拉杆无电";
                }

                return IsOn ? "按 E 关闭拉杆" : "按 E 拉开拉杆";
            }
        }

        /// <summary>
        /// 压力板供电状态由 GameLoop 每帧同步；无电时拉杆仍显示提示，但交互不生效。
        /// </summary>
        public void SetPowered(bool powered)
        {
            _isPowered = powered;
        }

        public bool CanInteract(int actorId)
        {
            return true;
        }

        public void Interact(int actorId)
        {
            if (!_isPowered)
            {
                return;
            }

            _lever.Toggle();
            ApplyColor();

            if (targetDoor != null)
            {
                targetDoor.SetOpen(_lever.IsOn);
            }
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (_renderer != null)
            {
                _renderer.material.color = _lever.IsOn ? onColor : offColor;
            }
        }
    }
}
