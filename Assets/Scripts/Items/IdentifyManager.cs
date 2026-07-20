using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    [System.Serializable]
    public class AppearancePool
    {
        public string groupName;             // ItemData.appearanceGroup과 매칭 (예: "Potion")
        public List<string> appearanceNames = new(); // 예: "빨간 물약", "파란 물약", "탁한 물약"...
    }

    /// <summary>
    /// 이번 플레이(런) 동안의 감정 상태를 관리하는 싱글턴.
    /// - 어떤 ItemData가 감정됐는지 추적
    /// - 미감정 아이템에 보여줄 가짜 이름을 그룹별로 섞어서 배정 (매 런마다 달라짐)
    /// 새 게임 시작 시 CreateNew()를 반드시 호출해서 초기화해야 한다.
    /// </summary>
    public class IdentifyManager
    {
        private static IdentifyManager instance;

        public static IdentifyManager Instance
        {
            get
            {
                // 안전장치: 명시적으로 CreateNew()를 안 불렀으면 빈 상태로라도 만들어서 널 참조 방지
                if (instance == null) instance = new IdentifyManager();
                return instance;
            }
        }

        private readonly HashSet<ItemData> identifiedItems = new();
        private readonly Dictionary<ItemData, string> unidentifiedNames = new();

        /// <summary>
        /// 새 게임(런) 시작 시 호출. allItems는 게임에 존재하는 모든 ItemData,
        /// pools는 그룹별 미감정 이름 후보 목록.
        /// </summary>
        public static void CreateNew(List<ItemData> allItems, List<AppearancePool> pools)
        {
            instance = new IdentifyManager();
            instance.AssignAppearances(allItems, pools);
        }

        private void AssignAppearances(List<ItemData> allItems, List<AppearancePool> pools)
        {
            var shuffledByGroup = pools.ToDictionary(p => p.groupName, p => Shuffle(p.appearanceNames));
            var cursor = shuffledByGroup.Keys.ToDictionary(k => k, _ => 0);

            foreach (var item in allItems)
            {
                if (item.identifiedByDefault) continue;
                if (string.IsNullOrEmpty(item.appearanceGroup)) continue;
                if (!shuffledByGroup.TryGetValue(item.appearanceGroup, out var names) || names.Count == 0) continue;

                int index = cursor[item.appearanceGroup] % names.Count; // 아이템 수가 이름 풀보다 많으면 재사용
                unidentifiedNames[item] = names[index];
                cursor[item.appearanceGroup]++;
            }
        }

        private static List<string> Shuffle(List<string> source)
        {
            var list = new List<string>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        public bool IsIdentified(ItemData data) => identifiedItems.Contains(data);

        public void MarkIdentified(ItemData data) => identifiedItems.Add(data);

        public string GetUnidentifiedName(ItemData data)
        {
            if (unidentifiedNames.TryGetValue(data, out var name))
                return name;
            return $"이름 모를 {data.itemType}"; // 안전장치: 그룹 매핑이 안 된 경우
        }
    }
}
