using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace MiniGolfGame
{
    public class MiniGolfGameManager : MonoBehaviour
    {
        // 싱글톤: 다른 스크립트에서 매니저를 쉽게 부를 수 있도록 합니다.
        public static MiniGolfGameManager Instance;

        [Header("스테이지 설정 (인스펙터에서 1~5스테이지 시작점 5개를 넣어주세요)")]
        public Transform[] stageStartPositions; 
        public MiniGolfBall ball;
        
        [Header("UI 설정")]
        public TextMeshProUGUI clearMessageText;

        private int currentStage = 0; // 0이 스테이지 1, 4가 스테이지 5를 의미합니다.
        private bool isStageCleared = false;

        public bool IsStageCleared => isStageCleared; // 밖에서 클리어 여부를 확인할 수 있게 열어줍니다.

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            GoToStage(0); // 게임 시작 시 1스테이지로 보냅니다.
        }

        private void Update()
        {
            // 클리어된 상태에서 스페이스바를 누르면 다음 스테이지로 이동
            if (isStageCleared && SimpleInput.GetKeyDown(Key.T))
            {
                NextStage();
            }
        }

        // 골프 홀(목표)에 들어가면 호출될 함수
        public void StageCleared()
        {
            isStageCleared = true;
            
            // 클리어 시 화면에 텍스트를 켭니다.
            if (clearMessageText != null)
            {
                clearMessageText.gameObject.SetActive(true);
                // 텍스트 내용은 여기서 원하는 대로 수정하시면 됩니다. \n은 줄바꿈입니다.
                clearMessageText.text = $"Press 'T' to Next Stage"; 
            }
            
            Debug.Log($"스테이지 {currentStage + 1} 클리어! T키를 누르면 다음으로 갑니다.");
        }

        private void NextStage()
        {
            currentStage++;
            
            // 5스테이지(인덱스 4)를 클리어하고 넘어왔다면 다시 1스테이지(인덱스 0)로 순환
            if (currentStage >= 5) 
            {
                currentStage = 0;
            }

            GoToStage(currentStage);
        }

        // 특정 스테이지로 강제 이동시키는 함수
        public void GoToStage(int stageIndex)
        {
            if (stageIndex >= 0 && stageIndex < stageStartPositions.Length)
            {
                currentStage = stageIndex;
                isStageCleared = false; // 클리어 상태 초기화
                
                // 새 스테이지를 시작할 때 화면의 클리어 문구를 다시 숨깁니다.
                if (clearMessageText != null)
                {
                    clearMessageText.gameObject.SetActive(false);
                }
                
                ball.PlaceAt(stageStartPositions[stageIndex].position);
            }
        }
    }
}