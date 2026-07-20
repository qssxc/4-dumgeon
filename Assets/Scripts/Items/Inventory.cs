using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 아이템 보관 목록. 슬롯 하나에 스택 가능한 아이템은 뭉쳐서 들어간다.
    /// UI와는 완전히 분리된 순수 데이터/로직 클래스 - PlayerController가 필드로 들고 있는 형태로 사용.
    /// </summary>
    public class Inventory
    {
        public readonly int maxSlots;
        private readonly List<ItemInstance> items = new();

        public IReadOnlyList<ItemInstance> Items => items;

        public Inventory(int maxSlots = 20)
        {
            this.maxSlots = maxSlots;
        }

        /// <summary>
        /// 아이템 추가를 시도. 스택 가능한 아이템이면 기존 스택에 먼저 합치고,
        /// 남는 게 있거나 스택 불가 아이템이면 새 슬롯에 넣는다. 슬롯이 꽉 차면 실패.
        /// </summary>
        public bool TryAdd(ItemInstance newItem)
        {
            if (newItem.data.stackable)
            {
                var existing = items.FirstOrDefault(i => CanStackWith(i, newItem));
                if (existing != null)
                {
                    int spaceLeft = existing.data.maxStack - existing.quantity;
                    int amountToAdd = Mathf.Min(spaceLeft, newItem.quantity);
                    existing.quantity += amountToAdd;
                    newItem.quantity -= amountToAdd;

                    if (newItem.quantity <= 0) return true; // 기존 스택에 전부 합쳐짐
                }
            }

            if (items.Count >= maxSlots) return false; // 슬롯 부족

            items.Add(newItem);
            return true;
        }

        public void Remove(ItemInstance item, int count = 1)
        {
            item.quantity -= count;
            if (item.quantity <= 0) items.Remove(item);
        }

        private bool CanStackWith(ItemInstance a, ItemInstance b)
        {
            return a.data == b.data && a.quantity < a.data.maxStack;
        }
    }
}
