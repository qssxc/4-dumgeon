using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 캐릭터 클래스 정의. 지금은 시연용이라 표시 정보(이름/스프라이트)만 쓰지만,
    /// 나중에 클래스별 시작 스탯/스킬을 여기에 추가하면 선택 화면은 그대로 재활용된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/CharacterClass", fileName = "NewCharacterClass")]
    public class CharacterClassData : ScriptableObject
    {
        public string className = "Warrior";
        [TextArea] public string description = "";
        public Sprite portrait;

        [Header("외형 (인게임 스프라이트/애니메이션)")]
        public Sprite baseSprite;      // 정지 시 기본 스프라이트 (애니메이션 없을 때도 이걸 씀)
        public Sprite[] idleFrames;
        public Sprite[] walkFrames;
        public Sprite[] attackFrames;
        public float idleFps = 4f;

        [Header("시작 스탯 (지금은 미사용, 확장용)")]
        public int startingHP = 20;
        public int startingAttack = 5;
        public int startingDefense = 2;

        [Header("특성 (클래스당 4개 권장, 각 0~2레벨)")]
        public System.Collections.Generic.List<TalentData> talents = new();
    }
}
