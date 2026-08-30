using UnityEngine;

namespace DungeonCore
{
    public enum ItemType
    {
        Weapon,
        Armor,
        Potion,
        Scroll,
        Wand,
        Food,
        Misc
    }

    /// <summary>
    /// 소비 아이템(포션/스크롤 등)의 효과 종류. 데이터 주도로 처리하기 위해 enum으로 둠 -
    /// 새 효과를 추가할 때 ItemInstance.ApplyEffect()에 케이스만 늘리면 된다.
    /// </summary>
    public enum ConsumableEffect
    {
        None,
        Heal,        // 즉시 체력 회복 (effectAmount만큼)
        Strength,    // 영구 공격력 상승 (effectAmount만큼)
        Haste,       // 일정 턴(effectDuration) 동안 속도 상승 (effectAmount만큼)
        Fire,        // 환경 효과 (아직 미구현 - 다음 단계)
    }

    /// <summary>
    /// 아이템의 정적 정보. 몬스터의 MonsterData와 같은 패턴 -
    /// 개별 상태(수량, 감정 여부)는 여기 안 두고 ItemInstance/IdentifyManager가 담당.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Item", fileName = "NewItem")]
    public class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        public string itemName = "Item";
        [TextArea] public string description = "";
        public ItemType itemType = ItemType.Misc;
        public Sprite icon;

        [Header("스택")]
        public bool stackable = false;
        public int maxStack = 1;

        [Header("투척")]
        public bool throwable = false; // 던질 수 있는 아이템인지 (포션, 투척무기 등)

        [Header("감정(Identify)")]
        [Tooltip("체크하면 항상 감정된 상태로 시작 (무기/방어구/음식 등). 포션/스크롤/반지류는 보통 false.")]
        public bool identifiedByDefault = true;

        [Tooltip("미감정 상태일 때 '어떤 이름 풀을 쓸지' 묶는 키. 같은 그룹끼리 매 판마다 랜덤 이름을 나눠 갖는다. 예: 'Potion', 'Scroll'")]
        public string appearanceGroup = "";

        [Header("장비 수치 (Weapon/Armor일 때만 사용)")]
        public int attackBonus = 0;
        public int defenseBonus = 0;

        [Header("소비 효과 (Potion/Scroll 등)")]
        public ConsumableEffect effect = ConsumableEffect.None;
        public int effectAmount = 0;      // 회복량, 스탯 증가치 등
        public int effectDuration = 0;    // 버프 지속 턴 수 (신속 등)
    }
}
