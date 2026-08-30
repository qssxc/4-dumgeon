using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 인벤토리에 실제로 들어가는 아이템 한 덩어리(스택 포함).
    /// 감정 여부는 이 인스턴스가 아니라 IdentifyManager가 ItemData 단위로 관리한다 -
    /// "포션 A 하나"가 아니라 "포션이라는 종류 전체"가 감정되는 것이므로.
    /// </summary>
    public class ItemInstance
    {
        public readonly ItemData data;
        public int quantity;

        public ItemInstance(ItemData data, int quantity = 1)
        {
            this.data = data;
            this.quantity = quantity;
        }

        public bool IsIdentified => data.identifiedByDefault || IdentifyManager.Instance.IsIdentified(data);

        /// <summary>플레이어에게 실제로 보여줄 이름. 미감정이면 이번 플레이용으로 배정된 가짜 이름.</summary>
        public string DisplayName => IsIdentified ? data.itemName : IdentifyManager.Instance.GetUnidentifiedName(data);

        /// <summary>이 아이템을 감정 상태로 확정 (사용, 감정 스크롤 등에서 호출).</summary>
        public void Identify()
        {
            IdentifyManager.Instance.MarkIdentified(data);
        }

        /// <summary>
        /// 아이템 사용의 진입점. 무기/방어구는 장착(소모되지 않음),
        /// 그 외(포션/스크롤 등)는 아직 효과가 더미이며 사용하면 하나 줄어든다.
        /// </summary>
        public bool Use(PlayerController user)
        {
            Identify(); // 원작처럼 사용하는 순간 그 종류 전체가 감정됨

            bool isEquipment = data.itemType == ItemType.Weapon || data.itemType == ItemType.Armor;
            if (isEquipment)
            {
                // 이미 착용 중인 걸 다시 누르면 해제
                if (user.Equipment.IsEquipped(this))
                {
                    user.Equipment.Unequip(data.itemType);
                    Debug.Log($"{DisplayName} 해제");
                    return true;
                }

                // 같은 슬롯에 있던 기존 장비는 인벤토리로 되돌림
                var previous = user.Equipment.Equip(this);
                if (previous != null)
                    Debug.Log($"{previous.DisplayName} 해제하고 {DisplayName} 장착");
                else
                    Debug.Log($"{DisplayName} 장착");

                Debug.Log($"현재 공격력 {user.Combatant.attack}, 방어력 {user.Combatant.defense}");
                return true; // 장비는 인벤토리에서 사라지지 않음
            }

            Debug.Log($"{DisplayName} 사용");
            ApplyEffect(user);
            user.Inventory.Remove(this, 1);
            return true;
        }

        /// <summary>
        /// ItemData에 설정된 소비 효과를 실제로 적용. 데이터 주도 방식이라
        /// 새 효과를 추가할 때 여기 케이스만 늘리고 ConsumableEffect enum에 항목을 추가하면 된다.
        /// </summary>
        private void ApplyEffect(PlayerController user)
        {
            var c = user.Combatant;
            switch (data.effect)
            {
                case ConsumableEffect.Heal:
                    int healed = c.Heal(data.effectAmount);
                    Debug.Log($"체력 {healed} 회복 (현재 {c.currentHP}/{c.maxHP})");
                    break;

                case ConsumableEffect.Strength:
                    c.AddPermanentAttack(data.effectAmount);
                    Debug.Log($"공격력이 영구적으로 {data.effectAmount} 상승 (현재 {c.attack})");
                    break;

                case ConsumableEffect.Haste:
                    c.ApplyHaste(data.effectAmount, data.effectDuration);
                    Debug.Log($"{data.effectDuration}턴 동안 속도 +{data.effectAmount}");
                    break;

                case ConsumableEffect.Fire:
                    Debug.Log("화염 효과는 아직 미구현 (환경 효과는 다음 단계)");
                    break;

                case ConsumableEffect.None:
                default:
                    Debug.Log("이 아이템은 별다른 효과가 없다");
                    break;
            }
        }
        /// <summary>
        /// 이 아이템이 target 타일로 던져졌을 때 호출. 지금은 도착 알림만 하고,
        /// 실제 효과(화염 타일 생성 등)는 다음 단계에서 TileEffectManager와 연결한다.
        /// </summary>
        public void OnThrownTo(PlayerController thrower, Vector2Int target)
        {
            switch (data.effect)
            {
                case ConsumableEffect.Fire:
                    Debug.Log($"{target}에 화염 포션이 깨졌다 (화염 타일 효과는 다음 단계)");
                    break;
                default:
                    Debug.Log($"{DisplayName}을(를) 던졌지만 깨져도 별 효과가 없다");
                    break;
            }
        }
    }
}
