using CY.Network;
using UnityEngine;

namespace CY.Vehicle
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }
        private NetworkSession networkSession;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource accelLoopSource;
        [SerializeField] private AudioSource brakeLoopSource;
        [SerializeField] private AudioSource miniGameLoopSource; 

        [Header("Audio Clips")]
        [SerializeField] private AudioClip accelClip;
        [SerializeField] private AudioClip brakeClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip gearShiftClip;
        
        [Header("Audio Clips - Minigame 1: Fuel")]
        [SerializeField] private AudioClip fuelLoopClip;
        [SerializeField] private AudioClip fuelSuccessClip;
        [SerializeField] private AudioClip fuelFailClip;

        [Header("Audio Clips - Minigame 2: Barricade (바리케이드)")]
        [SerializeField] private AudioClip barricadeButtonClickClip;
        [SerializeField] private AudioClip barricadeSuccessClip;
        [SerializeField] private AudioClip barricadeFailClip;

        [Header("Audio Clips - Minigame 3: RepairShop (차량 정비소)")]
        [SerializeField] private AudioClip repairShopDrillingClip;
        [SerializeField] private AudioClip repairShopPlateGrabClip;
        [SerializeField] private AudioClip repairShopPlateDropClip;
        [SerializeField] private AudioClip repairShopSuccessClip;

        [Header("Crash Clips (Random)")]
        [SerializeField] private AudioClip[] middleCrashClips;
        [SerializeField] private AudioClip[] hardCrashClips;
        
        [Header("BGM Clips")]
        [SerializeField] private AudioClip titleBgm; 
        [SerializeField] private AudioClip lobbyBgm;
        [SerializeField] private AudioClip tutorialBgm;
        [SerializeField] private AudioClip[] inGameBgms;         
        [SerializeField] private AudioClip gameClearBgm;         
        [SerializeField] private AudioClip gameOverBgm;          

        private bool isPlayingInGameBgmList = false;
        private int lastInGameBgmIndex = -1;
        private bool isNetworkSubscribed = false; // [안전장치 추가]
        
        // 현재 기어 상태를 기억할 변수
        private Gear currentGear;
        
        // 마지막으로 기어 소리가 난 시간을 기억할 변수 (귀 테러 방지용)
        private float lastGearShiftTime = 0f;
        
        // 자동차의 현재 속도를 기억할 변수
        private float currentSpeed = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject); 
                return;
            }

            if (accelLoopSource != null) accelLoopSource.loop = true;
            if (brakeLoopSource != null) brakeLoopSource.loop = true;
            if (miniGameLoopSource != null) miniGameLoopSource.loop = true;
        }
        
        private void Update()
        {
            if (isPlayingInGameBgmList && bgmSource != null && !bgmSource.isPlaying)
            {
                PlayNextRandomInGameBgm();
            }

            TrySubscribeNetworkSession();
        }

        private void OnEnable()
        {
            VehicleSoundEvents.AccelChanged += HandleAccel;
            VehicleSoundEvents.BrakeChanged += HandleBrake;
            VehicleSoundEvents.GearChanged += HandleGearChanged;
            VehicleDamage.OnMiddleCrash += PlayMiddleCrashSound;
            VehicleDamage.OnHardCrash += PlayHardCrashSound;
            
            // 클라이언트(비호스트)용 네트워크 충돌 이벤트 구독
            CY.Network.RoundStateSync.OnNetworkCrash += PlayNetworkCrashSound;
        }

        private void OnDisable()
        {
            VehicleSoundEvents.AccelChanged -= HandleAccel;
            VehicleSoundEvents.BrakeChanged -= HandleBrake;
            VehicleSoundEvents.GearChanged -= HandleGearChanged;
            VehicleDamage.OnMiddleCrash -= PlayMiddleCrashSound;
            VehicleDamage.OnHardCrash -= PlayHardCrashSound;
            
            // 구독 해제
            CY.Network.RoundStateSync.OnNetworkCrash -= PlayNetworkCrashSound;
        }
        
        private void TrySubscribeNetworkSession()
        {
            if (isNetworkSubscribed) return;

            if (CY.Network.NetworkSession.IsInitialized == false) return;

            networkSession = CY.Network.NetworkSession.Instance;

            if (networkSession == null) return;

            networkSession.MessageReceived += OnNetworkMessageReceived;
            isNetworkSubscribed = true;
        }
        
        // 네트워크로 전달받은 충돌 처리
        private void PlayNetworkCrashSound(bool isHard)
        {
            if (isHard) PlayHardCrashSound();
            else PlayMiddleCrashSound();
        }

        private void OnDestroy()
        {
            if (isNetworkSubscribed && networkSession != null)
            {
                networkSession.MessageReceived -= OnNetworkMessageReceived;
            }

            isNetworkSubscribed = false;
            networkSession = null;
        }

        // SON 파싱을 안전하게 도와줄 작은 클래스 (메서드 바로 위에 추가)
        private class NetCrashMsg { public bool isHard; }

        // [수정] 충돌음과 BGM 패킷을 모두 안전하게 수신하는 메서드
        private void OnNetworkMessageReceived(string type, string json)
        {
            // 호스트는 이미 자기 화면에서 다 재생했으므로 튕겨낸다
            if (Network.Session.IsHost) return; 

            if (type == "vehicle_crash")
            {
                // 띄어쓰기 오류가 없도록 완벽하게 JSON으로 변환해서 읽는다
                var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<NetCrashMsg>(json);
                if (msg != null && msg.isHard)
                {
                    PlayHardCrashSound();
                }
                else
                {
                    PlayMiddleCrashSound();
                }
            }
            else if (type == "round_success")
            {
                // 비호스트 화면에도 게임 클리어 BGM 재생
                PlayGameClearBgm();
            }
            else if (type == "round_failed")
            {
                // 비호스트 화면에도 게임 오버 BGM 재생 (인게임 BGM을 자동으로 덮어씀)
                PlayGameOverBgm();
            }
        }
        
        // --- BGM 제어 메서드 ---

        // 타이틀 BGM 재생
        public void PlayTitleBgm()
        {
            if (titleBgm != null) PlayBgmSingle(titleBgm, true);
        }
        public void PlayLobbyBgm() { isPlayingInGameBgmList = false; PlayBgmSingle(lobbyBgm, isLoop: true); }
        public void PlayTutorialBgm() { isPlayingInGameBgmList = false; PlayBgmSingle(tutorialBgm, isLoop: true); }
        public void PlayGameClearBgm() { isPlayingInGameBgmList = false; PlayBgmSingle(gameClearBgm, isLoop: false); }
        public void PlayGameOverBgm() { isPlayingInGameBgmList = false; PlayBgmSingle(gameOverBgm, isLoop: false); }
        public void StopBgm() { isPlayingInGameBgmList = false; if (bgmSource != null) bgmSource.Stop(); }

        public void PlayInGameBgm()
        {
            if (bgmSource != null) bgmSource.Stop();
            if (inGameBgms == null || inGameBgms.Length == 0) return;
            
            isPlayingInGameBgmList = true;
            lastInGameBgmIndex = -1;
            PlayNextRandomInGameBgm();
        }

        private void PlayBgmSingle(AudioClip clip, bool isLoop)
        {
            if (bgmSource == null || clip == null) return;
            
            // 지금 틀려는 노래가 이미 나오고 있는 노래랑 똑같다면
            // 처음부터 다시 틀지 않고 그냥 돌아간다(return)
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            
            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.loop = isLoop;
            bgmSource.Play();
        }

        private void PlayNextRandomInGameBgm()
        {
            if (inGameBgms == null || inGameBgms.Length == 0 || bgmSource == null) return;
            int randomIndex = Random.Range(0, inGameBgms.Length);
            if (inGameBgms.Length > 1 && randomIndex == lastInGameBgmIndex) randomIndex = (randomIndex + 1) % inGameBgms.Length;
            lastInGameBgmIndex = randomIndex;
            bgmSource.Stop();
            bgmSource.clip = inGameBgms[randomIndex];
            bgmSource.loop = false; 
            bgmSource.Play();
        }
        
        public void PlayUIClick() { if (uiClickClip != null && sfxSource != null) sfxSource.PlayOneShot(uiClickClip); }
        
        public void StartMiniGameLoop(AudioClip clip)
        {
            if (miniGameLoopSource == null || clip == null) return;
            if (miniGameLoopSource.clip != clip) { miniGameLoopSource.clip = clip; miniGameLoopSource.time = 0f; }
            if (!miniGameLoopSource.isPlaying) { miniGameLoopSource.time = 0f; miniGameLoopSource.Play(); }
        }

        // 주유소 미니게임 호출 메서드
        public void StopMiniGameLoop() { if (miniGameLoopSource != null && miniGameLoopSource.isPlaying) miniGameLoopSource.Stop(); }
        public void StartFuelLoopSound() => StartMiniGameLoop(fuelLoopClip);
        public void StopFuelLoopSound() => StopMiniGameLoop();
        public void PlayFuelSuccessSound() { StopMiniGameLoop(); 
            if (fuelSuccessClip != null && sfxSource != null) sfxSource.PlayOneShot(fuelSuccessClip); }
        public void PlayFuelFailSound() { StopMiniGameLoop(); 
            if (fuelFailClip != null && sfxSource != null) sfxSource.PlayOneShot(fuelFailClip); }
        
        // --- 2. 바리케이드 미니게임 전용 호출 메서드 ---

        /// <summary> 바리케이드 버튼 클릭 효과음 </summary>
        public void PlayBarricadeButtonClickSound()
        {
            if (barricadeButtonClickClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(barricadeButtonClickClip);
            }
        }

        /// <summary> 바리케이드 해체 성공 효과음 </summary>
        public void PlayBarricadeSuccessSound()
        {
            if (barricadeSuccessClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(barricadeSuccessClip);
            }
        }

        /// <summary> 바리케이드 해체 실패 효과음 </summary>
        public void PlayBarricadeFailSound()
        {
            if (barricadeFailClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(barricadeFailClip);
            }
        }
        
        // 3. 번호판 교체 미니 게임 호출 메서드

        public void PlayRepairShopDrillingSound()
        {
            if (repairShopDrillingClip != null && sfxSource != null)
                sfxSource.PlayOneShot(repairShopDrillingClip);
        }

        public void PlayRepairShopPlateGrabSound()
        {
            if (repairShopPlateGrabClip != null && sfxSource != null)
                sfxSource.PlayOneShot(repairShopPlateGrabClip);
        }

        public void PlayRepairShopPlateDropSound()
        {
            if (repairShopPlateDropClip != null && sfxSource != null)
                sfxSource.PlayOneShot(repairShopPlateDropClip);
        }

        public void PlayRepairShopSuccessSound()
        {
            if (repairShopSuccessClip != null && sfxSource != null)
                sfxSource.PlayOneShot(repairShopSuccessClip);
        }
        
        

        private void HandleAccel(bool on) 
        { 
            // 밟았지만(on) 현재 기어가 N단이면 소리 내지 않고 돌아가기
            if (on && currentGear == Gear.N) { StopAccelSound(); return; }
            
            // 브레이크를 밟았지만 속도가 0(혹은 0.1 이하)이면 소리 내지 않기
            //if (on && currentSpeed <= 0.1f) return;

            if (on) StartAccelSound(); else StopAccelSound(); 
        }

        private void HandleBrake(bool on) 
        { 
            // 밟았지만(on) 현재 기어가 N단이면 소리 내지 않고 돌아가기
            if (on && currentGear == Gear.N) { StopBrakeSound(); return; }
            
            // 브레이크를 밟았지만 속도가 0(혹은 0.1 이하)이면 소리 내지 않기
            if (on && currentSpeed <= 0.1f) 
            { 
                StopBrakeSound(); 
                return; 
            }

            if (on) StartBrakeSound(); else StopBrakeSound(); 
        }

        private void StartAccelSound()
        {
            if (accelLoopSource == null || accelClip == null) return;
            if (accelLoopSource.clip != accelClip) accelLoopSource.clip = accelClip;
            if (!accelLoopSource.isPlaying) accelLoopSource.Play();
        }
        private void StopAccelSound() { if (accelLoopSource != null) accelLoopSource.Stop(); }

        private void StartBrakeSound()
        {
            if (brakeLoopSource == null || brakeClip == null) return;
            if (brakeLoopSource.clip != brakeClip) brakeLoopSource.clip = brakeClip;
            if (!brakeLoopSource.isPlaying) brakeLoopSource.Play();
        }
        private void StopBrakeSound() { if (brakeLoopSource != null) brakeLoopSource.Stop(); }
        
        // 자동차 스크립트에서 매 프레임마다 현재 속도를 넘겨줄 함수
        public void UpdateVehicleSpeed(float speed)
        {
            currentSpeed = Mathf.Abs(speed); // 후진할 수도 있으니 절대값으로 저장

            // 꾹 누르고 있는 상태에서 차가 완전히 멈추면(0.1 이하) 강제로 끔
            if (currentSpeed <= 0.1f && brakeLoopSource != null && brakeLoopSource.isPlaying)
            {
                StopBrakeSound();
            }
        }

        private void PlayMiddleCrashSound() => PlayRandomClip(middleCrashClips);
        private void PlayHardCrashSound() => PlayRandomClip(hardCrashClips);

        private void PlayRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || sfxSource == null) return;
            int randomIndex = Random.Range(0, clips.Length);
            AudioClip selectedClip = clips[randomIndex];
            if (selectedClip != null) sfxSource.PlayOneShot(selectedClip);
        }
        
        private void HandleGearChanged(Gear gear)
        {
            if (currentGear == gear) return;
            if (Time.time - lastGearShiftTime < 0.2f) return;
            
            currentGear = gear;
            lastGearShiftTime = Time.time;
            
            if (gearShiftClip != null && sfxSource != null) sfxSource.PlayOneShot(gearShiftClip);
            if (currentGear == Gear.N) { StopAccelSound(); StopBrakeSound(); }
        }
    }
}