using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 바닥에 숨어 있는 함정의 월드 표현. 상태는 세 단계:
    ///   Hidden    - 바닥과 구분 안 됨(렌더러 꺼서 안 보임)
    ///   Detected  - 플레이어가 근처(탐지 반경)에서 시야로 발견 → 표시됨. 지형처럼 기억된다.
    ///   Triggered - 밟아서 발동됨 → 터진 자국만 남고 더는 작동 안 함.
    ///
    /// 발동 시점: PlayerController가 이동을 마친 직후 FindAt(pos)?.OnSteppedOn(player) 호출.
    /// 탐지: 매 턴 RunDetection()이 모든 함정을 훑어 조건을 만족하면 Detected로 전환.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TrapController : MonoBehaviour
    {
        public enum State { Hidden, Detected, Triggered }

        private static readonly List<TrapController> all = new();
        public static IReadOnlyList<TrapController> All => all;

        public TrapData Data { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public State CurrentState { get; private set; } = State.Hidden;

        private SpriteRenderer spriteRenderer;

        public void Init(TrapData data, Vector2Int pos)
        {
            Data = data;
            GridPosition = pos;
            transform.position = GridUtils.GridToWorld(pos);

            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 1; // 바닥 위, 아이템/액터 아래
            spriteRenderer.enabled = false;  // 처음엔 숨김
            RefreshVisual();

            all.Add(this);
        }

        public static TrapController FindAt(Vector2Int pos)
        {
            foreach (var t in all)
                if (t != null && t.GridPosition == pos && t.CurrentState != State.Triggered)
                    return t;
            return null;
        }

        /// <summary>
        /// 모든 함정에 대해 탐지를 시도. 플레이어가 탐지 반경 안 + 시야가 트이면 Detected로.
        /// 매 턴(그리고 레벨 시작 시) DungeonBootstrap이 호출.
        /// </summary>
        public static void RunDetection(Vector2Int playerPos, DungeonMap map, int detectionRadius)
        {
            foreach (var t in all)
            {
                if (t == null || t.CurrentState != State.Hidden) continue;
                if (GridUtils.ChebyshevDistance(playerPos, t.GridPosition) > detectionRadius) continue;
                if (!GridUtils.HasLineOfSight(map, playerPos, t.GridPosition)) continue;

                t.CurrentState = State.Detected;
                t.RefreshVisual();
            }
        }

        /// <summary>플레이어가 이 칸을 밟았을 때 호출. 발동되지 않았으면 효과 적용.</summary>
        public void OnSteppedOn(PlayerController player)
        {
            if (CurrentState == State.Triggered) return;

            CurrentState = State.Triggered;
            RefreshVisual();

            switch (Data.type)
            {
                case TrapType.Fire:
                    Debug.Log($"{Data.trapName} 발동! 화염이 번진다.");
                    MessageLog.Post($"{Data.trapName} 발동! 화염이 번진다!", MessageLog.Bad);
                    if (TileEffectManager.Instance != null)
                        TileEffectManager.Instance.IgniteArea(GridPosition, Data.fireRadius, Data.fireDuration);
                    break;

                default: // Damage
                    Debug.Log($"{Data.trapName} 발동! 플레이어가 {Data.damage} 피해.");
                    MessageLog.Post($"{Data.trapName} 발동! {Data.damage} 피해!", MessageLog.Bad);
                    player.TakeDamage(Data.damage);
                    break;
            }
        }

        // 흰 사각형 스프라이트 + 색 틴트 방식(바닥 렌더러의 fog 처리와 동일).
        // 커스텀 스프라이트가 지정되면 아트가 그대로 나오도록 틴트는 흰색 기준.
        private Sprite SpriteForState() => CurrentState == State.Triggered
            ? (Data.triggeredSprite != null ? Data.triggeredSprite : SpriteUtils.GetSharedWhiteSquare())
            : (Data.detectedSprite != null ? Data.detectedSprite : SpriteUtils.GetSharedWhiteSquare());

        private Color BaseDisplayColor()
        {
            if (CurrentState == State.Triggered)
                return Data.triggeredSprite != null ? Color.white : Data.triggeredColor;
            return Data.detectedSprite != null ? Color.white : Data.detectedColor;
        }

        /// <summary>현재 상태의 스프라이트/색을 "보이는" 기준으로 적용. 밝기 조절은 fog가 덮어씀.</summary>
        private void RefreshVisual()
        {
            if (spriteRenderer == null || CurrentState == State.Hidden) return;
            spriteRenderer.sprite = SpriteForState();
            spriteRenderer.color = BaseDisplayColor();
            spriteRenderer.enabled = true; // fog가 켜져 있으면 다음 Recompute에서 다시 조절함
        }

        /// <summary>
        /// fog of war 적용. 미탐지 함정은 항상 숨김(바닥처럼 보임).
        /// 탐지·발동된 함정은 지형처럼 취급: 보이면 원색, 기억(explored)이면 어둡게, 아니면 숨김.
        /// </summary>
        public void ApplyVisibility(bool visibleNow, bool explored, float exploredDim)
        {
            if (spriteRenderer == null) return;

            if (CurrentState == State.Hidden)
            {
                spriteRenderer.enabled = false; // 아직 못 찾은 함정은 안 보임
                return;
            }

            bool seen = visibleNow || explored;
            spriteRenderer.enabled = seen;
            if (!seen) return;

            Color c = BaseDisplayColor();
            spriteRenderer.color = visibleNow
                ? c
                : new Color(c.r * exploredDim, c.g * exploredDim, c.b * exploredDim, c.a);
        }

        /// <summary>레벨 전환 시 현재 층의 함정을 전부 정리.</summary>
        public static void DestroyAll()
        {
            foreach (var t in new List<TrapController>(all))
                if (t != null) Destroy(t.gameObject);
            all.Clear();
        }

        private void OnDestroy()
        {
            all.Remove(this);
        }
    }
}
