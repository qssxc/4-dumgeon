using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonCore
{
    public enum TurnState
    {
        WaitingForPlayerInput,  // 플레이어 입력 대기 중 (루프는 여기서 멈춤)
        Processing,             // 액터들이 순서대로 행동 처리 중
    }

    /// <summary>
    /// 게임의 심장부. 모든 ITurnActor를 등록받아 에너지 기반으로 행동 순서를 관리한다.
    /// 씬에 하나만 존재하는 싱글턴으로 두되, 나중에 필요하면 DI로 바꿔도 됨.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        private const float ActionCost = 100f;

        private readonly List<ITurnActor> actors = new();
        public TurnState State { get; private set; } = TurnState.WaitingForPlayerInput;

        private void Awake()
        {
            Instance = this;
        }

        public void Register(ITurnActor actor)
        {
            if (!actors.Contains(actor))
                actors.Add(actor);
        }

        public void Unregister(ITurnActor actor)
        {
            actors.Remove(actor);
        }

        /// <summary>
        /// 플레이어가 입력(이동/공격 등)을 했을 때 외부(PlayerController)에서 호출.
        /// 이 호출을 기점으로 "한 턴"이 처리된다.
        /// </summary>
        public void RequestProcessTurn()
        {
            if (State != TurnState.WaitingForPlayerInput) return;
            StartCoroutine(ProcessTurnRoutine());
        }

        private IEnumerator ProcessTurnRoutine()
        {
            State = TurnState.Processing;

            // 살아있는 모든 액터에게 속도만큼 에너지 충전
            var living = actors.Where(a => a.IsAlive).ToList();
            foreach (var actor in living)
                actor.Energy += actor.Speed;

            // 에너지가 충분한 순서대로 행동 (에너지 높은 순 = 빠른 놈부터)
            var readyActors = living
                .Where(a => a.Energy >= ActionCost)
                .OrderByDescending(a => a.Energy)
                .ToList();

            foreach (var actor in readyActors)
            {
                if (!actor.IsAlive) continue; // 이전 행동으로 죽었을 수 있음

                actor.Energy -= ActionCost;
                yield return StartCoroutine(actor.TakeTurn(this));
            }

            State = TurnState.WaitingForPlayerInput;
        }
    }
}
