using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 시야(fog of war). 플레이어 기준으로 매 턴 가시 영역을 다시 계산한다.
    /// 타일은 세 가지 상태:
    ///   - 보이는 중(visible): 원래 색 그대로
    ///   - 탐험함(explored): 한 번 본 적 있어 어둡게만 표시(기억)
    ///   - 미탐험(hidden): 아예 렌더 끔
    /// 몬스터/바닥 아이템은 "지금 보이는 칸"에 있을 때만 표시한다(기억에는 안 남김).
    ///
    /// MonoBehaviour가 아니라 평범한 클래스 - 층마다 새로 만들어 쓰고,
    /// 재계산 시점은 DungeonBootstrap이 TurnManager.OnTurnCompleted에 맞춰 호출한다.
    /// </summary>
    public class FogOfWar
    {
        private readonly DungeonMap map;
        private readonly PlayerController player;
        private readonly IReadOnlyDictionary<Vector2Int, List<DungeonRenderer.TileVisual>> tiles;
        private readonly int radius;

        private readonly HashSet<Vector2Int> explored = new();
        private readonly HashSet<Vector2Int> visibleNow = new();

        // 탐험했지만 지금은 안 보이는 칸의 밝기(원래 색에 이 값을 곱함).
        private const float ExploredDim = 0.35f;

        public FogOfWar(DungeonMap map, PlayerController player,
            IReadOnlyDictionary<Vector2Int, List<DungeonRenderer.TileVisual>> tiles, int radius)
        {
            this.map = map;
            this.player = player;
            this.tiles = tiles;
            this.radius = Mathf.Max(1, radius);
        }

        /// <summary>플레이어 위치 기준으로 시야를 다시 계산하고 화면에 반영.</summary>
        public void Recompute()
        {
            if (player == null) return;

            visibleNow.Clear();
            Vector2Int p = player.GridPosition;

            // 반경 안의 칸들 중 시야가 트인(직선에 벽이 없는) 칸을 "보이는 중"으로.
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                var pos = new Vector2Int(p.x + dx, p.y + dy);
                if (GridUtils.ChebyshevDistance(p, pos) > radius) continue;
                if (!tiles.ContainsKey(pos)) continue; // 그 자리에 그려진 게 없음

                if (pos == p || GridUtils.HasLineOfSight(map, p, pos))
                {
                    visibleNow.Add(pos);
                    explored.Add(pos);
                }
            }

            ApplyTiles();
            ApplyEntities();
        }

        private void ApplyTiles()
        {
            foreach (var kv in tiles)
            {
                var pos = kv.Key;
                bool visible = visibleNow.Contains(pos);
                bool seen = visible || explored.Contains(pos);

                foreach (var tv in kv.Value)
                {
                    if (tv.renderer == null) continue;

                    if (!seen)
                    {
                        tv.renderer.enabled = false;
                        continue;
                    }

                    tv.renderer.enabled = true;
                    if (visible)
                    {
                        tv.renderer.color = tv.baseColor;
                    }
                    else
                    {
                        // 탐험 기억: 색상·알파는 유지하되 밝기만 낮춤
                        var c = tv.baseColor;
                        tv.renderer.color = new Color(c.r * ExploredDim, c.g * ExploredDim, c.b * ExploredDim, c.a);
                    }
                }
            }
        }

        private void ApplyEntities()
        {
            // 몬스터/아이템은 "지금 보이는" 칸에 있을 때만 표시
            foreach (var m in MonsterController.All)
                if (m != null) m.SetSeen(visibleNow.Contains(m.GridPosition));

            foreach (var it in ItemPickup.All)
                if (it != null) it.SetSeen(visibleNow.Contains(it.GridPosition));

            // 함정은 지형처럼 취급: 탐지/발동된 함정은 기억(explored)에도 남는다.
            foreach (var t in TrapController.All)
                if (t != null)
                    t.ApplyVisibility(visibleNow.Contains(t.GridPosition), explored.Contains(t.GridPosition), ExploredDim);
        }
    }
}
