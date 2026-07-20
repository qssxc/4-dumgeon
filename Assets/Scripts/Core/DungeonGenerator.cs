using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 원작 Pixel Dungeon의 Builder.placeRoom() / findFreeSpace()를 참고한 자유배치 생성기.
    /// 고정 그리드 대신, 이전 방의 벽 위 무작위 지점(anchor)을 기준으로 새 방을 붙여나간다.
    /// anchor가 두 방 모두의 범위 안에 들어가도록 새 방을 배치하므로 문이 항상 유효한
    /// 위치에 생기면서도, 방끼리 격자에 맞춰 정렬되지 않고 자연스럽게 어긋난다.
    /// 방 크기도 매번 랜덤이라 원작처럼 크고 작은 방이 섞인다.
    ///
    /// 통로(TunnelRoom)는 "실제 방 - 통로 체인 - 실제 방"이 확정될 때만 통로로 확정하고,
    /// 체인이 막히면 만들어진 칸을 버리지 않고 콘텐츠 방으로 편입한다 (막다른 통로 방지).
    /// </summary>
    public class DungeonGenerator
    {
        private class PlacedRoom
        {
            public RectInt Bounds;
            public PlacedRoom(RectInt bounds) { Bounds = bounds; }
        }

        private struct Edge
        {
            public int A, B;
            public Edge(int a, int b) { A = a; B = b; }
        }

        private GenerationSettings settings;

        public GeneratedDungeon Generate(GenerationSettings genSettings)
        {
            settings = genSettings;

            var placed = new List<PlacedRoom>();
            var edges = new List<Edge>();
            var tunnelIndices = new HashSet<int>();
            var contentIndices = new HashSet<int>();

            // 입구를 원점에 배치
            int entranceSize = Random.Range(settings.contentMinSize, settings.contentMaxSize + 1);
            placed.Add(new PlacedRoom(new RectInt(0, 0, entranceSize, entranceSize)));
            const int entranceIndex = 0;
            contentIndices.Add(entranceIndex);

            int contentRemaining = settings.roomCount - 1;
            var pathWeights = (float[])settings.pathTunnelChances.Clone();
            int safety = 0;

            // 1) 콘텐츠 방 배치 -> (필요시) 통로 체인 -> 체인 끝에 콘텐츠 방 확정 부착
            while (contentRemaining > 0 && safety < 5000)
            {
                safety++;

                int sourceIndex = contentIndices.ElementAt(Random.Range(0, contentIndices.Count));
                if (!TryPlaceRoom(placed, edges, sourceIndex, settings.contentMinSize, settings.contentMaxSize, out int targetIndex))
                    continue;

                contentIndices.Add(targetIndex);
                contentRemaining--;

                if (contentRemaining <= 0) break;

                int tunnelRun = WeightedChoice(pathWeights);
                if (tunnelRun < 0)
                {
                    pathWeights = (float[])settings.pathTunnelChances.Clone();
                    tunnelRun = WeightedChoice(pathWeights);
                }
                pathWeights[tunnelRun]--;

                if (tunnelRun <= 0) continue;

                var chain = new List<int>();
                int cursor = targetIndex;
                bool chainOk = true;
                for (int j = 0; j < tunnelRun; j++)
                {
                    if (!TryPlaceRoom(placed, edges, cursor, settings.tunnelMinSize, settings.tunnelMaxSize, out int chainIdx))
                    {
                        chainOk = false;
                        break;
                    }
                    chain.Add(chainIdx);
                    cursor = chainIdx;
                }

                int? finalIndex = null;
                if (chainOk && TryPlaceRoom(placed, edges, cursor, settings.contentMinSize, settings.contentMaxSize, out int finalIdx))
                    finalIndex = finalIdx;

                if (finalIndex.HasValue)
                {
                    foreach (var c in chain) tunnelIndices.Add(c);
                    contentIndices.Add(finalIndex.Value);
                    contentRemaining--;
                }
                else
                {
                    foreach (var c in chain) contentIndices.Add(c); // 실패분은 버리지 않고 콘텐츠로 편입
                }
            }

            // 2) 기하학적으로 붙어있지만 안 이어진 방들을 확률적으로 추가 연결 (루프)
            AddLoopConnections(placed, edges, tunnelIndices);

            // 3) 콘텐츠 방 중 입구에서 가장 먼 방을 출구로
            int exitIndex = FindFarthestAmong(entranceIndex, placed.Count, edges, contentIndices);

            // 4) 보물방 후보 (콘텐츠 방 중에서만, 최대 1개)
            int? treasureIndex = null;
            if (Random.value < settings.treasureRoomChance)
            {
                treasureIndex = contentIndices
                    .Where(i => i != entranceIndex && i != exitIndex)
                    .OrderBy(_ => Random.value)
                    .Select(i => (int?)i)
                    .FirstOrDefault();
            }

            // 5) 인덱스 -> 실제 Room 인스턴스
            var rooms = new Room[placed.Count];
            for (int i = 0; i < placed.Count; i++)
            {
                var bounds = placed[i].Bounds;
                rooms[i] =
                    tunnelIndices.Contains(i) ? new TunnelRoom(bounds) :
                    i == entranceIndex ? new EntranceRoom(bounds) :
                    i == exitIndex ? new ExitRoom(bounds) :
                    i == treasureIndex ? new TreasureRoom(bounds) :
                    new StandardRoom(bounds);
            }

            // 6) 전체를 감싸는 경계 계산 후, 좌표가 전부 양수가 되도록 이동
            int margin = 2;
            int minX = placed.Min(p => p.Bounds.xMin);
            int minY = placed.Min(p => p.Bounds.yMin);
            int maxX = placed.Max(p => p.Bounds.xMax);
            int maxY = placed.Max(p => p.Bounds.yMax);
            var offset = new Vector2Int(-minX + margin, -minY + margin);

            int mapWidth = (maxX - minX) + margin * 2;
            int mapHeight = (maxY - minY) + margin * 2;

            for (int i = 0; i < rooms.Length; i++)
            {
                var b = rooms[i].Bounds;
                var shifted = new RectInt(b.x + offset.x, b.y + offset.y, b.width, b.height);
                rooms[i] = RebuildWithBounds(rooms[i], shifted);
            }

            // 7) 문 위치 계산 (실제 겹치는 구간의 중앙을 사용하므로 방 크기가 달라도 정확함)
            var doorPositions = new List<Vector2Int>();
            var doorsByIndex = new List<List<Vector2Int>>();
            for (int i = 0; i < rooms.Length; i++) doorsByIndex.Add(new List<Vector2Int>());

            foreach (var e in edges)
            {
                var doorPos = ComputeDoorPosition(rooms[e.A].Bounds, rooms[e.B].Bounds);
                doorPositions.Add(doorPos);
                doorsByIndex[e.A].Add(doorPos);
                doorsByIndex[e.B].Add(doorPos);
            }

            // 8) TunnelRoom들에 문 목록 주입
            foreach (var i in tunnelIndices)
                if (rooms[i] is TunnelRoom tunnel)
                    tunnel.SetConnectedDoors(doorsByIndex[i]);

            // 9) 캔버스에 방 그리기 + 문 뚫기
            var canvas = new DungeonCanvas(mapWidth, mapHeight);
            foreach (var room in rooms)
                room.Paint(canvas);
            foreach (var doorPos in doorPositions)
                canvas.Carve(doorPos);

            var map = new DungeonMap(mapWidth, mapHeight, canvas.GetFloorTiles());

            return new GeneratedDungeon
            {
                Map = map,
                Rooms = rooms.ToList(),
                Entrance = (EntranceRoom)rooms[entranceIndex],
                Exit = (ExitRoom)rooms[exitIndex],
                DoorPositions = doorPositions,
            };
        }

        /// <summary>
        /// sourceIndex 방의 한쪽 벽에서 무작위 지점을 골라, 그 지점을 공유하도록
        /// 새 방을 배치한다 (원작 placeRoom()의 단순화 버전). 4방향 x 여러 크기 시도 중
        /// 겹치지 않는 조합을 찾으면 성공.
        /// </summary>
        private bool TryPlaceRoom(List<PlacedRoom> placed, List<Edge> edges, int sourceIndex, int minSize, int maxSize, out int newIndex)
        {
            newIndex = -1;
            var prevBounds = placed[sourceIndex].Bounds;

            foreach (var dir in GridUtils.Directions4.OrderBy(_ => Random.value))
            {
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    int w = Random.Range(minSize, maxSize + 1);
                    int h = Random.Range(minSize, maxSize + 1);
                    var candidate = ComputeCandidateBounds(prevBounds, dir, w, h);

                    if (CollidesWithAny(candidate, placed)) continue;

                    placed.Add(new PlacedRoom(candidate));
                    newIndex = placed.Count - 1;
                    edges.Add(new Edge(sourceIndex, newIndex));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// prev의 dir쪽 벽 위에서 anchor(무작위 지점)를 고르고, 그 지점이 새 방의
        /// 범위 안에도 들어가도록 새 방의 위치를 정한다. 이러면 항상 겹치는 구간이
        /// 최소 1칸 보장되면서도(문이 생길 자리), 방끼리 중앙이 안 맞고 자연스럽게 어긋난다.
        /// </summary>
        private RectInt ComputeCandidateBounds(RectInt prev, Vector2Int dir, int w, int h)
        {
            if (dir.x != 0)
            {
                int anchor = Random.Range(prev.yMin, prev.yMax);
                int offset = Random.Range(0, h);
                int yMin = anchor - offset;
                int xMin = dir.x > 0 ? prev.xMax + 1 : prev.xMin - w - 1;
                return new RectInt(xMin, yMin, w, h);
            }
            else
            {
                int anchor = Random.Range(prev.xMin, prev.xMax);
                int offset = Random.Range(0, w);
                int xMin = anchor - offset;
                int yMin = dir.y > 0 ? prev.yMax + 1 : prev.yMin - h - 1;
                return new RectInt(xMin, yMin, w, h);
            }
        }

        private bool CollidesWithAny(RectInt candidate, List<PlacedRoom> placed)
        {
            // 1칸 여유를 두고 검사해서 방끼리 우연히 딱 붙어버리는 걸 방지
            var inflated = new RectInt(candidate.x - 1, candidate.y - 1, candidate.width + 2, candidate.height + 2);
            foreach (var p in placed)
                if (inflated.Overlaps(p.Bounds))
                    return true;
            return false;
        }

        private void AddLoopConnections(List<PlacedRoom> placed, List<Edge> edges, HashSet<int> tunnelIndices)
        {
            var existing = new HashSet<(int, int)>();
            foreach (var e in edges) existing.Add(Canonical(e.A, e.B));

            for (int i = 0; i < placed.Count; i++)
            {
                if (tunnelIndices.Contains(i)) continue;
                for (int j = i + 1; j < placed.Count; j++)
                {
                    if (tunnelIndices.Contains(j)) continue;

                    var key = Canonical(i, j);
                    if (existing.Contains(key)) continue;
                    if (!AreAdjacent(placed[i].Bounds, placed[j].Bounds)) continue;

                    if (Random.value < settings.extraLoopChance)
                    {
                        edges.Add(new Edge(i, j));
                        existing.Add(key);
                    }
                }
            }
        }

        private (int, int) Canonical(int a, int b) => a < b ? (a, b) : (b, a);

        /// <summary>두 방이 정확히 1칸 벽을 사이에 두고, 겹치는 구간이 있는지 확인.</summary>
        private bool AreAdjacent(RectInt a, RectInt b)
        {
            bool horiz = (b.xMin == a.xMax + 1 || a.xMin == b.xMax + 1)
                         && Mathf.Max(a.yMin, b.yMin) < Mathf.Min(a.yMax, b.yMax);
            bool vert = (b.yMin == a.yMax + 1 || a.yMin == b.yMax + 1)
                        && Mathf.Max(a.xMin, b.xMin) < Mathf.Min(a.xMax, b.xMax);
            return horiz || vert;
        }

        /// <summary>원작의 Random.chances()에 대응. 모든 가중치가 0 이하면 -1 반환.</summary>
        private int WeightedChoice(float[] weights)
        {
            float total = 0f;
            foreach (var w in weights) total += Mathf.Max(0f, w);
            if (total <= 0f) return -1;

            float roll = Random.value * total;
            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += Mathf.Max(0f, weights[i]);
                if (roll <= cumulative) return i;
            }
            return weights.Length - 1;
        }

        private int FindFarthestAmong(int start, int roomCount, List<Edge> edges, HashSet<int> candidates)
        {
            var dist = ComputeBfsDistances(start, roomCount, edges);
            int farthest = start;
            int best = -1;
            foreach (var kv in dist)
            {
                if (!candidates.Contains(kv.Key)) continue;
                if (kv.Value > best)
                {
                    best = kv.Value;
                    farthest = kv.Key;
                }
            }
            return farthest;
        }

        private Dictionary<int, int> ComputeBfsDistances(int start, int roomCount, List<Edge> edges)
        {
            var adjacency = new List<int>[roomCount];
            for (int i = 0; i < roomCount; i++) adjacency[i] = new List<int>();
            foreach (var e in edges)
            {
                adjacency[e.A].Add(e.B);
                adjacency[e.B].Add(e.A);
            }

            var dist = new Dictionary<int, int> { [start] = 0 };
            var queue = new Queue<int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in adjacency[current])
                {
                    if (dist.ContainsKey(neighbor)) continue;
                    dist[neighbor] = dist[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
            return dist;
        }

        // Room은 생성자에서 Bounds가 고정되므로, 좌표 이동 후 같은 타입으로 다시 만들어서 교체한다.
        private Room RebuildWithBounds(Room original, RectInt newBounds)
        {
            return original switch
            {
                EntranceRoom => new EntranceRoom(newBounds),
                ExitRoom => new ExitRoom(newBounds),
                TreasureRoom => new TreasureRoom(newBounds),
                TunnelRoom => new TunnelRoom(newBounds),
                _ => new StandardRoom(newBounds),
            };
        }

        /// <summary>
        /// 두 인접한 방 사이의 문 위치를, 실제 겹치는 구간의 중앙으로 계산.
        /// 방 크기가 서로 달라도(자유배치라 흔함) 항상 유효한 위치를 찾는다.
        /// </summary>
        private Vector2Int ComputeDoorPosition(RectInt a, RectInt b)
        {
            if (b.xMin > a.xMax) // b가 오른쪽
            {
                int yMin = Mathf.Max(a.yMin, b.yMin), yMax = Mathf.Min(a.yMax, b.yMax);
                int y = Mathf.Clamp((yMin + yMax - 1) / 2, yMin, yMax - 1);
                return new Vector2Int(a.xMax, y);
            }
            if (a.xMin > b.xMax) // b가 왼쪽
            {
                int yMin = Mathf.Max(a.yMin, b.yMin), yMax = Mathf.Min(a.yMax, b.yMax);
                int y = Mathf.Clamp((yMin + yMax - 1) / 2, yMin, yMax - 1);
                return new Vector2Int(b.xMax, y);
            }
            if (b.yMin > a.yMax) // b가 위
            {
                int xMin = Mathf.Max(a.xMin, b.xMin), xMax = Mathf.Min(a.xMax, b.xMax);
                int x = Mathf.Clamp((xMin + xMax - 1) / 2, xMin, xMax - 1);
                return new Vector2Int(x, a.yMax);
            }
            // b가 아래
            {
                int xMin = Mathf.Max(a.xMin, b.xMin), xMax = Mathf.Min(a.xMax, b.xMax);
                int x = Mathf.Clamp((xMin + xMax - 1) / 2, xMin, xMax - 1);
                return new Vector2Int(x, b.yMax);
            }
        }
    }
}
