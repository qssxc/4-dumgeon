using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DungeonCore
{
    /// <summary>
    /// 항상 표시되는 좌상단 HUD (Canvas). 체력바 + 경험치바 + 레벨을 보여준다.
    /// 텍스트는 TextMeshPro(TMP) 사용. 코드로 UI를 생성하므로 에디터 세팅 불필요.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        public PlayerController player;

        private Image hpFill;
        private TextMeshProUGUI hpText;
        private Image xpFill;
        private TextMeshProUGUI levelText;

        private static readonly Color HpColor = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color XpColor = new Color(0.3f, 0.6f, 0.9f);
        private static readonly Color BarBg = new Color(0.1f, 0.1f, 0.12f, 0.9f);

        private void Start()
        {
            BuildUI();
        }

        private void Update()
        {
            if (player == null || player.Combatant == null) return;

            var c = player.Combatant;
            float hpRatio = c.maxHP > 0 ? Mathf.Clamp01((float)c.currentHP / c.maxHP) : 0f;
            hpFill.fillAmount = hpRatio;
            hpText.text = $"HP {c.currentHP} / {c.maxHP}";

            var p = player.Progression;
            if (p != null)
            {
                float xpRatio = p.XpToNext > 0 ? Mathf.Clamp01((float)p.CurrentXP / p.XpToNext) : 0f;
                xpFill.fillAmount = xpRatio;
                string pts = p.TalentPoints > 0 ? $"  (특성P {p.TalentPoints})" : "";
                levelText.text = $"Lv. {p.Level}{pts}";
            }
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("PlayerHUDCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // 인벤토리(100)보다 아래, 월드 위
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // 너비·높이 절반씩 반영 - 16:9 외 비율에서도 안정적

            // 좌상단 고정 컨테이너
            var panel = new GameObject("HUD", typeof(RectTransform));
            panel.transform.SetParent(canvasGO.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 1f);
            prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 1f);
            prt.anchoredPosition = new Vector2(24, -24);
            prt.sizeDelta = new Vector2(360, 80);

            levelText = MakeText(panel.transform, new Vector2(0, 0), 320, 28, 20, TextAlignmentOptions.BottomLeft);
            hpText = MakeText(panel.transform, new Vector2(0, -2), 340, 24, 16, TextAlignmentOptions.Right);

            MakeBar(panel.transform, new Vector2(0, -30), 340, 22, BarBg, HpColor, out hpFill);
            MakeBar(panel.transform, new Vector2(0, -56), 340, 10, BarBg, XpColor, out xpFill);
        }

        private void MakeBar(Transform parent, Vector2 pos, float w, float h, Color bg, Color fillColor, out Image fill)
        {
            var sprite = SpriteUtils.GetSharedWhiteSquare();

            var barGO = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(parent, false);
            var brt = barGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = pos;
            brt.sizeDelta = new Vector2(w, h);
            var bgImg = barGO.GetComponent<Image>();
            bgImg.sprite = sprite;
            bgImg.color = bg;

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(barGO.transform, false);
            var frt = fillGO.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(2, 2);
            frt.offsetMax = new Vector2(-2, -2);
            fill = fillGO.GetComponent<Image>();
            fill.sprite = sprite;
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
        }

        private TextMeshProUGUI MakeText(Transform parent, Vector2 pos, float w, float h, float fontSize,
            TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            UIFont.ApplyTMP(text);
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(w, h);
            return text;
        }
    }
}
