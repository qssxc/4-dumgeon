using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCore
{
    /// <summary>
    /// 개발용 인게임 콘솔. 백틱(`) 키로 열고 닫으며, 명령어를 입력해 런타임에
    /// 체력/스탯 조절, 몬스터 처치, 층 이동, 무적 토글 등을 할 수 있다.
    ///
    /// 명령어는 레지스트리 방식이라, 게임 쪽(DungeonBootstrap)에서 RegisterCommand로
    /// 자유롭게 추가한다. 콘솔 본체는 UI와 파싱만 담당하고 게임 로직은 모른다.
    /// </summary>
    public class DevConsole : MonoBehaviour
    {
        private class Command
        {
            public string help;
            public Action<string[]> handler;
        }

        private readonly Dictionary<string, Command> commands = new();
        private readonly List<string> log = new();
        private string input = "";
        private bool isOpen;
        private Vector2 scroll;
        private const string ControlName = "DevConsoleInput";

        public void RegisterCommand(string name, string help, Action<string[]> handler)
        {
            commands[name.ToLower()] = new Command { help = help, handler = handler };
        }

        public void Print(string message) => log.Add(message);

        private void Awake()
        {
            // 기본 내장 명령어
            RegisterCommand("help", "사용 가능한 명령어 목록", _ =>
            {
                Print("명령어 목록:");
                foreach (var kv in commands.OrderBy(k => k.Key))
                    Print($"  {kv.Key} - {kv.Value.help}");
            });
            RegisterCommand("clear", "콘솔 로그 지우기", _ => log.Clear());
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.backquoteKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;
                if (isOpen) input = "";
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            float h = Screen.height * 0.4f;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var logStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            float inputH = 28f;

            // 로그 영역 (스크롤)
            var logRect = new Rect(10, 6, Screen.width - 20, h - inputH - 16);
            float contentH = Mathf.Max(logRect.height, log.Count * 18f);
            scroll = GUI.BeginScrollView(logRect, scroll, new Rect(0, 0, logRect.width - 20, contentH));
            for (int i = 0; i < log.Count; i++)
                GUI.Label(new Rect(0, i * 18f, logRect.width - 20, 18f), log[i], logStyle);
            GUI.EndScrollView();

            // 입력 필드
            var inputRect = new Rect(10, h - inputH - 6, Screen.width - 20, inputH);

            // Enter 처리 (TextField가 이벤트를 소비하기 전에 감지)
            var e = Event.current;
            bool submit = e.type == EventType.KeyDown &&
                          (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

            GUI.SetNextControlName(ControlName);
            var fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 16 };
            input = GUI.TextField(inputRect, input, fieldStyle);
            GUI.FocusControl(ControlName);

            // 백틱 문자가 입력창에 섞이는 것 방지 (토글 키라서)
            input = input.Replace("`", "");

            if (submit && !string.IsNullOrWhiteSpace(input))
            {
                Execute(input);
                input = "";
                scroll.y = float.MaxValue; // 최신 로그로 스크롤
                e.Use();
            }
        }

        private void Execute(string line)
        {
            Print($"> {line}");
            var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string cmd = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (!commands.TryGetValue(cmd, out var command))
            {
                Print($"<color=#ff8080>알 수 없는 명령어: {cmd} (help 입력)</color>");
                return;
            }

            try
            {
                command.handler(args);
            }
            catch (Exception ex)
            {
                Print($"<color=#ff8080>오류: {ex.Message}</color>");
            }
        }
    }
}
