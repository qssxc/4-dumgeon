using UnityEngine;

namespace DungeonCore
{
    public readonly struct CombatResult
    {
        public readonly bool didHit;
        public readonly int damage;

        public CombatResult(bool didHit, int damage)
        {
            this.didHit = didHit;
            this.damage = damage;
        }
    }

    /// <summary>
    /// 전투 계산만 담당하는 정적 클래스. HP 차감이나 연출은 여기서 하지 않는다.
    /// (호출부인 PlayerController/MonsterController가 결과를 받아서 처리)
    /// 이렇게 분리해두면 나중에 "치명타", "회피" 같은 걸 추가할 때
    /// 이 함수만 수정하면 되고 호출부는 안 건드려도 됨.
    /// </summary>
    public static class CombatResolver
    {
        private const float BaseHitChance = 0.85f;

        public static CombatResult Resolve(Combatant attacker, Combatant defender)
        {
            bool hit = Random.value < BaseHitChance;
            if (!hit)
                return new CombatResult(false, 0);

            // 아주 단순한 공식: 공격력 - 방어력의 절반, 최소 1 데미지
            // 오리지널 Pixel Dungeon처럼 min~max 랜덤 롤로 바꾸고 싶으면 여기만 수정
            int rawDamage = attacker.attack - (defender.defense / 2);
            int damage = Mathf.Max(1, rawDamage);

            return new CombatResult(true, damage);
        }
    }
}
