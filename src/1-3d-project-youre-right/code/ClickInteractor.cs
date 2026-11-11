using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace YoureRight
{
    // 마우스 좌클릭 시 커서가 있는 화면의 카메라에서 Raycast를 발사해 정답/오답을 판정합니다.
    // 맞은 Collider에 AnswerTarget이 있으면 정답, 다른 물체면 오답입니다.
    // 빈 곳(아무 Collider도 맞지 않음)을 클릭하면 아무 반응도 하지 않습니다.
    // 무반응 레이어(기본: Background)의 물체(플랫폼, 바닥, 길 등)는 클릭을 막아 주기만 하고 반응하지 않습니다.
    // 씬에 PlayArea가 있으면, 클릭 지점이 어느 PlayArea에도 없을 때(섬 바깥 장식 등) 반응하지 않습니다.
    // 오답을 클릭하면 잠시(기본 3초) 좌클릭을 받지 않습니다. 카메라 조작은 그대로 됩니다.
    public class ClickInteractor : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private Camera[] targetCameras;           // 분할 화면의 카메라들 (비워 두면 Main Camera 하나만 사용)

        [Header("Raycast")]
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private LayerMask clickableLayers = ~0;   // 여기서 뺀 레이어는 클릭이 그대로 뚫고 지나갑니다 (보통은 Everything)

        // Raycast에는 맞지만(뒤쪽 물체가 대신 맞지 않도록 막아 줌) 정답도 오답도 아닌 레이어 입니다.
        // 비워 두면(Nothing) "Background" 레이어를 자동으로 사용합니다.
        [SerializeField] private LayerMask silentLayers;

        [Header("Wrong Click Lock")]
        // 오답 후 클릭을 막는 시간 입니다. 스톱워치는 계속 돌기 때문에 사실상 벌점이 됩니다. (0이면 사용 안 함)
        [SerializeField] private float wrongClickLockTime = 3f;

        private const string DefaultSilentLayerName = "Background";

        public float LockDuration => wrongClickLockTime;
        public float LockRemaining { get; private set; }
        public bool IsLocked => LockRemaining > 0f;

        // 정답 쌍을 새로 찾았을 때 (클릭한 오브젝트)
        public event Action<AnswerTarget> OnAnswerClicked;

        // 오답일 때 (커서 화면 좌표, 맞은 Collider)
        public event Action<Vector2, Collider> OnWrongClicked;

        private StageManager stageManager;
        private PlayArea[] playAreas;

        private void Awake()
        {
            if (targetCameras == null || targetCameras.Length == 0) targetCameras = new[] { Camera.main };
            stageManager = FindAnyObjectByType<StageManager>();
            playAreas = FindObjectsByType<PlayArea>(FindObjectsSortMode.None);

            // 씬마다 따로 설정하지 않아도 되도록, 비어 있으면 Background 레이어를 씁니다.
            // (레이어가 아직 없으면 GetMask가 0을 돌려주므로 지금처럼 모든 물체가 반응합니다)
            if (silentLayers.value == 0) silentLayers = LayerMask.GetMask(DefaultSilentLayerName);
        }

        private void Update()
        {
            // 제한 시간은 Time.deltaTime으로 줄이므로 일시정지 중에는 같이 멈춥니다. (일시정지로 기다리는 꼼수 방지)
            if (LockRemaining > 0f) LockRemaining = Mathf.Max(0f, LockRemaining - Time.deltaTime);

            // 일시정지 중이거나, 스테이지를 클리어했거나, UI 위를 클릭한 경우에는 판정하지 않습니다.
            if (Time.timeScale == 0f) return;
            if (stageManager != null && stageManager.IsCleared) return;
            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left) == false) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // 오답 제한 중에는 정답·오답 모두 받지 않습니다.
            if (IsLocked) return;

            Vector2 screenPos = SimpleInput.GetMousePosition();
            Camera cam = FindCameraAt(screenPos);
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);

            // 빈 곳을 클릭했으면 정답도 오답도 아니므로 아무것도 하지 않습니다.
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickableLayers, QueryTriggerInteraction.Ignore) == false) return;

            HandleHit(hit, screenPos);
        }

        // 커서가 어느 카메라의 화면 영역(Viewport Rect) 안에 있는지 찾습니다.
        private Camera FindCameraAt(Vector2 screenPos)
        {
            foreach (Camera cam in targetCameras)
            {
                if (cam != null && cam.pixelRect.Contains(screenPos)) return cam;
            }
            return null;
        }

        private void HandleHit(RaycastHit hit, Vector2 screenPos)
        {
            // 섬 바깥(어느 PlayArea에도 없는 지점)을 클릭했으면 빈 곳처럼 아무것도 하지 않습니다.
            if (IsInsidePlayArea(hit.point) == false) return;

            // 무반응 레이어(플랫폼, 바닥 등)를 클릭했으면 빈 곳처럼 아무것도 하지 않습니다.
            // 정답 오브젝트라도 이 레이어에 있으면 반응하지 않으므로, 정답은 Background에 두지 않습니다.
            if ((silentLayers.value & (1 << hit.collider.gameObject.layer)) != 0) return;

            // 자식 Collider를 클릭해도 부모의 AnswerTarget을 찾을 수 있게 합니다.
            AnswerTarget target = hit.collider.GetComponentInParent<AnswerTarget>();
            if (target == null || target.Pair == null)
            {
                HandleWrong(screenPos, hit.collider);
                return;
            }

            // 이미 찾은 쌍을 다시 누른 경우는 정답도 오답도 아니므로 무시합니다.
            if (target.Pair.TryFind() == false) return;

            OnAnswerClicked?.Invoke(target);
        }

        // 씬에 PlayArea가 하나도 없으면 모든 곳을 구역 안으로 봅니다. (PlayArea를 아직 두지 않은 씬도 예전처럼 동작)
        private bool IsInsidePlayArea(Vector3 worldPoint)
        {
            if (playAreas.Length == 0) return true;

            foreach (PlayArea area in playAreas)
            {
                if (area != null && area.Contains(worldPoint)) return true;
            }
            return false;
        }

        private void HandleWrong(Vector2 screenPos, Collider hitCollider)
        {
            Debug.Log($"[ClickInteractor] 오답: {hitCollider.name}");
            LockRemaining = wrongClickLockTime;
            OnWrongClicked?.Invoke(screenPos, hitCollider);
        }
    }
}
