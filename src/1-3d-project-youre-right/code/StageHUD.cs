using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YoureRight
{
    // 플레이 화면 상단 HUD 입니다.
    // 좌측: 스테이지 이름 / 중앙: 스톱워치 (00:00.00) / 우측: 정답 진행도
    // 진행도는 점(Progress Dots)으로 보여 줍니다. 정답 쌍 수만큼 점을 만들고, 찾을 때마다 금색으로 채우며 살짝 튀게 합니다.
    // Progress Dots를 비워 두고 Progress Text만 연결하면 예전처럼 ( 2 / 5 ) 글자로 보여 줍니다.
    // StageManager의 값을 읽어 표시만 하고, 게임 진행에는 관여하지 않습니다.
    public class StageHUD : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text progressText;     // 선택. 점과 같이 써도 됩니다

        [Header("Timer")]
        // 숫자마다 글자 폭이 달라 시간이 흐를 때 글자가 좌우로 흔들리는 것을 막습니다. (0이면 사용 안 함)
        [SerializeField] private float timerMonospaceEm = 0.6f;

        [Header("Progress Dots")]
        [SerializeField] private RectTransform progressDots;   // 점을 담는 부모 (Horizontal Layout Group)
        [SerializeField] private Image dotTemplate;            // 복제할 점 하나 (꺼 둠)
        [SerializeField] private Sprite foundSprite;           // 찾은 점 (채운 원)
        [SerializeField] private Sprite emptySprite;           // 남은 점 (원 테두리)
        [SerializeField] private Color foundColor = new Color(0.910f, 0.745f, 0.353f);         // E8BE5A
        [SerializeField] private Color emptyColor = new Color(0.953f, 0.918f, 0.843f, 0.6f);   // F3EAD7
        [SerializeField] private float popScale = 1.5f;        // 찾은 순간 커지는 배율
        [SerializeField] private float popTime = 0.3f;

        [Header("Move Lock")]
        // Space로 이동을 잠그면 켜지는 표시 (선택). 잠금 중에 WASD·Q/E를 누르면 살짝 튑니다.
        [SerializeField] private RectTransform moveLockBadge;
        [SerializeField] private float badgePopScale = 1.15f;
        // 이동을 모두 꺼 둔 스테이지(3-1)에서는 알약을 처음부터 띄우고 글자·색을 이것으로 바꿉니다. (Space로 풀 수 없으므로 SPACE를 빼고 빨간색)
        [SerializeField] private string moveDisabledText = "MOVE DISABLED";
        [SerializeField] private Color moveDisabledColor = new Color(0.902f, 0.361f, 0.361f);   // E65C5C

        private readonly List<Image> dots = new List<Image>();
        private int shownFound;
        private OrbitCameraController orbitCamera;
        private Coroutine badgePop;

        private void Awake()
        {
            if (stageManager == null) stageManager = FindAnyObjectByType<StageManager>();
            if (dotTemplate != null) dotTemplate.gameObject.SetActive(false);
            orbitCamera = FindAnyObjectByType<OrbitCameraController>();
            if (moveLockBadge != null) moveLockBadge.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            stageManager.OnProgressChanged += HandleProgressChanged;
            if (orbitCamera != null)
            {
                orbitCamera.OnMoveLockChanged += HandleMoveLockChanged;
                orbitCamera.OnMoveLockedInput += HandleMoveLockedInput;
                orbitCamera.OnLockedMoveInput += HandleMoveLockedInput;
            }
        }

        private void OnDisable()
        {
            stageManager.OnProgressChanged -= HandleProgressChanged;
            if (orbitCamera != null)
            {
                orbitCamera.OnMoveLockChanged -= HandleMoveLockChanged;
                orbitCamera.OnMoveLockedInput -= HandleMoveLockedInput;
                orbitCamera.OnLockedMoveInput -= HandleMoveLockedInput;
            }
        }

        private void HandleMoveLockChanged(bool locked)
        {
            if (moveLockBadge == null) return;
            moveLockBadge.gameObject.SetActive(locked);
            if (locked) PopBadge();
        }

        // 누르고 있는 동안 매 프레임 오므로, 튀는 중이면 끝날 때까지 기다렸다가 다시 튑니다. (꾹 누르면 두근거리듯 반복)
        private void HandleMoveLockedInput()
        {
            if (moveLockBadge != null && badgePop == null) PopBadge();
        }

        private void PopBadge()
        {
            if (badgePop != null) StopCoroutine(badgePop);
            badgePop = StartCoroutine(Pop(moveLockBadge, badgePopScale, () => badgePop = null));
        }

        private void Start()
        {
            stageNameText.text = stageManager.StageName;

            if (moveLockBadge != null && orbitCamera != null && orbitCamera.IsMoveDisabled)
            {
                TMP_Text badgeText = moveLockBadge.GetComponentInChildren<TMP_Text>(true);
                if (badgeText != null)
                {
                    badgeText.text = moveDisabledText;
                    badgeText.color = moveDisabledColor;
                }
                moveLockBadge.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            string time = TimeFormatter.Format(stageManager.ElapsedTime);
            timerText.text = timerMonospaceEm > 0f ? $"<mspace={timerMonospaceEm}em>{time}</mspace>" : time;
        }

        private void HandleProgressChanged(int found, int total)
        {
            if (progressText != null) progressText.text = $"( {found} / {total} )";
            if (progressDots == null || dotTemplate == null) return;

            EnsureDots(total);
            for (int i = 0; i < dots.Count; i++)
            {
                bool isFound = i < found;
                dots[i].sprite = isFound ? foundSprite : emptySprite;
                dots[i].color = isFound ? foundColor : emptyColor;
            }

            // 새로 찾은 점만 튀게 합니다. (처음 0 / N 알림이나 같은 값이 다시 와도 튀지 않음)
            for (int i = shownFound; i < found && i < dots.Count; i++) StartCoroutine(Pop(dots[i].rectTransform, popScale));
            shownFound = found;
        }

        // 정답 쌍 수만큼 점을 맞춥니다. 스테이지마다 3~5쌍이라 씬에 미리 만들어 두지 않습니다.
        private void EnsureDots(int total)
        {
            while (dots.Count < total)
            {
                Image dot = Instantiate(dotTemplate, progressDots);
                dot.name = $"Dot {dots.Count + 1}";
                dot.gameObject.SetActive(true);
                dots.Add(dot);
            }
            for (int i = 0; i < dots.Count; i++) dots[i].gameObject.SetActive(i < total);
        }

        private IEnumerator Pop(RectTransform target, float fromScale, System.Action onDone = null)
        {
            // 일시정지와 상관없이 끝까지 재생되도록 unscaledDeltaTime을 씁니다.
            for (float t = 0f; t < popTime; t += Time.unscaledDeltaTime)
            {
                float k = t / popTime;
                float scale = Mathf.Lerp(fromScale, 1f, 1f - (1f - k) * (1f - k));   // EaseOut
                target.localScale = Vector3.one * scale;
                yield return null;
            }
            target.localScale = Vector3.one;
            onDone?.Invoke();
        }
    }
}
