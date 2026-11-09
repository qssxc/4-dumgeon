using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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

        [Header("몬스터 풀")]
        [Tooltip("스폰 후보 몬스터들. weight로 등장 비율, minDepth로 등장 시작 층을 조절.")]
        [SerializeField] private System.Collections.Generic.List<MonsterSpawnEntry> monsterPool = new();

        /// <summary>몬스터 풀 한 칸. 에셋 + 가중치 + 등장 시작 층.</summary>
        [System.Serializable]
        public class MonsterSpawnEntry
        {
            public MonsterData data;
            [Tooltip("등장 비율(상대값). 클수록 자주 나온다.")]
            public float weight = 1f;
            [Tooltip("이 층 이상에서만 등장. 1이면 1층부터.")]
            public int minDepth = 1;
        }

        [SerializeField] private int monsterCount = 12;
        [SerializeField] private int entranceSafeRadius = 4; // 입구 주변 이만큼은 스폰 제외

        [Header("시야 (Fog of War)")]
        [Tooltip("플레이어가 볼 수 있는 반경(칸). 0 이하면 fog of war를 끈다.")]
        [SerializeField] private int visionRadius = 7;

        [Header("함정")]
        [SerializeField] private TrapController trapPrefab;
        [Tooltip("스폰 후보 함정들. weight로 등장 비율, minDepth로 등장 시작 층.")]
        [SerializeField] private System.Collections.Generic.List<TrapSpawnEntry> trapPool = new();
        [SerializeField] private int trapCount = 6;
        [Tooltip("이 반경 안 + 시야가 트이면 함정이 탐지된다. 시야 반경보다 작게.")]
        [SerializeField] private int trapDetectionRadius = 2;

        /// <summary>함정 풀 한 칸. 에셋 + 가중치 + 등장 시작 층.</summary>
        [System.Serializable]
        public class TrapSpawnEntry
        {
            public TrapData data;
            public float weight = 1f;
            public int minDepth = 1;
        }

        [Header("아이템")]
        [SerializeField] private ItemPickup itemPickupPrefab;
        [SerializeField] private System.Collections.Generic.List<ItemData> itemPool = new();
        [SerializeField, Range(0f, 1f)] private float itemSpawnChancePerRoom = 0.35f;
        [SerializeField] private System.Collections.Generic.List<AppearanceData> appearances = new();

        [Header("캐릭터 선택 (시연용)")]
        [SerializeField] private System.Collections.Generic.List<CharacterClassData> characterClasses = new();

        private GameObject currentRendererGO;
        private PlayerController currentPlayer;
        private Vector2Int currentStairsPos;
        private int currentDepth = 1;

        private FogOfWar currentFog;
        private bool fogHooked; // OnTurnCompleted 구독은 최초 1회만

        private CharacterSelectUI selectUI;
        private GameOverUI gameOverUI;
        private PauseMenu pauseMenu;
        private DevConsole devConsole;

        [Header("UI")]
        [SerializeField] private Font uiFont;            // OnGUI 화면용 (메뉴/콘솔 등). 비우면 OS 한글 폰트 폴백.
        [SerializeField] private TMP_FontAsset uiFontTMP; // Canvas UI(TMP)용. 한글 글리프 포함 TMP 폰트 에셋 지정.

        private void Start()
        {
            // 타이틀 화면은 이제 별도 MainMenu 씬 담당. Game 씬에 진입하면 캐릭터 선택부터 시작.

            // UI 폰트를 전역 지정 (uGUI/OnGUI 전부 여기서 가져감). 가장 먼저 설정.
            UIFont.SetOverride(uiFont);
            UIFont.SetTMPOverride(uiFontTMP);

            // 새 게임 시작 시 한 번만 초기화 - 층 전환마다 다시 부르면 감정 상태가 리셋되므로
            // GenerateLevel()이 아니라 여기(최초 1회만 실행되는 Start)에 둔다.
            IdentifyManager.CreateNew(itemPool, appearances);

            devConsole = gameObject.AddComponent<DevConsole>();
            RegisterConsoleCommands();

            selectUI = gameObject.AddComponent<CharacterSelectUI>();
            selectUI.classes = characterClasses;
            selectUI.OnClassChosen += OnCharacterChosen;
            selectUI.Show();
        }

        private void RegisterConsoleCommands()
        {
            devConsole.RegisterCommand("hp", "현재 체력 설정. 예: hp 5", args =>
            {
                RequirePlayer();
                int v = int.Parse(args[0]);
                currentPlayer.Combatant.currentHP = Mathf.Clamp(v, 0, currentPlayer.Combatant.maxHP);
                devConsole.Print($"체력 = {currentPlayer.Combatant.currentHP}/{currentPlayer.Combatant.maxHP}");
            });

            devConsole.RegisterCommand("maxhp", "최대 체력 설정. 예: maxhp 10", args =>
            {
                RequirePlayer();
                int v = Mathf.Max(1, int.Parse(args[0]));
                currentPlayer.Combatant.maxHP = v;
                currentPlayer.Combatant.currentHP = Mathf.Min(currentPlayer.Combatant.currentHP, v);
                devConsole.Print($"최대 체력 = {v}");
            });

            devConsole.RegisterCommand("heal", "체력을 최대치로 회복", _ =>
            {
                RequirePlayer();
                currentPlayer.Combatant.currentHP = currentPlayer.Combatant.maxHP;
                devConsole.Print($"체력 = {currentPlayer.Combatant.maxHP} (풀 회복)");
            });

            devConsole.RegisterCommand("damage", "플레이어에게 데미지. 예: damage 3", args =>
            {
                RequirePlayer();
                currentPlayer.TakeDamage(int.Parse(args[0]));
                devConsole.Print($"체력 = {currentPlayer.Combatant.currentHP}");
            });

            devConsole.RegisterCommand("atk", "기본 공격력 설정. 예: atk 10", args =>
            {
                RequirePlayer();
                currentPlayer.Combatant.baseAttack = int.Parse(args[0]);
                devConsole.Print($"공격력 = {currentPlayer.Combatant.attack}");
            });

            devConsole.RegisterCommand("def", "기본 방어력 설정. 예: def 5", args =>
            {
                RequirePlayer();
                currentPlayer.Combatant.baseDefense = int.Parse(args[0]);
                devConsole.Print($"방어력 = {currentPlayer.Combatant.defense}");
            });

            devConsole.RegisterCommand("god", "무적 모드 토글", _ =>
            {
                RequirePlayer();
                currentPlayer.GodMode = !currentPlayer.GodMode;
                devConsole.Print($"무적 모드: {(currentPlayer.GodMode ? "ON" : "OFF")}");
            });

            devConsole.RegisterCommand("kill", "현재 층의 몬스터 전부 제거", _ =>
            {
                MonsterController.DestroyAll();
                devConsole.Print("모든 몬스터 제거됨");
            });

            devConsole.RegisterCommand("descend", "다음 층으로 즉시 이동", _ =>
            {
                currentDepth++;
                devConsole.Print($"{currentDepth}층으로 이동");
                GenerateLevel();
            });

            devConsole.RegisterCommand("xp", "경험치 획득. 예: xp 50", args =>
            {
                RequirePlayer();
                currentPlayer.Progression.AddXP(int.Parse(args[0]));
                var p = currentPlayer.Progression;
                devConsole.Print($"Lv.{p.Level}  XP {p.CurrentXP}/{p.XpToNext}  특성P {p.TalentPoints}");
            });

            devConsole.RegisterCommand("talents", "보유 특성 목록(번호/레벨) 출력", _ =>
            {
                RequirePlayer();
                var t = currentPlayer.Talents;
                if (t == null || t.Count == 0) { devConsole.Print("특성 없음 (클래스에 특성 미설정)"); return; }
                devConsole.Print($"특성 포인트: {currentPlayer.Progression.TalentPoints}");
                for (int i = 0; i < t.Count; i++)
                {
                    var d = t.GetTalent(i);
                    devConsole.Print($"  {i}: {d.talentName} Lv.{t.GetLevel(i)}/{d.maxLevel} [{d.EffectSummary(System.Math.Max(1, t.GetLevel(i)))}]");
                }
            });

            devConsole.RegisterCommand("talent", "특성에 투자. 예: talent 0", args =>
            {
                RequirePlayer();
                int i = int.Parse(args[0]);
                if (currentPlayer.Talents.Invest(i))
                {
                    var d = currentPlayer.Talents.GetTalent(i);
                    devConsole.Print($"{d.talentName} → Lv.{currentPlayer.Talents.GetLevel(i)} / 공{currentPlayer.Combatant.attack} 방{currentPlayer.Combatant.defense}");
                }
                else devConsole.Print("투자 불가 (포인트 부족 또는 최대 레벨)");
            });

            devConsole.RegisterCommand("level", "현재 레벨/경험치/특성포인트 출력", _ =>
            {
                RequirePlayer();
                var p = currentPlayer.Progression;
                devConsole.Print($"Lv.{p.Level}  XP {p.CurrentXP}/{p.XpToNext}  특성P {p.TalentPoints} (총 지급 {p.TalentPointsAwarded}/{Progression.MaxTalentPoints})");
            });

            devConsole.RegisterCommand("depth", "현재 층 번호 출력", _ =>
                devConsole.Print($"현재 층: {currentDepth}"));

            devConsole.RegisterCommand("give", "아이템 지급. 예: give 0 (목록은 items)", args =>
            {
                RequirePlayer();
                int idx = int.Parse(args[0]);
                if (idx < 0 || idx >= itemPool.Count) { devConsole.Print("잘못된 번호 (items로 목록 확인)"); return; }
                var inst = ItemInstance.Create(itemPool[idx], 1);
                if (currentPlayer.Inventory.TryAdd(inst))
                    devConsole.Print($"지급: {itemPool[idx].itemName}");
                else
                    devConsole.Print("인벤토리가 가득 참");
            });

            devConsole.RegisterCommand("items", "지급 가능한 아이템 목록(번호)", _ =>
            {
                for (int i = 0; i < itemPool.Count; i++)
                    devConsole.Print($"  {i}: {itemPool[i].itemName}");
            });

            devConsole.RegisterCommand("spawn", "플레이어 주변에 몬스터 스폰. 예: spawn 3", args =>
            {
                RequirePlayer();
                int n = args.Length > 0 ? int.Parse(args[0]) : 1;
                SpawnMonstersNear(currentPlayer.GridPosition, n);
                devConsole.Print($"몬스터 {n}마리 스폰");
            });
        }

        private void RequirePlayer()
        {
            if (currentPlayer == null)
                throw new System.Exception("아직 게임이 시작되지 않음 (캐릭터 선택 먼저)");
        }

        private CharacterClassData selectedClass;

        private void OnCharacterChosen(CharacterClassData chosenClass)
        {
            // 시연용이라 선택된 클래스의 스탯은 아직 반영하지 않지만, 외형은 적용됨.
            selectedClass = chosenClass;
            GenerateLevel();
        }

        private DungeonMap currentMap; // 콘솔 spawn 등에서 쓸 현재 층 맵 참조

        private void GenerateLevel()
        {
            var generator = new DungeonGenerator();
            GeneratedDungeon dungeon = generator.Generate(settings);
            currentMap = dungeon.Map;

            // 이전 층 정리 (렌더러 오브젝트 + 몬스터 + 아이템 + 함정 전부)
            if (currentRendererGO != null) Destroy(currentRendererGO);
            MonsterController.DestroyAll();
            ItemPickup.DestroyAll();
            TrapController.DestroyAll();

            var rendererGO = new GameObject("DungeonRenderer");
            var renderer = rendererGO.AddComponent<DungeonRenderer>();
            renderer.Render(dungeon);
            currentRendererGO = rendererGO;

            Vector2Int playerStart = dungeon.Entrance.Center;

            if (currentPlayer == null)
            {
                currentPlayer = Instantiate(playerPrefab);
                currentPlayer.OnInteractPressed += HandleInteract; // 플레이어는 층 전환에도 유지, 구독은 최초 1회만
                currentPlayer.OnDied += HandlePlayerDied;
                currentPlayer.ApplyClass(selectedClass);           // 선택된 클래스 외형 적용 (Init 전에)

                var inventoryScreen = currentPlayer.gameObject.AddComponent<InventoryScreen>();
                inventoryScreen.player = currentPlayer;

                var hud = currentPlayer.gameObject.AddComponent<PlayerHUD>();
                hud.player = currentPlayer;

                var talentScreen = currentPlayer.gameObject.AddComponent<TalentScreen>();
                talentScreen.player = currentPlayer;

                // ESC 일시정지 메뉴 (계속/재시작/메인) - 게임오버와 같은 전환 로직 재활용
                pauseMenu = gameObject.AddComponent<PauseMenu>();
                pauseMenu.OnRestart += RestartGame;
                pauseMenu.OnToMenu += ReturnToMenu;
            }
            currentPlayer.Init(dungeon.Map, playerStart);

            currentStairsPos = dungeon.Exit.StairsPosition;

            // 화면 메시지 로그 (최초 1회 생성)
            if (MessageLog.Instance == null)
                gameObject.AddComponent<MessageLog>();

            // 타일 효과(화염 등) 매니저를 이번 층의 맵/플레이어에 연결
            if (TileEffectManager.Instance == null)
                gameObject.AddComponent<TileEffectManager>();
            TileEffectManager.Instance.Bind(dungeon.Map, currentPlayer);

            SpawnMonstersAcrossMap(dungeon, currentPlayer, playerStart);
            SpawnItems(dungeon);
            SpawnTraps(dungeon, playerStart);
            SetupCamera(currentPlayer.transform);

            SetupFog(dungeon.Map, renderer);

            int treasureCount = dungeon.Rooms.FindAll(r => r is TreasureRoom).Count;
            int standardCount = dungeon.Rooms.FindAll(r => r is StandardRoom).Count;
            Debug.Log($"던전 생성 완료. 방 {dungeon.Rooms.Count}개 " +
                      $"(입구 1, 출구 1, 보물방 {treasureCount}, 일반 {standardCount}), " +
                      $"몬스터 {monsterCount}마리. WASD로 이동/공격, 계단에서 E로 다음 층.");

            MessageLog.Post($"지하 {currentDepth}층 진입", MessageLog.Highlight);
        }

        /// <summary>이번 층의 fog of war를 만들고 턴 종료마다 재계산되도록 연결.</summary>
        private void SetupFog(DungeonMap map, DungeonRenderer renderer)
        {
            currentFog = visionRadius > 0
                ? new FogOfWar(map, currentPlayer, renderer.TileVisuals, visionRadius)
                : null; // 0 이하면 fog 비활성화(전체가 항상 보임)

            // 턴이 끝날 때마다(= 플레이어가 행동할 때마다) 탐지+시야 갱신. 구독은 최초 1회만.
            if (!fogHooked && TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnCompleted += OnTurnTick;
                fogHooked = true;
            }

            OnTurnTick(); // 첫 장면부터 탐지/시야 적용
        }

        /// <summary>매 턴: 함정 탐지 → 시야 재계산. (함정 탐지는 fog 사용 여부와 무관하게 동작)</summary>
        private void OnTurnTick()
        {
            if (currentMap != null && currentPlayer != null)
                TrapController.RunDetection(currentPlayer.GridPosition, currentMap, trapDetectionRadius);
            currentFog?.Recompute();
        }

        private void SpawnTraps(GeneratedDungeon dungeon, Vector2Int playerStart)
        {
            if (trapPrefab == null || trapPool.Count == 0) return;

            // 입구 근처/계단/몬스터·아이템이 이미 있는 칸은 제외
            var candidates = dungeon.Map.GetAllWalkableTiles()
                .Where(t => GridUtils.ManhattanDistance(t, playerStart) > entranceSafeRadius)
                .Where(t => t != dungeon.Exit.StairsPosition)
                .Where(t => MonsterController.FindAt(t) == null && ItemPickup.FindAt(t) == null)
                .OrderBy(_ => Random.value)
                .ToList();

            int count = Mathf.Min(trapCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                var data = PickTrap();
                if (data == null) break;
                var trap = Instantiate(trapPrefab);
                trap.Init(data, candidates[i]);
            }
        }

        /// <summary>현재 층에 등장 가능한 함정 중 weight에 비례해 하나를 뽑는다.</summary>
        private TrapData PickTrap()
        {
            float total = 0f;
            foreach (var e in trapPool)
                if (e != null && e.data != null && currentDepth >= e.minDepth && e.weight > 0f)
                    total += e.weight;

            if (total <= 0f) return null;

            float roll = Random.value * total;
            foreach (var e in trapPool)
            {
                if (e == null || e.data == null || currentDepth < e.minDepth || e.weight <= 0f) continue;
                roll -= e.weight;
                if (roll <= 0f) return e.data;
            }
            return null;
        }

        private void HandleInteract()
        {
            if (currentPlayer.GridPosition != currentStairsPos) return;

            currentDepth++;
            Debug.Log($"계단을 타고 {currentDepth}층으로 이동합니다.");
            GenerateLevel();
        }

        private void HandlePlayerDied()
        {
            // 게임오버 화면 표시. 진행 중인 불/몬스터 정리는 하지 않고 화면만 덮는다
            // (플레이어 입력은 PlayerController가 isDead로 이미 막음).
            if (gameOverUI == null)
            {
                gameOverUI = gameObject.AddComponent<GameOverUI>();
                gameOverUI.OnRestart += RestartGame;
                gameOverUI.OnToMenu += ReturnToMenu;
            }
            // 사망 후에는 ESC 일시정지가 게임오버 화면 위에 겹치지 않도록 막는다
            if (pauseMenu != null)
            {
                pauseMenu.Hide();
                pauseMenu.Suppressed = true;
            }
            gameOverUI.Show(currentDepth);
        }

        private void RestartGame()
        {
            // 현재(Game) 씬을 통째로 다시 로드 - 모든 상태가 깨끗하게 초기화됨
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnToMenu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
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
                    var instance = ItemInstance.Create(itemData, 1);
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
                var data = PickMonster();
                if (data == null) break; // 풀이 비었거나 현재 층에 등장 가능한 몬스터가 없음
                var monster = Instantiate(monsterPrefab);
                monster.Init(data, dungeon.Map, player, candidates[i]);
            }
        }

        /// <summary>
        /// 현재 층(currentDepth)에 등장 가능한 몬스터 중 weight에 비례해 하나를 뽑는다.
        /// minDepth 조건에 맞는 후보가 하나도 없으면 null.
        /// </summary>
        private MonsterData PickMonster()
        {
            float total = 0f;
            foreach (var e in monsterPool)
                if (e != null && e.data != null && currentDepth >= e.minDepth && e.weight > 0f)
                    total += e.weight;

            if (total <= 0f) return null;

            float roll = Random.value * total;
            foreach (var e in monsterPool)
            {
                if (e == null || e.data == null || currentDepth < e.minDepth || e.weight <= 0f) continue;
                roll -= e.weight;
                if (roll <= 0f) return e.data;
            }
            return null; // 부동소수 오차 방어 (사실상 도달 안 함)
        }

        /// <summary>콘솔 spawn용 - center 주변의 비어있는 바닥 타일에 몬스터를 스폰.</summary>
        private void SpawnMonstersNear(Vector2Int center, int n)
        {
            if (currentMap == null || currentPlayer == null) return;

            var candidates = currentMap.GetAllWalkableTiles()
                .Where(t => t != currentPlayer.GridPosition && MonsterController.FindAt(t) == null)
                .OrderBy(t => GridUtils.ManhattanDistance(t, center))
                .Take(Mathf.Max(n * 3, n))   // 가까운 후보 중에서
                .OrderBy(_ => Random.value)
                .Take(n)
                .ToList();

            foreach (var pos in candidates)
            {
                var data = PickMonster();
                if (data == null) break;
                var monster = Instantiate(monsterPrefab);
                monster.Init(data, currentMap, currentPlayer, pos);
            }
        }
    }
}
