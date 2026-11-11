using UnityEngine;

namespace MiniGolfGame
{
    public class MiniGolfDirectionBoostPad : MonoBehaviour
    {
        [Header("부스터 속도 (진입한 방향으로 이 속도만큼 밀어줌)")]
        public float boostSpeed = 20f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Rigidbody ballRb = other.GetComponent<Rigidbody>();
            
                if (ballRb != null)
                {
                    // 1. 공이 현재 굴러가고 있는 이동 방향(벡터)을 추출합니다.
                    // .normalized는 속도 크기를 빼고 '순수 방향'만 1의 길이로 뽑아내는 명령어입니다.
                    Vector3 currentDirection = ballRb.linearVelocity.normalized;
                    
                    // (안전장치) 공이 완전히 멈춰있는 상태에서 발판에 닿았다면 방향이 0이 되므로,
                    // 이 경우에만 예외적으로 발판이 바라보는 앞쪽으로 날려보냅니다.
                    if (currentDirection == Vector3.zero)
                    {
                        currentDirection = transform.forward;
                    }

                    // 2. 공이 가던 방향 그대로, 내가 설정한 부스터 속도로 강제 이동시킵니다.
                    ballRb.linearVelocity = currentDirection * boostSpeed;
                    
                    Debug.Log("진입 방향 부스터 작동!");
                }
            }
        }
    }
}