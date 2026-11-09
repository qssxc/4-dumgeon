using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 단순 패시브 스탯 특성 (공격/방어/속도/최대체력). 흔한 경우를 위한 공용 특성.
    /// 복잡한 특성은 이걸 쓰지 말고 자기 전용 클래스를 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Talent/Stat", fileName = "NewStatTalent")]
    public class StatTalent : TalentData, IStatTalent
    {
        public enum Stat { Attack, Defense, Speed, MaxHP }

        public Stat stat = Stat.Attack;
        public int amountPerLevel = 1;

        public int AttackBonus(int level) => stat == Stat.Attack ? amountPerLevel * level : 0;
        public int DefenseBonus(int level) => stat == Stat.Defense ? amountPerLevel * level : 0;
        public int SpeedBonus(int level) => stat == Stat.Speed ? amountPerLevel * level : 0;

        // MaxHP는 패시브 합산이 아니라 투자 순간 즉시 반영
        public override void OnInvested(TalentContext ctx, int newLevel)
        {
            if (stat == Stat.MaxHP && ctx.self != null)
            {
                ctx.self.maxHP += amountPerLevel;
                ctx.self.Heal(amountPerLevel);
            }
        }

        public override string EffectSummary(int level)
        {
            string label = stat switch
            {
                Stat.Attack => "공격력", Stat.Defense => "방어력",
                Stat.Speed => "속도", Stat.MaxHP => "최대 체력", _ => ""
            };
            return $"{label} +{amountPerLevel * level}";
        }
    }
}
