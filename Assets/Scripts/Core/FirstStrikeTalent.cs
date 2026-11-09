using UnityEngine;

namespace DungeonCore
{
    /// <summary>[전투 특성] 각 적에게 가하는 "첫 공격"에 추가 피해(치명타)를 준다.</summary>
    [CreateAssetMenu(menuName = "Dungeon/Talent/FirstStrike", fileName = "FirstStrikeTalent")]
    public class FirstStrikeTalent : TalentData, IDamageTalent
    {
        [Tooltip("첫 공격 배수 보너스. 레벨당 더해짐. 예: 0.5 -> Lv2에서 +1.0배(2배 피해)")]
        public float bonusMultiplierPerLevel = 0.5f;

        public int ModifyDamage(TalentContext ctx, int level, int baseDamage, in AttackInfo info)
        {
            if (!info.isFirstHitOnTarget || level <= 0) return baseDamage;

            float mult = 1f + bonusMultiplierPerLevel * level;
            int result = Mathf.RoundToInt(baseDamage * mult);
            if (result > baseDamage)
                Debug.Log($"[특성] 기습! {baseDamage} -> {result}");
            return result;
        }

        public override string EffectSummary(int level) =>
            $"첫 공격 피해 x{1f + bonusMultiplierPerLevel * level:0.0#}";
    }
}
