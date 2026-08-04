using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 캐릭터 선택 화면 (OnGUI 기반, 시연용).
    /// 시연 목적상 어느 버튼을 눌러도 게임이 시작된다 - 다만 나중에 실제 클래스별
    /// 스탯을 붙일 수 있도록, 선택된 CharacterClassData를 콜백으로 넘기는 구조로 둠.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        public List<CharacterClassData> classes = new();

        /// <summary>버튼을 눌렀을 때 발생. 선택된 클래스를 전달 (없으면 null).</summary>
        public event Action<CharacterClassData> OnClassChosen;

        private bool isVisible = true;

        public void Show() => isVisible = true;
        public void Hide() => isVisible = false;

        private void OnGUI()
        {
            if (!isVisible) return;

            const float buttonWidth = 200f;
            const float buttonHeight = 240f;
            const float spacing = 20f;

            int count = Mathf.Max(1, classes.Count);
            float totalWidth = count * buttonWidth + (count - 1) * spacing;
            float startX = (Screen.width - totalWidth) / 2f;
            float y = (Screen.height - buttonHeight) / 2f;

            // 타이틀
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
            };
            GUI.Label(new Rect(0, y - 80, Screen.width, 50), "캐릭터 선택", titleStyle);

            if (classes.Count == 0)
            {
                // 클래스 데이터가 없어도 시연은 되도록 기본 시작 버튼 하나 표시
                if (GUI.Button(new Rect((Screen.width - buttonWidth) / 2f, y, buttonWidth, buttonHeight), "시작"))
                {
                    Hide();
                    OnClassChosen?.Invoke(null);
                }
                return;
            }

            for (int i = 0; i < classes.Count; i++)
            {
                float x = startX + i * (buttonWidth + spacing);
                var rect = new Rect(x, y, buttonWidth, buttonHeight);

                if (GUI.Button(rect, ""))
                {
                    Hide();
                    OnClassChosen?.Invoke(classes[i]); // 어느 버튼이든 게임 시작 (시연용)
                }

                // 버튼 위에 초상화 + 이름 그리기
                var cls = classes[i];
                if (cls.portrait != null)
                {
                    var tex = cls.portrait.texture;
                    GUI.DrawTexture(new Rect(x + 40, y + 20, buttonWidth - 80, buttonWidth - 80), tex, ScaleMode.ScaleToFit);
                }
                var nameStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18,
                };
                GUI.Label(new Rect(x, y + buttonHeight - 50, buttonWidth, 30), cls.className, nameStyle);
            }
        }
    }
}
