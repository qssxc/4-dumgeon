using System;

namespace DungeonCore
{
    /// <summary>
    /// 전투 계산에 필요한 최소 데이터. PlayerController와 MonsterController가
    /// 각각 이 클래스를 필드로 들고 있는 형태로 사용 (상속보다 조합 선호).
    ///
    /// 장비 보너스는 baseAttack/baseDefense에 직접 더하지 않고, 외부에서 주입한
    /// 보너스 제공자(bonusProvider)를 통해 조회 시점에 합산한다. 이래야 장비를
    /// 갈아끼워도 기본 수치가 오염되지 않는다.
    /// </summary>
    public class Combatant
    {
        public string displayName;
        public int maxHP;
        public int currentHP;
        public int baseAttack;
        public int baseDefense;

        // 장비 등 외부 보너스 공급원. 없으면(몬스터 등) 0으로 취급.
        private Func<int> attackBonusProvider;
        private Func<int> defenseBonusProvider;

        public int attack => baseAttack + (attackBonusProvider?.Invoke() ?? 0);
        public int defense => baseDefense + (defenseBonusProvider?.Invoke() ?? 0);

        // 임시 속도 버프 (신속 포션 등). 남은 턴이 0이면 효과 없음.
        private int hasteAmount;
        private int hasteTurnsLeft;
        public int SpeedBonus => hasteTurnsLeft > 0 ? hasteAmount : 0;

        public bool IsAlive => currentHP > 0;

        public Combatant(string name, int maxHP, int attack, int defense)
        {
            this.displayName = name;
            this.maxHP = maxHP;
            this.currentHP = maxHP;
            this.baseAttack = attack;
            this.baseDefense = defense;
        }

        /// <summary>장비 보너스 공급원을 연결 (플레이어 전용, 몬스터는 호출하지 않음).</summary>
        public void SetBonusProviders(Func<int> attackBonus, Func<int> defenseBonus)
        {
            attackBonusProvider = attackBonus;
            defenseBonusProvider = defenseBonus;
        }

        public void TakeDamage(int amount)
        {
            currentHP = UnityEngine.Mathf.Max(0, currentHP - amount);
        }

        /// <summary>체력 회복 (최대치 초과 안 함). 실제 회복된 양을 반환.</summary>
        public int Heal(int amount)
        {
            int before = currentHP;
            currentHP = UnityEngine.Mathf.Min(maxHP, currentHP + amount);
            return currentHP - before;
        }

        /// <summary>영구 공격력 상승 (힘 포션 등).</summary>
        public void AddPermanentAttack(int amount)
        {
            baseAttack += amount;
        }

        /// <summary>임시 속도 버프 부여 (신속 포션 등). 겹치면 더 긴 지속시간으로 갱신.</summary>
        public void ApplyHaste(int amount, int turns)
        {
            hasteAmount = amount;
            hasteTurnsLeft = UnityEngine.Mathf.Max(hasteTurnsLeft, turns);
        }

        /// <summary>턴이 하나 지날 때 호출 - 버프 지속시간 감소.</summary>
        public void OnTurnPassed()
        {
            if (hasteTurnsLeft > 0) hasteTurnsLeft--;
        }
    }
}
