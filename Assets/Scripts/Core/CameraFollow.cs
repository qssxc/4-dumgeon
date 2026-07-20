using UnityEngine;

namespace DungeonCore
{
    /// <summary>
    /// 매 프레임 target(플레이어) 위치로 카메라를 이동시킴.
    /// 즉시 이동이 아니라 Lerp로 살짝 지연시켜서 뚝뚝 끊기지 않게 함.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private float zOffset = -10f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = new Vector3(target.position.x, target.position.y, zOffset);
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        }
    }
}
