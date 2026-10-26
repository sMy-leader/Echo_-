using Cysharp.Threading.Tasks;
using Game.Application;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// MainMenu 大厅占位 UI：先用 OnGUI，正式 UI 后续换 UGUI/UI Toolkit。
    /// 关卡全部可选，解锁/存档在流程骨架稳定后再接。
    /// </summary>
    public sealed class HubMenu : MonoBehaviour
    {
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;

        private void OnGUI()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 34,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };

                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 20
                };
            }

            float width = 420f;
            float height = 430f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);
            GUILayout.Label("ECHO / 残响", _titleStyle);
            GUILayout.Space(12f);

            DrawLevelButton(LevelIds.Level00, "Level 00 — 教学", "Level_00");
            DrawLevelButton(LevelIds.Level01, "Level 01 — 室外 / 激光", "Level_01");
            DrawLevelButton(LevelIds.Level02, "Level 02 — 室内第三人称原型", "Level_02_InteriorPrototype");

            GUI.enabled = false;
            GUILayout.Button("Level 03 — 正式 Kit 关（待制作）", _buttonStyle, GUILayout.Height(44f));
            GUI.enabled = true;

            GUILayout.Space(8f);
            if (GUILayout.Button("解锁全部（调试）", _buttonStyle, GUILayout.Height(32f)))
            {
                LevelFlow.Instance.Progress.MarkCompleted(LevelIds.Level00);
                LevelFlow.Instance.Progress.MarkCompleted(LevelIds.Level01);
                LevelFlow.Instance.Progress.MarkCompleted(LevelIds.Level02);
            }

            if (GUILayout.Button("退出", _buttonStyle, GUILayout.Height(36f)))
            {
                UnityEngine.Application.Quit();
            }

            GUILayout.EndArea();
        }

        private void DrawLevelButton(string levelId, string label, string sceneName)
        {
            bool unlocked = LevelFlow.Instance.Progress.IsUnlocked(levelId);
            GUI.enabled = unlocked;

            string text = unlocked ? label : label + "  [LOCKED]";
            if (GUILayout.Button(text, _buttonStyle, GUILayout.Height(44f)))
            {
                LevelFlow.Instance.LoadScene(sceneName).Forget();
            }

            GUI.enabled = true;
        }
    }
}
