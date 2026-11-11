using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YoureRight
{
    // 클릭 결과에 따른 3D 연출을 담당합니다. (기획서 5.1)
    // 정답: 쌍둥이 두 오브젝트가 황금빛으로 부드럽게 빛나고, 위쪽에 반짝임 파티클이 재생됩니다.
    // 오답: 클릭한 오브젝트가 빨갛게 빛났다가 서서히 원래 색으로 돌아갑니다.
    //       흔들림은 보류 중 (관련 코드를 // 주석으로 막아 두었습니다. 주석을 풀면 다시 동작합니다)
    // ClickInteractor의 이벤트만 구독하므로 다른 게임 로직에는 영향을 주지 않습니다.
    public class ClickFeedback : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private ClickInteractor clickInteractor;

        [Header("Answer - Glow")]
        [SerializeField] private Color glowColor = new Color(1f, 0.78f, 0.25f);   // 황금빛
        [SerializeField] private float glowPeakIntensity = 2.5f;   // 가장 밝을 때 Emission 세기
        [SerializeField] private float glowSettleIntensity = 0.6f; // 연출이 끝난 뒤 남는 세기 (찾은 쌍 표시, 0이면 꺼짐)
        [SerializeField] private float glowRiseTime = 0.25f;
        [SerializeField] private float glowFallTime = 0.6f;
        [Range(0f, 1f)]
        [SerializeField] private float baseColorTint = 0.35f;      // 기본 색을 황금색 쪽으로 섞는 비율 (Emission이 없는 셰이더 대비)

        [Header("Answer - Sparkle")]
        [SerializeField] private ParticleSystem sparklePrefab;
        [SerializeField] private float sparkleHeightOffset = 0.3f; // 오브젝트 윗면에서 얼마나 위에 띄울지

        [Header("Wrong - Flash")]
        [SerializeField] private Color wrongColor = new Color(1f, 0.15f, 0.15f);   // 빨강
        [SerializeField] private float wrongPeakIntensity = 2f;    // 클릭한 순간 Emission 세기
        [SerializeField] private float wrongFadeTime = 1f;         // 원래 색으로 돌아가는 시간
        [Range(0f, 1f)]
        [SerializeField] private float wrongBaseColorTint = 0.5f;  // 기본 색을 빨간색 쪽으로 섞는 비율 (Emission이 없는 셰이더 대비)

        // [보류] 오답 흔들림
        // [Header("Wrong - Shake")]
        // [SerializeField] private float shakeDuration = 0.3f;
        // [SerializeField] private float shakeStrength = 0.08f;      // 흔들리는 최대 거리
        // [SerializeField] private float shakeFrequency = 40f;       // 초당 흔들림 횟수

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // 원래 색으로 되돌리기 위해 머티리얼마다 처음 값을 기억합니다.
        private class FlashState
        {
            public Coroutine routine;
            public readonly List<Material> materials = new List<Material>();
            public readonly List<Color> baseColors = new List<Color>();
            public readonly List<Color> emissionColors = new List<Color>();
            public readonly List<bool> emissionWasOn = new List<bool>();
        }

        // 같은 오브젝트를 연달아 클릭하면 진행 중인 깜빡임을 처음부터 다시 합니다.
        private readonly Dictionary<Transform, FlashState> flashing = new Dictionary<Transform, FlashState>();

        // [보류] 같은 오브젝트를 연달아 클릭해도 흔들림이 겹치지 않도록 진행 중인 흔들림을 기억합니다.
        // private readonly Dictionary<Transform, (Coroutine routine, Vector3 origin)> shaking =
        //     new Dictionary<Transform, (Coroutine, Vector3)>();

        private void Awake()
        {
            if (clickInteractor == null) clickInteractor = FindAnyObjectByType<ClickInteractor>();
        }

        private void OnEnable()
        {
            clickInteractor.OnAnswerClicked += HandleAnswer;
            clickInteractor.OnWrongClicked += HandleWrongFlash;
            // clickInteractor.OnWrongClicked += HandleWrong;   // [보류] 오답 흔들림
        }

        private void OnDisable()
        {
            clickInteractor.OnAnswerClicked -= HandleAnswer;
            clickInteractor.OnWrongClicked -= HandleWrongFlash;
            // clickInteractor.OnWrongClicked -= HandleWrong;   // [보류] 오답 흔들림

            // 깜빡이는 도중에 꺼지면 색이 빨간 채로 남지 않게 바로 되돌립니다.
            foreach (FlashState state in flashing.Values)
            {
                if (state.routine != null) StopCoroutine(state.routine);
                RestoreFlash(state);
            }
            flashing.Clear();
        }

        #region 정답 연출

        private void HandleAnswer(AnswerTarget clicked)
        {
            AnswerPair pair = clicked.Pair;
            PlayAnswerEffect(pair.TargetA);
            PlayAnswerEffect(pair.TargetB);
        }

        private void PlayAnswerEffect(AnswerTarget target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            StartCoroutine(GlowRoutine(renderers));
            SpawnSparkle(renderers);
        }

        private IEnumerator GlowRoutine(Renderer[] renderers)
        {
            // renderer.materials는 이 오브젝트 전용 복사본을 만들어 주므로,
            // 같은 머티리얼을 쓰는 다른 오브젝트(오답 미끼 등)는 빛나지 않습니다.
            var materials = new List<Material>();
            var baseColors = new List<Color>();
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.materials)
                {
                    materials.Add(m);
                    baseColors.Add(GetBaseColor(m));
                    if (m.HasProperty(EmissionColorId)) m.EnableKeyword("_EMISSION");
                }
            }

            // 0 → 최고 밝기 (빠르게)
            yield return Animate(glowRiseTime, t => ApplyGlow(materials, baseColors, EaseOut(t) * glowPeakIntensity, EaseOut(t)));

            // 최고 밝기 → 남는 밝기 (천천히)
            yield return Animate(glowFallTime, t =>
            {
                float intensity = Mathf.Lerp(glowPeakIntensity, glowSettleIntensity, EaseOut(t));
                float tint = Mathf.Lerp(1f, glowSettleIntensity > 0f ? 0.5f : 0f, EaseOut(t));
                ApplyGlow(materials, baseColors, intensity, tint);
            });
        }

        private void ApplyGlow(List<Material> materials, List<Color> baseColors, float intensity, float tintWeight)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                Material m = materials[i];
                if (m.HasProperty(EmissionColorId)) m.SetColor(EmissionColorId, glowColor * intensity);

                Color tinted = Color.Lerp(baseColors[i], glowColor, baseColorTint * tintWeight);
                SetBaseColor(m, tinted);
            }
        }

        private void SpawnSparkle(Renderer[] renderers)
        {
            if (sparklePrefab == null) return;

            // 자식 렌더러까지 포함한 전체 크기의 윗면 중앙에 파티클을 띄웁니다.
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Vector3 position = new Vector3(bounds.center.x, bounds.max.y + sparkleHeightOffset, bounds.center.z);
            ParticleSystem sparkle = Instantiate(sparklePrefab, position, Quaternion.identity);

            // 파티클이 모두 사라진 뒤 오브젝트를 정리합니다.
            ParticleSystem.MainModule main = sparkle.main;
            Destroy(sparkle.gameObject, main.duration + main.startLifetime.constantMax);
        }

        private static Color GetBaseColor(Material m)
        {
            if (m.HasProperty(BaseColorId)) return m.GetColor(BaseColorId);   // URP Lit
            if (m.HasProperty(ColorId)) return m.GetColor(ColorId);           // Built-in / 일부 에셋 셰이더
            return Color.white;
        }

        private static void SetBaseColor(Material m, Color color)
        {
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color);
            else if (m.HasProperty(ColorId)) m.SetColor(ColorId, color);
        }

        #endregion

        #region 오답 연출 - 빨간 깜빡임

        private void HandleWrongFlash(Vector2 screenPos, Collider hitCollider)
        {
            // 맞은 Collider의 물체만 빨갛게 합니다. (부품마다 Collider가 있으면 그 부품만)
            // 부모에 WrongFlashGroup이 있으면 그 오브젝트 전체를 빨갛게 합니다. (예: Info Stand)
            WrongFlashGroup group = hitCollider.GetComponentInParent<WrongFlashGroup>();
            Transform target = group != null ? group.transform : hitCollider.transform;

            // 이미 깜빡이는 중이면 원래 색으로 돌려놓고 처음부터 다시 합니다.
            if (flashing.TryGetValue(target, out FlashState running))
            {
                StopCoroutine(running.routine);
                RestoreFlash(running);
                flashing.Remove(target);
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var state = new FlashState();
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.materials)
                {
                    state.materials.Add(m);
                    state.baseColors.Add(GetBaseColor(m));
                    state.emissionColors.Add(m.HasProperty(EmissionColorId) ? m.GetColor(EmissionColorId) : Color.black);
                    state.emissionWasOn.Add(m.IsKeywordEnabled("_EMISSION"));
                    if (m.HasProperty(EmissionColorId)) m.EnableKeyword("_EMISSION");
                }
            }

            flashing[target] = state;
            state.routine = StartCoroutine(WrongFlashRoutine(target, state));
        }

        private IEnumerator WrongFlashRoutine(Transform key, FlashState state)
        {
            // 클릭한 순간 가장 빨갛고, wrongFadeTime 동안 서서히 원래 색으로 돌아갑니다.
            yield return Animate(wrongFadeTime, t =>
            {
                float weight = 1f - EaseOut(t);
                for (int i = 0; i < state.materials.Count; i++)
                {
                    Material m = state.materials[i];
                    if (m == null) continue;
                    if (m.HasProperty(EmissionColorId))
                        m.SetColor(EmissionColorId, Color.Lerp(state.emissionColors[i], wrongColor * wrongPeakIntensity, weight));

                    SetBaseColor(m, Color.Lerp(state.baseColors[i], wrongColor, wrongBaseColorTint * weight));
                }
            });

            RestoreFlash(state);
            flashing.Remove(key);
        }

        private static void RestoreFlash(FlashState state)
        {
            for (int i = 0; i < state.materials.Count; i++)
            {
                Material m = state.materials[i];
                if (m == null) continue;
                if (m.HasProperty(EmissionColorId)) m.SetColor(EmissionColorId, state.emissionColors[i]);
                if (!state.emissionWasOn[i]) m.DisableKeyword("_EMISSION");
                SetBaseColor(m, state.baseColors[i]);
            }
        }

        #endregion

        #region 오답 연출 - [보류] 오답 흔들림 (주석을 풀면 다시 동작합니다)

        // private void HandleWrong(Vector2 screenPos, Collider hitCollider)
        // {
        //     Transform target = hitCollider.transform;
        //
        //     // 이미 흔들리는 중이면 원래 위치로 돌려놓고 처음부터 다시 흔듭니다.
        //     Vector3 origin = target.localPosition;
        //     if (shaking.TryGetValue(target, out var running))
        //     {
        //         StopCoroutine(running.routine);
        //         origin = running.origin;
        //         target.localPosition = origin;
        //     }
        //
        //     shaking[target] = (StartCoroutine(ShakeRoutine(target, origin)), origin);
        // }
        //
        // private IEnumerator ShakeRoutine(Transform target, Vector3 origin)
        // {
        //     float elapsed = 0f;
        //     while (elapsed < shakeDuration)
        //     {
        //         if (target == null) yield break;
        //
        //         // 시간이 지날수록 흔들림이 약해지도록 감쇠시킵니다.
        //         float damper = 1f - elapsed / shakeDuration;
        //         float offset = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2f) * shakeStrength * damper;
        //
        //         // 카메라가 어느 방향에 있어도 좌우로 흔들려 보이도록 카메라의 오른쪽 방향을 사용합니다.
        //         Vector3 side = Camera.main != null ? Camera.main.transform.right : Vector3.right;
        //         Vector3 localSide = target.parent != null ? target.parent.InverseTransformDirection(side) : side;
        //         target.localPosition = origin + localSide * offset;
        //
        //         elapsed += Time.deltaTime;
        //         yield return null;
        //     }
        //
        //     if (target != null) target.localPosition = origin;
        //     shaking.Remove(target);
        // }

        #endregion

        private static IEnumerator Animate(float duration, System.Action<float> step)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += duration > 0f ? Time.deltaTime / duration : 1f;
                step(Mathf.Clamp01(t));
                yield return null;
            }
        }

        private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
    }
}
