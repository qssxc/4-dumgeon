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
    }
}
