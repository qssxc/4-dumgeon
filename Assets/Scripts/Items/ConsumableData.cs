using UnityEngine;

namespace DungeonCore
{
    /// <summary>포션/스크롤 등 소비 아이템 데이터. 효과 종류와 수치를 가진다.</summary>
    [CreateAssetMenu(menuName = "Dungeon/Consumable", fileName = "NewConsumable")]
    public class ConsumableData : ItemData
    {
        [Header("소비 효과")]
        public ConsumableEffect effect = ConsumableEffect.None;
        public int effectAmount = 0;      // 회복량, 스탯 증가치 등
        public int effectDuration = 0;    // 버프 지속 턴 수 (신속 등)
    }
}
