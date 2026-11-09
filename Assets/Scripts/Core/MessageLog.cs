using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DungeonCore
{
    /// <summary>
    /// 화면 좌하단에 최근 이벤트를 몇 줄 띄우는 메시지 로그. 콘솔(Debug.Log)과 달리
    /// 테스터가 바로 볼 수 있다. 어디서든 MessageLog.Post("...")로 한 줄 추가.
    ///
    /// 코드로 Canvas를 생성하므로 에디터 세팅 불필요. 입력을 가로채지 않도록
    /// raycastTarget을 끄고 GraphicRaycaster/EventSystem도 쓰지 않는다.
    /// 오래된 줄은 서서히 사라진다.
    /// </summary>
    public class MessageLog : MonoBehaviour
    {
        public static MessageLog Instance { get; private set; }

        // 자주 쓰는 색
        public static readonly Color Neutral = Color.white;
        public static readonly Color Good = new Color(0.5f, 0.9f, 0.5f);
        public static readonly Color Bad = new Color(1f, 0.5f, 0.45f);
        public static readonly Color Highlight = new Color(1f, 0.9f, 0.4f);

        [SerializeField] private int maxLines = 5;
        [SerializeField] private float holdSeconds = 4f;   // 이 시간 동안은 완전 불투명
        [SerializeField] private float fadeSeconds = 1.5f; // 그 뒤 서서히 사라짐
        [SerializeField] private float lineHeight = 28f;
        [SerializeField] private float fontSize = 20f;

        private struct Entry { public string text; public Color color; public float timeAdded; }

        private readonly List<Entry> entries = new();
        private TextMeshProUGUI[] lines;

        /// <summary>어디서든 호출 가능한 정적 진입점. Instance가 없으면 조용히 무시.</summary>
        public static void Post(string message, Color color)
        {
            if (Instance != null) Instance.Add(message, color);
        }

        public static void Post(string message) => Post(message, Neutral);

        private void Awake()
        {
            Instance = this;
            Build();
        }

        public void Add(string message, Color color)
        {
            entries.Add(new Entry { text = message, color = color, timeAdded = Time.time });
            // 너무 많이 쌓이지 않게 (화면에 안 보일 만큼 오래된 건 버림)
            if (entries.Count > maxLines * 2)
                entries.RemoveRange(0, entries.Count - maxLines * 2);
        }

        private void Build()
        {
            var canvasGO = new GameObject("MessageLogCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // HUD 위, 인벤토리/특성 같은 모달 창(110)보다는 아래
            var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // 너비·높이 절반씩 반영 - 16:9 외 비율에서도 안정적

            lines = new TextMeshProUGUI[maxLines];
            for (int i = 0; i < maxLines; i++)
            {
                var go = new GameObject($"Line_{i}", typeof(RectTransform));
                go.transform.SetParent(canvasGO.transform, false);
                var text = go.AddComponent<TextMeshProUGUI>();
                UIFont.ApplyTMP(text);
                text.fontSize = fontSize;
                text.alignment = TextAlignmentOptions.BottomLeft;
                text.raycastTarget = false; // 입력 통과
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Overflow;
                text.text = "";

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(900, lineHeight);
                rt.anchoredPosition = new Vector2(24, 24 + i * lineHeight); // i=0이 가장 아래(최신)

                lines[i] = text;
            }
        }

        private void Update()
        {
            if (lines == null) return;

            // 완전히 사라진(hold+fade 초과) 오래된 줄 제거
            float now = Time.time;
            float total = holdSeconds + fadeSeconds;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (now - entries[i].timeAdded > total)
                    entries.RemoveAt(i);

            int visible = Mathf.Min(entries.Count, maxLines);
            for (int li = 0; li < maxLines; li++)
            {
                if (li < visible)
                {
                    // li=0 = 가장 아래 = 최신 엔트리
                    var e = entries[entries.Count - 1 - li];
                    float age = now - e.timeAdded;
                    float alpha = age <= holdSeconds ? 1f : Mathf.Clamp01(1f - (age - holdSeconds) / fadeSeconds);
                    lines[li].text = e.text;
                    lines[li].color = new Color(e.color.r, e.color.g, e.color.b, alpha);
                }
                else
                {
                    lines[li].text = "";
                }
            }
        }
    }
}
