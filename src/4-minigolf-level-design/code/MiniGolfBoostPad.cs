using UnityEngine;

namespace MiniGolfGame
{
    public class MiniGolfBoostPad : MonoBehaviour
    {
        [Header("강제 이동 속도 (이 속도와 방향으로만 튀어나감)")]
        public float boostSpeed = 20f;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Rigidbody ballRb = other.GetComponent<Rigidbody>();
                    
                if (ballRb != null)
                {
                    // 기존에 공이 가지고 있던 속도와 방향을 완전히 무시하고,
                    // 무조건 발판의 앞쪽(파란색 화살표 방향)으로 지정한 속도만큼 날려보냅니다.
                    ballRb.linearVelocity = transform.forward * boostSpeed;
                            
                    Debug.Log("방향 강제 부스터 작동!");
                }
            }
        }
    }
}