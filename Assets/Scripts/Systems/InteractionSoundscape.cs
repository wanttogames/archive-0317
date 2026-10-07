using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Archive0317
{
    /// <summary>
    /// Shared procedural interaction/spatial audio layer.
    /// Keeps authored narrative cues intact and adds restrained tactile feedback around them.
    /// </summary>
    public sealed class InteractionSoundscape : MonoBehaviour
    {
        private static InteractionSoundscape instance;
        private static int footstepIndex;

        private AudioSource uiSource;
        private AudioClip stepHardA, stepHardB, stepSoftA, stepSoftB;
        private AudioClip inspectTap, paperOpen, paperPage, paperClose;
        private AudioClip doorLatch, doorCreak, lockedRattle;
        private AudioClip uiClick, uiBack;
        private AudioClip spareKey, evidenceKey, crtStatic;
        private float uiSuppressedUntil;
        public int SpareKeyPlays { get; private set; }
        public int EvidenceKeyPlays { get; private set; }
        public int RearKeyPlays { get; private set; }
        public float LastRearDelay { get; private set; }
        public Vector3 LastKeyRearPosition { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; footstepIndex = 0; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime() => Ensure();

        public static InteractionSoundscape Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("InteractionSoundscape");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<InteractionSoundscape>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.loop = false;
            uiSource.spatialBlend = 0f;
            uiSource.priority = 140;

            stepHardA = Build("StepHardA", .16f, StepHardSample, 11);
            stepHardB = Build("StepHardB", .17f, StepHardSample, 29);
            stepSoftA = Build("StepSoftA", .19f, StepSoftSample, 41);
            stepSoftB = Build("StepSoftB", .2f, StepSoftSample, 67);
            inspectTap = Build("InspectTap", .12f, InspectSample, 83);
            paperOpen = Build("PaperOpen", .24f, PaperOpenSample, 101);
            paperPage = Build("PaperPage", .19f, PaperPageSample, 131);
            paperClose = Build("PaperClose", .16f, PaperCloseSample, 151);
            doorLatch = Build("DoorLatch", .2f, DoorLatchSample, 173);
            doorCreak = Build("DoorCreak", .75f, DoorCreakSample, 197);
            lockedRattle = Build("LockedRattle", .34f, LockedSample, 211);
            uiClick = Build("UIClick", .08f, UiClickSample, 239);
            uiBack = Build("UIBack", .09f, UiBackSample, 257);
            spareKey=Build("SpareKeyRing",.36f,LightKeySample,281);
            evidenceKey=Build("EvidenceKeyRing",.5f,HeavyKeySample,307);
            crtStatic=Build("CRTStatic",.35f,(t,i,seed)=>Noise(i,seed)*Mathf.Sin(Mathf.PI*t/.35f)*.35f,331);
        }

        public static void PlayFootstep(Vector3 position, bool sprinting)
        {
            var sound = Ensure();
            bool soft = SceneManager.GetActiveScene().name == "Case001_Motel";
            AudioClip clip;
            if (soft) clip = (footstepIndex++ & 1) == 0 ? sound.stepSoftA : sound.stepSoftB;
            else clip = (footstepIndex++ & 1) == 0 ? sound.stepHardA : sound.stepHardB;
            sound.PlaySpatial(clip, position, sprinting ? .078f : .058f, sprinting ? 1.03f : .97f, 4.2f);
        }

        public static void PlayInspect(Vector3 position)
            => Ensure().PlaySpatial(Ensure().inspectTap, position, .035f, Random.Range(.96f, 1.04f), 3f);

        public static void PlayCRTStatic(Vector3 position)
            => Ensure().PlaySpatial(Ensure().crtStatic, position, .035f, 1f, 3f);

        private void OnDestroy(){if(crtStatic!=null)Destroy(crtStatic);if(instance==this)instance=null;}

        public static void PlayDoorLatch(Vector3 position)
            => Ensure().PlaySpatial(Ensure().doorLatch, position, .08f, Random.Range(.96f, 1.03f), 7f);

        public static void PlayDoorCreak(Vector3 position)
            => Ensure().PlaySpatial(Ensure().doorCreak, position, .075f, Random.Range(.94f, 1.04f), 8f);

        public static void PlayLockedDoor(Vector3 position)
            => Ensure().PlaySpatial(Ensure().lockedRattle, position, .065f, Random.Range(.96f, 1.03f), 6f);

        public static void PlayDocumentOpen()
            => Ensure().PlayUI(Ensure().paperOpen, .07f, Random.Range(.98f, 1.02f));

        public static void PlayDocumentPage()
            => Ensure().PlayUI(Ensure().paperPage, .065f, Random.Range(.97f, 1.03f));

        public static void PlayDocumentClose()
            => Ensure().PlayUI(Ensure().paperClose, .055f, Random.Range(.98f, 1.02f));

        public static void PlayUIClick()
            => Ensure().PlayUI(Ensure().uiClick, .045f, Random.Range(.98f, 1.03f));

        public static void PlayUIBack()
            => Ensure().PlayUI(Ensure().uiBack, .04f, .98f);

        public static void PlaySpareKey(Vector3 position)
        {
            var sound=Ensure();sound.SpareKeyPlays++;sound.QuietenUI(.4f);
            sound.PlaySpatial(sound.spareKey,position,.10f,1f,5f);
        }
        public static void PlayKeyEvidence(Transform view)
        {
            if(view==null)return;
            var sound=Ensure();sound.EvidenceKeyPlays++;sound.QuietenUI(.9f);
            sound.PlaySpatial(sound.evidenceKey,view.position+view.forward*.35f-Vector3.up*.25f,.13f,.9f,5f);
            var back=-Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;
            if(back.sqrMagnitude<.1f)back=-view.parent.forward;
            var rear=view.position+back*2.1f;
            if(Physics.Linecast(view.position,rear,out var hit,~0,QueryTriggerInteraction.Ignore))rear=hit.point-back*.12f;
            sound.StartCoroutine(sound.DelayedKeyRear(rear,SceneManager.GetActiveScene().handle));
        }
        private IEnumerator DelayedKeyRear(Vector3 position,int sceneHandle)
        {
            float started=Time.unscaledTime;yield return new WaitForSecondsRealtime(.4f);
            if(SceneManager.GetActiveScene().handle!=sceneHandle)yield break;
            LastRearDelay=Time.unscaledTime-started;LastKeyRearPosition=position;RearKeyPlays++;
            PlaySpatial(doorLatch,position,.06f,.78f,5f);
        }
        private void QuietenUI(float seconds)
        {uiSource.Stop();uiSuppressedUntil=Time.unscaledTime+seconds;}

        private void PlayUI(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || uiSource == null || Time.unscaledTime<uiSuppressedUntil) return;
            uiSource.pitch = pitch;
            uiSource.PlayOneShot(clip, volume);
        }

        private void PlaySpatial(AudioClip clip, Vector3 position, float volume, float pitch, float maxDistance)
        {
            if (clip == null) return;
            var go = new GameObject("SpatialOneShot_" + clip.name);
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = .55f;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
            source.spread = 35f;
            source.priority = 150;
            source.Play();
            Destroy(go, Mathf.Max(.3f, clip.length / Mathf.Max(.2f, Mathf.Abs(pitch)) + .12f));
        }

        private delegate float Sampler(float t, int i, int seed);

        private static AudioClip Build(string name, float seconds, Sampler sampler, int seed)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(seconds * rate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                samples[i] = Mathf.Clamp(sampler(t, i, seed), -.92f, .92f);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Noise(int i, int seed)
        {
            unchecked
            {
                uint x = (uint)(i * 747796405 + seed * 2891336453);
                x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737;
                x = (x >> 22) ^ x;
                return x / (float)uint.MaxValue * 2f - 1f;
            }
        }

        private static float StepHardSample(float t, int i, int seed)
        {
            float env = Mathf.Exp(-t * 26f);
            float sole = Mathf.Sin(t * Mathf.PI * 2f * (105f - t * 65f)) * .48f;
            float grit = Noise(i / 2, seed) * .16f;
            float heel = t < .045f ? Mathf.Sin(t * Mathf.PI * 2f * 245f) * .16f : 0f;
            return (sole + grit + heel) * env;
        }

        private static float StepSoftSample(float t, int i, int seed)
        {
            float env = Mathf.Exp(-t * 19f);
            float body = Mathf.Sin(t * Mathf.PI * 2f * (72f - t * 28f)) * .27f;
            float cloth = Noise(i / 5, seed) * .19f;
            return (body + cloth) * env;
        }

        private static float InspectSample(float t, int i, int seed)
        {
            float env = Mathf.Exp(-t * 34f);
            return (Mathf.Sin(t * Mathf.PI * 2f * 720f) * .16f + Noise(i, seed) * .09f) * env;
        }

        private static float PaperOpenSample(float t, int i, int seed)
        {
            float env = Mathf.Sin(Mathf.Clamp01(t / .24f) * Mathf.PI);
            float rustle = Noise(i / 3, seed) * .24f;
            float sweep = Mathf.Sin(t * Mathf.PI * 2f * (330f + t * 900f)) * .035f;
            return (rustle + sweep) * env;
        }

        private static float PaperPageSample(float t, int i, int seed)
        {
            float env = Mathf.Sin(Mathf.Clamp01(t / .19f) * Mathf.PI);
            float rustle = Noise(i / 2, seed) * .28f;
            return rustle * env * (.8f + .2f * Mathf.Sin(t * 42f));
        }

        private static float PaperCloseSample(float t, int i, int seed)
        {
            float env = Mathf.Exp(-t * 18f);
            return (Noise(i / 3, seed) * .18f + Mathf.Sin(t * Mathf.PI * 2f * 95f) * .11f) * env;
        }

        private static float DoorLatchSample(float t, int i, int seed)
        {
            float first = Mathf.Exp(-t * 48f) * (Noise(i, seed) * .26f + Mathf.Sin(t * Mathf.PI * 2f * 480f) * .17f);
            float delayed = t > .08f ? Mathf.Exp(-(t - .08f) * 65f) * Noise(i * 3, seed + 3) * .17f : 0f;
            return first + delayed;
        }

        private static float DoorCreakSample(float t, int i, int seed)
        {
            float env = Mathf.Sin(Mathf.Clamp01(t / .75f) * Mathf.PI);
            float freq = 410f - t * 160f + Mathf.Sin(t * 18f) * 45f;
            float tone = Mathf.Sin(t * Mathf.PI * 2f * freq) * .15f;
            float scrape = Noise(i / 4, seed) * .13f;
            return (tone + scrape) * env;
        }

        private static float LockedSample(float t, int i, int seed)
        {
            float a = Mathf.Exp(-t * 28f) * Noise(i, seed) * .25f;
            float bTime = t - .105f;
            float b = bTime > 0 ? Mathf.Exp(-bTime * 31f) * Noise(i * 2, seed + 7) * .22f : 0f;
            float metal = Mathf.Sin(t * Mathf.PI * 2f * 690f) * .055f * Mathf.Exp(-t * 9f);
            return a + b + metal;
        }

        private static float UiClickSample(float t, int i, int seed)
        {
            float env = Mathf.Exp(-t * 58f);
            return (Mathf.Sin(t * Mathf.PI * 2f * 980f) * .18f + Noise(i, seed) * .035f) * env;
        }
        private static float KeySample(float t,int i,int seed,bool heavy)
        {
            float value=0;
            for(int strike=0;strike<3;strike++)
            {
                float age=t-(strike==0?.005f:strike==1?.095f:.185f);if(age<0)continue;
                float attack=Mathf.Clamp01(age/.004f),decay=Mathf.Exp(-age*(heavy?15:23));
                float frequency=heavy?530:1250;
                float ring=Mathf.Sin(age*Mathf.PI*2*frequency)+.55f*Mathf.Sin(age*Mathf.PI*2*frequency*1.79f)+.25f*Mathf.Sin(age*Mathf.PI*2*frequency*2.93f);
                value+=(ring*.19f*decay+Noise(i,seed+strike)*.10f*Mathf.Exp(-age*95))*attack*(1-strike*.2f);
            }
            return value;
        }
        private static float LightKeySample(float t,int i,int seed)=>KeySample(t,i,seed,false);
        private static float HeavyKeySample(float t,int i,int seed)=>KeySample(t,i,seed,true);

        private static float UiBackSample(float t, int i, int seed)
        {
            float env = Mathf.Exp(-t * 45f);
            return Mathf.Sin(t * Mathf.PI * 2f * 520f) * .15f * env + Noise(i, seed) * .025f * env;
        }
    }
}
