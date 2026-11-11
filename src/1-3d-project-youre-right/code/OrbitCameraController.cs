using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YoureRight
{
    // 분할 화면의 카메라들을 각자의 디오라마 중앙(pivot) 기준으로 똑같이 공전시키는 오빗 카메라 입니다.
    // 회전: 마우스 우클릭 드래그
    // 이동: WASD 키 (바라보는 방향 기준 앞뒤좌우, 바닥과 평행하게. 섬(PlayArea) 범위 안에서만)
    //       Q 하강 / E 상승 (PlayArea 높이 범위 안에서만)
    //       Horizontal Move / Vertical Move로 스테이지마다 끌 수 있고, 꺼 둔 키를 누르면 OnLockedMoveInput을 보냅니다.
    //       Space를 누를 때마다 WASD·Q/E 이동을 잠그고 풉니다(지금 위치에 시점 고정). 회전·줌은 그대로 됩니다.
    // 줌: 마우스 휠
    // 입력은 목표값(target)에 쌓고, 실제 카메라는 목표값을 부드럽게 따라갑니다.
    // 모든 카메라가 같은 각도·거리를 쓰므로 두 화면이 항상 같은 시점으로 보입니다.
    // Block Layers를 지정하면 카메라가 그 레이어의 Collider(예: 열차 몸통 상자) 안으로 들어가지 않습니다.
    public class OrbitCameraController : MonoBehaviour
    {
        // 카메라 한 대와 그 카메라가 바라볼 중심점 한 쌍
        [System.Serializable]
        private class OrbitView
        {
            public Transform camera;
            public Transform pivot;
        }

        [Header("Ref")]
        [SerializeField] private OrbitView[] views;              // 0번 카메라의 배치 위치가 시작 시점이 됩니다

        [Header("Rotate")]
        [SerializeField] private float degreesPerPixel = 0.2f;   // 드래그 1픽셀당 회전 각도
        [SerializeField] private float pitchMin = -80f;          // 음수면 디오라마 아래쪽에서도 볼 수 있습니다
        [SerializeField] private float pitchMax = 80f;           // ±90도에 가까우면 화면이 뒤집히니 80도 정도로 제한합니다

        [Header("Move")]
        [SerializeField] private float moveSpeed = 10f;          // WASD 입력시 초당 이동 거리
        [SerializeField] private float moveLimit = 10f;          // 씬에 PlayArea가 없을 때 섬 중심에서 움직일 수 있는 거리
        [SerializeField] private bool horizontalMove = true;     // 끄면 WASD 이동을 쓰지 않습니다
        [SerializeField] private bool verticalMove = true;       // 끄면 Q/E 상하 이동을 쓰지 않습니다
        [SerializeField] private Key downKey = Key.Q;
        [SerializeField] private Key upKey = Key.E;
        [SerializeField] private Key moveLockKey = Key.Space;    // 누를 때마다 이동 잠금을 켜고 끕니다 (None이면 쓰지 않음)

        [Header("Zoom")]
        [SerializeField] private float zoomStep = 1f;            // 휠 한 칸당 거리 변화량
        [SerializeField] private float distanceMin = 5f;
        [SerializeField] private float distanceMax = 25f;

        [Header("Collision (비워 두면 끔)")]
        [SerializeField] private LayerMask blockLayers;          // 카메라가 뚫고 들어가지 못할 레이어 (예: CameraBlock)
        [SerializeField] private float collisionRadius = 0.3f;   // 카메라를 감싸는 공의 반지름. Near Clip(0.3)보다 작으면 벽이 잘려 보입니다

        [Header("Smoothing")]
        [SerializeField] private float sharpness = 10f;          // 클수록 목표값을 빨리 따라갑니다

        // 입력이 쌓이는 목표값
        private float targetYaw;
        private float targetPitch;
        private float targetDistance;
        private Vector3 targetMove;

        // 실제 카메라에 적용되는 현재값
        private float yaw;
        private float pitch;
        private float distance;
        private Vector3 move;            // 모든 Pivot에 똑같이 더하는 이동량 (Pivot 자체는 옮기지 않음)

        // 이동 범위 (0번 Pivot 기준 상대 좌표)
        private Vector3 moveMin;
        private Vector3 moveMax;

        // 꺼 둔 이동 키(WASD 또는 Q/E)를 누르고 있는 동안 매 프레임 보냅니다. (StageHUD의 이동 잠금 알약이 튐)
        public event System.Action OnLockedMoveInput;

        // 스테이지 설정으로 WASD·Q/E 이동을 모두 꺼 둔 스테이지(3-1)입니다. StageHUD가 알약을 처음부터 띄웁니다.
        public bool IsMoveDisabled => horizontalMove == false && verticalMove == false;

        // 플레이어가 Space로 켜는 이동 잠금입니다. 스테이지 설정(Horizontal / Vertical Move)과는 따로이고, 씬마다 꺼진 채 시작합니다.
        public bool IsMoveLocked { get; private set; }
        public event System.Action<bool> OnMoveLockChanged;      // 잠금을 켜고 끌 때 (StageHUD가 표시를 켜고 끔)
        public event System.Action OnMoveLockedInput;            // 잠금 중에 WASD·Q/E를 누르고 있는 동안 매 프레임

        // 이 스테이지에서 쓸 수 있는 이동이 하나라도 있어야 잠금이 의미가 있습니다. (3-1처럼 둘 다 꺼 두면 Space도 무시)
        public bool CanLockMove => moveLockKey != Key.None && IsMoveDisabled == false;

        private void Awake()
        {
            if (views == null || views.Length == 0 || views[0].camera == null || views[0].pivot == null)
            {
                Debug.LogError("[OrbitCameraController] Views에 카메라와 Pivot을 지정해 주세요.", this);
                enabled = false;
                return;
            }

            // 0번 카메라를 씬에 배치해 둔 위치에서 시작하도록 현재 각도와 거리를 역으로 계산합니다.
            Vector3 offset = views[0].camera.position - views[0].pivot.position;
            distance = Mathf.Clamp(offset.magnitude, distanceMin, distanceMax);
            pitch = Mathf.Clamp(Mathf.Asin(offset.y / offset.magnitude) * Mathf.Rad2Deg, pitchMin, pitchMax);
            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;

            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;

            SetupMoveBounds();

            // 첫 프레임부터 모든 카메라가 같은 시점을 갖도록 바로 맞춥니다.
            ApplyTransform();
        }

        private void Update()
        {
            // 일시정지(timeScale = 0) 중에는 카메라를 조작하지 않습니다.
            if (Time.timeScale == 0f) return;

            UpdateRotateInput();
            UpdateMoveLockInput();
            UpdateMoveInput();
            UpdateZoomInput();
        }

        private void LateUpdate()
        {
            // 프레임 속도와 상관없이 같은 느낌으로 따라가도록 지수 감쇠 비율을 사용합니다.
            float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            yaw = Mathf.LerpAngle(yaw, targetYaw, t);
            pitch = Mathf.Lerp(pitch, targetPitch, t);
            distance = Mathf.Lerp(distance, targetDistance, t);
            move = Vector3.Lerp(move, targetMove, t);

            ApplyTransform();
        }

        // 0번 Pivot이 들어 있는 PlayArea 크기만큼 움직일 수 있게 합니다. 없으면 Move Limit 반경의 사각형입니다.
        private void SetupMoveBounds()
        {
            Vector3 pivot = views[0].pivot.position;
            PlayArea area = FindObjectsByType<PlayArea>(FindObjectsSortMode.None)
                .OrderBy(a => Vector3.Distance(a.transform.position, pivot))
                .FirstOrDefault();

            if (area == null)
            {
                moveMin = new Vector3(-moveLimit, -moveLimit, -moveLimit);
                moveMax = new Vector3(moveLimit, moveLimit, moveLimit);
                return;
            }

            // PlayArea는 보통 회전하지 않으므로 축에 맞춘 상자로 계산합니다.
            Vector3 center = area.transform.TransformPoint(area.Center) - pivot;
            Vector3 half = Vector3.Scale(area.Size, area.transform.lossyScale) * 0.5f;
            moveMin = center - half;
            moveMax = center + half;
        }

        private void UpdateMoveInput()
        {
            // 바라보는 방향(Yaw)만 따서 바닥과 평행하게 움직입니다. W/S 앞뒤, A/D 좌우
            Vector2 axis = Vector2.ClampMagnitude(SimpleInput.GetMoveAxisRaw(), 1f);
            float vertical = GetVerticalAxis();

            // 꺼 둔 키를 눌렀으면 알리고, 그 입력은 버립니다.
            bool locked = (horizontalMove == false && axis != Vector2.zero) || (verticalMove == false && vertical != 0f);
            if (locked) OnLockedMoveInput?.Invoke();
            if (horizontalMove == false) axis = Vector2.zero;
            if (verticalMove == false) vertical = 0f;
            if (axis == Vector2.zero && vertical == 0f) return;

            // Space로 잠가 두었으면 남은 이동 입력도 버리고 알립니다.
            if (IsMoveLocked)
            {
                OnMoveLockedInput?.Invoke();
                return;
            }

            Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
            Vector3 direction = heading * new Vector3(axis.x, 0f, axis.y);
            Vector3 step = (direction + Vector3.up * vertical) * moveSpeed * Time.deltaTime;

            // 막는 물체 안으로는 들어가지 않습니다. 축마다 따로 시도해서 벽을 따라 미끄러지듯 움직입니다.
            TryMove(new Vector3(step.x, 0f, 0f));
            TryMove(new Vector3(0f, 0f, step.z));
            TryMove(new Vector3(0f, step.y, 0f));
        }

        private void UpdateMoveLockInput()
        {
            if (CanLockMove == false || SimpleInput.GetKeyDown(moveLockKey) == false) return;

            IsMoveLocked = !IsMoveLocked;
            OnMoveLockChanged?.Invoke(IsMoveLocked);
        }

        // E를 누르면 1(상승), Q를 누르면 -1(하강), 둘 다거나 안 누르면 0 입니다. (Vertical Move가 꺼져 있어도 키는 읽음)
        private float GetVerticalAxis()
        {
            float value = 0f;
            if (SimpleInput.GetKey(upKey)) value += 1f;
            if (SimpleInput.GetKey(downKey)) value -= 1f;
            return value;
        }

        // 한 축으로 이동해 보고, 범위 밖이거나 어느 Pivot이라도 막는 물체에 닿으면 그 축은 움직이지 않습니다.
        private void TryMove(Vector3 step)
        {
            Vector3 next = targetMove + step;
            next.x = Mathf.Clamp(next.x, moveMin.x, moveMax.x);
            next.y = Mathf.Clamp(next.y, moveMin.y, moveMax.y);
            next.z = Mathf.Clamp(next.z, moveMin.z, moveMax.z);

            if (IsBlockedAt(next)) return;
            targetMove = next;
        }

        // 이동량 offset을 더한 Pivot 자리 중 하나라도 막는 물체와 겹치면 true 입니다.
        private bool IsBlockedAt(Vector3 offset)
        {
            if (blockLayers.value == 0) return false;

            foreach (OrbitView view in views)
            {
                if (view.pivot == null) continue;
                if (Physics.CheckSphere(view.pivot.position + offset, collisionRadius, blockLayers, QueryTriggerInteraction.Ignore)) return true;
            }
            return false;
        }

        private void UpdateRotateInput()
        {
            if (SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right))
            {
                Vector2 delta = SimpleInput.GetMouseDelta();
                targetYaw += delta.x * degreesPerPixel;
                targetPitch -= delta.y * degreesPerPixel;
            }

            targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);
        }

        private void UpdateZoomInput()
        {
            float scroll = SimpleInput.GetMouseScrollDelta();
            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomStep, distanceMin, distanceMax);
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // 두 화면 중 더 많이 막힌 쪽 거리를 같이 써서 두 카메라가 항상 같은 시점을 유지합니다.
            // 휠로 정한 distance는 그대로 두므로, 막는 물체를 벗어나면 원래 거리로 돌아갑니다.
            float shownDistance = GetUnblockedDistance(back, distance);
            Vector3 offset = back * shownDistance;

            foreach (OrbitView view in views)
            {
                if (view.camera == null || view.pivot == null) continue;

                view.camera.position = view.pivot.position + move + offset;
                view.camera.rotation = rotation;
            }
        }

        // 각 Pivot에서 카메라 쪽으로 공을 굴려 보고, 막는 물체 바로 앞까지의 가장 짧은 거리를 돌려줍니다.
        private float GetUnblockedDistance(Vector3 back, float wanted)
        {
            if (blockLayers.value == 0) return wanted;

            float shortest = wanted;
            foreach (OrbitView view in views)
            {
                if (view.pivot == null) continue;

                Vector3 origin = view.pivot.position + move;
                if (Physics.SphereCast(origin, collisionRadius, back, out RaycastHit hit, wanted, blockLayers, QueryTriggerInteraction.Ignore))
                {
                    shortest = Mathf.Min(shortest, hit.distance);
                }
            }
            return shortest;
        }

        private void OnValidate()
        {
            pitchMax = Mathf.Max(pitchMin, pitchMax);
            distanceMax = Mathf.Max(distanceMin, distanceMax);
            collisionRadius = Mathf.Max(0.01f, collisionRadius);
        }

        private void OnDrawGizmosSelected()
        {
            if (views == null) return;

            Gizmos.color = Color.yellow;
            foreach (OrbitView view in views)
            {
                if (view.camera == null || view.pivot == null) continue;

                Gizmos.DrawWireSphere(view.pivot.position, 0.3f);
                Gizmos.DrawLine(view.camera.position, view.pivot.position);
            }
        }
    }
}
