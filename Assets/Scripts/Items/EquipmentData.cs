using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 무기/방어구/반지 공통 장비 데이터. 셋은 필드 구성이 같아서 하나로 묶고,
    /// ItemType으로 슬롯을 구분한다 (클래스를 불필요하게 늘리지 않음).
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Equipment", fileName = "NewEquipment")]
    public class EquipmentData : ItemData
    {
        [Header("장비 수치")]
        public int attackBonus = 0;
        public int defenseBonus = 0;

        [Header("생성 시 랜덤 강화/저주")]
        public int minEnhance = 0;              // 생성 시 강화 수치 최소
        public int maxEnhance = 0;              // 생성 시 강화 수치 최대 (0,0이면 강화 없음)
        [Range(0f, 1f)] public float curseChance = 0.2f; // 저주받은 채로 생성될 확률
    }
}
