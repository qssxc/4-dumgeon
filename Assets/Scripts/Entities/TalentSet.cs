using System.Collections.Generic;

namespace DungeonCore
{
    /// <summary>
    /// 플레이어가 보유한 특성들의 런타임 상태(현재 레벨)를 관리하고,
    /// 능력 인터페이스(IStatTalent/IKillTalent/IDamageTalent)를 통해 효과를 디스패치한다.
    ///
    /// 특성 효과 로직은 각 특성 클래스 안에 있고, 여기선 "어떤 특성을 몇 레벨 가졌는지"와
    /// "해당 인터페이스를 구현한 특성들에게 훅을 돌리는 것"만 담당한다.
    /// 새 특성을 추가해도 이 파일은 거의 손대지 않는다 (인터페이스로 자동 처리).
    /// </summary>
    public class TalentSet
    {
        private readonly List<TalentData> talents;
        private readonly int[] levels;

        private readonly TalentContext ctx;
        private readonly Progression progression;

        public TalentSet(List<TalentData> classTalents, PlayerController player, Progression progression)
        {
            talents = classTalents ?? new List<TalentData>();
            levels = new int[talents.Count];
            ctx = new TalentContext(player);
            this.progression = progression;
        }

        public int Count => talents.Count;
        public TalentData GetTalent(int i) => talents[i];
        public int GetLevel(int i) => (i >= 0 && i < levels.Length) ? levels[i] : 0;

        /// <summary>특정 특성 에셋의 현재 레벨 (시스템이 쿼리할 때 사용).</summary>
        public int GetLevel(TalentData talent)
        {
            int idx = talents.IndexOf(talent);
            return idx >= 0 ? levels[idx] : 0;
        }

        public bool CanInvest(int i) =>
            i >= 0 && i < talents.Count && talents[i] != null
            && levels[i] < talents[i].maxLevel && progression.TalentPoints > 0;

        /// <summary>특성에 포인트 1 투자. 성공 여부 반환.</summary>
        public bool Invest(int i)
        {
            if (!CanInvest(i)) return false;
            if (!progression.SpendTalentPoint()) return false;

            levels[i]++;
            talents[i].OnInvested(ctx, levels[i]); // 즉발 효과(최대체력 등)
            return true;
        }

        // ===== 스탯 합산 (IStatTalent) - Combatant 보너스 공급자에 연결 =====

        public int AttackBonus => SumStat(t => t.AttackBonus);
        public int DefenseBonus => SumStat(t => t.DefenseBonus);
        public int SpeedBonus => SumStat(t => t.SpeedBonus);

        private int SumStat(System.Func<IStatTalent, System.Func<int, int>> pick)
        {
            int sum = 0;
            for (int i = 0; i < talents.Count; i++)
                if (levels[i] > 0 && talents[i] is IStatTalent st)
                    sum += pick(st)(levels[i]);
            return sum;
        }

        // ===== 전투 훅 디스패치 =====

        /// <summary>적 처치 시 호출 - 모든 IKillTalent에게 통지.</summary>
        public void DispatchKill(MonsterController victim)
        {
            for (int i = 0; i < talents.Count; i++)
                if (levels[i] > 0 && talents[i] is IKillTalent kt)
                    kt.OnKill(ctx, levels[i], victim);
        }

        /// <summary>공격 피해에 IDamageTalent들을 순서대로 적용해 최종 피해 반환.</summary>
        public int ModifyDamage(int baseDamage, in AttackInfo info)
        {
            int dmg = baseDamage;
            for (int i = 0; i < talents.Count; i++)
                if (levels[i] > 0 && talents[i] is IDamageTalent dt)
                    dmg = dt.ModifyDamage(ctx, levels[i], dmg, info);
            return dmg;
        }
    }
}
