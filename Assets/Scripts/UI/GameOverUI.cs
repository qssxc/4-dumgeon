using System;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 사망 시 뜨는 게임오버 화면 (OnGUI 기반, 메뉴 화면들과 같은 방식).
    /// 다시 시작 / 메인으로 버튼을 제공하고, 각각 콜백으로 전달한다.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        public event Action OnRestart;
        public event Action OnToMenu;

        private bool isVisible;
        private int reachedDepth = 1;

        public void Show(int depth)
        {
            isVisible = true;
            reachedDepth = depth;
        }

        public void Hide() => isVisible = false;

        private void OnGUI()
        {
            if (!isVisible) return;

            // 붉은 기 도는 어두운 배경
            GUI.color = new Color(0.1f, 0f, 0f, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 52,
                fontStyle = FontStyle.Bold,
            };
            titleStyle.normal.textColor = new Color(0.85f, 0.2f, 0.2f);
            GUI.Label(new Rect(0, Screen.height * 0.28f, Screen.width, 80), "YOU DIED", titleStyle);

            var subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
            };
            GUI.Label(new Rect(0, Screen.height * 0.28f + 90, Screen.width, 40),
                $"도달한 층: {reachedDepth}", subStyle);

            const float bw = 220f, bh = 56f;
            float bx = (Screen.width - bw) / 2f;
            float by = Screen.height * 0.55f;
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };

            if (GUI.Button(new Rect(bx, by, bw, bh), "다시 시작", buttonStyle))
            {
                Hide();
                OnRestart?.Invoke();
            }

            if (GUI.Button(new Rect(bx, by + bh + 16, bw, bh), "메인으로", buttonStyle))
            {
                Hide();
                OnToMenu?.Invoke();
            }
        }
    }
}
