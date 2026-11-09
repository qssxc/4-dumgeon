using UnityEngine;

namespace DungeonCore
{
    public enum ItemType
    {
        Weapon,
        Armor,
        Ring,
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
        Fire,        // 환경 효과 (던지면 대상 타일 주변에 화염)
    }

    /// <summary>
    /// 모든 아이템 데이터의 공통 베이스 (abstract). 타입별 고유 필드는 서브클래스로 분리:
    /// EquipmentData(무기/방어구/반지), ConsumableData(포션/스크롤), MiscData(기타).
    /// abstract라 이 클래스 자체로는 에셋을 만들 수 없다 - 반드시 서브클래스로 생성.
    ///
    /// 개별 상태(수량, 감정, 강화, 저주)는 여기 안 두고 ItemInstance/IdentifyManager가 담당.
    /// </summary>
    public abstract class ItemData : ScriptableObject
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
        [Tooltip("체크하면 항상 감정된 상태로 시작. 포션/스크롤/미감정 장비류는 false.")]
        public bool identifiedByDefault = true;

        [Tooltip("미감정 상태일 때 '어떤 이름 풀을 쓸지' 묶는 키. 같은 그룹끼리 매 판마다 랜덤 이름을 나눠 갖는다. 예: 'Potion', 'Scroll'")]
        public string appearanceGroup = "";

        /// <summary>이 아이템이 장비인지 (무기/방어구/반지). 서브클래스 판정에 사용.</summary>
        public bool IsEquipment =>
            itemType == ItemType.Weapon || itemType == ItemType.Armor || itemType == ItemType.Ring;
    }
}
