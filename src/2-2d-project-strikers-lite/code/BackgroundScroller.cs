using System.Collections.Generic;
using UnityEngine;

namespace Study.LayerAndScroll
{
    public class ScrollController : MonoBehaviour
    {
        public enum ScrollDirection { Left, Right, Up, Down }

        [Header("Scroll Settings")]
        public float speed = 1.0f;
        public ScrollDirection direction = ScrollDirection.Down;

        [Header("Resources")]
        public GameObject[] layerPrefabs;

        [Header("Ref Objects")]
        public GameObject startLayer;
        public Transform endPivot;
        public Transform spawnPivot;

        private List<GameObject> enableLayerList = new List<GameObject>();

        private void Start()
        {
            enableLayerList.Add(startLayer);
        }

        private void Update()
        {
            MoveLayerList();
            CheckDestroyAbleLayer();
            //CheckInstantiateLayer();
            CheckLayer();
        }

        private Vector3 GetMoveDirection(ScrollDirection dir)
        {
            switch (dir)
            {
                case ScrollDirection.Left:
                    return Vector3.left;
                case ScrollDirection.Right:
                    return Vector3.right;
                case ScrollDirection.Up:
                    return Vector3.up;
                case ScrollDirection.Down:
                    return Vector3.down;
                default:
                    return Vector3.left;
            }
        }

        private void MoveLayerList()
        {
            Vector3 dir = GetMoveDirection(direction);
            Vector3 moveVector = dir * (speed * Time.deltaTime);

            for (int i = 0; i < enableLayerList.Count; ++i)
            {
                enableLayerList[i].transform.Translate(moveVector);
            }
        }

        private void CheckDestroyAbleLayer()
        {
            // 2. 가장 첫번째 Layer(enableLayerList[0])가
            // EndPivot의 경계를 넘어간다면(x값보다 작아진다면)
            // 삭제한다.

            GameObject headLayer = enableLayerList[0];
            // 가장 앞에있는 Layer오브젝트를 가져옵니다

            bool check = false;

            switch (direction)
            {
                case ScrollDirection.Down:
                    check = headLayer.transform.position.y <= endPivot.position.y;
                    break;
            }

            // 왼쪽케이스
            if (check)
            {
                enableLayerList.RemoveAt(0);
                Destroy(headLayer);
            }
        }

        private void CheckInstantiateLayer()
        {
            // 3. Layer를 생성해줍니다. 현재 필요한 LayerObject 2 ~ 3개입니다

            //  GameObject.Instantiate(GameObject object) || .Instantiate(GameObject object)
            // 실행중(런타임)에 매개변수로 들어온 object의 사본을 생성합니다.
            // 생성한 객체는 생성할 객체의 타입으로 반환받을 수 있습니다                  
            while (enableLayerList.Count < 2)
            {
                GameObject layer = SpawnRandomLayer();
                enableLayerList.Add(layer);
            }
        }

        // 랜덤한 Layer를 대기열에 넣어두고, 앞으로 나올 Layer들을 미리 알아봅시다.
        private Queue<GameObject> layerQueue = new Queue<GameObject>();

        // 이 함수는 호출되면 랜덤한 레이어를 생성만 해서 반환합니다.
        private GameObject SpawnRandomLayer()
        {
            int randIdx = Random.Range(0, layerPrefabs.Length);
            GameObject layer = Instantiate(layerPrefabs[randIdx], transform);
            // 기본적으로 .Instantiate()함수에는 여러가지 활용법이 있고,
            // 그 중 하나로 Instantiate(생성할 견본, Transform parent); 와 같이
            // 생성하면서 동시에 GameObject의 부모 관계를 설정해주는 활용법이 있습니다.

            layer.transform.position = spawnPivot.position;
            return layer;
        }

        // 이 함수는 대기열의 잔량을 체크해서 일정량 이하가 된다면
        // 랜덤한 레이어를 대기열에 추가합니다.
        private void CheckLayer()
        {
            //1. Layer를 생성해서 대기열 넣고,
            //2. 기존의 삭제로직이 실행되면(CheckDestroyAbleLayer())
            //3. 대기열에 있는 Layer를 꺼내와서 enableLayerList에 넣어줍니다
            //4. if(대기열의 갯수가 충분치 않다면)
            //5.    미리 레이어를 생성해서 대기열을 채워줍니다

            // layerQueue에 레이어 담아둠 => 레이어가 지나가면 =>
            // layerQueue에 있는 레이어를 enableLayerList에 옮겨줌

            if (layerQueue.Count < 3)  // layerQueue가 두개 남았을때 아래 로직이 실행됩니다.
            {
                for (int i = 0; i < 10; ++i)
                {
                    GameObject randLayer = SpawnRandomLayer();
                    randLayer.SetActive(false);
                    layerQueue.Enqueue(randLayer);
                    Debug.Log($"{randLayer.gameObject.name}이 대기열에 추가되었습니다!");
                }
            }

            if (enableLayerList.Count < 2)
            {
                // 대기열의 가장 앞에 있는 개체를 꺼냅니다.
                GameObject enableLayer = layerQueue.Dequeue();
                enableLayer.SetActive(true);
                // enableLayerList에 추가해서 움직이게 만들어 줍니다.
                enableLayerList.Add(enableLayer);
            }
        }

    }
}
