using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// BSP로 리프 영역을 나누고, 각 리프에 방을 배치한 뒤,
    /// 트리 구조 연결(항상 도달 가능) + 추가 루프 연결(순환 동선)로 통로를 만든다.
    /// 방 타입(입구/출구/보물/일반)은 "그래프 위 거리"를 기준으로 배정한다.
    /// </summary>
    public class DungeonGenerator
    {
        private class BspNode
        {
            public RectInt Area;
            public BspNode Left;
            public BspNode Right;
            public RectInt? RoomBounds;
            public bool IsLeaf => Left == null;
        }

        private GenerationSettings settings;
        private readonly List<BspNode> leaves = new();
        private readonly List<(BspNode a, BspNode b)> edges = new();

        public GeneratedDungeon Generate(GenerationSettings genSettings)
        {
            settings = genSettings;
            leaves.Clear();
            edges.Clear();

            // 1) BSP 분할
            var root = new BspNode { Area = new RectInt(0, 0, settings.mapWidth, settings.mapHeight) };
            Split(root, settings.maxSplitDepth);

            // 2) 각 리프에 방 배치 (아직 타입은 결정 안 함, 위치/크기만)
            foreach (var leaf in leaves)
                CreateRoomBounds(leaf);

            // 3) 트리를 다시 타고 올라가며 형제 노드끼리 연결 (도달 가능성 보장)
            ConnectSubtree(root);

            // 4) 루프용 추가 연결 - 가까운데 안 이어진 리프 쌍을 확률적으로 추가 연결
            AddLoopConnections();

            // 5) 그래프 거리 계산 -> 입구/출구는 가장 먼 쌍으로 배정
            var (entranceLeaf, exitLeaf) = FindFarthestPair();

            // 6) 보물방 후보 선정 (입구/출구 제외, 5x5 이상, 최대 1개)
            BspNode treasureLeaf = null;
            if (Random.value < settings.treasureRoomChance)
            {
                treasureLeaf = leaves
                    .Where(l => l != entranceLeaf && l != exitLeaf)
                    .Where(l => l.RoomBounds.Value.width >= 5 && l.RoomBounds.Value.height >= 5)
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault();
            }

            // 7) 리프 -> 실제 Room 인스턴스로 변환
            var roomByLeaf = new Dictionary<BspNode, Room>();
            foreach (var leaf in leaves)
            {
                var bounds = leaf.RoomBounds.Value;
                Room room =
                    leaf == entranceLeaf ? new EntranceRoom(bounds) :
                    leaf == exitLeaf ? new ExitRoom(bounds) :
                    leaf == treasureLeaf ? new TreasureRoom(bounds) :
                    new StandardRoom(bounds);

                roomByLeaf[leaf] = room;
            }

            // 8) 캔버스에 방 + 통로 그리기
            var canvas = new DungeonCanvas(settings.mapWidth, settings.mapHeight);
            foreach (var room in roomByLeaf.Values)
                room.Paint(canvas);

            foreach (var (a, b) in edges)
                canvas.CarveLine(roomByLeaf[a].Center, roomByLeaf[b].Center);

            var map = new DungeonMap(settings.mapWidth, settings.mapHeight, canvas.GetFloorTiles());

            return new GeneratedDungeon
            {
                Map = map,
                Rooms = roomByLeaf.Values.ToList(),
                Entrance = (EntranceRoom)roomByLeaf[entranceLeaf],
                Exit = (ExitRoom)roomByLeaf[exitLeaf],
            };
        }

        // --- BSP 분할 ---

        private void Split(BspNode node, int depthRemaining)
        {
            bool tooSmall = node.Area.width < settings.minLeafSize * 2 &&
                             node.Area.height < settings.minLeafSize * 2;

            if (depthRemaining <= 0 || tooSmall)
            {
                leaves.Add(node);
                return;
            }

            bool splitHorizontal = node.Area.height > node.Area.width * 1.25f ? true
                : node.Area.width > node.Area.height * 1.25f ? false
                : Random.value > 0.5f;

            int lengthAlongSplit = splitHorizontal ? node.Area.height : node.Area.width;
            int max = lengthAlongSplit - settings.minLeafSize;

            if (max <= settings.minLeafSize)
            {
                leaves.Add(node);
                return;
            }

            int splitAt = Random.Range(settings.minLeafSize, max);

            if (splitHorizontal)
            {
                node.Left = new BspNode { Area = new RectInt(node.Area.x, node.Area.y, node.Area.width, splitAt) };
                node.Right = new BspNode { Area = new RectInt(node.Area.x, node.Area.y + splitAt, node.Area.width, node.Area.height - splitAt) };
            }
            else
            {
                node.Left = new BspNode { Area = new RectInt(node.Area.x, node.Area.y, splitAt, node.Area.height) };
                node.Right = new BspNode { Area = new RectInt(node.Area.x + splitAt, node.Area.y, node.Area.width - splitAt, node.Area.height) };
            }

            Split(node.Left, depthRemaining - 1);
            Split(node.Right, depthRemaining - 1);
        }

        private void CreateRoomBounds(BspNode leaf)
        {
            int pad = settings.roomPadding;
            int maxW = Mathf.Max(settings.minRoomSize, leaf.Area.width - pad);
            int maxH = Mathf.Max(settings.minRoomSize, leaf.Area.height - pad);

            int w = Random.Range(settings.minRoomSize, maxW + 1);
            int h = Random.Range(settings.minRoomSize, maxH + 1);

            int xSlack = Mathf.Max(1, leaf.Area.width - w);
            int ySlack = Mathf.Max(1, leaf.Area.height - h);

            int x = leaf.Area.x + Random.Range(0, xSlack);
            int y = leaf.Area.y + Random.Range(0, ySlack);

            leaf.RoomBounds = new RectInt(x, y, w, h);
        }

        // --- 연결 (트리 + 루프) ---

        /// <summary>서브트리를 연결하고, 상위에서 이어붙일 대표 리프 하나를 반환.</summary>
        private BspNode ConnectSubtree(BspNode node)
        {
            if (node.IsLeaf) return node;

            var leftRep = ConnectSubtree(node.Left);
            var rightRep = ConnectSubtree(node.Right);
            edges.Add((leftRep, rightRep));

            return Random.value > 0.5f ? leftRep : rightRep;
        }

        private void AddLoopConnections()
        {
            var existing = new HashSet<(BspNode, BspNode)>(edges);

            foreach (var leaf in leaves)
            {
                // 자신과 가장 가까운 다른 리프를 찾음
                BspNode nearest = null;
                float nearestDist = float.MaxValue;

                foreach (var other in leaves)
                {
                    if (other == leaf) continue;
                    if (existing.Contains((leaf, other)) || existing.Contains((other, leaf))) continue;

                    float dist = Vector2.Distance(RoomCenter(leaf), RoomCenter(other));
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = other;
                    }
                }

                if (nearest != null && Random.value < settings.extraLoopChance)
                {
                    edges.Add((leaf, nearest));
                    existing.Add((leaf, nearest));
                }
            }
        }

        private Vector2 RoomCenter(BspNode leaf)
        {
            var b = leaf.RoomBounds.Value;
            return new Vector2(b.x + b.width / 2f, b.y + b.height / 2f);
        }

        // --- 그래프 거리 (BFS) ---

        private (BspNode, BspNode) FindFarthestPair()
        {
            // 인접 리스트 구성
            var adjacency = leaves.ToDictionary(l => l, _ => new List<BspNode>());
            foreach (var (a, b) in edges)
            {
                adjacency[a].Add(b);
                adjacency[b].Add(a);
            }

            BspNode bestA = leaves[0], bestB = leaves[0];
            int bestDist = -1;

            // 리프 수가 적으므로(보통 5~15개) 모든 쌍을 BFS로 확인해도 부담 없음
            foreach (var start in leaves)
            {
                var distances = BfsDistances(start, adjacency);
                foreach (var kv in distances)
                {
                    if (kv.Value > bestDist)
                    {
                        bestDist = kv.Value;
                        bestA = start;
                        bestB = kv.Key;
                    }
                }
            }

            return (bestA, bestB);
        }

        private Dictionary<BspNode, int> BfsDistances(BspNode start, Dictionary<BspNode, List<BspNode>> adjacency)
        {
            var dist = new Dictionary<BspNode, int> { [start] = 0 };
            var queue = new Queue<BspNode>();
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
    }
}
