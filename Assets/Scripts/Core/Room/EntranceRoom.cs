using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 플레이어가 시작하는 방. 모양은 StandardRoom과 같지만
    /// 몬스터/함정을 스폰하지 않는다는 규칙을 타입으로 강제.
    /// </summary>
    public class EntranceRoom : Room
    {
        public EntranceRoom(RectInt bounds) : base(bounds) { }

        public override void Paint(DungeonCanvas canvas)
        {
            canvas.CarveRect(Bounds);
        }

        public override List<Vector2Int> GetContentSpawnPoints(int count)
        {
            return new List<Vector2Int>(); // 입구방은 항상 안전지대
        }
    }
}
