using System.Collections;

namespace DungeonCore
{
    /// <summary>
    /// 플레이어든 몬스터든, "턴을 가지는 모든 것"은 이 인터페이스를 구현한다.
    /// TurnManager는 이 인터페이스만 알면 되고, 누가 플레이어이고 몬스터인지 몰라도 됨.
    /// </summary>
    public interface ITurnActor
    {
        int Speed { get; }          // 100 = 표준 속도. 높을수록 자주 행동.
        float Energy { get; set; }
        bool IsAlive { get; }

        /// <summary>
        /// 이 액터의 턴 차례가 왔을 때 호출됨.
        /// 플레이어는 입력을 기다리고, 몬스터는 AI로 즉시 행동을 결정한다.
        /// 코루틴으로 만든 이유: 애니메이션 재생이 끝날 때까지 다음 턴 계산을 미루기 위함.
        /// </summary>
        IEnumerator TakeTurn(TurnManager manager);
    }
}
