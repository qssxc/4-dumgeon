namespace DungeonCore
{
    /// <summary>
    /// 인벤토리에 실제로 들어가는 아이템 한 덩어리(스택 포함).
    /// 감정 여부는 이 인스턴스가 아니라 IdentifyManager가 ItemData 단위로 관리한다 -
    /// "포션 A 하나"가 아니라 "포션이라는 종류 전체"가 감정되는 것이므로.
    /// </summary>
    public class ItemInstance
    {
        public readonly ItemData data;
        public int quantity;

        public ItemInstance(ItemData data, int quantity = 1)
        {
            this.data = data;
            this.quantity = quantity;
        }

        public bool IsIdentified => data.identifiedByDefault || IdentifyManager.Instance.IsIdentified(data);

        /// <summary>플레이어에게 실제로 보여줄 이름. 미감정이면 이번 플레이용으로 배정된 가짜 이름.</summary>
        public string DisplayName => IsIdentified ? data.itemName : IdentifyManager.Instance.GetUnidentifiedName(data);

        /// <summary>이 아이템을 감정 상태로 확정 (사용, 감정 스크롤 등에서 호출).</summary>
        public void Identify()
        {
            IdentifyManager.Instance.MarkIdentified(data);
        }
    }
}
