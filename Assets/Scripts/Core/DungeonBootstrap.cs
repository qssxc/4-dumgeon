using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 2단계 테스트용 씬 초기화. DungeonGenerator로 절차적 던전을 만들고,
    /// 몬스터는 특정 방이 아니라 던전 전역의 바닥 타일에서 랜덤 스폰한다.
    /// </summary>
    public class DungeonBootstrap : MonoBehaviour
    {
        [SerializeField] private GenerationSettings settings = new();

        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private MonsterController monsterPrefab;
        [SerializeField] private MonsterData ratData;

        [SerializeField] private int monsterCount = 12;
        [SerializeField] private int entranceSafeRadius = 4; // 입구 주변 이만큼은 스폰 제외

        private void Start()
        {
            var generator = new DungeonGenerator();
            GeneratedDungeon dungeon = generator.Generate(settings);

            Vector2Int playerStart = dungeon.Entrance.Center;
            var player = Instantiate(playerPrefab);
            player.Init(dungeon.Map, playerStart);

            SpawnMonstersAcrossMap(dungeon, player, playerStart);

            Debug.Log($"던전 생성 완료. 방 {dungeon.Rooms.Count}개, 몬스터 {monsterCount}마리. WASD로 이동/공격하세요.");
        }

        private void SpawnMonstersAcrossMap(GeneratedDungeon dungeon, PlayerController player, Vector2Int playerStart)
        {
            var candidates = dungeon.Map.GetAllWalkableTiles()
                .Where(tile => GridUtils.ManhattanDistance(tile, playerStart) > entranceSafeRadius)
                .OrderBy(_ => Random.value)
                .ToList();

            int count = Mathf.Min(monsterCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                var monster = Instantiate(monsterPrefab);
                monster.Init(ratData, dungeon.Map, player, candidates[i]);
            }
        }
    }
}
