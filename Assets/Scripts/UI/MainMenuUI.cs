using System;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 메인 타이틀 화면 (OnGUI 기반, 시연용). 제목과 시작 버튼만.
    /// 시작 버튼을 누르면 OnStartPressed가 발생하고 화면이 숨겨진다.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public string gameTitle = "PIXEL DUNGEON";

        /// <summary>시작 버튼을 눌렀을 때 발생.</summary>
        public event Action OnStartPressed;

        private bool isVisible = true;

        public void Show() => isVisible = true;
        public void Hide() => isVisible = false;

        private void OnGUI()
        {
            if (!isVisible) return;

            // 반투명 배경으로 화면을 덮어 타이틀에 집중되게 함
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 48,
                fontStyle = FontStyle.Bold,
            };
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 80), gameTitle, titleStyle);

            const float buttonWidth = 220f;
            const float buttonHeight = 56f;
            float bx = (Screen.width - buttonWidth) / 2f;
            float by = Screen.height * 0.55f;

            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };

            if (GUI.Button(new Rect(bx, by, buttonWidth, buttonHeight), "게임 시작", buttonStyle))
            {
                Hide();
                OnStartPressed?.Invoke();
            }

            if (GUI.Button(new Rect(bx, by + buttonHeight + 16, buttonWidth, buttonHeight), "종료", buttonStyle))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }
    }
}
