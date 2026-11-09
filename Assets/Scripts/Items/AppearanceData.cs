using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 미감정 아이템의 "겉모습" 에셋. 정체(효과)와 분리되어 있고, 매 판마다
    /// IdentifyManager가 같은 그룹의 정체들과 무작위로 1:1 짝짓는다.
    /// 예: "빨간 물약"(이 에셋)이 이번 판엔 회복 포션에, 다음 판엔 화염 포션에 붙을 수 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Appearance", fileName = "NewAppearance")]
    public class AppearanceData : ScriptableObject
    {
        [Tooltip("같은 그룹끼리 짝지어진다. 아이템(ConsumableData 등)의 appearanceGroup과 일치해야 함. 예: 'Potion', 'Scroll'")]
        public string group = "Potion";

        [Tooltip("미감정 상태로 보여줄 이름. 예: '빨간 물약'")]
        public string unidentifiedName = "이름 모를 물약";

        [Tooltip("미감정 상태로 보여줄 아이콘. 없으면 색 사각형으로 대체됨.")]
        public Sprite icon;

        [Tooltip("아이콘이 없을 때 쓸 대표 색.")]
        public Color color = Color.magenta;
    }
}
