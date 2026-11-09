using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 월드 공간 체력바 (OnGUI 아님). 배경 + 채움 두 개의 스프라이트로 구성되며,
    /// 보통 액터의 자식으로 붙어서 머리 위에 떠 따라다닌다.
    /// Combatant를 참조해 매 프레임 채움 비율과 색을 갱신한다.
    ///
    /// 코드로 전부 생성하므로 프리팹 세팅이 필요 없다. 부모의 flipX(스프라이트 반전)에는
    /// 영향받지 않는다(자식 transform은 그대로이므로).
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        private Combatant target;
        private Transform fill;
        private SpriteRenderer fillRenderer;
        private SpriteRenderer bgRenderer;

        private float barWidth;
        private float barHeight;
        private bool hideWhenFull;

        /// <summary>시야(fog of war) 밖에 있을 때 외부에서 강제로 숨기는 플래그.</summary>
        public bool ForceHidden { get; set; }

        private static readonly Color BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        /// <summary>
        /// 체력바 생성 후 반환. parent 밑에 붙고, 로컬 y=yOffset 위치에 뜬다.
        /// hideWhenFull=true면 체력이 가득 찼을 땐 숨긴다 (몬스터용으로 적합).
        /// </summary>
        public static HealthBar Create(Transform parent, Combatant target, float yOffset,
            float width = 0.8f, float height = 0.12f, int sortingOrder = 10, bool hideWhenFull = true)
        {
            var rootGO = new GameObject("HealthBar");
            rootGO.transform.SetParent(parent, false);
            rootGO.transform.localPosition = new Vector3(0f, yOffset, 0f);

            var bar = rootGO.AddComponent<HealthBar>();
            bar.target = target;
            bar.barWidth = width;
            bar.barHeight = height;
            bar.hideWhenFull = hideWhenFull;

            var sprite = SpriteUtils.GetSharedWhiteSquare();

            // 배경 (중앙 피벗, 전체 너비)
            var bg = new GameObject("BG");
            bg.transform.SetParent(rootGO.transform, false);
            bg.transform.localScale = new Vector3(width, height, 1f);
            bar.bgRenderer = bg.AddComponent<SpriteRenderer>();
            bar.bgRenderer.sprite = sprite;
            bar.bgRenderer.color = BgColor;
            bar.bgRenderer.sortingOrder = sortingOrder;

            // 채움 (왼쪽 고정으로 줄어들도록 위치를 보정)
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(rootGO.transform, false);
            bar.fill = fillGO.transform;
            bar.fillRenderer = fillGO.AddComponent<SpriteRenderer>();
            bar.fillRenderer.sprite = sprite;
            bar.fillRenderer.sortingOrder = sortingOrder + 1; // 배경보다 위

            bar.Refresh();
            return bar;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Refresh();
        }

        private void Refresh()
        {
            float ratio = target.maxHP > 0 ? Mathf.Clamp01((float)target.currentHP / target.maxHP) : 0f;

            // root는 계속 활성 상태로 두고(LateUpdate가 계속 돌아야 함),
            // 표시 여부는 렌더러를 껐다 켜서 제어한다.
            bool visible = !ForceHidden && !(hideWhenFull && ratio >= 1f) && target.IsAlive;
            bgRenderer.enabled = visible;
            fillRenderer.enabled = visible;
            if (!visible) return;

            // 채움 너비 조절 + 왼쪽 끝 고정 (중앙 피벗이라 위치 보정 필요)
            float w = barWidth * ratio;
            fill.localScale = new Vector3(w, barHeight, 1f);
            fill.localPosition = new Vector3(-barWidth * 0.5f + w * 0.5f, 0f, 0f);

            // 색: 가득=초록 → 절반=노랑 → 바닥=빨강
            fillRenderer.color = ratio > 0.5f
                ? Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f)
                : Color.Lerp(Color.red, Color.yellow, ratio * 2f);
        }
    }
}
