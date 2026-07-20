namespace DungeonCore
{
    /// <summary>절차적 생성 관련 튜닝 값.</summary>
    public class GenerationSettings
    {
        public int roomCount = 11; // 배치할 콘텐츠 방 개수 (입구 포함)

        public int contentMinSize = 6;  // 콘텐츠 방 최소 크기
        public int contentMaxSize = 11; // 콘텐츠 방 최대 크기 - 방마다 크기가 달라지게 함

        public int tunnelMinSize = 3;   // 통로(TunnelRoom) 최소 크기 - 콘텐츠 방보다 작게 둬야 진짜 통로처럼 보임
        public int tunnelMaxSize = 5;

        // 실제 방 하나를 지날 때마다 그다음에 통로를 몇 칸 넣을지 뽑는 가중치.
        // 인덱스 = 통로 개수, 값 = 가중치. 뽑을 때마다 해당 가중치가 깎여서 몰림을 방지.
        public float[] pathTunnelChances = { 1f, 2f, 3f };

        [UnityEngine.Range(0f, 1f)]
        public float extraLoopChance = 0.3f; // 기하학적으로 붙어있지만 안 이어진 방을 추가 연결할 확률

        [UnityEngine.Range(0f, 1f)]
        public float treasureRoomChance = 0.5f; // 층당 보물방이 생길 확률 (0~1개 목표)
    }
}
