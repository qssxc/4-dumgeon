using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 방 하나의 추상 표현. BSP가 넘겨준 영역(bounds) 안에서
    /// 실제로 어떤 모양을 그릴지는 서브클래스가 각자 책임진다.
    /// 이렇게 분리하면 "보물방은 항상 기둥이 있다" 같은 일관성을
    /// 타입 단위로 보장할 수 있다.
    /// </summary>
    public abstract class Room
    {
        public RectInt Bounds { get; }
        public List<Vector2Int> ConnectionPoints { get; } = new(); // 통로가 붙는 위치(문)

        protected Room(RectInt bounds)
        {
            Bounds = bounds;
        }

        public Vector2Int Center =>
            new Vector2Int(Bounds.x + Bounds.width / 2, Bounds.y + Bounds.height / 2);

        /// <summary>이 방의 모양을 캔버스에 그린다. 서브클래스마다 다르게 구현.</summary>
        public abstract void Paint(DungeonCanvas canvas);

        /// <summary>몬스터/아이템을 놓을 후보 위치. 기본은 방 내부 랜덤 지점.</summary>
        public virtual List<Vector2Int> GetContentSpawnPoints(int count)
        {
            var points = new List<Vector2Int>();
            int attempts = 0;
            while (points.Count < count && attempts < count * 10)
            {
                attempts++;
                var p = new Vector2Int(
                    Random.Range(Bounds.xMin + 1, Bounds.xMax - 1),
                    Random.Range(Bounds.yMin + 1, Bounds.yMax - 1)
                );
                if (!points.Contains(p)) points.Add(p);
            }
            return points;
        }
    }
}
