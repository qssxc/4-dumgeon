using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 원작 Pixel Dungeon의 TunnelRoom을 참고한 구현.
    /// 다른 방처럼 크기(Bounds)를 배정받지만, 영역을 꽉 채우지 않고
    /// 자신에게 연결된 문들을 중앙의 한 지점(hub)으로 최소한의 L자 선으로만 잇는다.
    /// 나머지 공간은 벽으로 남아서 겉보기엔 가늘고 긴 통로처럼 보인다.
    ///
    /// 원작과 달리 이 방은 자신의 연결 정보를 스스로 추적하지 않으므로,
    /// Paint() 호출 전에 반드시 SetConnectedDoors()로 문 목록을 주입해야 한다.
    /// </summary>
    public class TunnelRoom : Room
    {
        private readonly List<Vector2Int> doorEntryPoints = new();

        public TunnelRoom(RectInt bounds) : base(bounds) { }

        /// <summary>이 방과 맞닿은 문 위치들을 등록. Paint() 전에 반드시 호출해야 함.</summary>
        public void SetConnectedDoors(IEnumerable<Vector2Int> doorPositions)
        {
            doorEntryPoints.Clear();
            foreach (var doorPos in doorPositions)
            {
                // 문은 방 경계 바로 바깥(벽 틈)에 있으므로, 경로 시작점은 경계 안쪽으로 클램프
                int x = Mathf.Clamp(doorPos.x, Bounds.xMin, Bounds.xMax - 1);
                int y = Mathf.Clamp(doorPos.y, Bounds.yMin, Bounds.yMax - 1);
                doorEntryPoints.Add(new Vector2Int(x, y));
            }
        }

        public override void Paint(DungeonCanvas canvas)
        {
            if (doorEntryPoints.Count == 0)
            {
                // 안전장치: 문 정보 없이 호출되면(설정 누락 등) 그냥 일반 방처럼 채움
                canvas.CarveRect(Bounds);
                return;
            }

            var hub = ComputeHub();
            foreach (var entry in doorEntryPoints)
                CarveLShape(canvas, entry, hub);
        }

        /// <summary>모든 문에서 대략 같은 거리에 있는 중심점 (원작의 getDoorCenter()에 해당).</summary>
        private Vector2Int ComputeHub()
        {
            int sumX = 0, sumY = 0;
            foreach (var p in doorEntryPoints)
            {
                sumX += p.x;
                sumY += p.y;
            }

            int hubX = Mathf.Clamp(sumX / doorEntryPoints.Count, Bounds.xMin, Bounds.xMax - 1);
            int hubY = Mathf.Clamp(sumY / doorEntryPoints.Count, Bounds.yMin, Bounds.yMax - 1);
            return new Vector2Int(hubX, hubY);
        }

        private void CarveLShape(DungeonCanvas canvas, Vector2Int from, Vector2Int to)
        {
            var corner = new Vector2Int(to.x, from.y);
            CarveStraight(canvas, from, corner);
            CarveStraight(canvas, corner, to);
        }

        private void CarveStraight(DungeonCanvas canvas, Vector2Int a, Vector2Int b)
        {
            if (a.x == b.x)
            {
                int minY = Mathf.Min(a.y, b.y), maxY = Mathf.Max(a.y, b.y);
                for (int y = minY; y <= maxY; y++)
                    canvas.Carve(new Vector2Int(a.x, y));
            }
            else
            {
                int minX = Mathf.Min(a.x, b.x), maxX = Mathf.Max(a.x, b.x);
                for (int x = minX; x <= maxX; x++)
                    canvas.Carve(new Vector2Int(x, a.y));
            }
        }

        public override List<Vector2Int> GetContentSpawnPoints(int count)
        {
            return new List<Vector2Int>(); // 통로 방에는 몬스터/아이템을 두지 않음
        }
    }
}
