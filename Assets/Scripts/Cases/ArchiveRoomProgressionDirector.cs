using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archive0317
{
    /// <summary>
    /// Persistent visual evolution for ArchiveRoom after CASE 001.
    /// Uses existing case progress only; no gameplay gate depends on these cosmetic changes.
    /// </summary>
    public sealed class ArchiveRoomProgressionDirector : MonoBehaviour
    {
        private CaseDefinition definition;
        private CaseProgress progress;
        private GameObject archivedCase;
        private GameObject nextCaseSlot;
        private Light changedLight;
        private float baseLightIntensity;
        private bool completionApplied;
        private bool teaserApplied;
        private float nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (SceneManager.GetActiveScene().name != "ArchiveRoom") return;
            if (Object.FindFirstObjectByType<ArchiveRoomProgressionDirector>() != null) return;
            new GameObject("ArchiveRoomProgressionDirector").AddComponent<ArchiveRoomProgressionDirector>();
        }

        private void Start()
        {
            var file = Object.FindFirstObjectByType<CaseFile>();
            definition = file != null ? file.Definition : null;
            if (definition == null) return;

            progress = CaseProgressStore.Get(definition);
            BuildPersistentProps(file);
            ApplyCurrentState(false);
        }

        private void Update()
        {
            if (definition == null || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .35f;
            progress = CaseProgressStore.Get(definition);
            ApplyCurrentState(true);
        }

        private void BuildPersistentProps(CaseFile file)
        {
            Transform case00 = FindTransform("CASE 00");
            Vector3 archiveAnchor = case00 != null
                ? case00.position + new Vector3(-.53f, -.02f, 0f)
                : new Vector3(2.1f, 1.4f, 3.77f);

            archivedCase = CreateFolder(
                "CASE001_ArchivedShelfCopy",
                archiveAnchor,
                new Vector3(.38f, .37f, .2f),
                "CASE 001\nARCHIVED",
                file != null ? file.gameObject : null);
            archivedCase.SetActive(false);

            Vector3 slotAnchor = case00 != null
                ? case00.position + new Vector3(.54f, 0f, 0f)
                : new Vector3(3.15f, 1.42f, 3.77f);

            nextCaseSlot = CreateSealedSlot(slotAnchor);
            nextCaseSlot.SetActive(false);

            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(l => l.name == "ColdLight")
                .OrderByDescending(l => l.transform.position.z)
                .ToArray();
            if (lights.Length > 0)
            {
                changedLight = lights[0];
                baseLightIntensity = changedLight.intensity;
            }
        }

        private void ApplyCurrentState(bool allowCue)
        {
            if (progress == null) return;

            bool completed = progress.Has("Case001Completed") || progress.Has("CaseCompleted");
            if (completed && !completionApplied)
            {
                completionApplied = true;
                ApplyCompletedRoom();
                if (allowCue) StartCoroutine(CompletionLightSettling());
            }

            bool case00Seen = progress.Has("Case00Inspected");
            if (completed && case00Seen && !teaserApplied)
            {
                teaserApplied = true;
                if (nextCaseSlot != null) nextCaseSlot.SetActive(true);
            }
        }

        private void ApplyCompletedRoom()
        {
            if (archivedCase != null) archivedCase.SetActive(true);

            if (changedLight != null)
            {
                changedLight.color = new Color(.55f, .69f, .64f);
                changedLight.intensity = Mathf.Min(baseLightIntensity * .58f, changedLight.intensity);
            }

            Transform clock = FindTransform("WallClock");
            if (clock != null)
            {
                var labels = clock.GetComponentsInChildren<TextMesh>(true);
                foreach (var label in labels)
                {
                    if (label.name.IndexOf("Label", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        label.text.Contains(":"))
                        label.text = "03:17";
                }

                foreach (var child in clock.GetComponentsInChildren<Transform>(true))
                {
                    string n = child.name.ToLowerInvariant();
                    if (n.Contains("minute") && n.Contains("hand")) child.localRotation = Quaternion.Euler(0f, 0f, -102f);
                    else if (n.Contains("hour") && n.Contains("hand")) child.localRotation = Quaternion.Euler(0f, 0f, -8.5f);
                }
            }

            Transform archiveSign = FindTransform("ArchiveSign");
            if (archiveSign != null)
            {
                var sign = archiveSign.GetComponent<TextMesh>();
                if (sign != null && !sign.text.Contains("001"))
                    sign.text += "\nCASE 001 / ARCHIVED";
            }
        }

        private IEnumerator CompletionLightSettling()
        {
            if (changedLight == null) yield break;
            float target = changedLight.intensity;

            for (int i = 0; i < 4; i++)
            {
                changedLight.intensity = i == 1 ? target * .08f : target * .32f;
                yield return new WaitForSecondsRealtime(.08f + i * .015f);
                changedLight.intensity = target;
                yield return new WaitForSecondsRealtime(.07f);
            }
        }

        private GameObject CreateFolder(string name, Vector3 position, Vector3 scale, string text, GameObject source)
        {
            var folder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            folder.name = name;
            folder.transform.position = position;
            folder.transform.localScale = scale;
            var collider = folder.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var renderer = folder.GetComponent<Renderer>();
            Material sourceMaterial = source != null ? source.GetComponentsInChildren<Renderer>(true)
                .Select(r => r.sharedMaterial).FirstOrDefault(m => m != null) : null;
            if (sourceMaterial != null) renderer.sharedMaterial = sourceMaterial;
            else
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(.39f, .35f, .23f));
                renderer.material = material;
            }

            var labelGO = new GameObject("ArchiveSpineLabel");
            labelGO.transform.SetParent(folder.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, .01f, -.515f);
            labelGO.transform.localRotation = Quaternion.identity;
            var label = labelGO.AddComponent<TextMesh>();
            CopyFont(label);
            label.text = text;
            label.fontSize = 64;
            label.characterSize = .055f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(.72f, .72f, .63f);
            return folder;
        }

        private GameObject CreateSealedSlot(Vector3 position)
        {
            var root = new GameObject("CASE002_ReservedSlot");
            root.transform.position = position;

            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "EmptySlotBacking";
            backing.transform.SetParent(root.transform, false);
            backing.transform.localPosition = Vector3.zero;
            backing.transform.localScale = new Vector3(.43f, .4f, .045f);
            var collider = backing.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(.045f, .052f, .048f));
            material.SetFloat("_Metallic", .15f);
            material.SetFloat("_Smoothness", .08f);
            backing.GetComponent<Renderer>().material = material;

            var labelGO = new GameObject("ReservedSlotLabel");
            labelGO.transform.SetParent(root.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, .02f, -.026f);
            var label = labelGO.AddComponent<TextMesh>();
            CopyFont(label);
            label.text = "CASE 002\nSEALED";
            label.fontSize = 64;
            label.characterSize = .017f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(.46f, .5f, .45f);

            var seal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seal.name = "RedSeal";
            seal.transform.SetParent(root.transform, false);
            seal.transform.localPosition = new Vector3(0f, -.13f, -.03f);
            seal.transform.localScale = new Vector3(.25f, .025f, .012f);
            var sealCollider = seal.GetComponent<Collider>();
            if (sealCollider != null) Destroy(sealCollider);
            var sealMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            sealMaterial.SetColor("_BaseColor", new Color(.32f, .065f, .05f));
            seal.GetComponent<Renderer>().material = sealMaterial;

            return root;
        }

        private static void CopyFont(TextMesh target)
        {
            var source = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.font != null);
            if (source == null) return;
            target.font = source.font;
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = source.GetComponent<MeshRenderer>().sharedMaterial;
        }

        private static Transform FindTransform(string name)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
        }
    }
}
