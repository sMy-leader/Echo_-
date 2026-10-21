using Game.Application;
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
        private GUIStyle _labelStyle;

        public void Bind(GameSession session)
        {
            _session = session;
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

            GUILayout.BeginArea(new Rect(20f, 20f, 420f, 220f), GUI.skin.box);
            GUILayout.Label("ECHO / 残响 — M0 Vertical Slice", _labelStyle);
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
            GUILayout.Label("WASD move · Shift sneak · R rewind", _labelStyle);
            GUILayout.EndArea();
        }
    }
}
