using System.Collections;
using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 피격 연출. 스프라이트를 잠깐 flashColor로 틴트했다가 원래 색으로 되돌리고,
    /// 살짝 커졌다 돌아오는 스케일 펀치를 준다. 어떤 스프라이트에도 붙일 수 있는 범용 컴포넌트.
    ///
    /// 액터(플레이어/몬스터)의 SpriteRenderer와 같은 오브젝트에 붙여서 Flash()를 호출.
    /// flipX(좌우 반전)는 SpriteRenderer 쪽이라 localScale을 건드려도 영향 없음.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new Color(1f, 0.4f, 0.4f); // 붉은 틴트(아트 스프라이트에서 잘 보임)
        [SerializeField] private float duration = 0.14f;
        [SerializeField] private float punchScale = 0.15f;

        private SpriteRenderer sr;
        private Color baseColor;
        private Vector3 baseScale;
        private Coroutine running;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseColor = sr != null ? sr.color : Color.white;
            baseScale = transform.localScale;
        }

        public void Flash()
        {
            if (sr == null) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            Vector3 punched = baseScale * (1f + punchScale);
            sr.color = flashColor;
            transform.localScale = punched;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                sr.color = Color.Lerp(flashColor, baseColor, k);
                transform.localScale = Vector3.Lerp(punched, baseScale, k);
                yield return null;
            }

            sr.color = baseColor;
            transform.localScale = baseScale;
            running = null;
        }
    }
}
