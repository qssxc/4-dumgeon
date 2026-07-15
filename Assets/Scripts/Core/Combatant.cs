namespace DungeonCore
{
    /// <summary>
    /// 전투 계산에 필요한 최소 데이터. PlayerController와 MonsterController가
    /// 각각 이 클래스를 필드로 들고 있는 형태로 사용 (상속보다 조합 선호).
    /// </summary>
    public class Combatant
    {
        public string displayName;
        public int maxHP;
        public int currentHP;
        public int attack;
        public int defense;

        public bool IsAlive => currentHP > 0;

        public Combatant(string name, int maxHP, int attack, int defense)
        {
            this.displayName = name;
            this.maxHP = maxHP;
            this.currentHP = maxHP;
            this.attack = attack;
            this.defense = defense;
        }

        public void TakeDamage(int amount)
        {
            currentHP = UnityEngine.Mathf.Max(0, currentHP - amount);
        }
    }
}
