using UnityEngine;

namespace DungeonCore
{
    /// <summary>다음 층 계단이 놓이는 방. StairsPosition은 항상 방 중앙.</summary>
    public class ExitRoom : Room
    {
        public Vector2Int StairsPosition => Center;

        public ExitRoom(RectInt bounds) : base(bounds) { }

        public override void Paint(DungeonCanvas canvas)
        {
            canvas.CarveRect(Bounds);
        }
    }
}
