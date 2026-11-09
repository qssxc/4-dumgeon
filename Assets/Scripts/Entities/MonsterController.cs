using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 최소 AI: 플레이어가 감지 범위 안에 있으면 다가가고, 인접하면 공격.
    /// 이동은 A* 없이 "거리를 줄이는 방향" 그리디 방식으로 처리 (1단계에서는 충분).
    /// 나중에 통로가 복잡해지면 Pathfinder 클래스로 교체.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class MonsterController : MonoBehaviour, ITurnActor
    {
        private static readonly List<MonsterController> all = new();
        public static IReadOnlyList<MonsterController> All => all;

        [SerializeField] private Color fallbackColor = Color.red;

        public MonsterData Data { get; private set; }
        public Combatant Combatant { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public bool HasBeenHitByPlayer { get; set; } // 첫 공격 특성(FirstStrike) 판정용

        private DungeonMap map;
        private PlayerController player;
        private SpriteAnimator spriteAnimator;
        private SpriteRenderer spriteRenderer;
        private HealthBar healthBar;
        private HitFlash hitFlash;

        public int Speed => Data.speed;
        public float Energy { get; set; }
        public bool IsAlive => Combatant.IsAlive;

        public void Init(MonsterData data, DungeonMap dungeonMap, PlayerController playerRef, Vector2Int startPos)
        {
            Data = data;
            map = dungeonMap;
            player = playerRef;
            GridPosition = startPos;
            Combatant = new Combatant(data.monsterName, data.maxHP, data.attack, data.defense);
            transform.position = GridUtils.GridToWorld(startPos);

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (data.sprite != null)
                spriteRenderer.sprite = data.sprite;
            else if (spriteRenderer.sprite == null)
                spriteRenderer.sprite = SpriteUtils.CreateSolidSprite(fallbackColor);
            spriteRenderer.sortingOrder = 4; // 바닥/벽보다 위, 플레이어보다는 아래

            spriteAnimator = GetComponent<SpriteAnimator>(); // 없으면 null - 애니메이션 없이 동작
            if (spriteAnimator != null)
            {
                // 기본 스프라이트를 알려주고, idle 프레임은 조건 없이 적용(없으면 프리팹 루프를 멈추고 기본 스프라이트 유지)
                spriteAnimator.SetBaseSprite(spriteRenderer.sprite);
                spriteAnimator.SetIdleFrames(data.idleFrames, data.idleFps);
            }

            hitFlash = GetComponent<HitFlash>();
            if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();

            // 머리 위 체력바 (피해를 입었을 때만 표시)
            healthBar = HealthBar.Create(transform, Combatant, yOffset: 0.6f);

            all.Add(this);
            TurnManager.Instance.Register(this);
        }

        /// <summary>피격 연출(스프라이트 플래시 + 스케일 펀치). 외부에서 피해를 입힐 때 호출.</summary>
        public void FlashHit() => hitFlash?.Flash();

        private bool isSeen = true; // fog 기준 현재 보이는지. 안 보이면 이동 애니메이션을 건너뛴다.

        /// <summary>fog of war: 시야 밖이면 스프라이트와 체력바를 숨긴다(행동 로직은 계속 돈다).</summary>
        public void SetSeen(bool seen)
        {
            isSeen = seen;
            if (spriteRenderer != null) spriteRenderer.enabled = seen;
            if (healthBar != null) healthBar.ForceHidden = !seen;
        }

        public static MonsterController FindAt(Vector2Int pos)
        {
            foreach (var m in all)
                if (m.IsAlive && m.GridPosition == pos)
                    return m;
            return null;
        }

        [SerializeField] private float wanderChance = 0.5f; // 배회 중 매턴 실제로 움직일 확률

        /// <summary>
        /// 한 턴에 actionsPerTurn번 행동한다. 빠른 적은 2로 두면 턴당 2번 움직이거나 공격.
        /// TurnManager는 건드리지 않으므로(에너지/턴 순서는 그대로) 플레이어 로직에 영향이 없다.
        /// </summary>
        public IEnumerator TakeTurn(TurnManager manager)
        {
            int actions = Mathf.Max(1, Data.actionsPerTurn);
            for (int i = 0; i < actions; i++)
            {
                // 중간에 죽거나 플레이어가 죽으면 남은 행동 취소
                if (!IsAlive || player == null || !player.Combatant.IsAlive) yield break;
                yield return StartCoroutine(DecideAndAct());
            }
        }

        /// <summary>한 번의 행동. behavior에 따라 분기.</summary>
        private IEnumerator DecideAndAct()
        {
            int distToPlayer = GridUtils.ManhattanDistance(GridPosition, player.GridPosition);

            // 감지 범위 밖 -> 배회 (픽셀던전처럼 몬스터가 맵을 돌아다님)
            if (distToPlayer > Data.detectRange)
            {
                if (Random.value < wanderChance)
                    yield return StartCoroutine(WanderStep());
                yield break;
            }

            switch (Data.behavior)
            {
                case MonsterBehavior.Ranged:
                    yield return StartCoroutine(ActRanged(distToPlayer));
                    break;
                default:
                    yield return StartCoroutine(ActMelee(distToPlayer));
                    break;
            }
        }

        // ── 근접(기본): 인접하면 공격, 아니면 다가감 ─────────────────────────
        private IEnumerator ActMelee(int distToPlayer)
        {
            if (distToPlayer == 1)
            {
                yield return StartCoroutine(MeleeAttack());
                yield break;
            }
            yield return StartCoroutine(StepToward());
        }

        // ── 원거리: 사거리+시야 확보 시 투사체, 너무 붙으면 거리 벌림 ─────────
        private IEnumerator ActRanged(int distToPlayer)
        {
            bool inRange = GridUtils.ChebyshevDistance(GridPosition, player.GridPosition) <= Data.attackRange;
            bool los = GridUtils.HasLineOfSight(map, GridPosition, player.GridPosition);

            // 플레이어가 바로 옆까지 붙음 -> 거리를 벌려 원거리 이점을 유지(카이팅).
            // 물러날 곳이 없으면 어쩔 수 없이 근접 공격.
            if (distToPlayer == 1)
            {
                Vector2Int retreat;
                if (TryFindRetreat(out retreat))
                    yield return StartCoroutine(MoveStep(retreat));
                else
                    yield return StartCoroutine(MeleeAttack());
                yield break;
            }

            if (inRange && los)
            {
                yield return StartCoroutine(RangedAttack());
                yield break;
            }

            // 사거리 밖이거나 시야가 막힘 -> 쏠 수 있는 위치로 접근
            yield return StartCoroutine(StepToward());
        }

        // ── 공통 행동 ──────────────────────────────────────────────────────

        /// <summary>인접한 플레이어를 근접 공격.</summary>
        private IEnumerator MeleeAttack()
        {
            UpdateFacing(player.GridPosition - GridPosition);

            // 플레이어 쪽으로 살짝 돌진(bump)
            yield return StartCoroutine(BumpToward(player.GridPosition - GridPosition));

            if (spriteAnimator != null && Data.attackFrames != null && Data.attackFrames.Length > 0)
                yield return StartCoroutine(spriteAnimator.PlayOnce(Data.attackFrames, Data.attackAnimSeconds));

            var result = CombatResolver.Resolve(Combatant, player.Combatant);
            if (result.didHit)
            {
                player.TakeDamage(result.damage);
                Debug.Log($"{Combatant.displayName}이(가) 플레이어에게 {result.damage} 데미지!");
            }
            else
            {
                Debug.Log($"{Combatant.displayName}의 공격이 빗나감");
            }
        }

        /// <summary>원거리 공격: 투사체를 날린 뒤 데미지 적용.</summary>
        private IEnumerator RangedAttack()
        {
            UpdateFacing(player.GridPosition - GridPosition);

            if (spriteAnimator != null && Data.attackFrames != null && Data.attackFrames.Length > 0)
                yield return StartCoroutine(spriteAnimator.PlayOnce(Data.attackFrames, Data.attackAnimSeconds));

            // 투사체가 날아가는 연출(도착하면 데미지)
            yield return StartCoroutine(FireProjectile(GridPosition, player.GridPosition));

            var result = CombatResolver.Resolve(Combatant, player.Combatant);
            if (result.didHit)
            {
                player.TakeDamage(result.damage);
                Debug.Log($"{Combatant.displayName}이(가) 원거리로 플레이어에게 {result.damage} 데미지!");
            }
            else
            {
                Debug.Log($"{Combatant.displayName}의 원거리 공격이 빗나감");
            }
        }

        /// <summary>작은 사각형 투사체를 from->to 월드 좌표로 날리는 간단한 연출.</summary>
        private IEnumerator FireProjectile(Vector2Int from, Vector2Int to)
        {
            var go = new GameObject("MonsterProjectile", typeof(SpriteRenderer));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteUtils.CreateSolidSprite(Data.projectileColor);
            sr.sortingOrder = 6; // 몬스터/플레이어 위로 보이게
            go.transform.localScale = Vector3.one * 0.3f;

            Vector3 start = GridUtils.GridToWorld(from);
            Vector3 end = GridUtils.GridToWorld(to);
            go.transform.position = start;

            float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                go.transform.position = Vector3.Lerp(start, end, t / duration);
                yield return null;
            }
            Destroy(go);
        }

        /// <summary>플레이어 방향으로 거리를 줄이는 한 칸 이동(그리디).</summary>
        private IEnumerator StepToward()
        {
            int bestDist = GridUtils.ManhattanDistance(GridPosition, player.GridPosition);
            Vector2Int bestMove = GridPosition;

            foreach (var dir in GridUtils.Directions4)
            {
                Vector2Int candidate = GridPosition + dir;
                if (!CanStandOn(candidate)) continue;

                int d = GridUtils.ManhattanDistance(candidate, player.GridPosition);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestMove = candidate;
                }
            }

            if (bestMove != GridPosition)
                yield return StartCoroutine(MoveStep(bestMove));
        }

        /// <summary>플레이어에게서 멀어지는 칸을 찾는다(카이팅). 찾으면 true.</summary>
        private bool TryFindRetreat(out Vector2Int result)
        {
            int curDist = GridUtils.ManhattanDistance(GridPosition, player.GridPosition);
            int bestDist = curDist;
            result = GridPosition;

            foreach (var dir in GridUtils.Directions4)
            {
                Vector2Int candidate = GridPosition + dir;
                if (!CanStandOn(candidate)) continue;

                int d = GridUtils.ManhattanDistance(candidate, player.GridPosition);
                if (d > bestDist)
                {
                    bestDist = d;
                    result = candidate;
                }
            }
            return result != GridPosition;
        }

        /// <summary>해당 칸으로 실제 이동(애니메이션 포함).</summary>
        private IEnumerator MoveStep(Vector2Int target)
        {
            UpdateFacing(target - GridPosition);
            if (spriteAnimator != null && Data.walkFrames != null && Data.walkFrames.Length > 0)
                StartCoroutine(spriteAnimator.PlayOnce(Data.walkFrames, Data.walkAnimSeconds));
            GridPosition = target;
            yield return StartCoroutine(AnimateMoveTo(target));
        }

        /// <summary>그 칸에 설 수 있는지(벽 아님 + 다른 몬스터 없음 + 플레이어 칸 아님).</summary>
        private bool CanStandOn(Vector2Int pos)
        {
            if (!map.IsWalkable(pos)) return false;
            if (FindAt(pos) != null) return false;
            if (pos == player.GridPosition) return false;
            return true;
        }

        private IEnumerator WanderStep()
        {
            // 4방향 중 이동 가능한 칸을 랜덤하게 골라 한 칸 이동. 정교한 경로 없이 그냥 배회.
            var candidates = new List<Vector2Int>();
            foreach (var dir in GridUtils.Directions4)
            {
                var candidate = GridPosition + dir;
                if (CanStandOn(candidate)) candidates.Add(candidate);
            }

            if (candidates.Count == 0) yield break;

            var target = candidates[Random.Range(0, candidates.Count)];
            yield return StartCoroutine(MoveStep(target));
        }

        /// <summary>이동/공격 방향에 따라 스프라이트 좌우 뒤집기 (좌우 성분 있을 때만).</summary>
        private void UpdateFacing(Vector2Int dir)
        {
            if (dir.x == 0) return;
            spriteRenderer.flipX = dir.x < 0;
        }

        private IEnumerator AnimateMoveTo(Vector2Int gridPos)
        {
            Vector3 endPos = GridUtils.GridToWorld(gridPos);

            // 화면 밖(안 보이는) 몬스터는 애니메이션을 기다리지 않고 즉시 이동 -
            // 턴당 '이동 후 멈추는 시간'을 크게 줄여준다(안 보이는 적을 기다릴 이유가 없음).
            if (!isSeen)
            {
                transform.position = endPos;
                yield break;
            }

            Vector3 startPos = transform.position;
            float duration = 0.05f; // 0.15 → 0.05 (약 3배 빠르게)
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, t / duration);
                yield return null;
            }
            transform.position = endPos;
        }

        /// <summary>공격 시 대상 방향으로 살짝 나갔다 돌아오는 돌진 모션(타격감).</summary>
        private IEnumerator BumpToward(Vector2Int dir)
        {
            Vector3 start = transform.position;
            Vector3 peak = start + ((Vector3)(Vector2)dir).normalized * 0.28f;
            float half = 0.06f;

            float t = 0f;
            while (t < half) { t += Time.deltaTime; transform.position = Vector3.Lerp(start, peak, t / half); yield return null; }
            t = 0f;
            while (t < half) { t += Time.deltaTime; transform.position = Vector3.Lerp(peak, start, t / half); yield return null; }
            transform.position = start;
        }

        /// <summary>화염 등 환경 효과로부터 데미지를 받음. 죽으면 정리까지 처리.</summary>
        public void TakeEnvironmentDamage(int amount)
        {
            Combatant.TakeDamage(amount);
            hitFlash?.Flash();
            Debug.Log($"{Combatant.displayName}이(가) 화염으로 {amount} 데미지!");
            if (!Combatant.IsAlive)
            {
                Debug.Log($"{Combatant.displayName}이(가) 불에 타 죽었다");
                Die();
            }
        }

        public void Die()
        {
            Destroy(gameObject); // 실제 정리는 OnDestroy()에서 일괄 처리 (중복 방지)
        }

        private void OnDestroy()
        {
            all.Remove(this);
            TurnManager.Instance?.Unregister(this); // Destroy()로 직접 없앨 때도 등록이 안 남도록
        }

        /// <summary>레벨 전환 시 현재 층의 몬스터를 전부 정리할 때 사용.</summary>
        public static void DestroyAll()
        {
            // 순회 중 리스트가 변경되므로 복사본으로 순회
            foreach (var monster in new List<MonsterController>(all))
                Destroy(monster.gameObject);
        }
    }
}
