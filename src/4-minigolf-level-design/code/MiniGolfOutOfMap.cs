using UnityEngine;

namespace MiniGolfGame
{
    public class MiniGolfOutOfMap : MonoBehaviour
    {
        [Header("낙하 판정 높이 (이 Y값 아래로 떨어지면 아웃)")]
        public float fallThreshold = -10f;

        private MiniGolfBall ball;
        private Rigidbody rb;

        private void Awake()
        {
            // 이 스크립트는 공(Player)에 붙을 것이므로, 공의 컴포넌트들을 가져옵니다.
            ball = GetComponent<MiniGolfBall>();
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            // 공의 현재 높이(Y)가 설정한 한계선보다 아래로 내려갔는지 확인합니다.
            if (transform.position.y < fallThreshold)
            {
                if (ball != null)
                {
                    // 1. 공을 치기 직전 위치로 되돌립니다.
                    ball.ReturnToLasShotPosition(); 
                    
                    // 2. 중요: 추락하면서 쌓인 가속도를 0으로 초기화해서, 소환되자마자 튀어 나가지 않게 만듭니다.
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    Debug.Log("맵 밖으로 추락! 직전 위치로 복귀합니다.");
                }
            }
        }
    }
}