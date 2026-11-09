using UnityEngine;

namespace DungeonCore
{
    /// <summary>[전투 특성] 적 처치 시 체력을 회복한다.</summary>
    [CreateAssetMenu(menuName = "Dungeon/Talent/HealOnKill", fileName = "HealOnKillTalent")]
    public class HealOnKillTalent : TalentData, IKillTalent
    {
        public int healPerLevel = 2;

        public void OnKill(TalentContext ctx, int level, MonsterController victim)
        {
            if (ctx.self == null) return;
            int healed = ctx.self.Heal(healPerLevel * level);
            if (healed > 0)
                Debug.Log($"[특성] 처치 회복 +{healed}");
        }

        public override string EffectSummary(int level) => $"처치 시 체력 +{healPerLevel * level}";
    }
}
