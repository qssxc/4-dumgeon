using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 어디에 생성되든 항상 "보물방답게" 보이도록 고정 패턴(모서리 기둥)을 사용.
    /// 이게 "방마다 일관된 생성 로직"의 핵심 예시.
    /// </summary>
    public class TreasureRoom : Room
    {
        public TreasureRoom(RectInt bounds) : base(bounds) { }

        public override void Paint(DungeonCanvas canvas)
        {
            for (int x = Bounds.xMin; x < Bounds.xMax; x++)
            {
                for (int y = Bounds.yMin; y < Bounds.yMax; y++)
                {
                    if (IsPillarPosition(x, y)) continue; // 기둥 자리는 뚫지 않고 남겨둠
                    canvas.Carve(new Vector2Int(x, y));
                }
            }
        }

        private bool IsPillarPosition(int x, int y)
        {
            // 5x5 미만이면 기둥을 넣기엔 너무 좁음 -> 그냥 빈 사각형으로
            if (Bounds.width < 5 || Bounds.height < 5) return false;

            bool nearLeft = x == Bounds.xMin + 1;
            bool nearRight = x == Bounds.xMax - 2;
            bool nearBottom = y == Bounds.yMin + 1;
            bool nearTop = y == Bounds.yMax - 2;

            return (nearLeft || nearRight) && (nearBottom || nearTop);
        }

        public override List<Vector2Int> GetContentSpawnPoints(int count)
        {
            // 보물방 규칙: 항상 중앙에 딱 하나만 (보물이 여러 개 흩어져 있지 않게)
            return new List<Vector2Int> { Center };
        }
    }
}
