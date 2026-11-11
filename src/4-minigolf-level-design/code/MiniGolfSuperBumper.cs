using UnityEngine;

namespace MiniGolfGame
{
    public class FixedSpeedBumper : MonoBehaviour
    {
        [Header("고정 반사 속도 (어떤 속도로 부딪혀도 이 속도로 튕김)")]
        public float fixedSpeed = 15f; 

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            
                if (ballRb != null)
                {
                    // 1. 유니티 물리 엔진이 부딪힌 후 튕겨 나갈 방향을 계산해 둔 것을 가져옵니다.
                    Vector3 bounceDirection = ballRb.linearVelocity.normalized;
                    
                    // (안전장치) 공이 멈출 정도로 너무 느리게 닿아서 방향이 0이 되었다면, 벽이 밀어내는 수직 방향을 사용합니다.
                    if (bounceDirection == Vector3.zero)
                    {
                        bounceDirection = collision.contacts[0].normal;
                    }

                    // 2. 이전 속도는 완전히 무시하고, [방향 × 내가 정한 고정 속도]로 물리 엔진의 속도를 강제로 조작합니다.
                    ballRb.linearVelocity = bounceDirection * fixedSpeed;
                }
            }
        }
    }
}