using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 생성 과정에서만 쓰는 "쓰기 가능한" 맵 데이터.
    /// 방/통로가 여기에 그려지고, 다 끝나면 DungeonMap(읽기 전용)으로 변환된다.
    /// Room.Paint()는 이 클래스만 알면 되고 최종 DungeonMap 구조는 몰라도 됨.
    /// </summary>
    public class DungeonCanvas
    {
        public readonly int width;
        public readonly int height;
        private readonly HashSet<Vector2Int> floorTiles = new();

        public DungeonCanvas(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void Carve(Vector2Int pos)
        {
            if (pos.x <= 0 || pos.y <= 0 || pos.x >= width - 1 || pos.y >= height - 1)
                return; // 맵 최외곽은 항상 벽으로 남김
            floorTiles.Add(pos);
        }

        public void CarveRect(RectInt rect)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
                for (int y = rect.yMin; y < rect.yMax; y++)
                    Carve(new Vector2Int(x, y));
        }

        public void CarveLine(Vector2Int from, Vector2Int to)
        {
            // L자형 통로: 먼저 가로로, 그다음 세로로
            Vector2Int cursor = from;
            while (cursor.x != to.x)
            {
                Carve(cursor);
                cursor.x += cursor.x < to.x ? 1 : -1;
            }
            while (cursor.y != to.y)
            {
                Carve(cursor);
                cursor.y += cursor.y < to.y ? 1 : -1;
            }
            Carve(to);
        }

        public HashSet<Vector2Int> GetFloorTiles() => floorTiles;
    }
}
