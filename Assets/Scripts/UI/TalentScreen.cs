using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

namespace DungeonCore
{
    /// <summary>
    /// 특성 창 (Canvas). K로 열고 닫으며, 각 특성에 포인트를 투자한다.
    /// 코드로 UI를 생성하므로 에디터 세팅 불필요. 마우스 클릭 또는 숫자키(1~4)로 투자.
    /// </summary>
    public class TalentScreen : MonoBehaviour
    {
        public PlayerController player;

        private Canvas canvas;
        private GameObject panelRoot;
        private bool isOpen;

        private TextMeshProUGUI pointsText;
        private readonly List<TalentRow> rows = new();

        private class TalentRow
        {
            public int index;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI descText;
            public Image[] pips;
            public Button investButton;
            public TextMeshProUGUI buttonText;
        }

        private static readonly Color RowBg = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        private static readonly Color PipOn = new Color(0.4f, 0.8f, 0.4f);
        private static readonly Color PipOff = new Color(0.3f, 0.3f, 0.33f);

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.kKey.wasPressedThisFrame)
                Toggle();

            if (!isOpen || kb == null) return;

            // 숫자키 1~4로 투자 단축
            var digits = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key };
            for (int i = 0; i < digits.Length && i < rows.Count; i++)
                if (digits[i].wasPressedThisFrame)
                    TryInvest(i);
        }

        private void Toggle()
        {
            isOpen = !isOpen;
            if (isOpen)
            {
                EnsureBuilt();
                panelRoot.SetActive(true);
                Refresh();
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void TryInvest(int i)
        {
            if (player.Talents == null) return;
            if (player.Talents.Invest(i))
                Refresh();
        }

        private void EnsureBuilt()
        {
            if (canvas != null) return;

            var canvasGO = new GameObject("TalentCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // 너비·높이 절반씩 반영 - 16:9 외 비율에서도 안정적

            EnsureEventSystem();

            panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGO.transform, false);
            var pImg = panelRoot.GetComponent<Image>();
            pImg.color = new Color(0f, 0f, 0f, 0.7f);
            var prt = panelRoot.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

            // 타이틀 + 포인트
            MakeText(panelRoot.transform, "특성   (K: 닫기 / 클릭·숫자키: 투자)", new Vector2(0, -40), 900, 40, 28, TextAlignmentOptions.Top, new Vector2(0.5f, 1f));
            pointsText = MakeText(panelRoot.transform, "", new Vector2(0, -90), 900, 32, 22, TextAlignmentOptions.Top, new Vector2(0.5f, 1f));

            BuildRows();
        }

        private void BuildRows()
        {
            rows.Clear();
            var talents = player.Talents;
            if (talents == null) return;

            float rowH = 110f, rowW = 720f, spacing = 14f;
            float totalH = talents.Count * (rowH + spacing);
            float startY = totalH / 2f - rowH / 2f;

            for (int i = 0; i < talents.Count; i++)
            {
                var d = talents.GetTalent(i);

                var rowGO = new GameObject($"Talent_{i}", typeof(RectTransform), typeof(Image));
                rowGO.transform.SetParent(panelRoot.transform, false);
                rowGO.GetComponent<Image>().color = RowBg;
                var rrt = rowGO.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0.5f, 0.5f);
                rrt.anchorMax = new Vector2(0.5f, 0.5f);
                rrt.sizeDelta = new Vector2(rowW, rowH);
                rrt.anchoredPosition = new Vector2(0, startY - i * (rowH + spacing));

                var nameText = MakeText(rowGO.transform, "", new Vector2(20, -12), rowW - 200, 32, 22, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
                var descText = MakeText(rowGO.transform, "", new Vector2(20, -48), rowW - 200, 50, 16, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
                descText.color = new Color(0.8f, 0.8f, 0.8f);

                // 레벨 핍 (maxLevel개)
                var pips = new Image[d.maxLevel];
                for (int p = 0; p < d.maxLevel; p++)
                {
                    var pipGO = new GameObject($"Pip_{p}", typeof(RectTransform), typeof(Image));
                    pipGO.transform.SetParent(rowGO.transform, false);
                    var pipImg = pipGO.GetComponent<Image>();
                    pipImg.sprite = SpriteUtils.GetSharedWhiteSquare();
                    var pipRt = pipGO.GetComponent<RectTransform>();
                    pipRt.anchorMin = new Vector2(0f, 1f);
                    pipRt.anchorMax = new Vector2(0f, 1f);
                    pipRt.pivot = new Vector2(0f, 1f);
                    pipRt.sizeDelta = new Vector2(22, 22);
                    pipRt.anchoredPosition = new Vector2(20 + p * 28, -84);
                    pips[p] = pipImg;
                }

                // 투자 버튼
                var btnGO = new GameObject("Invest", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGO.transform.SetParent(rowGO.transform, false);
                btnGO.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.3f);
                var btnRt = btnGO.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(1f, 0.5f);
                btnRt.anchorMax = new Vector2(1f, 0.5f);
                btnRt.pivot = new Vector2(1f, 0.5f);
                btnRt.sizeDelta = new Vector2(120, 60);
                btnRt.anchoredPosition = new Vector2(-16, 0);
                var btnText = MakeText(btnGO.transform, "투자", Vector2.zero, 120, 60, 20, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));

                int captured = i;
                btnGO.GetComponent<Button>().onClick.AddListener(() => TryInvest(captured));

                rows.Add(new TalentRow
                {
                    index = i, nameText = nameText, descText = descText,
                    pips = pips, investButton = btnGO.GetComponent<Button>(), buttonText = btnText
                });
            }
        }

        private void Refresh()
        {
            var talents = player.Talents;
            if (talents == null) return;

            pointsText.text = $"보유 특성 포인트: {player.Progression.TalentPoints}";

            foreach (var row in rows)
            {
                var d = talents.GetTalent(row.index);
                int lv = talents.GetLevel(row.index);
                row.nameText.text = $"{d.talentName}  (Lv.{lv}/{d.maxLevel})";
                // 다음 레벨 기준 효과 요약을 보여줌 (현재 레벨이 0이면 Lv.1 효과 미리보기)
                int preview = Mathf.Max(1, lv);
                row.descText.text = $"{d.description}\n[{d.EffectSummary(preview)}]";

                for (int p = 0; p < row.pips.Length; p++)
                    row.pips[p].color = p < lv ? PipOn : PipOff;

                bool canInvest = talents.CanInvest(row.index);
                row.investButton.interactable = canInvest;
                row.buttonText.text = lv >= d.maxLevel ? "MAX" : "투자";
            }
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private TextMeshProUGUI MakeText(Transform parent, string content, Vector2 pos, float w, float h,
            float fontSize, TextAlignmentOptions anchor, Vector2 pivot)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            UIFont.ApplyTMP(text);
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.text = content;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = pivot;
            rt.anchorMax = pivot;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(w, h);
            return text;
        }
    }
}
