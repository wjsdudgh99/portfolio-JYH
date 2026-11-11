using UnityEngine;

namespace YoureRight
{
    // 정답 쌍둥이 오브젝트 하나하나에 붙이는 컴포넌트 입니다.
    // 마우스 Raycast에 잡혀야 하므로 자신이나 자식에 Collider가 반드시 필요합니다.
    // (ClickInteractor가 맞은 Collider의 부모에서 AnswerTarget을 찾으므로, 거울처럼 부품마다 Collider를 붙인 물체도 됩니다.
    //  그래서 RequireComponent는 쓰지 않습니다. 붙이면 루트에 기본 크기 상자가 저절로 생겨 빈 곳까지 눌림. 검사는 Validate Stage가 함)
    // 자신이 어느 쌍(AnswerPair)에 속하는지는 AnswerPair가 Awake에서 알려줍니다.
    public class AnswerTarget : MonoBehaviour
    {
        public AnswerPair Pair { get; private set; }

        public void SetPair(AnswerPair pair)
        {
            if (Pair != null && Pair != pair)
            {
                Debug.LogWarning($"[AnswerTarget] {name}이(가) 두 개의 AnswerPair({Pair.name}, {pair.name})에 등록되어 있습니다.", this);
            }
            Pair = pair;
        }
    }
}
