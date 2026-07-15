using UnityEngine;

namespace DungeonCore
{
    /// <summary>가장 흔한 방. 그냥 꽉 찬 사각형.</summary>
    public class StandardRoom : Room
    {
        public StandardRoom(RectInt bounds) : base(bounds) { }

        public override void Paint(DungeonCanvas canvas)
        {
            canvas.CarveRect(Bounds);
        }
    }
}
