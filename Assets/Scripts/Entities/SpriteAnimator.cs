using System.Collections;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 스프라이트 프레임 배열을 순서대로 재생하는 단순 재생기.
    /// Unity Animator 대신 코드로 제어하는 이유: 턴제라 "이동/공격 액션이 실행되는
    /// 그 순간에만" 애니메이션이 재생돼야 하는데, 그 타이밍 제어가 코루틴 기반인
    /// 우리 TurnManager 구조와 훨씬 잘 맞기 때문.
    ///
    /// 스프라이트 시트든 낱장이든 상관없이 Sprite[] 배열만 받으면 동작한다
    /// (시트는 Sprite Mode를 Multiple로 슬라이스하면 자동으로 여러 Sprite가 됨).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private float idleFps = 4f;

        private SpriteRenderer spriteRenderer;
        private Coroutine idleRoutine;
        private bool isPlayingOneShot;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>idle 프레임을 코드에서 설정 (CharacterClassData로부터 주입).</summary>
        public void SetIdleFrames(Sprite[] frames, float fps)
        {
            idleFrames = frames;
            idleFps = fps;
            StartIdle();
        }

        private void OnEnable()
        {
            StartIdle();
        }

        /// <summary>대기(idle) 애니메이션을 반복 재생. 프레임이 없으면 아무 것도 안 함.</summary>
        public void StartIdle()
        {
            if (idleFrames == null || idleFrames.Length == 0) return;
            if (idleRoutine != null) StopCoroutine(idleRoutine);
            idleRoutine = StartCoroutine(LoopFrames(idleFrames, idleFps));
        }

        private IEnumerator LoopFrames(Sprite[] frames, float fps)
        {
            float frameTime = 1f / Mathf.Max(1f, fps);
            int index = 0;
            while (true)
            {
                if (!isPlayingOneShot)
                {
                    spriteRenderer.sprite = frames[index];
                    index = (index + 1) % frames.Length;
                }
                yield return new WaitForSeconds(frameTime);
            }
        }

        /// <summary>
        /// 걷기/공격 같은 1회성 애니메이션을 duration 동안 재생한 뒤 idle로 복귀.
        /// 이동/공격 코루틴에서 yield로 기다릴 수 있도록 IEnumerator를 반환한다.
        /// 프레임이 없으면 그냥 duration만큼 대기 (스프라이트 없이도 로직이 안 깨지도록).
        /// </summary>
        public IEnumerator PlayOnce(Sprite[] frames, float duration)
        {
            if (frames == null || frames.Length == 0)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            isPlayingOneShot = true;
            float frameTime = duration / frames.Length;
            foreach (var frame in frames)
            {
                spriteRenderer.sprite = frame;
                yield return new WaitForSeconds(frameTime);
            }
            isPlayingOneShot = false;
        }
    }
}
