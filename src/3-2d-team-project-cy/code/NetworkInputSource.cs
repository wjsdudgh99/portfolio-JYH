using CY.Player;
using CY.Vehicle;
using HelloServer;
using Newtonsoft.Json;
using UnityEngine;

namespace CY.Network
{
    public class NetworkInputSource: MonoBehaviour, IVehicleInputSource
    {
        [SerializeField] private VehicleRoleAssigner roleAssigner;
        [SerializeField] private LocalKeyboardInputSource localInput;
        [SerializeField] private float inputTimeout = 0.25f;

        private readonly VehicleInputMerger merger = new();
        private readonly float[] lastReceivedAt = new float[4];
        private NetworkSession session;
        
        private void Awake()
        {
            session = NetworkSession.Instance;
            if (roleAssigner == null) roleAssigner = GetComponent<VehicleRoleAssigner>();
            if (localInput == null) localInput = GetComponent<LocalKeyboardInputSource>();
        }
        
        private void OnEnable()
        {
            if (session != null) session.MessageReceived += HandleMessage;
        }

        private void OnDisable()
        {
            if (session != null) session.MessageReceived -= HandleMessage;
        }
        
        private void Update()
        {
            ClearTimedOut();
            
            // 1. 내 로컬 키보드 입력을 읽어서 즉시 병합기에 넣습니다.
            if (Session.HasAssignedRole && localInput != null)
            {
                VehicleInput readInput = localInput.Read();
                lastReceivedAt[(int)Session.MyRole] = Time.unscaledTime;
                merger.Set(Session.MyRole, readInput);
            }

            // 2. [핵심] 호스트/비호스트 상관없이 병합된 4명의 최종 입력으로 사운드를 재생합니다!
            //CY.Vehicle.VehicleSoundEvents.Raise(merger.Merge());
        }
        
        public VehicleInput Read()
        {
            //if (Session.IsHost == false) return default;
            ClearTimedOut();
            return merger.Merge();
        }
        
        private void HandleMessage(string type, string json)
        {
            if (type != "vehicle_input") return;
            
            VehicleInputMessage message = JsonConvert.DeserializeObject<VehicleInputMessage>(json);
            if (message == null) return;
            
            // 내가 보낸 패킷은 Update에서 처리하므로 스킵
            if (session != null && session.Me != null && message.UserId == session.Me.Id) return;
            if (roleAssigner == null || !roleAssigner.TryGetRole(message.UserId, out VehicleRole role)) return;

            VehicleInput input = new VehicleInput
            {
                Steer = Mathf.Clamp(message.Steer, -1f, 1f),
                Accel = Mathf.Clamp01(message.Accel),
                Brake = Mathf.Clamp01(message.Brake),
                CurrentGear = ToGear(message.Gear)
            };

            // 3. 타인의 네트워크 입력을 병합기에 넣습니다.
            int index = (int)role;
            if (index >= 0 && index < lastReceivedAt.Length)
            {
                lastReceivedAt[index] = Time.unscaledTime;
                merger.Set(role, input);
            }
        }
        
        private void ClearTimedOut()
        {
            for (int i = 0; i < lastReceivedAt.Length; i++)
            {
                if ((VehicleRole)i == VehicleRole.Gear) continue;
                if (Time.unscaledTime - lastReceivedAt[i] <= inputTimeout) continue;
                merger.Set((VehicleRole)i, default);
            }
        }

        private static Gear ToGear(int value) => value switch { 1 => Gear.D, 2 => Gear.R, _ => Gear.N };
    }
}