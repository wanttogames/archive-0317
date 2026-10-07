using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archive0317
{
    /// <summary>
    /// Procedural low-volume ambience pass for Archive 03:17.
    /// It intentionally avoids music and large jump-scare peaks: the building itself becomes the score.
    /// </summary>
    public sealed class AtmosphereSoundscape : MonoBehaviour
    {
        private enum Profile { Menu, Archive, Motel, Room403 }

        private static AtmosphereSoundscape instance;
        private AudioSource tapeHiss;
        private AudioSource electricalHum;
        private AudioSource airTone;
        private AudioSource lowRumble;
        private AudioSource transient;
        private AudioClip metalCreak;
        private AudioClip distantThump;
        private AudioClip relayTick;
        private Profile profile;
        private float nextTransientAt;
        private System.Random random;
        private FirstPersonPlayer player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("AtmosphereSoundscape");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AtmosphereSoundscape>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            random = new System.Random(317);

            tapeHiss = Loop("TapeHiss", BuildLoop("Archive_TapeHiss", 6f, HissSample));
            electricalHum = Loop("ElectricalHum", BuildLoop("Archive_ElectricalHum", 5f, HumSample));
            airTone = Loop("VentilationAir", BuildLoop("Archive_VentilationAir", 7f, AirSample));
            lowRumble = Loop("PipeRumble", BuildLoop("Archive_PipeRumble", 8f, RumbleSample));

            var transientObject = new GameObject("BuildingTransient");
            transientObject.transform.SetParent(transform, false);
            transient = transientObject.AddComponent<AudioSource>();
            transient.playOnAwake = false;
            transient.loop = false;
            transient.spatialBlend = 0f;
            transient.priority = 190;

            metalCreak = BuildOneShot("Archive_MetalCreak", 1.65f, MetalCreakSample);
            distantThump = BuildOneShot("Archive_DistantThump", .85f, DistantThumpSample);
            relayTick = BuildOneShot("Archive_RelayTick", .32f, RelaySample);

            SceneManager.activeSceneChanged += OnSceneChanged;
            RefreshScene();
            ScheduleTransient(true);
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (instance == this) instance = null;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            RefreshScene();
            ScheduleTransient(true);
        }

        private void RefreshScene()
        {
            player = Object.FindFirstObjectByType<FirstPersonPlayer>();
            RebalanceAuthoredAudio();
            profile = ResolveProfile();
            ApplyTargets(profile, true);
        }

        private void Update()
        {
            var resolved = ResolveProfile();
            if (resolved != profile)
            {
                profile = resolved;
                ScheduleTransient(true);
            }

            ApplyTargets(profile, false);

            if (Time.unscaledTime >= nextTransientAt && !MainMenuController.IsMenuOpen)
            {
                PlayTransient(profile);
                ScheduleTransient(false);
            }
        }

        private Profile ResolveProfile()
        {
            if (MainMenuController.IsMenuOpen) return Profile.Menu;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "ArchiveRoom") return Profile.Archive;
            if (scene == "Case001_Motel")
            {
                if (player == null) player = Object.FindFirstObjectByType<FirstPersonPlayer>();
                if (player != null && player.transform.position.x < -16f && player.transform.position.y > 8f)
                    return Profile.Room403;
                return Profile.Motel;
            }
            return Profile.Archive;
        }

        private void ApplyTargets(Profile current, bool immediate)
        {
            float hiss, hum, air, rumble;
            switch (current)
            {
                case Profile.Menu:
                    hiss = .030f; hum = .014f; air = .004f; rumble = .002f;
                    break;
                case Profile.Motel:
                    hiss = .006f; hum = .014f; air = .027f; rumble = .008f;
                    break;
                case Profile.Room403:
                    hiss = .010f; hum = .023f; air = .016f; rumble = .018f;
                    break;
                default:
                    hiss = .008f; hum = .030f; air = .015f; rumble = .010f;
                    break;
            }

            SetVolume(tapeHiss, hiss, immediate);
            SetVolume(electricalHum, hum, immediate);
            SetVolume(airTone, air, immediate);
            SetVolume(lowRumble, rumble, immediate);
        }

        private static void SetVolume(AudioSource source, float target, bool immediate)
        {
            if (source == null) return;
            if (immediate) source.volume = target;
            else
            {
                float blend = 1f - Mathf.Exp(-3.2f * Time.unscaledDeltaTime);
                source.volume = Mathf.Lerp(source.volume, target, blend);
            }
        }

        private void RebalanceAuthoredAudio()
        {
            foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (source == null || source.transform.IsChildOf(transform)) continue;
                string id = source.gameObject.name.ToLowerInvariant();
                if (source.loop && id.Contains("hum")) source.volume = Mathf.Min(source.volume, .032f);
                else if (source.loop && id.Contains("pipe")) source.volume = Mathf.Min(source.volume, .022f);
                else if (source.loop && id.Contains("tv") && id.Contains("electronic")) source.volume = Mathf.Min(source.volume, .018f);
            }
        }

        private void ScheduleTransient(bool soon)
        {
            float min, max;
            switch (profile)
            {
                case Profile.Room403: min = 8f; max = 19f; break;
                case Profile.Motel: min = 13f; max = 30f; break;
                case Profile.Archive: min = 11f; max = 26f; break;
                default: min = 24f; max = 38f; break;
            }
            float delay = soon ? Mathf.Min(9f, min) : Mathf.Lerp(min, max, (float)random.NextDouble());
            nextTransientAt = Time.unscaledTime + delay;
        }

        private void PlayTransient(Profile current)
        {
            if (transient == null || transient.isPlaying) return;

            double roll = random.NextDouble();
            if (current == Profile.Room403)
                transient.clip = roll < .48 ? metalCreak : roll < .8 ? relayTick : distantThump;
            else if (current == Profile.Motel)
                transient.clip = roll < .46 ? relayTick : roll < .75 ? distantThump : metalCreak;
            else
                transient.clip = roll < .42 ? distantThump : roll < .73 ? relayTick : metalCreak;

            transient.volume = current == Profile.Room403 ? RandomRange(.032f, .052f)
                : current == Profile.Motel ? RandomRange(.024f, .041f)
                : RandomRange(.021f, .038f);
            transient.pitch = RandomRange(.92f, 1.06f);
            transient.panStereo = RandomRange(-.72f, .72f);
            transient.Play();
        }

        private float RandomRange(float min, float max)
            => Mathf.Lerp(min, max, (float)random.NextDouble());

        private AudioSource Loop(string name, AudioClip clip)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(transform, false);
            var source = layer.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.priority = 210;
            source.volume = 0f;
            source.Play();
            return source;
        }

        private static AudioClip BuildLoop(string name, float seconds, System.Func<float, int, float> sampler)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(seconds * rate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float edge = Mathf.Clamp01(Mathf.Min(i, count - 1 - i) / (rate * .08f));
                samples[i] = Mathf.Clamp(sampler(t, i) * edge, -.85f, .85f);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip BuildOneShot(string name, float seconds, System.Func<float, int, float> sampler)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(seconds * rate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                samples[i] = Mathf.Clamp(sampler(t, i), -.9f, .9f);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private static float HashNoise(int i)
        {
            unchecked
            {
                uint x = (uint)(i * 747796405 + 2891336453);
                x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737;
                x = (x >> 22) ^ x;
                return (x / (float)uint.MaxValue) * 2f - 1f;
            }
        }

        private static float HissSample(float t, int i)
        {
            float white = HashNoise(i);
            float slow = HashNoise(i / 31) * .25f;
            return (white * .36f + slow * .12f) * (.82f + Mathf.Sin(t * .31f) * .08f);
        }

        private static float HumSample(float t, int i)
        {
            float mains = Mathf.Sin(t * Mathf.PI * 2f * 60f) * .24f;
            float harmonic = Mathf.Sin(t * Mathf.PI * 2f * 120f) * .08f;
            float ballast = Mathf.Sin(t * Mathf.PI * 2f * 238f + Mathf.Sin(t * .77f)) * .025f;
            return mains + harmonic + ballast + HashNoise(i / 17) * .012f;
        }

        private static float AirSample(float t, int i)
        {
            float noise = HashNoise(i / 5) * .21f + HashNoise(i / 29) * .12f;
            float breath = .72f + .18f * Mathf.Sin(t * .47f) + .08f * Mathf.Sin(t * 1.17f);
            return noise * breath + Mathf.Sin(t * Mathf.PI * 2f * 42f) * .04f;
        }

        private static float RumbleSample(float t, int i)
        {
            float fundamental = Mathf.Sin(t * Mathf.PI * 2f * 34f) * .22f;
            float second = Mathf.Sin(t * Mathf.PI * 2f * 51f + .7f) * .11f;
            float pulse = .7f + .2f * Mathf.Sin(t * .53f);
            return (fundamental + second) * pulse + HashNoise(i / 53) * .025f;
        }

        private static float MetalCreakSample(float t, int i)
        {
            float env = Mathf.Sin(Mathf.Clamp01(t / 1.65f) * Mathf.PI);
            float bend = 620f - t * 175f + Mathf.Sin(t * 8f) * 35f;
            float tone = Mathf.Sin(t * Mathf.PI * 2f * bend) * .19f;
            float scrape = HashNoise(i / 3) * .11f;
            return (tone + scrape) * env * .55f;
        }

        private static float DistantThumpSample(float t, int i)
        {
            float attack = Mathf.Clamp01(t / .012f);
            float decay = Mathf.Exp(-t * 8.5f);
            float body = Mathf.Sin(t * Mathf.PI * 2f * (72f - t * 18f)) * .56f;
            return body * attack * decay + HashNoise(i / 7) * .06f * decay;
        }

        private static float RelaySample(float t, int i)
        {
            float first = Mathf.Exp(-t * 42f) * HashNoise(i) * .38f;
            float secondTime = t - .115f;
            float second = secondTime > 0 ? Mathf.Exp(-secondTime * 55f) * HashNoise(i * 3 + 17) * .25f : 0f;
            return first + second;
        }
    }
}
