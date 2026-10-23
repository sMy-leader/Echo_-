using Game.Application;
using Game.Domain;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// M0 调试 HUD。使用 OnGUI 是为了零依赖、快速验证；
    /// 正式 UI 会换成 UGUI/UI Toolkit + ViewModel。
    /// 这里只读 GameSession，不修改任何玩法状态。
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        private GameSession _session;
        private GuardSystem _guards;
        private GUIStyle _labelStyle;
        private string _statusMessage;
        private float _statusTime;

        public void Bind(GameSession session)
        {
            _session = session;
        }

        /// <summary>
        /// 守卫状态绑定。HUD 只读 GuardSystem，不参与感知/状态判定。
        /// </summary>
        public void BindGuards(GuardSystem guards)
        {
            _guards = guards;
        }

        /// <summary>
        /// 显示一次性状态提示（如“被发现 — 时间回溯”）。
        /// 用 unscaledDeltaTime，暂停/慢动作时提示仍然会消失。
        /// </summary>
        public void ShowStatus(string message, float duration = 0.9f)
        {
            _statusMessage = message;
            _statusTime = Mathf.Max(0.01f, duration);
        }

        private void Update()
        {
            if (_statusTime > 0f)
            {
                _statusTime = Mathf.Max(0f, _statusTime - Time.unscaledDeltaTime);
                if (_statusTime <= 0f)
                {
                    _statusMessage = null;
                }
            }
        }

        private void OnGUI()
        {
            if (_session == null)
            {
                return;
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    normal = { textColor = Color.white }
                };
            }

            GUILayout.BeginArea(new Rect(20f, 20f, 460f, 250f), GUI.skin.box);
            GUILayout.Label("ECHO / 残响 — M0/M1 Prototype", _labelStyle);
            GUILayout.Label(
                string.Format("Echoes: {0}/{1}", _session.Echoes.Count, Game.Core.GameConstants.MaxEchoes),
                _labelStyle);
            GUILayout.Label(
                string.Format(
                    "Record: {0:0.0}s / {1:0}s",
                    _session.InputRecorder.Count / 60f,
                    Game.Core.GameConstants.RecordLimitSeconds),
                _labelStyle);
            GUILayout.Label(
                _session.CanRewind ? "Rewind: [R] READY" : "Rewind: [R] record a run first",
                _labelStyle);
            GUILayout.Label(
                string.Format("Plate: {0}   Cooldown: {1:0.00}",
                    _session.Plate.IsActive ? "ON" : "off",
                    _session.RewindCooldown),
                _labelStyle);
            GUILayout.Label(
                string.Format("Grounded: {0}   Position: {1}",
                    _session.Player.IsGrounded ? "yes" : "no",
                    _session.Player.Position),
                _labelStyle);

            if (_guards != null && _guards.Guards.Count > 0)
            {
                GuardBrain guard = _guards.Guards[0];
                string target = guard.HasVisibleTarget
                    ? guard.VisibleTargetId.ToString()
                    : "-";
                float distance = (guard.Position - _session.Player.Position).Magnitude;

                GUILayout.Label(
                    string.Format(
                        "Guard: {0}   Suspicion: {1:0}   Target: {2}   Dist: {3:0.00}m",
                        guard.State,
                        guard.Suspicion,
                        target,
                        distance),
                    _labelStyle);
            }

            GUILayout.Label("WASD move · Shift sneak · R rewind", _labelStyle);

            if (!string.IsNullOrEmpty(_statusMessage) && _statusTime > 0f)
            {
                GUILayout.Space(6f);
                GUILayout.Label(_statusMessage, _labelStyle);
            }

            GUILayout.EndArea();
        }
    }
}
