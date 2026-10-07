using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archive0317
{
    /// <summary>
    /// Adds observation-driven horror beats to Room 403 without changing evidence or progression gates.
    /// All changes are cosmetic and are keyed off existing CASE 001 progress flags.
    /// </summary>
    public sealed class Room403HorrorDirector : MonoBehaviour
    {
        private CaseDefinition definition;
        private FirstPersonPlayer player;
        private Collider roomArea;
        private Light roomLight;
        private Light bathroomLight;
        private Light exitPreviewLight;
        private Transform picture;
        private Transform picturePrint;
        private Renderer mirrorRenderer;
        private AudioSource fluorescentHum;
        private AudioSource pipeTone;

        private GameObject shadowFigure;
        private Renderer shadowRenderer;
        private bool shadowArmed;
        private float shadowArmedAt;
        private float visibleSince = -1f;
        private float nextEvaluate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (SceneManager.GetActiveScene().name != "Case001_Motel") return;
            if (Object.FindFirstObjectByType<Room403HorrorDirector>() != null) return;
            if (FindTransform("Room403Interior") == null) return;
            new GameObject("Room403HorrorDirector").AddComponent<Room403HorrorDirector>();
        }

        private void Start()
        {
            var context = Object.FindFirstObjectByType<CaseSceneContext>();
            definition = context != null ? context.Definition : null;
            player = Object.FindFirstObjectByType<FirstPersonPlayer>();

            roomArea = FindComponent<Collider>("Room403Area");
            roomLight = FindComponent<Light>("Room403WarmFluorescent");
            bathroomLight = FindComponent<Light>("Room403BathroomLight");
            exitPreviewLight = FindComponent<Light>("ExitPreviewLight");
            picture = FindTransform("Room403Picture");
            picturePrint = FindTransform("PicturePrint");
            mirrorRenderer = FindComponent<Renderer>("Room403Mirror");
            fluorescentHum = FindComponent<AudioSource>("Room403FluorescentHum");
            pipeTone = FindComponent<AudioSource>("Room403PipeTone");

            CreateShadowFigure();
            RestorePersistentAtmosphere();
        }

        private void Update()
        {
            if (definition == null || player == null || player.HUD == null) return;
            if (PauseMenuController.IsOpen || MainMenuController.IsMenuOpen || SceneTransitionManager.IsTransitioning) return;
            if (Time.time < nextEvaluate) return;
            nextEvaluate = Time.time + .08f;

            var progress = CaseProgressStore.Get(definition);
            bool inside = IsInsideRoom();

            if (inside && progress.Has("RoomAltered") && !progress.Has("Room403AfterBathroomDisturbance"))
            {
                if (!IsVisible(picture != null ? picture.GetComponent<Renderer>() : null))
                {
                    DisturbRoomDetails();
                    CaseProgressStore.Mark(definition, "Room403AfterBathroomDisturbance");
                    StartCoroutine(FlickerRoom(.95f, 5));
                }
            }

            if (inside && progress.Has("TelevisionPowerOn") && !progress.Has("Room403TelevisionFlicker"))
            {
                CaseProgressStore.Mark(definition, "Room403TelevisionFlicker");
                StartCoroutine(FlickerRoom(1.45f, 8));
            }

            if (inside && progress.Has("TelevisionInspected") && !progress.Has("Room403ShadowSeen"))
            {
                if (!shadowArmed)
                {
                    shadowArmed = true;
                    shadowArmedAt = Time.time + 1.15f;
                }
                UpdateShadowEncounter();
            }
            else if (shadowFigure != null && shadowFigure.activeSelf)
            {
                shadowFigure.SetActive(false);
                visibleSince = -1f;
            }

            if (inside && progress.Has("KeyEvidenceFound") && !progress.Has("Room403AtmosphereCollapsed"))
            {
                if (!IsVisible(mirrorRenderer))
                {
                    CollapseAtmosphere();
                    CaseProgressStore.Mark(definition, "Room403AtmosphereCollapsed");
                }
            }

            if (inside && progress.Has("ReceiptChangedSeen") && progress.Has("Room403ExitOpened")
                && !progress.Has("Room403ExitLightPulse"))
            {
                CaseProgressStore.Mark(definition, "Room403ExitLightPulse");
                StartCoroutine(PulseExitLight());
            }
        }

        private void RestorePersistentAtmosphere()
        {
            if (definition == null) return;
            var progress = CaseProgressStore.Get(definition);

            if (progress.Has("Room403AfterBathroomDisturbance")) ApplyDisturbedDetails();
            if (progress.Has("Room403AtmosphereCollapsed")) ApplyCollapsedAtmosphere();
            if (shadowFigure != null) shadowFigure.SetActive(false);
        }

        private void DisturbRoomDetails()
        {
            ApplyDisturbedDetails();
        }

        private void ApplyDisturbedDetails()
        {
            if (picture != null)
            {
                var euler = picture.localEulerAngles;
                picture.localEulerAngles = new Vector3(euler.x + 7.5f, euler.y, euler.z);
            }
            if (picturePrint != null)
            {
                var euler = picturePrint.localEulerAngles;
                picturePrint.localEulerAngles = new Vector3(euler.x + 7.5f, euler.y, euler.z);
            }

            var slippers = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "GuestSlipper").ToArray();
            if (slippers.Length > 0) slippers[0].position += new Vector3(0f, 0f, .18f);
            if (slippers.Length > 1)
            {
                slippers[1].position += new Vector3(0f, 0f, -.12f);
                slippers[1].Rotate(Vector3.up, 18f, Space.World);
            }
        }

        private void CollapseAtmosphere()
        {
            ApplyCollapsedAtmosphere();
            StartCoroutine(FlickerRoom(.75f, 4));
            CreateMirrorBlackout();
        }

        private void ApplyCollapsedAtmosphere()
        {
            if (roomLight != null)
            {
                roomLight.color = new Color(.52f, .64f, .58f);
                roomLight.intensity = Mathf.Min(roomLight.intensity, 2.15f);
            }
            if (bathroomLight != null)
            {
                bathroomLight.color = new Color(.48f, .58f, .52f);
                bathroomLight.intensity = Mathf.Min(bathroomLight.intensity, .72f);
            }
            if (fluorescentHum != null) fluorescentHum.volume = Mathf.Min(fluorescentHum.volume, .011f);
            if (pipeTone != null) pipeTone.volume = Mathf.Min(pipeTone.volume, .007f);
            CreateMirrorBlackout();
        }

        private void CreateMirrorBlackout()
        {
            if (mirrorRenderer == null || FindTransform("Room403MirrorBlackout") != null) return;

            var overlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overlay.name = "Room403MirrorBlackout";
            overlay.transform.SetParent(mirrorRenderer.transform.parent, true);
            overlay.transform.position = mirrorRenderer.bounds.center + new Vector3(0f, 0f, -.022f);
            overlay.transform.rotation = mirrorRenderer.transform.rotation;
            overlay.transform.localScale = new Vector3(
                mirrorRenderer.transform.lossyScale.x * 1.02f,
                mirrorRenderer.transform.lossyScale.y * 1.02f,
                .012f);

            var collider = overlay.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.name = "Room403MirrorBlackout_Runtime";
            material.SetColor("_BaseColor", new Color(.008f, .012f, .011f, 1f));
            overlay.GetComponent<Renderer>().material = material;
        }

        private void CreateShadowFigure()
        {
            shadowFigure = new GameObject("Room403DoorwayShadow");

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "ShadowBody";
            body.transform.SetParent(shadowFigure.transform, false);
            body.transform.localPosition = new Vector3(0f, .85f, 0f);
            body.transform.localScale = new Vector3(.42f, .86f, .32f);
            var bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) Destroy(bodyCollider);

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.name = "Room403Shadow_Runtime";
            material.SetColor("_BaseColor", new Color(.003f, .004f, .004f, 1f));
            shadowRenderer = body.GetComponent<Renderer>();
            shadowRenderer.material = material;

            shadowFigure.transform.position = new Vector3(-17.72f, 9.02f, 4.5f);
            shadowFigure.SetActive(false);
        }

        private void UpdateShadowEncounter()
        {
            if (shadowFigure == null || Time.time < shadowArmedAt) return;

            if (!shadowFigure.activeSelf)
            {
                var doorwayDirection = (shadowFigure.transform.position + Vector3.up - player.ViewCamera.transform.position).normalized;
                float facing = Vector3.Dot(player.ViewCamera.transform.forward, doorwayDirection);
                if (facing < .05f)
                {
                    shadowFigure.SetActive(true);
                    visibleSince = -1f;
                }
                return;
            }

            if (IsVisible(shadowRenderer))
            {
                if (visibleSince < 0f) visibleSince = Time.time;
                if (Time.time - visibleSince > .16f)
                {
                    shadowFigure.SetActive(false);
                    CaseProgressStore.Mark(definition, "Room403ShadowSeen");
                    StartCoroutine(FlickerRoom(.32f, 2));
                    player.HUD.ShowToast("문가에 있던 것이 사라졌다.", 2.2f);
                }
            }
            else visibleSince = -1f;
        }

        private IEnumerator FlickerRoom(float duration, int pulses)
        {
            if (roomLight == null) yield break;

            float baseIntensity = roomLight.intensity;
            bool baseEnabled = roomLight.enabled;
            for (int i = 0; i < pulses; i++)
            {
                roomLight.enabled = true;
                roomLight.intensity = baseIntensity * (i % 3 == 0 ? .18f : .48f);
                yield return new WaitForSeconds(duration / (pulses * 2f));
                roomLight.intensity = baseIntensity;
                yield return new WaitForSeconds(duration / (pulses * 2f));
            }
            roomLight.enabled = baseEnabled;
            roomLight.intensity = baseIntensity;
        }

        private IEnumerator PulseExitLight()
        {
            if (exitPreviewLight == null) yield break;

            float baseIntensity = exitPreviewLight.intensity;
            Color baseColor = exitPreviewLight.color;
            exitPreviewLight.color = new Color(.33f, .42f, .39f);

            for (int i = 0; i < 3; i++)
            {
                exitPreviewLight.intensity = i == 1 ? .05f : baseIntensity * .28f;
                yield return new WaitForSeconds(.11f);
                exitPreviewLight.intensity = baseIntensity;
                yield return new WaitForSeconds(.08f);
            }

            exitPreviewLight.color = baseColor;
            exitPreviewLight.intensity = baseIntensity;
        }

        private bool IsInsideRoom()
        {
            if (roomArea != null) return roomArea.bounds.Contains(player.transform.position + Vector3.up * .8f);
            var p = player.transform.position;
            return p.x < -17.7f && p.x > -22.5f && p.y > 8.5f && p.z > 2.1f && p.z < 6.9f;
        }

        private bool IsVisible(Renderer target)
        {
            if (target == null || player == null || player.ViewCamera == null || !target.gameObject.activeInHierarchy) return false;
            var camera = player.ViewCamera;
            if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), target.bounds)) return false;

            Vector3 origin = camera.transform.position;
            Vector3 destination = target.bounds.center;
            Vector3 direction = destination - origin;
            if (Physics.Raycast(origin, direction.normalized, out var hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider == null || hit.transform == target.transform || hit.transform.IsChildOf(target.transform);
            return true;
        }

        private static Transform FindTransform(string name)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
        }

        private static T FindComponent<T>(string name) where T : Component
        {
            var transform = FindTransform(name);
            return transform != null ? transform.GetComponent<T>() : null;
        }

        private void OnDestroy()
        {
            if (shadowFigure != null) Destroy(shadowFigure);
        }
    }
}
