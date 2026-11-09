using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 몬스터의 정적 수치. 코드에 하드코딩하지 않고 에셋으로 관리해서
    /// 재컴파일 없이 밸런스 조정이 가능하게 함.
    /// 우클릭 > Create > Dungeon > Monster 로 에셋 생성 가능.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Monster", fileName = "NewMonster")]
    public class MonsterData : ScriptableObject
    {
        public string monsterName = "Rat";
        public int maxHP = 10;
        public int attack = 3;
        public int defense = 1;
        public int speed = 100;
        public int detectRange = 5;   // 이 거리 안에 플레이어가 들어오면 추적 시작
        public int xpReward = 5;      // 처치 시 플레이어가 얻는 경험치
        public Sprite sprite;         // 없으면 MonsterController가 색 사각형으로 대체

        [Header("AI / 행동")]
        public MonsterBehavior behavior = MonsterBehavior.MeleeChaser;
        public int actionsPerTurn = 1;   // 한 턴에 행동하는 횟수 (빠른 적은 2)
        public int attackRange = 1;      // 원거리 적의 사거리 (Ranged일 때 사용, 체비셰프 거리)
        public Color projectileColor = new Color(0.8f, 0.4f, 1f); // 투사체 색 (스프라이트 없을 때)

        [Header("애니메이션 (선택)")]
        public Sprite[] idleFrames;
        public Sprite[] walkFrames;
        public Sprite[] attackFrames;
        public float idleFps = 4f;
        public float walkAnimSeconds = 0.25f;
        public float attackAnimSeconds = 0.3f;
    }
}
