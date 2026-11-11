using UnityEngine;

namespace MiniGolfGame
{
    public class MiniGolf360JumpPad : MonoBehaviour
    {
        [Header("위로 튕겨 올릴 힘")]
        public float jumpForce = 10f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Rigidbody ballRb = other.GetComponent<Rigidbody>();
            
                if (ballRb != null)
                {
                    // 공이 굴러오던 원래 속도와 방향(X, Z축)은 건드리지 않고,
                    // 발판의 위쪽(Y축, 초록색 화살표) 방향으로만 순간적인 가속도를 더해줍니다.
                    ballRb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
                    
                    Debug.Log("점프 패드 작동!");
                }
            }
        }
    }
}