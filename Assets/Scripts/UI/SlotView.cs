using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DungeonCore
{
    /// <summary>
    /// 인벤토리/장비 슬롯 하나를 표현하는 UI 요소. 런타임에 코드로 생성된다.
    /// 아이콘, (선택) 이름 라벨, 선택 하이라이트, 클릭 콜백을 담당.
    /// 슬롯이 '무엇을' 담는지는 몰라도 되고 ItemInstance만 받아서 표시한다.
    /// </summary>
    public class SlotView : MonoBehaviour, IPointerClickHandler
    {
        public int Index { get; private set; }
        public ItemInstance Item { get; private set; }

        private Image background;
        private Image iconImage;
        private TextMeshProUGUI labelText;
        private Action<int> onClick;

        private static readonly Color NormalColor = new Color(0.15f, 0.15f, 0.18f, 0.9f);
        private static readonly Color SelectedColor = new Color(0.35f, 0.5f, 0.7f, 0.95f);
        private static readonly Color EquippedTint = new Color(0.25f, 0.4f, 0.25f, 0.95f);

        public static SlotView Create(Transform parent, int index, Vector2 size, Action<int> onClick, string emptyLabel = "")
        {
            var go = new GameObject($"Slot_{index}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var slot = go.AddComponent<SlotView>();
            slot.Index = index;
            slot.onClick = onClick;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            slot.background = go.AddComponent<Image>();
            slot.background.color = NormalColor;

            // 아이콘
            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(go.transform, false);
            slot.iconImage = iconGO.AddComponent<Image>();
            slot.iconImage.preserveAspect = true;
            var iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0f);
            iconRt.anchorMax = new Vector2(1f, 1f);
            iconRt.offsetMin = new Vector2(6, 6);
            iconRt.offsetMax = new Vector2(-6, -6);
            slot.iconImage.enabled = false;

            // 이름/수량 라벨 (슬롯 하단)
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            slot.labelText = labelGO.AddComponent<TextMeshProUGUI>();
            UIFont.ApplyTMP(slot.labelText);
            slot.labelText.fontSize = 11;
            slot.labelText.alignment = TextAlignmentOptions.Bottom;
            slot.labelText.color = Color.white;
            slot.labelText.enableWordWrapping = false;
            slot.labelText.overflowMode = TextOverflowModes.Overflow;
            slot.labelText.text = emptyLabel;
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.sizeDelta = new Vector2(0, 16);
            labelRt.anchoredPosition = new Vector2(0, 2);

            return slot;
        }

        public void SetItem(ItemInstance item, bool isEquipped)
        {
            Item = item;

            if (item == null)
            {
                iconImage.enabled = false;
                background.color = NormalColor;
                return;
            }

            if (item.DisplayIcon != null)
            {
                iconImage.sprite = item.DisplayIcon;
                iconImage.color = Color.white;
                iconImage.enabled = true;
            }
            else if (item.DisplayColor != Color.white)
            {
                // 아이콘 없고 미감정 겉모습 색이 있으면 색 사각형으로 표시
                iconImage.sprite = null;
                iconImage.color = item.DisplayColor;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }

            string qty = item.data.stackable && item.quantity > 1 ? $" x{item.quantity}" : "";
            labelText.text = $"{item.DisplayName}{qty}";
            background.color = isEquipped ? EquippedTint : NormalColor;
        }

        /// <summary>선택 상태를 반영해 배경색 갱신 (아이템/장착 상태와 함께 재계산).</summary>
        public void Refresh(bool isSelected, bool isEquipped)
        {
            if (isSelected)
                background.color = SelectedColor;
            else if (Item != null && isEquipped)
                background.color = EquippedTint;
            else
                background.color = NormalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onClick?.Invoke(Index);
        }

        public RectTransform RectTransform => (RectTransform)transform;
    }
}
