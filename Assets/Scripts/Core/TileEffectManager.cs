using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 타일에 얹히는 환경 효과(현재는 화염)를 관리. 매 턴:
    /// 1) 타일 위의 액터에게 데미지  2) 지속시간 감소  3) 시각 표시 갱신.
    ///
    /// 확산(불이 번지는 것)은 지금 없다 - 나중에 "불이 붙는 오브젝트"를 배치하는 방식으로
    /// 대체할 예정. 지금은 물약이 깨진 지점에서 원형 범위로 붙었다가 시간이 지나면 꺼진다.
    ///
    /// TurnManager.OnTurnCompleted에 구독해서 매 턴 자동으로 진행된다.
    /// </summary>
    public class TileEffectManager : MonoBehaviour
    {
        public static TileEffectManager Instance { get; private set; }

        [SerializeField] private int fireDamagePerTurn = 3;
        [SerializeField] private Color fireColor = new Color(1f, 0.45f, 0.1f, 0.75f);

        private DungeonMap map;
        private PlayerController player;

        // 타일별 화염 남은 턴 수
        private readonly Dictionary<Vector2Int, int> fireTiles = new();
        // 화염 시각 표시용 오브젝트
        private readonly Dictionary<Vector2Int, GameObject> fireVisuals = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnCompleted += Tick;
        }

        private void OnDisable()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnCompleted -= Tick;
        }

        /// <summary>레벨 시작 시 현재 맵/플레이어를 연결. 층 전환 때마다 호출.</summary>
        public void Bind(DungeonMap dungeonMap, PlayerController playerRef)
        {
            map = dungeonMap;
            player = playerRef;
            ClearAll();
        }

        /// <summary>
        /// 중심 타일에서 반경 radius 이내(원형)의 걸을 수 있는 타일에 화염을 붙임.
        /// 마시기/던지기 양쪽에서 호출. 확산은 없으므로 여기서 붙은 타일이 곧 불의 전체 범위.
        /// </summary>
        public void IgniteArea(Vector2Int center, int radius, int duration)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // 원형 판정: 중심으로부터의 실제 거리가 radius 이내인 타일만
                    if (dx * dx + dy * dy > radius * radius) continue;

                    var pos = new Vector2Int(center.x + dx, center.y + dy);
                    if (map != null && !map.IsWalkable(pos)) continue; // 벽에는 불이 안 붙음
                    IgniteTile(pos, duration);
                }
            }
        }

        private void IgniteTile(Vector2Int pos, int duration)
        {
            // 이미 타는 중이면 더 긴 쪽으로 갱신
            fireTiles[pos] = fireTiles.TryGetValue(pos, out var existing)
                ? Mathf.Max(existing, duration)
                : duration;

            if (!fireVisuals.ContainsKey(pos))
                fireVisuals[pos] = CreateFireVisual(pos);
        }

        private void Tick()
        {
            if (fireTiles.Count == 0) return;

            // 1) 타일 위 액터에게 데미지
            ApplyDamageToActors();

            // 2) 지속시간 감소 + 꺼진 불 정리 (확산 없음 - 붙은 자리에서 타다가 꺼짐)
            var expired = new List<Vector2Int>();
            foreach (var key in fireTiles.Keys.ToList())
            {
                fireTiles[key]--;
                if (fireTiles[key] <= 0) expired.Add(key);
            }
            foreach (var pos in expired)
                Extinguish(pos);
        }

        private void ApplyDamageToActors()
        {
            foreach (var pos in fireTiles.Keys.ToList())
            {
                // 플레이어
                if (player != null && player.IsAlive && player.GridPosition == pos)
                    player.TakeDamage(fireDamagePerTurn);

                // 몬스터
                var monster = MonsterController.FindAt(pos);
                if (monster != null)
                    monster.TakeEnvironmentDamage(fireDamagePerTurn);
            }
        }

        private void Extinguish(Vector2Int pos)
        {
            fireTiles.Remove(pos);
            if (fireVisuals.TryGetValue(pos, out var go))
            {
                if (go != null) Destroy(go);
                fireVisuals.Remove(pos);
            }
        }

        private GameObject CreateFireVisual(Vector2Int pos)
        {
            var go = new GameObject("Fire");
            go.transform.SetParent(transform);
            go.transform.position = GridUtils.GridToWorld(pos);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteUtils.GetSharedWhiteSquare();
            sr.color = fireColor;
            sr.sortingOrder = 2; // 바닥/문보다 위, 액터보다는 아래
            return go;
        }

        private void ClearAll()
        {
            foreach (var go in fireVisuals.Values)
                if (go != null) Destroy(go);
            fireTiles.Clear();
            fireVisuals.Clear();
        }
    }
}
