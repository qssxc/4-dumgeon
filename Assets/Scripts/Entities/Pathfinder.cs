using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 격자 위 최단 경로(BFS, 8방향). 비용이 균일하므로 BFS로 충분하다.
    /// 코너 컷팅(대각선으로 벽 모서리 통과)과 막힌 칸(몬스터 등)을 걸러낸다.
    /// 클릭 이동(목표까지 자동 이동)에 사용.
    /// </summary>
    public static class Pathfinder
    {
        /// <summary>
        /// start에서 goal까지의 경로를 start 다음 칸부터 goal까지 순서대로 반환. 도달 불가면 null.
        /// blocked: 지나갈 수 없는 칸 판정(예: 몬스터가 있는 칸). goal 자체는 blocked여도 목적지로 허용.
        /// </summary>
        public static List<Vector2Int> FindPath(DungeonMap map, Vector2Int start, Vector2Int goal,
            Func<Vector2Int, bool> blocked = null)
        {
            if (start == goal) return new List<Vector2Int>();
            if (!map.IsWalkable(goal)) return null;

            var cameFrom = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            bool found = false;
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == goal) { found = true; break; }

                foreach (var dir in GridUtils.Directions8)
                {
                    var nb = cur + dir;
                    if (cameFrom.ContainsKey(nb)) continue;
                    if (!map.IsWalkable(nb)) continue;
                    if (GridUtils.IsDiagonalBlocked(map, cur, dir)) continue;       // 코너 컷팅 방지
                    if (nb != goal && blocked != null && blocked(nb)) continue;     // 막힌 칸은 경유 불가(목적지는 허용)

                    cameFrom[nb] = cur;
                    queue.Enqueue(nb);
                }
            }

            if (!found && !cameFrom.ContainsKey(goal)) return null;

            // 목표에서 역추적
            var path = new List<Vector2Int>();
            var c = goal;
            while (c != start)
            {
                path.Add(c);
                c = cameFrom[c];
            }
            path.Reverse();
            return path;
        }
    }
}
