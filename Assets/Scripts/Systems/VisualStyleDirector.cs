using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Archive0317
{
    /// <summary>
    /// Centralized world-only PS1/VHS presentation. UI remains crisp because ArchiveHUD and menus
    /// render in ScreenSpaceOverlay after the camera post process.
    /// </summary>
    public sealed class VisualStyleDirector : MonoBehaviour
    {
        private static VisualStyleDirector instance;
        private Volume volume;
        private VolumeProfile profile;
        private FilmGrain grain;
        private ChromaticAberration chromatic;
        private LensDistortion distortion;
        private Vignette vignette;
        private ColorAdjustments grading;
        private FirstPersonPlayer player;
        private Camera worldCamera;

        private float pulseStrength;
        private float pulseUntil;
        private float nextResolve;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureAfterLoad() => Ensure();

        public static VisualStyleDirector Ensure()
        {
            if (instance != null) return instance;
            var root = new GameObject("VisualStyleDirector");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<VisualStyleDirector>();
            return instance;
        }

        public static void Pulse(float strength = 1f, float duration = .35f)
        {
            var director = Ensure();
            director.pulseStrength = Mathf.Max(director.pulseStrength, Mathf.Clamp01(strength));
            director.pulseUntil = Mathf.Max(director.pulseUntil, Time.unscaledTime + Mathf.Max(.05f, duration));
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            CreateVolume();
            SceneManager.activeSceneChanged += OnSceneChanged;
            ResolveScene();
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (profile != null) Destroy(profile);
            if (instance == this) instance = null;
        }

        private void OnSceneChanged(Scene previous, Scene current)
        {
            pulseStrength = 0f;
            pulseUntil = 0f;
            ResolveScene();
        }

        private void CreateVolume()
        {
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 80f;
            volume.weight = 1f;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.DontSave;
            volume.profile = profile;

            grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.response.Override(.78f);

            chromatic = profile.Add<ChromaticAberration>(true);
            distortion = profile.Add<LensDistortion>(true);
            vignette = profile.Add<Vignette>(true);
            vignette.smoothness.Override(.7f);
            grading = profile.Add<ColorAdjustments>(true);

            SetImmediate(0f, 0f, 0f, .24f, -12f, 4f);
        }

        private void ResolveScene()
        {
            player = Object.FindFirstObjectByType<FirstPersonPlayer>();
            worldCamera = player != null ? player.ViewCamera : Camera.main;
            if (worldCamera != null)
            {
                var cameraData = worldCamera.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null) cameraData.renderPostProcessing = true;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextResolve)
            {
                nextResolve = Time.unscaledTime + .8f;
                if (player == null || worldCamera == null) ResolveScene();
            }

            float user = Mathf.Clamp01(PlayerPrefs.GetFloat(MainMenuController.VisualIntensityKey, .65f));
            if (user <= .001f)
            {
                SmoothTo(0f, 0f, 0f, 0f, 0f, 0f, 10f);
                return;
            }

            string scene = SceneManager.GetActiveScene().name;
            bool room403 = IsRoom403(scene);
            bool eventPulse = Time.unscaledTime < pulseUntil;

            float targetGrain;
            float targetChromatic;
            float targetDistortion;
            float targetVignette;
            float targetSaturation;
            float targetContrast;

            if (room403)
            {
                targetGrain = .105f;
                targetChromatic = .018f;
                targetDistortion = -.012f;
                targetVignette = .29f;
                targetSaturation = -19f;
                targetContrast = 7f;
            }
            else if (scene == "Case001_Motel")
            {
                targetGrain = .062f;
                targetChromatic = .006f;
                targetDistortion = -.004f;
                targetVignette = .25f;
                targetSaturation = -14f;
                targetContrast = 5f;
            }
            else
            {
                targetGrain = .045f;
                targetChromatic = .003f;
                targetDistortion = -.002f;
                targetVignette = .24f;
                targetSaturation = -12f;
                targetContrast = 4f;
            }

            if (eventPulse)
            {
                float strength = Mathf.Clamp01(pulseStrength);
                targetGrain = Mathf.Lerp(targetGrain, .24f, strength);
                targetChromatic = Mathf.Lerp(targetChromatic, .075f, strength);
                targetDistortion = Mathf.Lerp(targetDistortion, -.055f, strength);
                targetVignette = Mathf.Lerp(targetVignette, .36f, strength);
                targetSaturation = Mathf.Lerp(targetSaturation, -28f, strength);
                targetContrast = Mathf.Lerp(targetContrast, 11f, strength);
            }
            else pulseStrength = Mathf.MoveTowards(pulseStrength, 0f, Time.unscaledDeltaTime * 4f);

            // The integrated PS1 pass owns normal grain and scene grading.
            // Retain existing event hooks with a short, restrained chromatic pulse.
            if (worldCamera != null && worldCamera.TryGetComponent<RetroCameraStyle>(out var retro) && retro.isActiveAndEnabled && retro.Profile != null)
            {
                targetGrain = 0f;
                targetChromatic = eventPulse ? .008f * Mathf.Clamp01(pulseStrength) : 0f;
                targetDistortion = 0f;
                targetSaturation = 0f;
                targetContrast = 0f;
            }

            targetGrain *= user;
            targetChromatic *= user;
            targetDistortion *= user;
            targetVignette *= user;
            targetSaturation *= user;
            targetContrast *= user;

            SmoothTo(targetGrain, targetChromatic, targetDistortion, targetVignette, targetSaturation, targetContrast, eventPulse ? 14f : 3.2f);
        }

        private bool IsRoom403(string scene)
        {
            if (scene != "Case001_Motel" || player == null) return false;
            Vector3 p = player.transform.position;
            return p.x < -17.65f && p.x > -22.6f && p.y > 8.35f && p.z > 1.9f && p.z < 7.1f;
        }

        private void SmoothTo(float grainValue, float chromaticValue, float distortionValue, float vignetteValue, float saturationValue, float contrastValue, float speed)
        {
            float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
            grain.intensity.value = Mathf.Lerp(grain.intensity.value, grainValue, t);
            chromatic.intensity.value = Mathf.Lerp(chromatic.intensity.value, chromaticValue, t);
            distortion.intensity.value = Mathf.Lerp(distortion.intensity.value, distortionValue, t);
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, vignetteValue, t);
            grading.saturation.value = Mathf.Lerp(grading.saturation.value, saturationValue, t);
            grading.contrast.value = Mathf.Lerp(grading.contrast.value, contrastValue, t);
        }

        private void SetImmediate(float grainValue, float chromaticValue, float distortionValue, float vignetteValue, float saturationValue, float contrastValue)
        {
            grain.intensity.Override(grainValue);
            chromatic.intensity.Override(chromaticValue);
            distortion.intensity.Override(distortionValue);
            vignette.intensity.Override(vignetteValue);
            grading.saturation.Override(saturationValue);
            grading.contrast.Override(contrastValue);
        }
    }
}
