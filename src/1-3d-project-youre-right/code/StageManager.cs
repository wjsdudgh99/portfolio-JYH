using System;
using System.Collections;
using UnityEngine;

namespace YoureRight
{
    // 한 스테이지의 진행 상황을 관리합니다.
    // 씬에 있는 AnswerPair를 모두 모아 찾은 개수를 세고, 전부 찾으면 클리어를 알립니다.
    // 스테이지 시작과 동시에 스톱워치를 돌리고, 클리어 순간에 멈춥니다.
    // 클리어하면 클리어 창(StageClearPanel)이 뜨고, 창의 Next 버튼이 ProceedToNext로 기록을 넘기며 다음 씬으로 이동합니다.
    // 씬에 클리어 창이 없으면 예전처럼 잠시 기다렸다가 자동으로 넘어갑니다.
    public class StageManager : MonoBehaviour
    {
        [SerializeField] private string stageName = "Tutorial";   // HUD에 그대로 표시됩니다 (예: Stage 2 - Snowy Night)
        [SerializeField] private float nextStageDelay = 1.5f;     // 클리어 창이 없는 씬에서 다음 씬으로 자동으로 넘어가기 전 대기 시간
        [SerializeField] private bool addToRecords = true;        // 끄면 시간은 HUD에 보여주기만 하고 결과 기록에는 넣지 않습니다 (튜토리얼)

        private AnswerPair[] pairs;
        private int foundCount;
        private readonly StageTimer timer = new StageTimer();
        private bool skipNextTick;
        private TutorialGuide tutorialGuide;
        private bool proceeded;

        public string StageName => stageName;
        public bool AddToRecords => addToRecords;
        public int FoundCount => foundCount;
        public int TotalCount => pairs.Length;
        public bool IsCleared { get; private set; }
        public float ElapsedTime => timer.Elapsed;

        // (찾은 개수, 전체 개수) - HUD 진행도 표시용
        public event Action<int, int> OnProgressChanged;
        public event Action OnStageCleared;

        private void Awake()
        {
            // 씬에 배치된 정답 쌍을 자동으로 모읍니다. 하나하나 연결할 필요가 없습니다.
            pairs = FindObjectsByType<AnswerPair>(FindObjectsSortMode.None);
            if (pairs.Length == 0)
            {
                Debug.LogWarning("[StageManager] 씬에 AnswerPair가 하나도 없습니다.", this);
            }
        }

        private void OnEnable()
        {
            foreach (AnswerPair pair in pairs) pair.OnFound += HandlePairFound;
        }

        private void OnDisable()
        {
            foreach (AnswerPair pair in pairs) pair.OnFound -= HandlePairFound;
        }

        private void Start()
        {
            // 구독자들이 Awake/OnEnable에서 이벤트를 연결한 뒤 초기 진행도(0 / N)를 알려줍니다.
            OnProgressChanged?.Invoke(foundCount, TotalCount);

            // 시작 안내(TutorialGuide)가 있는 씬은 안내를 닫는 순간부터 시간을 잽니다.
            // (안내 패널은 씬 시작 때 켜져 있으므로, 켜진 오브젝트만 찾는 FindAnyObjectByType으로 찾을 수 있습니다)
            tutorialGuide = FindAnyObjectByType<TutorialGuide>();
            if (tutorialGuide != null) tutorialGuide.OnClosed += StartTimer;
            else StartTimer();
        }

        private void OnDestroy()
        {
            if (tutorialGuide != null) tutorialGuide.OnClosed -= StartTimer;
        }

        private void StartTimer()
        {
            timer.Start();
            skipNextTick = true;
        }

        private void Update()
        {
            // 타이머를 켠 뒤 첫 번째 Update는 건너뜁니다.
            // 씬 로딩 직후 첫 프레임은 로딩 시간 때문에 deltaTime이 커서 기록이 부풀려지기 때문입니다.
            // (Start와 첫 Update가 같은 프레임이 아닐 수도 있어서 프레임 번호 대신 "첫 호출"로 판단합니다)
            if (skipNextTick)
            {
                skipNextTick = false;
                return;
            }

            timer.Tick(Time.deltaTime);
        }

        private void HandlePairFound(AnswerPair pair)
        {
            if (IsCleared) return;

            foundCount++;
            Debug.Log($"[StageManager] 정답! {pair.name} ({foundCount} / {TotalCount})");
            OnProgressChanged?.Invoke(foundCount, TotalCount);

            if (foundCount >= TotalCount)
            {
                IsCleared = true;
                timer.Stop();
                Debug.Log($"[StageManager] {stageName} 클리어! 기록: {TimeFormatter.Format(timer.Elapsed)}");
                OnStageCleared?.Invoke();

                // 클리어 창이 있으면 Next 버튼을 누를 때까지 기다립니다.
                if (FindAnyObjectByType<StageClearPanel>(FindObjectsInactive.Include) == null)
                {
                    Debug.LogWarning("[StageManager] 씬에 StageClearPanel이 없어 잠시 뒤 자동으로 넘어갑니다.", this);
                    StartCoroutine(AutoProceedRoutine());
                }
            }
        }

        // 기록을 GameManager에 넘기고 다음 씬으로 이동합니다. (클리어 창의 Next 버튼)
        // 기록은 클리어 순간에 이미 멈췄으므로, 클리어 창을 보는 시간은 기록에 포함되지 않습니다.
        public void ProceedToNext()
        {
            if (IsCleared == false || proceeded) return;   // 버튼 연타로 두 번 넘어가는 것을 막습니다
            proceeded = true;
            GameManager.Instance.ReportStageClear(stageName, timer.Elapsed, addToRecords);
        }

        private IEnumerator AutoProceedRoutine()
        {
            yield return new WaitForSeconds(nextStageDelay);
            ProceedToNext();
        }
    }
}
