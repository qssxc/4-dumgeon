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
    public class MonsterController : MonoBehaviour, ITurnActor
    {
        private static readonly List<MonsterController> all = new();

        public MonsterData Data { get; private set; }
        public Combatant Combatant { get; private set; }
        public Vector2Int GridPosition { get; private set; }

        private DungeonMap map;
        private PlayerController player;

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

            all.Add(this);
            TurnManager.Instance.Register(this);
        }

        public static MonsterController FindAt(Vector2Int pos)
        {
            foreach (var m in all)
                if (m.IsAlive && m.GridPosition == pos)
                    return m;
            return null;
        }

        [SerializeField] private float wanderChance = 0.5f; // 배회 중 매턴 실제로 움직일 확률

        public IEnumerator TakeTurn(TurnManager manager)
        {
            int distToPlayer = GridUtils.ManhattanDistance(GridPosition, player.GridPosition);

            // 감지 범위 밖 -> 가만히 있지 않고 배회 (픽셀던전처럼 몬스터가 맵을 돌아다님)
            if (distToPlayer > Data.detectRange)
            {
                if (Random.value < wanderChance)
                    yield return StartCoroutine(WanderStep());
                yield break;
            }

            // 인접 -> 공격
            if (distToPlayer == 1)
            {
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
                yield break;
            }

            // 그 외 -> 플레이어 방향으로 한 칸 이동 (그리디)
            Vector2Int bestMove = GridPosition;
            int bestDist = distToPlayer;

            foreach (var dir in GridUtils.Directions4)
            {
                Vector2Int candidate = GridPosition + dir;
                if (!map.IsWalkable(candidate)) continue;
                if (FindAt(candidate) != null) continue; // 다른 몬스터가 있으면 패스
                if (candidate == player.GridPosition) continue; // 플레이어 칸은 이동 대상 아님(공격으로 처리)

                int d = GridUtils.ManhattanDistance(candidate, player.GridPosition);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestMove = candidate;
                }
            }

            if (bestMove != GridPosition)
            {
                GridPosition = bestMove;
                yield return StartCoroutine(AnimateMoveTo(bestMove));
            }
        }

        private IEnumerator WanderStep()
        {
            // 4방향 중 이동 가능한 칸을 랜덤하게 골라 한 칸 이동. 정교한 경로 없이 그냥 배회.
            var candidates = new List<Vector2Int>();
            foreach (var dir in GridUtils.Directions4)
            {
                var candidate = GridPosition + dir;
                if (!map.IsWalkable(candidate)) continue;
                if (FindAt(candidate) != null) continue;
                if (candidate == player.GridPosition) continue;
                candidates.Add(candidate);
            }

            if (candidates.Count == 0) yield break;

            var target = candidates[Random.Range(0, candidates.Count)];
            GridPosition = target;
            yield return StartCoroutine(AnimateMoveTo(target));
        }

        private IEnumerator AnimateMoveTo(Vector2Int gridPos)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = GridUtils.GridToWorld(gridPos);
            float duration = 0.15f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, t / duration);
                yield return null;
            }
            transform.position = endPos;
        }

        public void Die()
        {
            all.Remove(this);
            TurnManager.Instance.Unregister(this);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            all.Remove(this);
        }
    }
}
