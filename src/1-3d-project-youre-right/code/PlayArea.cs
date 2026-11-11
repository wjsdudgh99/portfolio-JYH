using UnityEngine;

namespace YoureRight
{
    // 클릭에 반응하는 구역(섬 하나)을 나타내는 보이지 않는 상자 입니다.
    // 물리 Collider 없이 점이 상자 안에 있는지만 계산합니다. (Transform의 위치·회전·스케일을 따릅니다)
    // ClickInteractor는 클릭 지점이 어느 PlayArea에도 없으면 반응하지 않습니다. (씬에 PlayArea가 없으면 모든 곳이 반응)
    // Mirror Of를 지정하면 그 PlayArea의 Center/Size를 그대로 따라가므로, 왼쪽 상자만 조절하면 오른쪽도 맞춰집니다.
    public class PlayArea : MonoBehaviour
    {
        [SerializeField] private PlayArea mirrorOf;                      // 비워 두면 아래 Center/Size를 직접 사용
        [SerializeField] private Vector3 center = Vector3.zero;          // 로컬 좌표 기준 상자 중심
        [SerializeField] private Vector3 size = new Vector3(20f, 15f, 20f);

        private static readonly Color GizmoLineColor = new Color(1f, 0.2f, 0.2f, 1f);
        private static readonly Color GizmoFillColor = new Color(1f, 0.2f, 0.2f, 0.15f);

        // 한 단계만 따라갑니다. (mirrorOf가 또 다른 상자를 따라가도 그 상자의 자기 값만 사용해 무한 반복을 막음)
        public Vector3 Center => mirrorOf != null && mirrorOf != this ? mirrorOf.center : center;
        public Vector3 Size => mirrorOf != null && mirrorOf != this ? mirrorOf.size : size;

        // 월드 좌표의 점이 상자 안에 있으면 true를 반환합니다. (경계면 포함)
        public bool Contains(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint) - Center;
            Vector3 half = Size * 0.5f;

            return Mathf.Abs(local.x) <= half.x
                && Mathf.Abs(local.y) <= half.y
                && Mathf.Abs(local.z) <= half.z;
        }

        private void OnValidate()
        {
            if (mirrorOf == this)
            {
                Debug.LogWarning($"[PlayArea] {name}의 Mirror Of에 자기 자신이 들어 있어 비웁니다.", this);
                mirrorOf = null;
            }
            else if (mirrorOf != null && mirrorOf.mirrorOf != null)
            {
                Debug.LogWarning($"[PlayArea] {name}이(가) 따라가는 {mirrorOf.name}도 다른 상자를 따라갑니다. {mirrorOf.name}의 자기 값만 사용합니다.", this);
            }
        }

        // 디오라마를 배치하면서 경계를 계속 볼 수 있도록 선택하지 않아도 빨간 선을 그립니다.
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = GizmoLineColor;
            Gizmos.DrawWireCube(Center, Size);
        }

        // 선택하면 반투명 면도 칠합니다.
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = GizmoFillColor;
            Gizmos.DrawCube(Center, Size);
        }
    }
}
