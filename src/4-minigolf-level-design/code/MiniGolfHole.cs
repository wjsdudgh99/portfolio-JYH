using UnityEngine;

namespace MiniGolfGame
{
    public class MiniGolfHole : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // 이미 클리어된 상태가 아니라면 매니저에게 클리어 신호를 보냅니다.
                if (MiniGolfGameManager.Instance != null && !MiniGolfGameManager.Instance.IsStageCleared)
                {
                    MiniGolfGameManager.Instance.StageCleared();
                }
            }
        }
    }
}