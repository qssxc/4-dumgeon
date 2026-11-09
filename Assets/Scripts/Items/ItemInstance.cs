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

        // 개별 상태 - 같은 종류라도 인스턴스마다 다를 수 있음 (강화 수치, 저주 여부)
        public int enhanceLevel;   // +N 강화 수치 (음수면 약화)
        public bool isCursed;      // 저주받으면 착용 시 해제 불가
        public bool curseKnown;    // 저주 여부를 플레이어가 아는지 (착용하거나 감정하면 드러남)

        public ItemInstance(ItemData data, int quantity = 1)
        {
            this.data = data;
            this.quantity = quantity;
        }

        // data를 서브클래스로 캐스팅하는 편의 프로퍼티 (해당 타입이 아니면 null)
        public EquipmentData AsEquipment => data as EquipmentData;
        public ConsumableData AsConsumable => data as ConsumableData;

        /// <summary>
        /// 아이템을 스폰할 때 사용하는 팩토리. 장비면 ItemData 설정에 따라
        /// 강화 수치와 저주 여부를 랜덤으로 굴린다. 소비 아이템은 그냥 기본 인스턴스.
        /// </summary>
        public static ItemInstance Create(ItemData data, int quantity = 1)
        {
            var instance = new ItemInstance(data, quantity);

            if (data is EquipmentData eq)
            {
                if (eq.maxEnhance != 0 || eq.minEnhance != 0)
                    instance.enhanceLevel = UnityEngine.Random.Range(eq.minEnhance, eq.maxEnhance + 1);

                instance.isCursed = UnityEngine.Random.value < eq.curseChance;
            }

            return instance;
        }

        public bool IsIdentified => data.identifiedByDefault || IdentifyManager.Instance.IsIdentified(data);

        /// <summary>이번 판 이 아이템에 배정된 겉모습 (미감정 표시용). 감정 불필요/미배정이면 null.</summary>
        public AppearanceData Appearance => IdentifyManager.Instance.GetAppearance(data);

        /// <summary>플레이어에게 실제로 보여줄 이름. 미감정이면 겉모습 이름, 감정됐으면 정체 이름 + 강화 수치.</summary>
        public string DisplayName
        {
            get
            {
                if (!IsIdentified)
                {
                    var ap = Appearance;
                    return ap != null ? ap.unidentifiedName : $"이름 모를 {data.itemType}";
                }

                string prefix = enhanceLevel > 0 ? $"+{enhanceLevel} " : enhanceLevel < 0 ? $"{enhanceLevel} " : "";
                string curseMark = (curseKnown && isCursed) ? " (저주)" : "";
                return $"{prefix}{data.itemName}{curseMark}";
            }
        }

        /// <summary>
        /// 화면에 보여줄 아이콘. 겉모습(Appearance)이 배정된 아이템(포션/스크롤 등)은
        /// 감정 여부와 상관없이 그 겉모습 이미지를 유지한다 (원작처럼 '빨간 물약' 모습은 그대로,
        /// 이름만 정체가 드러남). 겉모습이 없는 아이템(장비 등)은 정체 아이콘을 쓴다.
        /// </summary>
        public Sprite DisplayIcon
        {
            get
            {
                var ap = Appearance;
                if (ap != null && ap.icon != null)
                    return ap.icon;
                return data.icon;
            }
        }

        /// <summary>아이콘이 없을 때 쓸 대표 색. 겉모습이 배정됐으면 감정 후에도 그 색을 유지.</summary>
        public Color DisplayColor
        {
            get
            {
                var ap = Appearance;
                if (ap != null)
                    return ap.color;
                return Color.white;
            }
        }

        // 강화 수치가 반영된 실제 장비 보너스. 장비가 아니면 0. 강화 1당 공격/방어 +1.
        public int EffectiveAttackBonus => (AsEquipment?.attackBonus ?? 0) + (AsEquipment != null ? enhanceLevel : 0);
        public int EffectiveDefenseBonus => (AsEquipment?.defenseBonus ?? 0) + (AsEquipment != null ? enhanceLevel : 0);

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

            bool isEquipment = data.IsEquipment;
            if (isEquipment)
            {
                // 이미 착용 중인 걸 다시 누르면 해제 시도
                if (user.Equipment.IsEquipped(this))
                {
                    var removed = user.Equipment.UnequipItem(this);
                    if (removed == null)
                        Debug.Log($"{DisplayName}은(는) 저주받아 벗을 수 없다!");
                    else
                        Debug.Log($"{DisplayName} 해제");
                    return true;
                }

                // 교체할 슬롯이 저주 장비로 막혀있으면 착용 불가
                var blocker = user.Equipment.GetBlockingCursedItem(this);
                if (blocker != null)
                {
                    blocker.curseKnown = true;
                    Debug.Log($"{blocker.DisplayName}이(가) 저주받아 벗을 수 없어 교체 불가!");
                    return true;
                }

                var previous = user.Equipment.Equip(this);
                if (previous != null)
                    Debug.Log($"{previous.DisplayName} 해제하고 {DisplayName} 장착");
                else
                    Debug.Log($"{DisplayName} 장착");

                if (isCursed)
                    Debug.Log($"장착하는 순간 몸에 달라붙었다... {DisplayName}은(는) 저주받았다!");

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
            var consumable = AsConsumable;
            if (consumable == null)
            {
                Debug.Log("이 아이템은 별다른 효과가 없다");
                return;
            }

            var c = user.Combatant;
            switch (consumable.effect)
            {
                case ConsumableEffect.Heal:
                    int healed = c.Heal(consumable.effectAmount);
                    Debug.Log($"체력 {healed} 회복 (현재 {c.currentHP}/{c.maxHP})");
                    break;

                case ConsumableEffect.Strength:
                    c.AddPermanentAttack(consumable.effectAmount);
                    Debug.Log($"공격력이 영구적으로 {consumable.effectAmount} 상승 (현재 {c.attack})");
                    break;

                case ConsumableEffect.Haste:
                    c.ApplyHaste(consumable.effectAmount, consumable.effectDuration);
                    Debug.Log($"{consumable.effectDuration}턴 동안 속도 +{consumable.effectAmount}");
                    break;

                case ConsumableEffect.Fire:
                    // 마시면 자기 주변에 불 (self-immolation - 위험하지만 원작에도 있는 요소)
                    if (TileEffectManager.Instance != null)
                    {
                        int duration = consumable.effectDuration > 0 ? consumable.effectDuration : 5;
                        TileEffectManager.Instance.IgniteArea(user.GridPosition, 3, duration);
                        Debug.Log("화염 포션을 마셔 주변이 불길에 휩싸였다!");
                    }
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
            var consumable = AsConsumable;
            if (consumable != null && consumable.effect == ConsumableEffect.Fire)
            {
                if (TileEffectManager.Instance != null)
                {
                    int duration = consumable.effectDuration > 0 ? consumable.effectDuration : 5;
                    TileEffectManager.Instance.IgniteArea(target, 3, duration);
                    Debug.Log($"{target}에 화염 포션이 깨지며 불길이 번졌다!");
                }
                return;
            }

            Debug.Log($"{DisplayName}을(를) 던졌지만 깨져도 별 효과가 없다");
        }
    }
}
