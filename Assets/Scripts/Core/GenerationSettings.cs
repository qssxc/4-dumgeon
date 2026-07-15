namespace DungeonCore
{
    /// <summary>절차적 생성 관련 튜닝 값. 인스펙터 노출이 필요하면 나중에 SO로 옮겨도 됨.</summary>
    public class GenerationSettings
    {
        public int mapWidth = 40;
        public int mapHeight = 40;
        public int maxSplitDepth = 4;      // BSP 분할 깊이 (방 개수에 직결)
        public int minLeafSize = 8;        // 리프 영역 최소 크기
        public int minRoomSize = 4;        // 방 최소 크기
        public int roomPadding = 2;        // 리프 영역과 방 사이 여백

        [UnityEngine.Range(0f, 1f)]
        public float extraLoopChance = 0.25f; // 트리 연결 외 추가 루프 통로가 생길 확률

        [UnityEngine.Range(0f, 1f)]
        public float treasureRoomChance = 0.5f; // 층당 보물방이 "생기기라도 할" 확률 (0~1개 목표)
    }
}
