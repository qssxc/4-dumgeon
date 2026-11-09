namespace DungeonCore
{
    /// <summary>
    /// 몬스터의 기본 행동 패턴. MonsterData.behavior로 지정하며,
    /// MonsterController.DecideAndAct()에서 분기한다.
    /// 새 패턴(도망만 치는 적, 소환사 등)을 추가할 때 여기에 값만 늘리면 된다.
    /// </summary>
    public enum MonsterBehavior
    {
        MeleeChaser, // 근접: 다가가서 인접하면 때림 (기존 기본 동작)
        Ranged,      // 원거리: 사거리 안 + 시야 트이면 투사체 공격, 너무 붙으면 거리 벌림(카이팅)
    }
}
