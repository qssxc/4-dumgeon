using System;

namespace DungeonCore
{
    /// <summary>
    /// 플레이어의 성장(레벨/경험치/특성 포인트)을 관리하는 순수 데이터·로직 클래스.
    /// Combatant/Inventory/Equipment와 같은 조합 패턴 - PlayerController가 필드로 보유.
    ///
    /// 레벨업마다 특성 포인트를 1개씩 지급하되, 총 지급량은 상한(MaxTalentPoints)까지.
    /// 특성을 실제로 어디에 쓰는지는 이 클래스가 모른다(포인트만 관리) - 나중에 특성 시스템이 소비.
    /// </summary>
    public class Progression
    {
        public const int MaxTalentPoints = 6;
        private const int BaseXp = 10; // 다음 레벨까지 필요 경험치 = BaseXp * 현재레벨

        public int Level { get; private set; } = 1;
        public int CurrentXP { get; private set; }
        public int TalentPoints { get; private set; }          // 아직 투자하지 않은 포인트
        public int TalentPointsAwarded { get; private set; }   // 지금까지 총 지급한 포인트 (상한 체크용)

        /// <summary>다음 레벨까지 필요한 총 경험치.</summary>
        public int XpToNext => BaseXp * Level;

        /// <summary>레벨업 시 발생. 새 레벨을 전달. PlayerController가 구독해 체력 증가 등을 처리.</summary>
        public event Action<int> OnLevelUp;

        /// <summary>경험치 획득. 임계치를 넘으면 레벨업(여러 번 연속 가능).</summary>
        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            CurrentXP += amount;

            while (CurrentXP >= XpToNext)
            {
                CurrentXP -= XpToNext;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;

            // 특성 포인트 지급 (총 지급량 상한까지만)
            if (TalentPointsAwarded < MaxTalentPoints)
            {
                TalentPoints++;
                TalentPointsAwarded++;
            }

            OnLevelUp?.Invoke(Level);
        }

        /// <summary>특성에 포인트를 투자할 때 호출 (여유 포인트 차감). 성공 여부 반환.</summary>
        public bool SpendTalentPoint()
        {
            if (TalentPoints <= 0) return false;
            TalentPoints--;
            return true;
        }

        /// <summary>특성 초기화 등으로 포인트를 되돌려줄 때.</summary>
        public void RefundTalentPoint() => TalentPoints++;
    }
}
