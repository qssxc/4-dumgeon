using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCore
{
    /// <summary>
    /// 플레이어 입력을 받아서 "액션"으로 변환하고, TurnManager에게 턴 처리를 요청한다.
    /// 실제 이동/공격 실행은 TakeTurn()에서 일어난다 (입력 시점과 실행 시점을 분리).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour, ITurnActor
    {
        [SerializeField] private int maxHP = 20;
        [SerializeField] private int attack = 5;
        [SerializeField] private int defense = 2;
        [SerializeField] private float moveAnimSeconds = 0.05f; // 이동 1칸 애니메이션 시간. 작을수록 빠릿함.
        [SerializeField] private Color fallbackColor = Color.cyan;
        [Header("애니메이션 타이밍")]
        [SerializeField] private float walkAnimSeconds = 0.25f;   // 걷기 프레임 재생 시간 (이동 연출과 별개, 늘리면 느려짐)
        [SerializeField] private float attackAnimSeconds = 0.3f;  // 공격 프레임 재생 시간

        // 외형/애니메이션 프레임은 CharacterClassData로부터 주입받음 (클래스마다 다르므로)
        private Sprite[] walkFrames;
        private Sprite[] attackFrames;

        private SpriteAnimator spriteAnimator;
        private HitFlash hitFlash;
        private ThrowTargeting throwTargeting;

        [Header("클릭 이동")]
        [Tooltip("자동 이동 중, 이 반경 안에 몬스터가 들어오면 안전을 위해 멈춘다.")]
        [SerializeField] private int autoMoveAlertRadius = 4;
        private readonly System.Collections.Generic.Queue<Vector2Int> autoPath = new(); // 클릭 목적지까지의 남은 경로

        /// <summary>상호작용 키(E)를 눌렀을 때 발생. 계단 등은 이 이벤트를 구독해서 처리.</summary>
        public event Action OnInteractPressed;

        /// <summary>플레이어가 사망했을 때 한 번 발생. 게임오버 처리가 구독.</summary>
        public event Action OnDied;
        private bool isDead;

        /// <summary>개발용 무적 모드. 콘솔에서 토글. 켜지면 데미지를 받지 않음.</summary>
        public bool GodMode { get; set; }

        public Combatant Combatant { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public Inventory Inventory { get; private set; }
        public Equipment Equipment { get; private set; }
        public Progression Progression { get; private set; }
        public TalentSet Talents { get; private set; }
        private CharacterClassData appliedClass; // 특성 목록 등을 Init에서 쓰기 위해 보관

        [SerializeField] private int hpPerLevel = 3; // 레벨업 시 최대 체력 증가량

        private DungeonMap map;

        // 대기 중인 액션 (입력 시점에 결정, TakeTurn에서 실행)
        private enum ActionType { None, Move, Attack, UseItem }
        private ActionType pendingAction = ActionType.None;
        private Vector2Int pendingTarget;
        private Vector2Int pendingMoveDir;  // 이동/공격 방향 (좌우 플립용)
        private int pendingItemIndex;

        // --- ITurnActor ---
        public int Speed => 100 + (Combatant?.SpeedBonus ?? 0) + (Talents?.SpeedBonus ?? 0);
        public float Energy { get; set; }
        public bool IsAlive => Combatant.IsAlive;

        /// <summary>
        /// 선택된 캐릭터 클래스의 외형(스프라이트/애니메이션)을 플레이어에 적용.
        /// Init() 전에 호출해야 한다 (보통 DungeonBootstrap이 플레이어 생성 직후 호출).
        /// classData가 null이면 아무 것도 안 하고 기존 외형/색 사각형으로 동작.
        /// </summary>
        public void ApplyClass(CharacterClassData classData)
        {
            if (classData == null) return;
            appliedClass = classData; // Init에서 특성 세트 구성에 사용

            walkFrames = classData.walkFrames;
            attackFrames = classData.attackFrames;

            // 정지 시 보여줄 스프라이트 결정: baseSprite가 없으면 idle/walk 첫 프레임으로 폴백.
            // (baseSprite를 비우고 애니메이션 프레임만 채운 캐릭터도 생성 즉시 올바른 이미지가 뜨도록)
            Sprite restSprite = classData.baseSprite;
            if (restSprite == null && classData.idleFrames != null && classData.idleFrames.Length > 0)
                restSprite = classData.idleFrames[0];
            if (restSprite == null && classData.walkFrames != null && classData.walkFrames.Length > 0)
                restSprite = classData.walkFrames[0];

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (restSprite != null)
                spriteRenderer.sprite = restSprite;

            var animator = GetComponent<SpriteAnimator>();
            if (animator != null)
            {
                // 기본 스프라이트를 먼저 알려주고(프리팹 idle 루프가 덮어쓰지 않도록),
                // idle 프레임은 조건 없이 적용한다(없으면 내부에서 루프를 멈추고 기본 스프라이트로 복귀).
                animator.SetBaseSprite(restSprite);
                animator.SetIdleFrames(classData.idleFrames, classData.idleFps);
            }

            // 나중에 스탯도 반영하려면 여기서 classData.startingHP 등을 사용
        }

        public void Init(DungeonMap dungeonMap, Vector2Int startPos)
        {
            map = dungeonMap;
            GridPosition = startPos;
            transform.position = GridUtils.GridToWorld(startPos);

            // Init()은 층 이동 시에도 다시 호출됨 - Combatant/Inventory/Equipment는 런 전체에
            // 걸쳐 유지돼야 하므로 최초 1회만 생성 (안 그러면 층마다 체력/소지품이 초기화됨)
            if (Equipment == null)
                Equipment = new Equipment();
            if (Combatant == null)
            {
                Combatant = new Combatant("Player", maxHP, attack, defense);
                // 장비 + 특성 보너스를 함께 합산 (Talents는 아래에서 생성되므로 null 방어)
                Combatant.SetBonusProviders(
                    () => Equipment.TotalAttackBonus + (Talents?.AttackBonus ?? 0),
                    () => Equipment.TotalDefenseBonus + (Talents?.DefenseBonus ?? 0));
            }
            if (Inventory == null)
                Inventory = new Inventory();
            if (Progression == null)
            {
                Progression = new Progression();
                Progression.OnLevelUp += HandleLevelUp;
            }
            if (Talents == null)
                Talents = new TalentSet(appliedClass != null ? appliedClass.talents : null, this, Progression);

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null)
                spriteRenderer.sprite = SpriteUtils.CreateSolidSprite(fallbackColor);
            spriteRenderer.sortingOrder = 5; // 바닥/벽보다 항상 위에 그려지도록

            spriteAnimator = GetComponent<SpriteAnimator>(); // 없으면 null - 애니메이션 없이 동작

            hitFlash = GetComponent<HitFlash>();
            if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();

            throwTargeting = GetComponent<ThrowTargeting>();
            if (throwTargeting == null) throwTargeting = gameObject.AddComponent<ThrowTargeting>();

            TurnManager.Instance.Register(this);
        }

        private void Update()
        {
            if (isDead) return; // 사망하면 입력을 받지 않음
            if (PauseMenu.IsOpen) return; // 일시정지 중에는 입력 차단
            if (TurnManager.Instance == null) return;
            if (TurnManager.Instance.State != TurnState.WaitingForPlayerInput) return;

            // 조준 모드 중에는 이동/사용 입력을 막음 (ThrowTargeting이 마우스 입력을 처리)
            if (throwTargeting != null && throwTargeting.IsAiming) return;

            // 상호작용(계단 등)은 턴을 소모하는 이동/공격과 별개로 취급
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                OnInteractPressed?.Invoke();

            int itemSlot = ReadItemSlotInput();
            if (itemSlot >= 0)
            {
                bool throwMode = Keyboard.current != null &&
                    (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

                if (throwMode)
                    TryThrowItem(itemSlot);
                else
                    TryUseItem(itemSlot);
                return; // 같은 프레임에 이동 입력까지 처리하지 않도록
            }

            // 키보드 이동(홀드) - 누르는 즉시 자동 이동을 취소하고 수동 조작 우선
            Vector2Int dir = ReadDirectionInput();
            if (dir != Vector2Int.zero)
            {
                autoPath.Clear();
                TryDecideAction(dir);
                return;
            }

            // 좌클릭: 클릭한 타일까지의 경로를 계산해 자동 이동 시작 (UI 위 클릭은 무시)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI())
                HandleClickToMove();

            // 자동 이동 경로가 있으면 한 칸 진행
            StepAutoMove();
        }

        private bool IsPointerOverUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        /// <summary>클릭한 타일로 가는 경로를 계산해 autoPath에 담는다.</summary>
        private void HandleClickToMove()
        {
            var cam = Camera.main;
            if (cam == null || map == null) return;

            Vector3 world = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2Int goal = GridUtils.WorldToGrid(world);

            autoPath.Clear();
            if (goal == GridPosition) return;

            // 목적지에 몬스터가 있으면(또는 못 걷는 칸이면) 무시 - 자동 이동은 전투로 돌진하지 않음
            if (!map.IsWalkable(goal) || MonsterController.FindAt(goal) != null) return;

            var path = Pathfinder.FindPath(map, GridPosition, goal, t => MonsterController.FindAt(t) != null);
            if (path == null) return;

            foreach (var step in path)
                autoPath.Enqueue(step);
        }

        /// <summary>자동 이동 경로를 한 칸 소비. 몬스터가 근처에 있거나 길이 막히면 중단.</summary>
        private void StepAutoMove()
        {
            if (autoPath.Count == 0) return;

            // 안전장치: 근처에 몬스터가 있으면 멈춘다(블라인드 돌진 방지)
            if (MonsterNearby(autoMoveAlertRadius))
            {
                autoPath.Clear();
                return;
            }

            Vector2Int next = autoPath.Peek();
            Vector2Int dir = next - GridPosition;

            // 다음 칸이 막혔으면(몬스터가 들어왔거나 등) 중단
            if (!map.IsWalkable(next) || MonsterController.FindAt(next) != null
                || GridUtils.IsDiagonalBlocked(map, GridPosition, dir))
            {
                autoPath.Clear();
                return;
            }

            autoPath.Dequeue();
            TryDecideAction(dir);
        }

        private bool MonsterNearby(int radius)
        {
            foreach (var m in MonsterController.All)
                if (m != null && m.IsAlive && GridUtils.ChebyshevDistance(GridPosition, m.GridPosition) <= radius)
                    return true;
            return false;
        }

        /// <summary>이번 프레임에 눌린 숫자키(1~9)를 슬롯 인덱스(0~8)로 변환. 없으면 -1.</summary>
        private int ReadItemSlotInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return -1;

            var digitKeys = new[]
            {
                kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key,
                kb.digit6Key, kb.digit7Key, kb.digit8Key, kb.digit9Key,
            };

            for (int i = 0; i < digitKeys.Length; i++)
                if (digitKeys[i].wasPressedThisFrame)
                    return i;

            return -1;
        }

        private void TryThrowItem(int slotIndex)
        {
            if (Inventory == null) return;
            if (slotIndex < 0 || slotIndex >= Inventory.Items.Count) return;

            var item = Inventory.Items[slotIndex];
            if (!item.data.throwable)
            {
                Debug.Log($"{item.DisplayName}은(는) 던질 수 없다");
                return;
            }

            if (throwTargeting == null)
            {
                Debug.LogWarning("ThrowTargeting 컴포넌트가 없습니다.");
                return;
            }

            Debug.Log($"{item.DisplayName} 던지기 - 목표 타일을 클릭하세요 (우클릭/ESC 취소)");
            throwTargeting.BeginAiming(
                onConfirm: target => OnThrowConfirmed(item, target),
                onCancel: () => Debug.Log("던지기 취소"));
        }

        private void OnThrowConfirmed(ItemInstance item, Vector2Int target)
        {
            item.Identify();
            Debug.Log($"{item.DisplayName}을(를) {target}에 던짐");

            // 던진 아이템의 효과를 대상 타일에 발동 (지금은 도착 알림만 - 실제 화염 효과는 다음 단계)
            item.OnThrownTo(this, target);

            Inventory.Remove(item, 1);
            TurnManager.Instance.RequestProcessTurn(); // 던지기도 턴 소모
        }

        private void TryUseItem(int slotIndex)
        {
            if (Inventory == null) return;
            if (slotIndex < 0 || slotIndex >= Inventory.Items.Count) return; // 빈 슬롯이면 아무 일도 안 일어남

            pendingAction = ActionType.UseItem;
            pendingItemIndex = slotIndex;
            TurnManager.Instance.RequestProcessTurn();
        }

        private Vector2Int ReadDirectionInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2Int.zero; // 키보드 미연결 등 예외 상황 방어

            bool up = kb.upArrowKey.isPressed || kb.wKey.isPressed;
            bool down = kb.downArrowKey.isPressed || kb.sKey.isPressed;
            bool left = kb.leftArrowKey.isPressed || kb.aKey.isPressed;
            bool right = kb.rightArrowKey.isPressed || kb.dKey.isPressed;

            // 누르고 있는 동안 계속 이동한다(홀드 이동). 턴이 처리되는 동안엔
            // State가 Processing이라 Update가 입력을 안 받으므로, 연속 이동 속도는
            // 턴/애니메이션 길이로 자연스럽게 조절된다(너무 빠르지 않게).
            int x = 0, y = 0;
            if (up && !down) y = 1;
            else if (down && !up) y = -1;
            if (left && !right) x = -1;
            else if (right && !left) x = 1;

            // 예: Up을 누르고 있는 상태에서 Right를 새로 누르면 (1,1) 대각선이 됨
            return new Vector2Int(x, y);
        }

        private void TryDecideAction(Vector2Int dir)
        {
            Vector2Int target = GridPosition + dir;

            // 대각선인데 양옆이 벽으로 막혀있으면 코너 컷팅이므로 무시
            if (GridUtils.IsDiagonalBlocked(map, GridPosition, dir))
                return;

            var monsterAtTarget = MonsterController.FindAt(target);
            if (monsterAtTarget != null)
            {
                pendingAction = ActionType.Attack;
                pendingTarget = target;
                pendingMoveDir = dir;
                UpdateFacing(dir); // 공격은 방향을 즉시 반영 (애니메이션 전에)
                TurnManager.Instance.RequestProcessTurn();
                return;
            }

            if (map.IsWalkable(target))
            {
                pendingAction = ActionType.Move;
                pendingTarget = target;
                pendingMoveDir = dir;
                TurnManager.Instance.RequestProcessTurn();
            }
            // 벽이면 아무 일도 안 일어남 (턴 소모 없음) - 오조작에 턴 낭비하지 않도록
        }

        public IEnumerator TakeTurn(TurnManager manager)
        {
            switch (pendingAction)
            {
                case ActionType.Move:
                    GridPosition = pendingTarget;
                    UpdateFacing(pendingMoveDir);
                    if (spriteAnimator != null && walkFrames != null && walkFrames.Length > 0)
                        StartCoroutine(spriteAnimator.PlayOnce(walkFrames, walkAnimSeconds));
                    yield return StartCoroutine(AnimateMoveTo(pendingTarget));
                    TryPickupItemAt(GridPosition);
                    TrapController.FindAt(GridPosition)?.OnSteppedOn(this); // 밟은 즉시 함정 발동
                    break;

                case ActionType.Attack:
                    // 대상 쪽으로 살짝 돌진(bump) - 타격감
                    yield return StartCoroutine(BumpToward(pendingMoveDir));

                    if (spriteAnimator != null && attackFrames != null && attackFrames.Length > 0)
                        yield return StartCoroutine(spriteAnimator.PlayOnce(attackFrames, attackAnimSeconds));

                    var target = MonsterController.FindAt(pendingTarget);
                    if (target != null)
                    {
                        var result = CombatResolver.Resolve(Combatant, target.Combatant);
                        if (result.didHit)
                        {
                            // 특성(IDamageTalent)으로 피해 보정 - 첫 공격 치명타 등
                            var info = new AttackInfo(target, !target.HasBeenHitByPlayer);
                            int finalDamage = Talents != null ? Talents.ModifyDamage(result.damage, info) : result.damage;
                            target.HasBeenHitByPlayer = true;

                            target.Combatant.TakeDamage(finalDamage);
                            target.FlashHit(); // 피격 연출
                            Debug.Log($"플레이어가 {target.Combatant.displayName}에게 {finalDamage} 데미지!");
                            if (!target.Combatant.IsAlive)
                            {
                                Debug.Log($"{target.Combatant.displayName} 처치!");
                                MessageLog.Post($"처치: {target.Combatant.displayName} (+{target.Data.xpReward} XP)", MessageLog.Good);
                                Progression.AddXP(target.Data.xpReward);
                                Talents?.DispatchKill(target); // 처치 특성(IKillTalent) - 처치 시 회복 등
                                target.Die();
                            }
                        }
                        else
                        {
                            Debug.Log("플레이어의 공격이 빗나감");
                        }
                    }
                    break;

                case ActionType.UseItem:
                    if (pendingItemIndex >= 0 && pendingItemIndex < Inventory.Items.Count)
                    {
                        var item = Inventory.Items[pendingItemIndex];
                        item.Use(this);
                    }
                    break;
            }

            pendingAction = ActionType.None;
            Combatant.OnTurnPassed(); // 신속 등 임시 버프 카운트다운
            yield return null;
        }

        /// <summary>
        /// 이동/공격 방향에 따라 스프라이트 좌우를 뒤집는다 (회전이 아니라 flipX).
        /// 좌우 성분이 있을 때만 갱신하고, 순수 상하 이동은 기존 방향을 유지한다.
        /// </summary>
        private void UpdateFacing(Vector2Int dir)
        {
            if (dir.x == 0) return; // 위/아래만 움직이면 바라보는 방향 유지

            var sr = GetComponent<SpriteRenderer>();
            sr.flipX = dir.x < 0; // 왼쪽으로 가면 뒤집음 (원본 스프라이트가 오른쪽을 본다고 가정)
        }

        private IEnumerator AnimateMoveTo(Vector2Int gridPos)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = GridUtils.GridToWorld(gridPos);
            float t = 0f;
            while (t < moveAnimSeconds)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, t / moveAnimSeconds);
                yield return null;
            }
            transform.position = endPos;
        }

        /// <summary>공격 시 대상 방향으로 살짝 나갔다 돌아오는 돌진 모션(타격감).</summary>
        private IEnumerator BumpToward(Vector2Int dir)
        {
            Vector3 start = transform.position;
            Vector3 offset = ((Vector3)(Vector2)dir).normalized * 0.28f;
            Vector3 peak = start + offset;
            float half = 0.06f;

            float t = 0f;
            while (t < half) { t += Time.deltaTime; transform.position = Vector3.Lerp(start, peak, t / half); yield return null; }
            t = 0f;
            while (t < half) { t += Time.deltaTime; transform.position = Vector3.Lerp(peak, start, t / half); yield return null; }
            transform.position = start;
        }

        private void TryPickupItemAt(Vector2Int pos)
        {
            var pickup = ItemPickup.FindAt(pos);
            if (pickup == null) return;

            if (Inventory.TryAdd(pickup.Item))
            {
                Debug.Log($"{pickup.Item.DisplayName} 획득 (x{pickup.Item.quantity})");
                MessageLog.Post($"획득: {pickup.Item.DisplayName} (x{pickup.Item.quantity})", MessageLog.Good);
                Destroy(pickup.gameObject);
            }
            else
            {
                Debug.Log("인벤토리가 가득 차서 주울 수 없습니다.");
                MessageLog.Post("인벤토리가 가득 찼습니다.", MessageLog.Bad);
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            // 레벨업 시 최대 체력 증가 + 그만큼 즉시 회복 (피해 상태여도 보상이 체감되도록)
            Combatant.maxHP += hpPerLevel;
            Combatant.Heal(hpPerLevel);

            Debug.Log($"레벨업! Lv.{newLevel} (최대 체력 {Combatant.maxHP}, 특성 포인트 {Progression.TalentPoints}개 보유)");
            MessageLog.Post($"레벨 업! Lv.{newLevel} (특성 포인트 +1)", MessageLog.Highlight);
        }

        public void TakeDamage(int amount)
        {
            if (isDead) return;
            if (GodMode) return; // 무적 모드면 데미지 무시

            Combatant.TakeDamage(amount);
            hitFlash?.Flash();
            CameraFollow.Instance?.Shake(); // 플레이어가 맞으면 화면 흔들림
            if (!Combatant.IsAlive)
            {
                isDead = true;
                Debug.Log("플레이어 사망! 게임오버");
                TurnManager.Instance?.Unregister(this); // 더 이상 턴을 받지 않음
                OnDied?.Invoke();
            }
        }
    }
}
