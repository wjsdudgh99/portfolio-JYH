using UnityEngine;

namespace MiniGolfGame
{
    public class MiniGolfMovingObstacle : MonoBehaviour
    {
        [Header("이동 방향 및 거리 설정")]
        public Vector3 moveDirection = new Vector3(1, 0, 0);
        public float distance = 5f; // 왕복할 거리
        public float speed = 3f;    // 이동 속도

        [Header("대기 설정 (체크하면 끝에서 멈춤)")]
        public bool enablePause = false; // 끄고 킬 수 있는 스위치
        public float pauseTime = 1.5f;   // 대기 시간 (초)

        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 targetPosition;  // 현재 이동해야 할 목표 지점
        
        private bool isWaiting = false;  // 현재 멈춰있는 상태인지 확인
        private float waitTimer = 0f;    // 시간을 재는 타이머

        private void Start()
        {
            startPosition = transform.position;
            moveDirection = moveDirection.normalized;
            
            // 시작점(startPosition)과 도착점(endPosition)을 미리 계산해 둡니다.
            endPosition = startPosition + (moveDirection * distance);
            
            // 처음에는 도착점을 향해 출발합니다.
            targetPosition = endPosition;
        }

        private void Update()
        {
            // 1. 만약 대기 상태라면
            if (isWaiting)
            {
                waitTimer += Time.deltaTime; // 타이머에 시간을 더합니다.

                // 설정한 대기 시간이 다 지났다면
                if (waitTimer >= pauseTime)
                {
                    isWaiting = false; // 대기 상태를 풀고
                    waitTimer = 0f; // 타이머를 초기화합니다.

                    // 목표 지점을 반대로 뒤집습니다. (도착점이었다면 시작점으로, 시작점이었다면 도착점으로)
                    targetPosition = (targetPosition == endPosition) ? startPosition : endPosition;
                }

                return; // 대기 중일 때는 아래 이동 코드를 무시하고 여기서 Update를 끝냅니다.
            }

            // 2. 대기 상태가 아닐 때: 목표 지점을 향해 일정한 속도로 이동합니다.
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            // 3. 목표 지점에 거의 다 도착했다면
            if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
            {
                // 인스펙터에서 대기 기능을 켜뒀(true)다면 멈춤 상태로 들어갑니다.
                if (enablePause)
                {
                    isWaiting = true;
                }
                else
                {
                    // 대기 기능이 꺼져있(false)다면 멈추지 않고 즉시 목표 지점을 반대로 뒤집어 돌아갑니다.
                    targetPosition = (targetPosition == endPosition) ? startPosition : endPosition;
                }
            }
        }
    }
}