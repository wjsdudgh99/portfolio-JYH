using System;
using CY.Network;
using UnityEngine;

namespace CY.Vehicle
{
    public class Vehicle : MonoBehaviour
    {
        // 액셀 관련 이벤트 (시작 / 중지)
        public static event Action OnAccelStart;
        public static event Action OnAccelStop;

        // 브레이크 관련 이벤트 (시작 / 중지)
        public static event Action OnBrakeStart;
        public static event Action OnBrakeStop;

        private VehicleInputMerger vehicleMerger;

        private bool isAccelling = false;
        private bool isBraking = false;

        private void Awake()
        {
            if (vehicleMerger == null)
                vehicleMerger = new VehicleInputMerger();
        }
        
        // 네트워크 수신부에서 Set()을 호출할 수 있도록 프로퍼티 제공
        public VehicleInputMerger Merger => vehicleMerger; 

        private void Update()
        {
            if (vehicleMerger == null) return;

            // 역할별 입력이 병합된 최신 VehicleInput 데이터를 읽어옴
            VehicleInput input = vehicleMerger.Merge();

            // --- [1. 액셀 제어] ---
            // Accel 역할을 맡은 플레이어가 입력을 주면 input.Accel > 0f가 됨
            if (input.Accel > 0f && !isAccelling)
            {
                isAccelling = true;
                Debug.Log("[SoundCheck] OnAccelStart 이벤트 발화!");
                OnAccelStart?.Invoke();
            }
            else if (input.Accel <= 0f && isAccelling)
            {
                isAccelling = false;
                Debug.Log("[SoundCheck] OnAccelStop 이벤트 발화!");
                OnAccelStop?.Invoke();
            }

            // --- [2. 브레이크 제어] ---
            // Brake 역할을 맡은 플레이어가 입력을 주면 input.Brake > 0f가 됨
            if (input.Brake > 0f && !isBraking)
            {
                isBraking = true;
                Debug.Log("[SoundCheck] OnBrakeStart 이벤트 발화!");
                OnBrakeStart?.Invoke();
            }
            else if (input.Brake <= 0f && isBraking)
            {
                isBraking = false;
                Debug.Log("[SoundCheck] OnBrakeStop 이벤트 발화!");
                OnBrakeStop?.Invoke();
            }
        }
    }
}