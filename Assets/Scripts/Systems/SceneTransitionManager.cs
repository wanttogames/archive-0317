using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Archive0317
{
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        private static SceneTransitionManager instance;
        private CanvasGroup fade;
        public static bool IsTransitioning => instance != null && instance.busy;
        private bool busy;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; }
        public static bool Begin(CaseDefinition definition)
        {
            if (definition == null || !Application.CanStreamedLevelBeLoaded(definition.FieldScene) || IsTransitioning) return false;
            if (instance == null)
            {
                var root = new GameObject("SceneTransitionManager");
                DontDestroyOnLoad(root);
                instance = root.AddComponent<SceneTransitionManager>();
                instance.CreateFade();
            }
            instance.StartCoroutine(instance.Travel(definition));
            return true;
        }
        private void CreateFade()
        {
            var canvasGO = new GameObject("SceneFade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            fade = canvasGO.GetComponent<CanvasGroup>(); fade.alpha = 0;
            var imageGO = new GameObject("Black", typeof(RectTransform), typeof(Image)); imageGO.transform.SetParent(canvasGO.transform, false);
            var rect = imageGO.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            imageGO.GetComponent<Image>().color = Color.black;
        }
        private IEnumerator Travel(CaseDefinition definition)
        {
            busy = true; fade.blocksRaycasts = true;
            CaseProgressStore.Mark(definition, "CaseStarted");
            yield return Fade(0, 1);
            var operation = SceneManager.LoadSceneAsync(definition.FieldScene, LoadSceneMode.Single);
            while (operation != null && !operation.isDone) yield return null;
            yield return null;
            yield return Fade(1, 0);
            fade.blocksRaycasts = false; busy = false;
        }
        private IEnumerator Fade(float start, float end)
        {
            for (float elapsed = 0; elapsed < .45f; elapsed += Time.unscaledDeltaTime)
            { fade.alpha = Mathf.Lerp(start, end, elapsed / .45f); yield return null; }
            fade.alpha = end;
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
