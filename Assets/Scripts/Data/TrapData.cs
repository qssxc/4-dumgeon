using UnityEngine;

namespace DungeonCore
{
    public enum TrapType
    {
        Damage, // 밟으면 즉시 피해
        Fire,   // 밟으면 주변에 화염 생성(기존 TileEffectManager 재활용)
    }

    /// <summary>
    /// 함정의 정적 수치. 몬스터/아이템과 같은 ScriptableObject 데이터 패턴.
    /// 우클릭 > Create > Dungeon > Trap 으로 에셋 생성.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Trap", fileName = "NewTrap")]
    public class TrapData : ScriptableObject
    {
        public string trapName = "Spike Trap";
        public TrapType type = TrapType.Damage;

        [Header("피해형")]
        public int damage = 8;

        [Header("화염형")]
        public int fireRadius = 1;    // 발동 시 불이 붙는 원형 반경
        public int fireDuration = 3;  // 불이 유지되는 턴 수

        [Header("표시 색 (스프라이트 없을 때)")]
        public Color detectedColor = new Color(0.9f, 0.3f, 0.3f, 0.9f);   // 탐지된 함정
        public Color triggeredColor = new Color(0.35f, 0.2f, 0.2f, 0.8f); // 발동되어 터진 자국
        public Sprite detectedSprite;   // 있으면 이 스프라이트로 표시
        public Sprite triggeredSprite;
    }
}
