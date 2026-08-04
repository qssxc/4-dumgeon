namespace DungeonCore
{
    /// <summary>
    /// 착용 중인 장비를 관리. 전투 수치를 Combatant에 직접 더하지 않고
    /// 여기서 보너스를 조회해 합산하는 방식이라, 장비를 갈아끼워도
    /// 기본 수치가 오염되지 않는다 (직접 더하고 빼는 방식은 버그가 나기 쉬움).
    /// </summary>
    public class Equipment
    {
        public ItemInstance Weapon { get; private set; }
        public ItemInstance Armor { get; private set; }

        public int TotalAttackBonus => Weapon?.data.attackBonus ?? 0;
        public int TotalDefenseBonus => Armor?.data.defenseBonus ?? 0;

        /// <summary>
        /// 장비를 착용. 같은 슬롯에 이미 뭔가 있으면 그것을 반환하므로,
        /// 호출부가 인벤토리로 되돌려 넣는 처리를 할 수 있다.
        /// </summary>
        public ItemInstance Equip(ItemInstance item)
        {
            ItemInstance previous = null;

            switch (item.data.itemType)
            {
                case ItemType.Weapon:
                    previous = Weapon;
                    Weapon = item;
                    break;
                case ItemType.Armor:
                    previous = Armor;
                    Armor = item;
                    break;
            }

            return previous;
        }

        public ItemInstance Unequip(ItemType slot)
        {
            ItemInstance removed = null;

            switch (slot)
            {
                case ItemType.Weapon:
                    removed = Weapon;
                    Weapon = null;
                    break;
                case ItemType.Armor:
                    removed = Armor;
                    Armor = null;
                    break;
            }

            return removed;
        }

        public bool IsEquipped(ItemInstance item) => Weapon == item || Armor == item;
    }
}
