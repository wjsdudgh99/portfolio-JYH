using System;
using UnityEngine;
using TMPro;

namespace MiniGolfGame
{
    public class MiniGolfAimIndicator : MonoBehaviour
    {
        [Header("Settings")] 
        public float minLength;
        public float maxLength;
        
        [Header("UI (퍼센트 표시)")]
        public TextMeshPro powerText; // 3D 텍스트 연결
        public Vector3 textOffset = new Vector3(0, 1.5f, 0); // 공 위로 얼마나 띄울지 설정

        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true; // 월드 공간 좌표계를 사용한다고 설정합니다.
            line.positionCount = 2; // 점 두개의 위치로 선을 그린다고 설정합니다.
            Hide();
        }

        public void Show(Vector3 origin, Vector3 direction, float power01)
        {
            if (power01 > 0.01f)
            {
                line.enabled = true;
                
                // 0~1 사이의 비율로 들어온 power 값을
                // 내가 가진 최소 길이(minLength : 0)와 최대 길이(maxLength :0)으로
                // 치환합니다(바꿔줍니다)
                float length = Mathf.Lerp(minLength, maxLength, power01);
                line.SetPosition(0, origin);
                line.SetPosition(1, origin + direction * length);
            
                // 퍼센트 텍스트 띄우기 로직
                if (powerText != null)
                {
                    powerText.gameObject.SetActive(true); // 텍스트 켜기
                
                    // 0~1 값을 0~100으로 바꾸고 정수로 반올림하여 % 글자와 합칩니다.
                    int percentage = Mathf.RoundToInt(power01 * 100);
                    powerText.text = percentage + "%";
                
                    // 텍스트 위치를 공 위치(origin)보다 살짝 위(textOffset)로 옮깁니다.
                    powerText.transform.position = origin + textOffset;
                
                    // 텍스트가 항상 카메라를 정면으로 바라보게 하여 글자가 뒤집히거나 얇게 보이는 것을 막습니다.
                    if (Camera.main != null)
                    {
                        powerText.transform.rotation = Camera.main.transform.rotation;
                    }
                }
            }
            else
            {
                // 스페이스바를 누르지 않고 가만히 있을 때(파워가 0일 때)는 숨겨줍니다.
                Hide();
            }
        }

        public void Hide()
        {
            line.enabled = false;
            
            // 이 부분이 있어야 텍스트가 화면에서 사라집니다.
            if (powerText != null)
            {
                powerText.gameObject.SetActive(false); 
            }
        }
    }
}