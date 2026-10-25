using Game.Core;

namespace Game.Core
{
    /// <summary>
    /// 可交互物契约。纯 C#，不依赖 Unity；
    /// Presentation 用 Transform 适配，Application 负责找最近目标。
    /// </summary>
    public interface IInteractable
    {
        Vec3 Position { get; }
        float Radius { get; }
        string Prompt { get; }
        bool CanInteract(int actorId);
        void Interact(int actorId);
    }
}
