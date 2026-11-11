using UnityEngine;

namespace MiniGolfGame
{
    public class MiniGolfDirectionJumpPad : MonoBehaviour
    {
        [Header("앞으로 날아갈 속도 (발판의 화살표 방향)")]
        public float forwardSpeed = 5f;
        
        [Header("공중으로 솟구치는 속도 (무조건 위쪽)")]
        public float upwardSpeed = 7f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Rigidbody ballRb = other.GetComponent<Rigidbody>();
            
                if (ballRb != null)
                {
                    // 1. 발판이 바라보는 앞쪽 방향(Z축)과 하늘 방향(Y축)을 조합해 점프 궤적을 만듭니다.
                    Vector3 jumpVelocity = (transform.forward * forwardSpeed) + (Vector3.up * upwardSpeed);
                    
                    // 2. 공의 현재 속도를 지워버리고 방금 만든 점프 궤적으로 덮어씌웁니다.
                    ballRb.linearVelocity = jumpVelocity;
                    
                    Debug.Log("방향 강제 점프 패드 작동!");
                }
            }
        }
    }
}