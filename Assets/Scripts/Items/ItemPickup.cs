using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 바닥에 떨어진 아이템의 월드 표현. 실제 데이터(ItemInstance)를 들고 있고,
    /// 화면 표시와 위치 추적만 담당 - 로직은 Inventory/ItemInstance 쪽에 있음.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ItemPickup : MonoBehaviour
    {
        private static readonly List<ItemPickup> all = new();
        public static IReadOnlyList<ItemPickup> All => all;

        public ItemInstance Item { get; private set; }
        public Vector2Int GridPosition { get; private set; }

        private SpriteRenderer spriteRenderer;

        /// <summary>fog of war: 시야 밖이면 바닥 아이템을 숨긴다.</summary>
        public void SetSeen(bool seen)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = seen;
        }

        public void Init(ItemInstance item, Vector2Int gridPosition)
        {
            Item = item;
            GridPosition = gridPosition;
            transform.position = GridUtils.GridToWorld(gridPosition);

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (item.DisplayIcon != null)
            {
                spriteRenderer.sprite = item.DisplayIcon;
            }
            else
            {
                // 아이콘 없으면 색 사각형 - 미감정이면 겉모습 색, 아니면 타입 색
                Color c = item.DisplayColor != Color.white ? item.DisplayColor : ColorForType(item.data.itemType);
                spriteRenderer.sprite = SpriteUtils.CreateSolidSprite(c);
            }
            spriteRenderer.sortingOrder = 3; // 바닥/문보다 위, 몬스터/플레이어보다 아래

            all.Add(this);
        }

        // 아이콘이 없을 때 타입별로 구분되는 색이라도 보이도록 하는 임시용
        private Color ColorForType(ItemType type)
        {
            return type switch
            {
                ItemType.Weapon => new Color(0.7f, 0.7f, 0.75f),
                ItemType.Armor => new Color(0.5f, 0.4f, 0.3f),
                ItemType.Ring => new Color(0.9f, 0.8f, 0.3f),
                ItemType.Potion => new Color(0.8f, 0.2f, 0.6f),
                ItemType.Scroll => new Color(0.9f, 0.85f, 0.6f),
                ItemType.Wand => new Color(0.3f, 0.6f, 0.9f),
                ItemType.Food => new Color(0.9f, 0.6f, 0.2f),
                _ => Color.white,
            };
        }

        public static ItemPickup FindAt(Vector2Int pos)
        {
            foreach (var p in all)
                if (p != null && p.GridPosition == pos)
                    return p;
            return null;
        }

        /// <summary>레벨 전환 시 현재 층의 아이템을 전부 정리할 때 사용.</summary>
        public static void DestroyAll()
        {
            foreach (var p in new List<ItemPickup>(all))
                if (p != null) Destroy(p.gameObject);
            all.Clear();
        }

        private void OnDestroy()
        {
            all.Remove(this);
        }
    }
}
