using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

namespace DungeonCore
{
    /// <summary>
    /// 정식 Canvas 기반 인벤토리 화면. 런타임에 코드로 UI를 통째로 생성하므로
    /// 에디터에서 Canvas 계층을 손으로 짤 필요가 없다 (컴포넌트 하나만 붙이면 됨).
    ///
    /// 구성: 왼쪽에 장비 슬롯(무기/방어구/반지1/반지2), 오른쪽에 가방 그리드.
    /// 조작: I로 열고 닫기, 마우스 클릭 또는 방향키로 슬롯 선택, Enter/재클릭으로 사용/장착.
    /// </summary>
    public class InventoryScreen : MonoBehaviour
    {
        public PlayerController player;

        [SerializeField] private int columns = 4;
        [SerializeField] private Vector2 slotSize = new Vector2(90, 90);

        private Canvas canvas;
        private GameObject panelRoot;
        private bool isOpen;

        private readonly List<SlotView> bagSlots = new();
        private SlotView weaponSlot, armorSlot, ring1Slot, ring2Slot;

        // 선택 커서: 가방 슬롯 인덱스 (-1이면 없음)
        private int selectedIndex = -1;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.iKey.wasPressedThisFrame)
                Toggle();

            if (!isOpen) return;

            HandleKeyboardNavigation(kb);
        }

        private void Toggle()
        {
            isOpen = !isOpen;
            if (isOpen)
            {
                EnsureBuilt();
                panelRoot.SetActive(true);
                selectedIndex = -1;
                Refresh();
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void HandleKeyboardNavigation(Keyboard kb)
        {
            if (kb == null) return;
            int count = player.Inventory.Items.Count;
            if (count == 0) return;

            if (selectedIndex < 0) selectedIndex = 0;

            if (kb.rightArrowKey.wasPressedThisFrame) MoveSelection(1);
            else if (kb.leftArrowKey.wasPressedThisFrame) MoveSelection(-1);
            else if (kb.downArrowKey.wasPressedThisFrame) MoveSelection(columns);
            else if (kb.upArrowKey.wasPressedThisFrame) MoveSelection(-columns);
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                UseSelected();
        }

        private void MoveSelection(int delta)
        {
            int count = player.Inventory.Items.Count;
            if (count == 0) return;
            selectedIndex = Mathf.Clamp(selectedIndex + delta, 0, count - 1);
            Refresh();
        }

        private void UseSelected()
        {
            var items = player.Inventory.Items;
            if (selectedIndex < 0 || selectedIndex >= items.Count) return;

            var item = items[selectedIndex];
            item.Use(player);
            Refresh(); // 사용 후 목록/장비 상태 갱신
        }

        // 슬롯 클릭: 처음 클릭은 선택, 이미 선택된 걸 다시 클릭하면 사용
        private void OnBagSlotClicked(int index)
        {
            if (index >= player.Inventory.Items.Count) return;

            if (selectedIndex == index)
                UseSelected();
            else
            {
                selectedIndex = index;
                Refresh();
            }
        }

        // 장비 슬롯 클릭: 착용 중인 아이템을 해제 시도 (사용 로직이 토글이므로 Use 호출)
        private void OnEquipSlotClicked(ItemInstance equipped)
        {
            if (equipped == null) return;
            equipped.Use(player); // 이미 착용 중이면 Use가 해제 처리
            Refresh();
        }

        private void EnsureBuilt()
        {
            if (canvas != null) return;

            // Canvas
            var canvasGO = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // 너비·높이 절반씩 반영 - 16:9 외 비율에서도 안정적

            EnsureEventSystem();

            // 반투명 배경 패널
            panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGO.transform, false);
            var panelImg = panelRoot.GetComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.6f);
            var panelRt = panelRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            BuildEquipmentSlots(panelRoot.transform);
            BuildBagGrid(panelRoot.transform);
            BuildTitle(panelRoot.transform);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                // 새 Input System용 UI 모듈
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private void BuildTitle(Transform parent)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            UIFont.ApplyTMP(text);
            text.fontSize = 32;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = "인벤토리   (I: 닫기 / 방향키·클릭: 선택 / Enter·재클릭: 사용)";
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(1200, 60);
            rt.anchoredPosition = new Vector2(0, -50);
        }

        private void BuildEquipmentSlots(Transform parent)
        {
            var container = new GameObject("Equipment", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            var rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-420, 40);

            weaponSlot = MakeEquipSlot(container.transform, "무기", new Vector2(0, 150), () => player.Equipment.Weapon);
            armorSlot = MakeEquipSlot(container.transform, "방어구", new Vector2(0, 50), () => player.Equipment.Armor);
            ring1Slot = MakeEquipSlot(container.transform, "반지1", new Vector2(0, -50), () => player.Equipment.Ring1);
            ring2Slot = MakeEquipSlot(container.transform, "반지2", new Vector2(0, -150), () => player.Equipment.Ring2);
        }

        private SlotView MakeEquipSlot(Transform parent, string label, Vector2 pos, System.Func<ItemInstance> getter)
        {
            var slot = SlotView.Create(parent, -1, slotSize, _ => OnEquipSlotClicked(getter()), label);
            var rt = slot.RectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            return slot;
        }

        private void BuildBagGrid(Transform parent)
        {
            var container = new GameObject("Bag", typeof(RectTransform), typeof(GridLayoutGroup));
            container.transform.SetParent(parent, false);
            var rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(180, 0);
            rt.sizeDelta = new Vector2(columns * (slotSize.x + 8), 500);

            var grid = container.GetComponent<GridLayoutGroup>();
            grid.cellSize = slotSize;
            grid.spacing = new Vector2(8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            int slotCount = player.Inventory.maxSlots;
            for (int i = 0; i < slotCount; i++)
            {
                var slot = SlotView.Create(container.transform, i, slotSize, OnBagSlotClicked);
                bagSlots.Add(slot);
            }
        }

        private void Refresh()
        {
            var items = player.Inventory.Items;
            var eq = player.Equipment;

            for (int i = 0; i < bagSlots.Count; i++)
            {
                var item = i < items.Count ? items[i] : null;
                bagSlots[i].SetItem(item, item != null && eq.IsEquipped(item));
                bagSlots[i].Refresh(i == selectedIndex, item != null && eq.IsEquipped(item));
            }

            weaponSlot.SetItem(eq.Weapon, eq.Weapon != null);
            armorSlot.SetItem(eq.Armor, eq.Armor != null);
            ring1Slot.SetItem(eq.Ring1, eq.Ring1 != null);
            ring2Slot.SetItem(eq.Ring2, eq.Ring2 != null);
        }
    }
}
