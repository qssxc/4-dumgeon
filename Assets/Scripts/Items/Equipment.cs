using System.Collections.Generic;

namespace DungeonCore
{
    /// <summary>
    /// 착용 중인 장비를 관리. 전투 수치를 Combatant에 직접 더하지 않고
    /// 여기서 보너스를 조회해 합산하는 방식이라, 장비를 갈아끼워도
    /// 기본 수치가 오염되지 않는다.
    ///
    /// 무기/방어구는 슬롯당 1개, 반지는 2개(양손). 반지는 슬롯이 여러 개라
    /// 단일 슬롯과 다른 착용 로직을 탄다.
    /// </summary>
    public class Equipment
    {
        public ItemInstance Weapon { get; private set; }
        public ItemInstance Armor { get; private set; }
        public ItemInstance Ring1 { get; private set; }
        public ItemInstance Ring2 { get; private set; }

        // 반지도 공격/방어 보너스를 줄 수 있게 합산에 포함 (특수 효과는 나중에 확장)
        public int TotalAttackBonus =>
            (Weapon?.EffectiveAttackBonus ?? 0)
            + (Ring1?.EffectiveAttackBonus ?? 0)
            + (Ring2?.EffectiveAttackBonus ?? 0);

        public int TotalDefenseBonus =>
            (Armor?.EffectiveDefenseBonus ?? 0)
            + (Ring1?.EffectiveDefenseBonus ?? 0)
            + (Ring2?.EffectiveDefenseBonus ?? 0);

        /// <summary>
        /// 장비를 착용. 무기/방어구는 기존 것을 교체(반환), 반지는 빈 손 우선,
        /// 둘 다 차 있으면 첫 번째 손 것을 교체해 반환한다.
        /// 착용하는 순간 저주 여부가 드러난다.
        /// </summary>
        public ItemInstance Equip(ItemInstance item)
        {
            item.curseKnown = true;

            switch (item.data.itemType)
            {
                case ItemType.Weapon:
                {
                    var prev = Weapon;
                    Weapon = item;
                    return prev;
                }
                case ItemType.Armor:
                {
                    var prev = Armor;
                    Armor = item;
                    return prev;
                }
                case ItemType.Ring:
                {
                    if (Ring1 == null) { Ring1 = item; return null; }
                    if (Ring2 == null) { Ring2 = item; return null; }
                    var prev = Ring1; // 양손 다 찼으면 첫 손 것 교체
                    Ring1 = item;
                    return prev;
                }
            }
            return null;
        }

        /// <summary>
        /// 특정 아이템 인스턴스를 해제 시도. 저주받은 장비는 못 벗는다(null 반환).
        /// 반지는 슬롯이 2개라 타입이 아니라 인스턴스로 구분해야 하므로 이 방식을 쓴다.
        /// </summary>
        public ItemInstance UnequipItem(ItemInstance item)
        {
            if (item == null || !IsEquipped(item)) return null;
            if (item.isCursed)
            {
                item.curseKnown = true;
                return null; // 저주라 못 벗음
            }

            if (Weapon == item) Weapon = null;
            else if (Armor == item) Armor = null;
            else if (Ring1 == item) Ring1 = null;
            else if (Ring2 == item) Ring2 = null;

            return item;
        }

        public bool IsEquipped(ItemInstance item) =>
            Weapon == item || Armor == item || Ring1 == item || Ring2 == item;

        /// <summary>해당 아이템이 들어갈 슬롯이 이미 저주 장비로 막혀있는지 (교체 가능 여부 판단용).</summary>
        public ItemInstance GetBlockingCursedItem(ItemInstance item)
        {
            switch (item.data.itemType)
            {
                case ItemType.Weapon:
                    return (Weapon != null && Weapon.isCursed) ? Weapon : null;
                case ItemType.Armor:
                    return (Armor != null && Armor.isCursed) ? Armor : null;
                case ItemType.Ring:
                    // 빈 손이 하나라도 있으면 막히지 않음
                    if (Ring1 == null || Ring2 == null) return null;
                    // 둘 다 찼는데 교체 대상(Ring1)이 저주면 막힘
                    return Ring1.isCursed ? Ring1 : null;
            }
            return null;
        }

        public IEnumerable<ItemInstance> AllEquipped()
        {
            if (Weapon != null) yield return Weapon;
            if (Armor != null) yield return Armor;
            if (Ring1 != null) yield return Ring1;
            if (Ring2 != null) yield return Ring2;
        }
    }
}
