using CY.Vehicle;
using UnityEngine;

namespace CY.Audio
{
    public class InGameAudioTrigger : MonoBehaviour
    {
        private void Start()
        {
            // 인게임 씬이 시작되면 로비 BGM을 꺼주고 인게임 BGM 4종 중 하나를 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopBgm();
                SoundManager.Instance.PlayInGameBgm();
            }
        }
    }
}