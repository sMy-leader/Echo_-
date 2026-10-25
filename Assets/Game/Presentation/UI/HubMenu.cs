using Cysharp.Threading.Tasks;
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
            float height = 330f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);
            GUILayout.Label("ECHO / 残响", _titleStyle);
            GUILayout.Space(12f);

            //接入Levelflow单例进行场景加载
            if (GUILayout.Button("Level 00 — 教学", _buttonStyle, GUILayout.Height(44f)))
            {
                LevelFlow.Instance.LoadScene("Level_00").Forget();
            }

            if (GUILayout.Button("Level 01 — 室外 / 激光", _buttonStyle, GUILayout.Height(44f)))
            {
                LevelFlow.Instance.LoadScene("Level_01").Forget();
            }

            if (GUILayout.Button("Level 02 — 室内第三人称原型", _buttonStyle, GUILayout.Height(44f)))
            {
                LevelFlow.Instance.LoadScene("Level_02_InteriorPrototype").Forget();
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("退出", _buttonStyle, GUILayout.Height(36f)))
            {
                UnityEngine.Application.Quit();
            }

            GUILayout.EndArea();
        }
    }
}
