using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 灰盒场景的通用颜色组件：Awake 时把材质改成指定颜色。
    /// 仅用于 M0 快速区分地板/墙/检查点，正式美术会替换。
    /// </summary>
    public sealed class SceneColorView : MonoBehaviour
    {
        public Color color = Color.white;

        private void Awake()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }
}
