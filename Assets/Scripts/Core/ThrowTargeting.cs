using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCore
{
    /// <summary>
    /// 투척 조준 모드를 관리. 던지기 시작 -> 마우스로 타일 클릭 -> 그 타일이 타겟으로 확정.
    /// 실제로 무엇을 던지는지(화염 포션/투척 무기)는 몰라도 되고, "타겟 타일이 정해졌다"는
    /// 콜백만 넘긴다 - 그래서 화염 외에 투척 무기, 지팡이 조준에도 재사용 가능.
    /// </summary>
    public class ThrowTargeting : MonoBehaviour
    {
        public bool IsAiming { get; private set; }

        private Action<Vector2Int> onConfirm;
        private Action onCancel;

        /// <summary>조준 시작. 타일을 클릭하면 onConfirm(타일좌표), 우클릭/ESC면 onCancel.</summary>
        public void BeginAiming(Action<Vector2Int> onConfirm, Action onCancel)
        {
            IsAiming = true;
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
        }

        private void Update()
        {
            if (!IsAiming) return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            // 취소: 우클릭 또는 ESC
            if ((mouse != null && mouse.rightButton.wasPressedThisFrame) ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                Cancel();
                return;
            }

            // 확정: 좌클릭한 타일
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                var cam = Camera.main;
                if (cam == null) return;

                Vector3 worldPos = cam.ScreenToWorldPoint(mouse.position.ReadValue());
                worldPos.z = 0f;
                Vector2Int gridTarget = GridUtils.WorldToGrid(worldPos);

                Confirm(gridTarget);
            }
        }

        private void Confirm(Vector2Int target)
        {
            IsAiming = false;
            var callback = onConfirm;
            onConfirm = null;
            onCancel = null;
            callback?.Invoke(target);
        }

        private void Cancel()
        {
            IsAiming = false;
            var callback = onCancel;
            onConfirm = null;
            onCancel = null;
            callback?.Invoke();
        }
    }
}
