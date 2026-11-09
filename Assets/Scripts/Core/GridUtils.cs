using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 그리드 좌표(Vector2Int) <-> 월드 좌표(Vector3) 변환.
    /// 던전 생성 단계에서도 그대로 재사용하기 위해 별도 정적 클래스로 분리.
    /// </summary>
    public static class GridUtils
    {
        public const float CellSize = 1f;

        public static Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * CellSize, gridPos.y * CellSize, 0f);
        }

        public static Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / CellSize),
                Mathf.RoundToInt(worldPos.y / CellSize)
            );
        }

        // 4방향 인접 좌표
        public static readonly Vector2Int[] Directions4 =
        {
            new Vector2Int(0, 1),   // 위
            new Vector2Int(0, -1),  // 아래
            new Vector2Int(-1, 0),  // 왼쪽
            new Vector2Int(1, 0),   // 오른쪽
        };

        // 8방향 (대각선 포함)
        public static readonly Vector2Int[] Directions8 =
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1),
        };

        public static bool IsDiagonal(Vector2Int dir)
        {
            return dir.x != 0 && dir.y != 0;
        }

        /// <summary>
        /// 대각선 이동이 벽 모서리를 뚫고 지나가는지 검사.
        /// 양옆 두 칸이 모두 막혀 있으면 대각선 이동/공격을 막는다 (코너 컷팅 방지).
        /// </summary>
        public static bool IsDiagonalBlocked(DungeonMap map, Vector2Int from, Vector2Int dir)
        {
            if (!IsDiagonal(dir)) return false;

            bool sideXWalkable = map.IsWalkable(from + new Vector2Int(dir.x, 0));
            bool sideYWalkable = map.IsWalkable(from + new Vector2Int(0, dir.y));

            // 양쪽 다 막혀 있으면 대각선 이동 불가
            return !sideXWalkable && !sideYWalkable;
        }

        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            // 대각선 이동을 고려한 거리 (8방향 이동 시 실제 도달 턴 수와 일치)
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        /// <summary>
        /// from -> to 사이에 벽이 없는지(직선 시야가 트였는지) 검사.
        /// Bresenham 직선으로 중간 칸들을 훑어 하나라도 막혀 있으면 false.
        /// 양 끝 칸(몬스터 위치, 플레이어 위치)은 검사에서 제외한다.
        /// 원거리 몬스터가 "쏠 수 있는지" 판단할 때 사용.
        /// </summary>
        public static bool HasLineOfSight(DungeonMap map, Vector2Int from, Vector2Int to)
        {
            int x0 = from.x, y0 = from.y;
            int x1 = to.x, y1 = to.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0, y = y0;
            while (true)
            {
                if (x == x1 && y == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }

                if (x == x1 && y == y1) break; // 도착 칸은 검사 안 함
                if (!map.IsWalkable(new Vector2Int(x, y)))
                    return false; // 중간에 벽이 있으면 시야 막힘
            }
            return true;
        }
    }
}
