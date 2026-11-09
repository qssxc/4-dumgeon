using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 이번 플레이(런) 동안의 감정 상태를 관리하는 싱글턴.
    /// - 어떤 ItemData가 감정됐는지 추적
    /// - 미감정 아이템에 "겉모습(AppearanceData)"을 그룹별로 셔플해서 1:1 배정
    ///   (매 런마다 '빨간 물약'이 어떤 효과인지 달라짐 - 감정 시스템의 핵심)
    /// 새 게임 시작 시 CreateNew()를 반드시 호출해서 초기화해야 한다.
    /// </summary>
    public class IdentifyManager
    {
        private static IdentifyManager instance;

        public static IdentifyManager Instance
        {
            get
            {
                if (instance == null) instance = new IdentifyManager();
                return instance;
            }
        }

        private readonly HashSet<ItemData> identifiedItems = new();
        // 각 아이템(정체)에 이번 판 배정된 겉모습
        private readonly Dictionary<ItemData, AppearanceData> appearanceByItem = new();

        /// <summary>
        /// 새 게임(런) 시작 시 호출. allItems는 게임에 존재하는 모든 ItemData,
        /// appearances는 모든 겉모습 에셋. 같은 group끼리 셔플해서 짝짓는다.
        /// </summary>
        public static void CreateNew(List<ItemData> allItems, List<AppearanceData> appearances)
        {
            instance = new IdentifyManager();
            instance.AssignAppearances(allItems, appearances);
        }

        private void AssignAppearances(List<ItemData> allItems, List<AppearanceData> appearances)
        {
            // 그룹별로 겉모습을 모아 셔플
            var appearancesByGroup = appearances
                .Where(a => a != null)
                .GroupBy(a => a.group)
                .ToDictionary(g => g.Key, g => Shuffle(g.ToList()));

            var cursor = appearancesByGroup.Keys.ToDictionary(k => k, _ => 0);

            // 감정이 필요한 아이템에만 겉모습을 배정 (그룹 일치 기준)
            foreach (var item in allItems)
            {
                if (item == null) continue;
                if (item.identifiedByDefault) continue;
                if (string.IsNullOrEmpty(item.appearanceGroup)) continue;
                if (!appearancesByGroup.TryGetValue(item.appearanceGroup, out var pool) || pool.Count == 0) continue;

                int index = cursor[item.appearanceGroup] % pool.Count; // 겉모습보다 아이템이 많으면 재사용
                appearanceByItem[item] = pool[index];
                cursor[item.appearanceGroup]++;
            }
        }

        private static List<AppearanceData> Shuffle(List<AppearanceData> source)
        {
            var list = new List<AppearanceData>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        public bool IsIdentified(ItemData data) => identifiedItems.Contains(data);

        public void MarkIdentified(ItemData data) => identifiedItems.Add(data);

        /// <summary>이 아이템에 배정된 겉모습 (미감정 표시용). 없으면 null.</summary>
        public AppearanceData GetAppearance(ItemData data)
        {
            return appearanceByItem.TryGetValue(data, out var a) ? a : null;
        }
    }
}
