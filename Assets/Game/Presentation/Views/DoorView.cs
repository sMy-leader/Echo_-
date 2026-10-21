using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 门的表现层：只根据机关状态在“关/开”两个位置之间移动。
    /// BoxCollider 会跟着 Transform 一起移动，所以门升起后玩家可以通过。
    /// 这是纯表现，不参与“门是否该开”的判定。
    /// </summary>
    public sealed class DoorView : MonoBehaviour
    {
        public Vector3 openOffset = new Vector3(0f, 3f, 0f);
        public float speed = 4f;
        public Color closedColor = new Color(0.9f, 0.3f, 0.2f);
        public Color openColor = new Color(0.2f, 0.9f, 1f);

        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private bool _isOpen;
        private Renderer _renderer;

        private void Awake()
        {
            // 记录编辑态位置作为“关门位置”，之后只改 Transform.position。
            _closedPosition = transform.position;
            _openPosition = _closedPosition + openOffset;
            _renderer = GetComponent<Renderer>();
            ApplyColor(false);
        }

        public void SetOpen(bool open)
        {
            if (_isOpen == open)
            {
                return;
            }

            _isOpen = open;
            ApplyColor(open);
        }

        private void Update()
        {
            // 表现层用渲染帧时间，不影响 60Hz 的逻辑判定。
            Vector3 target = _isOpen ? _openPosition : _closedPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }

        private void ApplyColor(bool open)
        {
            if (_renderer != null)
            {
                _renderer.material.color = open ? openColor : closedColor;
            }
        }
    }
}
