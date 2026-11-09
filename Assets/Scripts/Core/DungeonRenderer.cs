using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// DungeonMap(로직 데이터)을 받아서 화면에 그리기만 하는 클래스.
    /// 생성 로직(DungeonGenerator)과는 완전히 분리되어 있음 - 나중에
    /// Tilemap이나 실제 픽셀아트 타일로 교체해도 이 클래스만 갈아끼우면 됨.
    /// </summary>
    public class DungeonRenderer : MonoBehaviour
    {
        /// <summary>한 타일의 렌더러 + 원래 색(fog of war가 밝기를 조절할 때 기준값).</summary>
        public class TileVisual
        {
            public SpriteRenderer renderer;
            public Color baseColor;
        }

        // 좌표별로 그 자리에 그려진 렌더러들(바닥 위에 문/계단이 겹칠 수 있어 리스트).
        private readonly Dictionary<Vector2Int, List<TileVisual>> tileVisuals = new();
        public IReadOnlyDictionary<Vector2Int, List<TileVisual>> TileVisuals => tileVisuals;

        [SerializeField] private Color floorColor = new Color(0.55f, 0.55f, 0.6f);
        [SerializeField] private Color wallColor = new Color(0.12f, 0.12f, 0.15f);

        [Header("디버그: 방 타입별 색 구분 (생성 검증용)")]
        [SerializeField] private Color entranceColor = new Color(0.3f, 0.9f, 0.3f);
        [SerializeField] private Color exitColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color treasureColor = new Color(0.95f, 0.85f, 0.2f);
        [SerializeField] private Color tunnelColor = new Color(0.4f, 0.4f, 0.48f); // 통로 - 일반 방보다 살짝 어둡게
        [SerializeField] private Color doorColor = new Color(0.5f, 0.32f, 0.15f); // 나무문 느낌의 갈색
        [SerializeField] private Color stairsColor = new Color(0.15f, 0.2f, 0.5f); // 계단 - 짙은 남색으로 눈에 띄게

        /// <summary>단순히 걸을 수 있는 타일만 그림 (방 타입 구분 없음).</summary>
        public void Render(DungeonMap map)
        {
            RenderInternal(map, null, null);
        }

        /// <summary>
        /// 방 타입별로 색을 구분하고, 방-통로 경계에 문을 표시해서 그림 -
        /// 입구/출구/보물방 배정과 방-통로 경계가 의도대로 됐는지 눈으로 검증할 수 있음.
        /// </summary>
        public void Render(GeneratedDungeon dungeon)
        {
            RenderInternal(dungeon.Map, dungeon.Rooms, dungeon.DoorPositions);

            // 계단은 출구 방 전체가 아니라 정확히 상호작용 가능한 그 한 칸에만 표시
            var sprite = SpriteUtils.GetSharedWhiteSquare();
            CreateTileQuad(dungeon.Exit.StairsPosition, sprite, stairsColor, sortingOrder: 2, name: "Stairs");
        }

        private void RenderInternal(DungeonMap map, List<Room> rooms, List<Vector2Int> doorPositions)
        {
            var sprite = SpriteUtils.GetSharedWhiteSquare();

            // 방 타입별 타일 색 미리 계산 (복도 타일은 그냥 기본 floorColor 유지)
            var roomTileColor = new Dictionary<Vector2Int, Color>();
            if (rooms != null)
            {
                foreach (var room in rooms)
                {
                    Color c = GetRoomDebugColor(room);
                    if (c == floorColor) continue; // StandardRoom은 굳이 표시 안 함

                    for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
                    for (int y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
                    {
                        var pos = new Vector2Int(x, y);
                        if (map.IsWalkable(pos))
                            roomTileColor[pos] = c;
                    }
                }
            }

            foreach (var tile in map.GetAllWalkableTiles())
            {
                Color color = roomTileColor.TryGetValue(tile, out var c) ? c : floorColor;
                CreateTileQuad(tile, sprite, color, sortingOrder: 0, name: "Floor");
            }

            // 바닥 타일 하나하나의 인접이 바닥이 아니면 그 자리에 벽을 그림
            var wallPositions = new HashSet<Vector2Int>();
            foreach (var tile in map.GetAllWalkableTiles())
            {
                foreach (var dir in GridUtils.Directions8)
                {
                    var neighbor = tile + dir;
                    if (!map.IsWalkable(neighbor))
                        wallPositions.Add(neighbor);
                }
            }

            foreach (var wallPos in wallPositions)
                CreateTileQuad(wallPos, sprite, wallColor, sortingOrder: -1, name: "Wall");

            // 문은 바닥 위에 덧그림 (방-통로 경계를 시각적으로 명확히 구분)
            if (doorPositions != null)
            {
                foreach (var doorPos in doorPositions)
                    CreateTileQuad(doorPos, sprite, doorColor, sortingOrder: 1, name: "Door");
            }
        }

        private Color GetRoomDebugColor(Room room)
        {
            if (room is EntranceRoom) return entranceColor;
            if (room is ExitRoom) return exitColor;
            if (room is TreasureRoom) return treasureColor;
            if (room is TunnelRoom) return tunnelColor;
            return floorColor;
        }

        private void CreateTileQuad(Vector2Int gridPos, Sprite sprite, Color color, int sortingOrder, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.position = GridUtils.GridToWorld(gridPos);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (!tileVisuals.TryGetValue(gridPos, out var list))
            {
                list = new List<TileVisual>();
                tileVisuals[gridPos] = list;
            }
            list.Add(new TileVisual { renderer = renderer, baseColor = color });
        }
    }
}

