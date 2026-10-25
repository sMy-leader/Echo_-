namespace Game.Domain
{
    /// <summary>
    /// 拉杆的纯逻辑状态：一次交互切换开/关。
    /// 后续接终端/开关时，可以复用“状态 + Toggle”的模式。
    /// </summary>
    public sealed class Lever
    {
        public bool IsOn { get; private set; }

        public void Toggle()
        {
            IsOn = !IsOn;
        }

        public void Reset()
        {
            IsOn = false;
        }
    }
}
