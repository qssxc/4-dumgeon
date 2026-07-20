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

        [Header("감정(Identify)")]
        [Tooltip("체크하면 항상 감정된 상태로 시작 (무기/방어구/음식 등). 포션/스크롤/반지류는 보통 false.")]
        public bool identifiedByDefault = true;

        [Tooltip("미감정 상태일 때 '어떤 이름 풀을 쓸지' 묶는 키. 같은 그룹끼리 매 판마다 랜덤 이름을 나눠 갖는다. 예: 'Potion', 'Scroll'")]
        public string appearanceGroup = "";
    }
}
