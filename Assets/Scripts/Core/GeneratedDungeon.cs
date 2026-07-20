using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>DungeonGenerator.Generate()의 결과물. 맵/방 목록/입구·출구/문 위치를 한 번에 담는다.</summary>
    public class GeneratedDungeon
    {
        public DungeonMap Map;
        public List<Room> Rooms;
        public EntranceRoom Entrance;
        public ExitRoom Exit;
        public List<Vector2Int> DoorPositions = new();
    }
}
