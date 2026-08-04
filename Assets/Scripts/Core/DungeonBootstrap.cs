using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 씬 초기화 + 층 전환을 담당. DungeonGenerator로 던전을 만들고,
    /// 플레이어가 계단에서 상호작용(E)하면 현재 층을 정리하고 새 층을 생성한다.
    /// </summary>
    public class DungeonBootstrap : MonoBehaviour
    {
        [SerializeField] private GenerationSettings settings = new();

        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private MonsterController monsterPrefab;
        [SerializeField] private MonsterData ratData;

        [SerializeField] private int monsterCount = 12;
        [SerializeField] private int entranceSafeRadius = 4; // 입구 주변 이만큼은 스폰 제외

        [Header("아이템")]
        [SerializeField] private ItemPickup itemPickupPrefab;
        [SerializeField] private System.Collections.Generic.List<ItemData> itemPool = new();
        [SerializeField, Range(0f, 1f)] private float itemSpawnChancePerRoom = 0.35f;
        [SerializeField] private System.Collections.Generic.List<AppearancePool> appearancePools = new();

        [Header("캐릭터 선택 (시연용)")]
        [SerializeField] private System.Collections.Generic.List<CharacterClassData> characterClasses = new();

        private GameObject currentRendererGO;
        private PlayerController currentPlayer;
        private Vector2Int currentStairsPos;

        private CharacterSelectUI selectUI;

        private void Start()
        {
            // 타이틀 화면은 이제 별도 MainMenu 씬 담당. Game 씬에 진입하면 캐릭터 선택부터 시작.

            // 새 게임 시작 시 한 번만 초기화 - 층 전환마다 다시 부르면 감정 상태가 리셋되므로
            // GenerateLevel()이 아니라 여기(최초 1회만 실행되는 Start)에 둔다.
            IdentifyManager.CreateNew(itemPool, appearancePools);

            selectUI = gameObject.AddComponent<CharacterSelectUI>();
            selectUI.classes = characterClasses;
            selectUI.OnClassChosen += OnCharacterChosen;
            selectUI.Show();
        }

        private CharacterClassData selectedClass;

        private void OnCharacterChosen(CharacterClassData chosenClass)
        {
            // 시연용이라 선택된 클래스의 스탯은 아직 반영하지 않지만, 외형은 적용됨.
            selectedClass = chosenClass;
            GenerateLevel();
        }

        private void GenerateLevel()
        {
            var generator = new DungeonGenerator();
            GeneratedDungeon dungeon = generator.Generate(settings);

            // 이전 층 정리 (렌더러 오브젝트 + 몬스터 + 아이템 전부)
            if (currentRendererGO != null) Destroy(currentRendererGO);
            MonsterController.DestroyAll();
            ItemPickup.DestroyAll();

            var rendererGO = new GameObject("DungeonRenderer");
            var renderer = rendererGO.AddComponent<DungeonRenderer>();
            renderer.Render(dungeon);
            currentRendererGO = rendererGO;

            Vector2Int playerStart = dungeon.Entrance.Center;

            if (currentPlayer == null)
            {
                currentPlayer = Instantiate(playerPrefab);
                currentPlayer.OnInteractPressed += HandleInteract; // 플레이어는 층 전환에도 유지, 구독은 최초 1회만
                currentPlayer.ApplyClass(selectedClass);           // 선택된 클래스 외형 적용 (Init 전에)

                var inventoryUI = currentPlayer.gameObject.AddComponent<InventoryUI>();
                inventoryUI.player = currentPlayer;
            }
            currentPlayer.Init(dungeon.Map, playerStart);

            currentStairsPos = dungeon.Exit.StairsPosition;

            SpawnMonstersAcrossMap(dungeon, currentPlayer, playerStart);
            SpawnItems(dungeon);
            SetupCamera(currentPlayer.transform);

            int treasureCount = dungeon.Rooms.FindAll(r => r is TreasureRoom).Count;
            int standardCount = dungeon.Rooms.FindAll(r => r is StandardRoom).Count;
            Debug.Log($"던전 생성 완료. 방 {dungeon.Rooms.Count}개 " +
                      $"(입구 1, 출구 1, 보물방 {treasureCount}, 일반 {standardCount}), " +
                      $"몬스터 {monsterCount}마리. WASD로 이동/공격, 계단에서 E로 다음 층.");
        }

        private void HandleInteract()
        {
            if (currentPlayer.GridPosition != currentStairsPos) return;

            Debug.Log("계단을 타고 다음 층으로 이동합니다.");
            GenerateLevel();
        }

        [Header("카메라")]
        [SerializeField] private float cameraOrthoSize = 8f; // 값이 작을수록 확대됨. 0 이하면 카메라 현재 값을 그대로 유지.

        private void SetupCamera(Transform playerTransform)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("Main Camera를 찾을 수 없습니다. 카메라 태그가 MainCamera인지 확인하세요.");
                return;
            }

            // cameraOrthoSize가 0 이하면 인스펙터에서 직접 맞춘 카메라 값을 존중해서 건드리지 않음
            if (cam.orthographic && cameraOrthoSize > 0f)
                cam.orthographicSize = cameraOrthoSize;

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = playerTransform;

            // 층이 바뀔 때마다 바로 플레이어 위치로 스냅 (Lerp로 서서히 다가가지 않도록)
            cam.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
        }

        private void SpawnItems(GeneratedDungeon dungeon)
        {
            if (itemPickupPrefab == null || itemPool.Count == 0) return;

            foreach (var room in dungeon.Rooms)
            {
                // 보물방은 항상 1개 확정, 나머지는 방마다 확률 판정 (EntranceRoom/TunnelRoom은
                // GetContentSpawnPoints()가 항상 빈 리스트를 주므로 자동으로 제외됨)
                bool guaranteed = room is TreasureRoom;
                if (!guaranteed && Random.value >= itemSpawnChancePerRoom) continue;

                var points = room.GetContentSpawnPoints(1);
                foreach (var point in points)
                {
                    var itemData = itemPool[Random.Range(0, itemPool.Count)];
                    var instance = new ItemInstance(itemData, 1);
                    var pickup = Instantiate(itemPickupPrefab);
                    pickup.Init(instance, point);
                }
            }
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
