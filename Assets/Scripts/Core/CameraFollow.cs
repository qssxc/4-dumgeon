using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 매 프레임 target(플레이어) 위치로 카메라를 이동시킴.
    /// 즉시 이동이 아니라 Lerp로 살짝 지연시켜서 뚝뚝 끊기지 않게 함.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        /// <summary>피격 등에서 어디서든 흔들 수 있도록 하는 접근점.</summary>
        public static CameraFollow Instance { get; private set; }

        public Transform target;
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private float zOffset = -10f;

        private float shakeDuration;
        private float shakeTimeLeft;
        private float shakeMagnitude;

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>카메라를 잠깐 흔든다. 플레이어가 피해를 입을 때 등에 호출.</summary>
        public void Shake(float duration = 0.18f, float magnitude = 0.18f)
        {
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeTimeLeft = shakeDuration;
            shakeMagnitude = magnitude;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = new Vector3(target.position.x, target.position.y, zOffset);
            Vector3 pos = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);

            if (shakeTimeLeft > 0f)
            {
                shakeTimeLeft -= Time.deltaTime;
                float falloff = Mathf.Clamp01(shakeTimeLeft / shakeDuration); // 점점 약해지게
                Vector2 off = Random.insideUnitCircle * (shakeMagnitude * falloff);
                pos.x += off.x;
                pos.y += off.y;
            }

            transform.position = pos;
        }
    }
}
