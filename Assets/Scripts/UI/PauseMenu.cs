using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCore
{
    /// <summary>
    /// ESC로 열고 닫는 일시정지 메뉴 (OnGUI 기반, 게임오버/메인 메뉴와 같은 방식).
    /// 계속하기 / 다시 시작 / 메인 메뉴를 제공한다.
    /// 열려 있는 동안 PlayerController가 IsOpen을 보고 입력을 막는다.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        public event Action OnRestart;
        public event Action OnToMenu;

        /// <summary>게임오버 등으로 일시정지를 막아야 할 때 true (ESC 무시).</summary>
        public bool Suppressed { get; set; }

        private void Awake()
        {
            IsOpen = false; // 씬 재로드 시 정적 값이 남아있지 않도록 초기화
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (Suppressed) return;
                if (IsOpen) Hide(); else Show();
            }
        }

        public void Show() => IsOpen = true;
        public void Hide() => IsOpen = false;
        private void OnDisable() => IsOpen = false;

        private void OnGUI()
        {
            if (!IsOpen) return;
            GUI.skin.font = UIFont.Get();

            // 어두운 반투명 배경
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 44,
                fontStyle = FontStyle.Bold,
            };
            GUI.Label(new Rect(0, Screen.height * 0.26f, Screen.width, 70), "일시정지", titleStyle);

            const float bw = 240f, bh = 56f, gap = 14f;
            float bx = (Screen.width - bw) / 2f;
            float by = Screen.height * 0.46f;
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };

            if (GUI.Button(new Rect(bx, by, bw, bh), "계속하기 (ESC)", buttonStyle))
                Hide();

            if (GUI.Button(new Rect(bx, by + (bh + gap), bw, bh), "다시 시작", buttonStyle))
            {
                Hide();
                OnRestart?.Invoke();
            }

            if (GUI.Button(new Rect(bx, by + (bh + gap) * 2, bw, bh), "메인 메뉴", buttonStyle))
            {
                Hide();
                OnToMenu?.Invoke();
            }
        }
    }
}
