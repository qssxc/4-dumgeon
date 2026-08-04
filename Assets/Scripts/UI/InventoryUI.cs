using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCore
{
    /// <summary>
    /// 인벤토리 내용을 화면에 보여주는 최소 UI. 정식 UI(캔버스/아이콘 그리드)를 붙이기 전까지
    /// "인벤토리 로직이 제대로 동작하는가"를 빠르게 눈으로 확인하기 위한 용도.
    /// I 키로 열고 닫음.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public PlayerController player;

        private bool isOpen;
        private const float LineHeight = 24f;
        private const float PanelWidth = 320f;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
                isOpen = !isOpen;
        }

        private void OnGUI()
        {
            if (!isOpen || player == null || player.Inventory == null) return;

            var items = player.Inventory.Items;
            float x = Screen.width - PanelWidth - 20f;
            float y = 20f;
            float panelHeight = 30f + Mathf.Max(1, items.Count) * LineHeight;

            GUI.Box(new Rect(x, y, PanelWidth, panelHeight + LineHeight), "");
            GUI.Label(new Rect(x + 10, y + 5, PanelWidth - 20, LineHeight),
                $"인벤토리 ({items.Count}/{player.Inventory.maxSlots})  [I]로 닫기");
            GUI.Label(new Rect(x + 10, y + 5 + LineHeight, PanelWidth - 20, LineHeight),
                $"공격력 {player.Combatant.attack}  방어력 {player.Combatant.defense}");

            if (items.Count == 0)
            {
                GUI.Label(new Rect(x + 10, y + 5 + LineHeight * 2, PanelWidth - 20, LineHeight), "비어있음");
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                string qty = item.data.stackable ? $" x{item.quantity}" : "";
                string equipped = player.Equipment.IsEquipped(item) ? "  [착용중]" : "";
                string label = $"{i + 1}. {item.DisplayName}{qty}{equipped}";
                GUI.Label(new Rect(x + 10, y + 5 + (i + 2) * LineHeight, PanelWidth - 20, LineHeight), label);
            }
        }
    }
}
