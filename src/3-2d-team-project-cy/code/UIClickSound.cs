using CY.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CY.UI
{
    public class UIClickSound : MonoBehaviour
    {
        private void OnEnable()
        {
            // 씬이 넘어갈 때마다 버튼들을 새로 찾아서 연결해 줍니다.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterAllButtons();
        }

        private void Start()
        {
            RegisterAllButtons();
        }

        public void RegisterAllButtons()
        {
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Button btn in allButtons)
            {
                // 중복 등록 방지를 위해 기존 리스너 제거 후 추가
                btn.onClick.RemoveListener(OnAnyButtonClicked);
                btn.onClick.AddListener(OnAnyButtonClicked);
            }
        }

        private void OnAnyButtonClicked()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayUIClick();
            }
        }
    }
}