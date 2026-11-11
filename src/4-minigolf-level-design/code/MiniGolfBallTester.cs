using System;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniGolfGame
{
    public class MiniGolfBallTester : MonoBehaviour
    {
        public MiniGolfBall ball;
        // 이제 매니저가 시작 위치를 관리하므로 startPosition 변수는 지워도 됩니다.

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.R))
            {
                ball.ReturnToLasShotPosition();
            }
            
            // 매니저를 통해 1번(0)부터 5번(4) 스테이지로 텔레포트
            if (MiniGolfGameManager.Instance != null)
            {
                if (SimpleInput.GetKeyDown(Key.Digit1)) MiniGolfGameManager.Instance.GoToStage(0);
                if (SimpleInput.GetKeyDown(Key.Digit2)) MiniGolfGameManager.Instance.GoToStage(1);
                if (SimpleInput.GetKeyDown(Key.Digit3)) MiniGolfGameManager.Instance.GoToStage(2);
                if (SimpleInput.GetKeyDown(Key.Digit4)) MiniGolfGameManager.Instance.GoToStage(3);
                if (SimpleInput.GetKeyDown(Key.Digit5)) MiniGolfGameManager.Instance.GoToStage(4);
            }
        }
    }
}