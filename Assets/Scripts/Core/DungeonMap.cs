using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 1단계에서는 절차적 생성 없이 고정 크기 방 하나만 사용.
    /// 나중에 DungeonGenerator가 이 클래스와 같은 형태의 데이터를 만들어내면
    /// 렌더러/이동 로직은 손댈 필요가 없도록 인터페이스를 맞춰둠.
    /// </summary>
    public class DungeonMap
    {
        public readonly int width;
        public readonly int height;
        private readonly HashSet<Vector2Int> walkableTiles = new();

        public DungeonMap(int width, int height)
        {
            this.width = width;
            this.height = height;

            // 1단계: 테두리는 벽, 안쪽은 전부 바닥인 단순한 사각형 방
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool isEdge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    if (!isEdge)
                        walkableTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>
        /// 절차적 생성 결과(DungeonCanvas)를 그대로 받아 DungeonMap을 만드는 생성자.
        /// 1단계의 고정 방 생성자와 별개로 존재 - PlayerController/MonsterController는
        /// 어느 쪽으로 만들어졌는지 몰라도 되고, IsWalkable()만 쓰면 됨.
        /// </summary>
        public DungeonMap(int width, int height, HashSet<Vector2Int> generatedFloorTiles)
        {
            this.width = width;
            this.height = height;
            walkableTiles = new HashSet<Vector2Int>(generatedFloorTiles);
        }

        public bool IsWalkable(Vector2Int pos)
        {
            return walkableTiles.Contains(pos);
        }

        public Vector2Int GetRandomWalkableTile()
        {
            int index = Random.Range(0, walkableTiles.Count);
            int i = 0;
            foreach (var tile in walkableTiles)
            {
                if (i == index) return tile;
                i++;
            }
            return Vector2Int.zero;
        }

        /// <summary>
        /// 던전 전체 바닥 타일 목록. 방 타입과 무관하게 몬스터를 맵 전체에
        /// 흩뿌리기 위해 사용 (특정 방에 종속시키지 않음).
        /// </summary>
        public IReadOnlyCollection<Vector2Int> GetAllWalkableTiles()
        {
            return walkableTiles;
        }
    }
}
