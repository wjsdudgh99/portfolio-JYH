using System;
using UnityEngine;

namespace YoureRight
{
    // 두 디오라마에 똑같이 놓인 쌍둥이 오브젝트(AnswerTarget 2개)를 하나로 묶습니다.
    // 둘 중 어느 쪽을 클릭해도 쌍 전체가 정답 처리되고, 이미 찾은 쌍은 다시 처리하지 않습니다.
    public class AnswerPair : MonoBehaviour
    {
        [SerializeField] private AnswerTarget targetA;
        [SerializeField] private AnswerTarget targetB;

        public bool IsFound { get; private set; }
        public AnswerTarget TargetA => targetA;
        public AnswerTarget TargetB => targetB;

        // 처음 찾았을 때 한 번만 호출됩니다. (StageManager, 피드백 연출이 구독)
        public event Action<AnswerPair> OnFound;

        private void Awake()
        {
            if (targetA == null || targetB == null)
            {
                Debug.LogError($"[AnswerPair] {name}의 Target A/B가 비어 있습니다.", this);
                return;
            }

            targetA.SetPair(this);
            targetB.SetPair(this);
        }

        // 정답 처리를 시도합니다. 이번 호출로 새로 찾았으면 true, 이미 찾은 쌍이면 false를 반환합니다.
        public bool TryFind()
        {
            if (IsFound) return false;

            IsFound = true;
            OnFound?.Invoke(this);
            return true;
        }

        private void OnValidate()
        {
            if (targetA != null && targetA == targetB)
            {
                Debug.LogWarning($"[AnswerPair] {name}의 Target A와 B가 같은 오브젝트입니다.", this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (targetA == null || targetB == null) return;

            // 선택하면 어떤 두 오브젝트가 한 쌍인지 선으로 보여줍니다.
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetA.transform.position, targetB.transform.position);
        }
    }
}
