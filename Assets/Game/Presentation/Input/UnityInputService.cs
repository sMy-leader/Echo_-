using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation
{
    /// <summary>
    /// M0 输入适配器：把 Keyboard/Gamepad 的原始状态压缩成逻辑层的 InputFrame。
    ///
    /// 为什么放在 Presentation：
    /// - InputSystem 是 Unity API，Domain 不能引用；
    /// - 录制/回放只需要 InputFrame，不需要知道设备类型；
    /// - 后续换 InputActionAsset 时，只改这个类。
    /// </summary>
    public sealed class UnityInputService
    {
        // 边沿检测：Rewind/Pause 只在“按下那一帧”置位，避免按住时每 tick 重复触发。
        private bool _rewindHeld;
        private bool _pauseHeld;

        /// <summary>
        /// 鼠标滚轮输入，供 CameraRig.Zoom 使用。只影响表现层，不影响逻辑。
        /// 在 Update 中每帧读取一次，避免固定 tick 多次消费同一滚轮事件。
        /// </summary>
        public float ReadScroll()
        {
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        }

        public InputFrame Sample()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            float x = 0f;
            float y = 0f;
            InputButtons buttons = InputButtons.None;

            // 键盘：WASD 映射到屏幕方向；Shift 潜行；E 交互；Tab 时间轴。
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed) x += 1f;
                if (keyboard.sKey.isPressed) y -= 1f;
                if (keyboard.wKey.isPressed) y += 1f;

                if (keyboard.leftShiftKey.isPressed) buttons |= InputButtons.Sneak;
                if (keyboard.eKey.isPressed) buttons |= InputButtons.Interact;
                if (keyboard.tabKey.isPressed) buttons |= InputButtons.Timeline;
            }

            // 手柄：左摇杆移动；LB 潜行；A 交互；十字键上 时间轴。
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                x += stick.x;
                y += stick.y;

                if (gamepad.leftShoulder.isPressed) buttons |= InputButtons.Sneak;
                if (gamepad.buttonSouth.isPressed) buttons |= InputButtons.Interact;
                if (gamepad.dpad.up.isPressed) buttons |= InputButtons.Timeline;
            }

            // 键盘和手柄可能同时输入，先 clamp 到 [-1,1]；斜向由 PlayerMotor 归一化。
            x = Mathf.Clamp(x, -1f, 1f);
            y = Mathf.Clamp(y, -1f, 1f);

            bool rewind = (keyboard != null && keyboard.rKey.isPressed)
                || (gamepad != null && gamepad.buttonEast.isPressed);
            if (rewind && !_rewindHeld)
            {
                buttons |= InputButtons.Rewind;
            }

            _rewindHeld = rewind;

            bool pause = (keyboard != null && keyboard.escapeKey.isPressed)
                || (gamepad != null && gamepad.startButton.isPressed);
            if (pause && !_pauseHeld)
            {
                buttons |= InputButtons.Pause;
            }

            _pauseHeld = pause;

            return InputFrame.FromAxes(x, y, buttons);//最终返回玩家输入等数据作为采样后的帧数据
        }
    }
}
