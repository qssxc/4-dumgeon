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
        [SerializeField] private float moveAnimSeconds = 0.12f;
        [SerializeField] private Color fallbackColor = Color.cyan;

        /// <summary>상호작용 키(E)를 눌렀을 때 발생. 계단 등은 이 이벤트를 구독해서 처리.</summary>
        public event Action OnInteractPressed;

        public Combatant Combatant { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public Inventory Inventory { get; private set; }

        private DungeonMap map;

        // 대기 중인 액션 (입력 시점에 결정, TakeTurn에서 실행)
        private enum ActionType { None, Move, Attack }
        private ActionType pendingAction = ActionType.None;
        private Vector2Int pendingTarget;

        // --- ITurnActor ---
        public int Speed => 100;
        public float Energy { get; set; }
        public bool IsAlive => Combatant.IsAlive;

        public void Init(DungeonMap dungeonMap, Vector2Int startPos)
        {
            map = dungeonMap;
            GridPosition = startPos;
            transform.position = GridUtils.GridToWorld(startPos);

            // Init()은 층 이동 시에도 다시 호출됨 - Combatant/Inventory는 런 전체에 걸쳐
            // 유지돼야 하므로 최초 1회만 생성 (안 그러면 층마다 체력/소지품이 초기화됨)
            if (Combatant == null)
                Combatant = new Combatant("Player", maxHP, attack, defense);
            if (Inventory == null)
                Inventory = new Inventory();

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null)
                spriteRenderer.sprite = SpriteUtils.CreateSolidSprite(fallbackColor);
            spriteRenderer.sortingOrder = 5; // 바닥/벽보다 항상 위에 그려지도록

            TurnManager.Instance.Register(this);
        }

        private void Update()
        {
            if (TurnManager.Instance == null) return;
            if (TurnManager.Instance.State != TurnState.WaitingForPlayerInput) return;

            // 상호작용(계단 등)은 턴을 소모하는 이동/공격과 별개로 취급
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                OnInteractPressed?.Invoke();

            Vector2Int dir = ReadDirectionInput();
            if (dir == Vector2Int.zero) return;

            TryDecideAction(dir);
        }

        private Vector2Int ReadDirectionInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2Int.zero; // 키보드 미연결 등 예외 상황 방어

            bool up = kb.upArrowKey.isPressed || kb.wKey.isPressed;
            bool down = kb.downArrowKey.isPressed || kb.sKey.isPressed;
            bool left = kb.leftArrowKey.isPressed || kb.aKey.isPressed;
            bool right = kb.rightArrowKey.isPressed || kb.dKey.isPressed;

            // 이번 프레임에 "새로 눌린" 방향키가 있어야 턴이 발생한다.
            bool newPress =
                kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame ||
                kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame ||
                kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ||
                kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;

            if (!newPress) return Vector2Int.zero;

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
                TurnManager.Instance.RequestProcessTurn();
                return;
            }

            if (map.IsWalkable(target))
            {
                pendingAction = ActionType.Move;
                pendingTarget = target;
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
                    yield return StartCoroutine(AnimateMoveTo(pendingTarget));
                    TryPickupItemAt(GridPosition);
                    break;

                case ActionType.Attack:
                    var target = MonsterController.FindAt(pendingTarget);
                    if (target != null)
                    {
                        var result = CombatResolver.Resolve(Combatant, target.Combatant);
                        if (result.didHit)
                        {
                            target.Combatant.TakeDamage(result.damage);
                            Debug.Log($"플레이어가 {target.Combatant.displayName}에게 {result.damage} 데미지!");
                            if (!target.Combatant.IsAlive)
                            {
                                Debug.Log($"{target.Combatant.displayName} 처치!");
                                target.Die();
                            }
                        }
                        else
                        {
                            Debug.Log("플레이어의 공격이 빗나감");
                        }
                    }
                    break;
            }

            pendingAction = ActionType.None;
            yield return null;
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

        private void TryPickupItemAt(Vector2Int pos)
        {
            var pickup = ItemPickup.FindAt(pos);
            if (pickup == null) return;

            if (Inventory.TryAdd(pickup.Item))
            {
                Debug.Log($"{pickup.Item.DisplayName} 획득 (x{pickup.Item.quantity})");
                Destroy(pickup.gameObject);
            }
            else
            {
                Debug.Log("인벤토리가 가득 차서 주울 수 없습니다.");
            }
        }

        public void TakeDamage(int amount)
        {
            Combatant.TakeDamage(amount);
            if (!Combatant.IsAlive)
            {
                Debug.Log("플레이어 사망! 게임오버");
                // TODO: 2단계 이후 - GameManager에 사망 이벤트 전파
            }
        }
    }
}
