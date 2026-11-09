using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 특성의 "정체성"만 담는 추상 ScriptableObject 베이스.
    /// 효과 로직은 여기 두지 않는다 - 특성마다 자기 하위 클래스가 필요한 능력
    /// 인터페이스(IStatTalent, IKillTalent, IDamageTalent 등)만 구현하고,
    /// 게임 시스템이 그 인터페이스를 통해 쿼리/훅으로 접근한다.
    ///
    /// 런타임 현재 레벨은 여기 두지 않는다(에셋은 공유되므로). TalentSet이 관리.
    /// 특성 하나 = 에셋 하나 = 클래스 하나 (한 파일에 몰아넣지 않음).
    /// </summary>
    public abstract class TalentData : ScriptableObject
    {
        public string talentName = "특성";
        [TextArea] public string description = "";
        public Sprite icon;
        public int maxLevel = 2;

        /// <summary>UI에 보여줄 현재 레벨 기준 효과 요약 (특성마다 다르게 표현).</summary>
        public virtual string EffectSummary(int level) => "";

        /// <summary>투자(레벨 증가) 순간 1회 적용되는 즉발 효과 (예: 최대 체력 증가). 기본 없음.</summary>
        public virtual void OnInvested(TalentContext ctx, int newLevel) { }
    }

    /// <summary>특성 훅이 게임 상태에 접근하기 위한 컨텍스트. 필요에 따라 필드를 늘린다.</summary>
    public class TalentContext
    {
        public PlayerController player;
        public Combatant self => player != null ? player.Combatant : null;

        public TalentContext(PlayerController player) { this.player = player; }
    }

    // ===== 능력 인터페이스 (특성은 필요한 것만 구현) =====

    /// <summary>패시브 스탯 보너스를 주는 특성. TalentSet이 합산해 Combatant에 공급.</summary>
    public interface IStatTalent
    {
        int AttackBonus(int level);
        int DefenseBonus(int level);
        int SpeedBonus(int level);
    }

    /// <summary>적 처치 순간 반응하는 특성 (예: 처치 시 회복).</summary>
    public interface IKillTalent
    {
        void OnKill(TalentContext ctx, int level, MonsterController victim);
    }

    /// <summary>플레이어의 공격 피해를 수정하는 특성 (예: 첫 공격 치명타).</summary>
    public interface IDamageTalent
    {
        int ModifyDamage(TalentContext ctx, int level, int baseDamage, in AttackInfo info);
    }

    /// <summary>공격 상황 정보 (피해 수정 특성이 조건 판단에 사용).</summary>
    public readonly struct AttackInfo
    {
        public readonly MonsterController target;
        public readonly bool isFirstHitOnTarget; // 이 대상에게 가하는 첫 공격인지

        public AttackInfo(MonsterController target, bool isFirstHitOnTarget)
        {
            this.target = target;
            this.isFirstHitOnTarget = isFirstHitOnTarget;
        }
    }
}
