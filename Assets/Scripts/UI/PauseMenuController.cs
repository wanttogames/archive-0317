using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Archive0317
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        public static bool IsOpen => instance != null;
        private static PauseMenuController instance;

        private FirstPersonPlayer player;
        private CanvasGroup rootGroup;
        private Canvas gameplayCanvas;
        private GameObject settingsPanel;
        private GameObject mainConfirmPanel;
        private Font font;
        private float previousTimeScale;
        private bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static bool Open(FirstPersonPlayer controller)
        {
            if (instance != null || controller == null || MainMenuController.IsMenuOpen || SceneTransitionManager.IsTransitioning) return false;
            var host = new GameObject("PauseMenuController");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<PauseMenuController>();
            instance.Build(controller);
            return true;
        }

        public static void CloseIfOpen()
        {
            if (instance != null) instance.Resume();
        }

        private void Build(FirstPersonPlayer controller)
        {
            player = controller;
            gameplayCanvas = player.HUD != null ? player.HUD.GetComponent<Canvas>() : null;
            if (gameplayCanvas != null) gameplayCanvas.enabled = false;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            player.SetCapture(false);

            font = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.font).FirstOrDefault(f => f != null);
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans CJK KR", "Malgun Gothic", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("PauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            canvas.pixelPerfect = true;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            rootGroup = canvasObject.GetComponent<CanvasGroup>();
            rootGroup.alpha = 0f;

            Stretch(Panel("Backdrop", canvasObject.transform, new Color(.004f, .006f, .006f, .72f)).GetComponent<RectTransform>());

            var card = Panel("PauseCard", canvasObject.transform, new Color(.022f, .029f, .028f, .97f));
            Place(card.GetComponent<RectTransform>(), new Vector2(520, 650), new Vector2(-360, 0));

            var stripe = Panel("PauseStripe", card.transform, new Color(.52f, .14f, .11f, .9f));
            var stripeRect = stripe.GetComponent<RectTransform>();
            stripeRect.anchorMin = stripeRect.anchorMax = new Vector2(0, .5f);
            stripeRect.pivot = new Vector2(0, .5f);
            stripeRect.sizeDelta = new Vector2(5, 520);
            stripeRect.anchoredPosition = new Vector2(36, 0);

            var overline = Label("Overline", card.transform, "RECORDS DIVISION // SESSION HOLD", 14, new Color(.5f, .58f, .54f, 1), TextAnchor.MiddleLeft);
            Place(overline.rectTransform, new Vector2(400, 26), new Vector2(30, 244));
            overline.fontStyle = FontStyle.Bold;

            var title = Label("Title", card.transform, "일시정지", 38, new Color(.92f, .94f, .89f, 1), TextAnchor.MiddleLeft);
            Place(title.rectTransform, new Vector2(400, 54), new Vector2(30, 194));
            title.fontStyle = FontStyle.Bold;

            var scene = Label("Scene", card.transform, SceneLabel(), 15, new Color(.54f, .61f, .56f, 1), TextAnchor.MiddleLeft);
            Place(scene.rectTransform, new Vector2(400, 30), new Vector2(30, 150));

            var resume = MenuButton(card.transform, "계속", new Vector2(30, 72));
            var settings = MenuButton(card.transform, "설정", new Vector2(30, 4));
            var main = MenuButton(card.transform, "메인화면", new Vector2(30, -64));
            var quit = MenuButton(card.transform, "종료", new Vector2(30, -132));

            resume.onClick.AddListener(Resume);
            settings.onClick.AddListener(() => ShowSettings(true));
            main.onClick.AddListener(ConfirmMainMenu);
            quit.onClick.AddListener(QuitGame);

            var help = Label("Help", card.transform, "ESC 계속   ·   방향키 선택   ·   ENTER 확인", 13, new Color(.4f, .46f, .42f, 1), TextAnchor.MiddleLeft);
            Place(help.rectTransform, new Vector2(400, 26), new Vector2(30, -226));

            BuildSettings(canvasObject.transform);
            BuildMainConfirmation(canvasObject.transform);
            EnsureEventSystem();

            resume.Select();
            InteractionSoundscape.PlayUIBack();
            StartCoroutine(Fade(0f, 1f, .14f));
        }

        private void Update()
        {
            if (busy) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (settingsPanel != null && settingsPanel.activeSelf) ShowSettings(false);
                else if (mainConfirmPanel != null && mainConfirmPanel.activeSelf)
                {
                    mainConfirmPanel.SetActive(false);
                    FindPauseButton("계속")?.Select();
                }
                else Resume();
            }
        }

        private string SceneLabel()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "Case001_Motel") return "CASE 001 / 현장 조사";
            if (scene == "ArchiveRoom") return "ARCHIVE / 기록보관실";
            return scene;
        }

        private void BuildSettings(Transform parent)
        {
            settingsPanel = Panel("PauseSettings", parent, new Color(.022f, .029f, .028f, .99f));
            Place(settingsPanel.GetComponent<RectTransform>(), new Vector2(520, 430), new Vector2(310, 0));

            var heading = Label("SettingsTitle", settingsPanel.transform, "설정", 31, new Color(.92f, .94f, .89f, 1), TextAnchor.MiddleLeft);
            Place(heading.rectTransform, new Vector2(420, 48), new Vector2(0, 150));
            heading.fontStyle = FontStyle.Bold;

            var volumeLabel = Label("VolumeLabel", settingsPanel.transform, "전체 음량", 19, new Color(.74f, .79f, .74f, 1), TextAnchor.MiddleLeft);
            Place(volumeLabel.rectTransform, new Vector2(180, 32), new Vector2(-120, 75));
            var volume = CreateSlider(settingsPanel.transform, new Vector2(82, 75), 0f, 1f, PlayerPrefs.GetFloat(MainMenuController.VolumeKey, 1f));
            volume.onValueChanged.AddListener(value =>
            {
                AudioListener.volume = value;
                PlayerPrefs.SetFloat(MainMenuController.VolumeKey, value);
                PlayerPrefs.Save();
            });

            var sensitivityLabel = Label("SensitivityLabel", settingsPanel.transform, "마우스 감도", 19, new Color(.74f, .79f, .74f, 1), TextAnchor.MiddleLeft);
            Place(sensitivityLabel.rectTransform, new Vector2(180, 32), new Vector2(-120, 5));
            var sensitivity = CreateSlider(settingsPanel.transform, new Vector2(82, 5), .04f, .2f, PlayerPrefs.GetFloat(MainMenuController.SensitivityKey, .09f));
            sensitivity.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetFloat(MainMenuController.SensitivityKey, value);
                PlayerPrefs.Save();
                if (player != null) player.ApplyPreferences();
            });

            var hint = Label("SettingsHint", settingsPanel.transform, "설정은 즉시 저장됩니다. ESC로 일시정지 화면으로 돌아갑니다.", 15,
                new Color(.5f, .57f, .53f, 1), TextAnchor.UpperLeft);
            Place(hint.rectTransform, new Vector2(420, 60), new Vector2(0, -62));

            var back = SmallButton(settingsPanel.transform, "돌아가기", new Vector2(0, -150), new Vector2(220, 48));
            back.onClick.AddListener(() => ShowSettings(false));
            settingsPanel.SetActive(false);
        }

        private void BuildMainConfirmation(Transform parent)
        {
            mainConfirmPanel = Panel("ReturnToMainConfirm", parent, new Color(.018f, .023f, .022f, .995f));
            Place(mainConfirmPanel.GetComponent<RectTransform>(), new Vector2(570, 310), new Vector2(310, 0));

            var heading = Label("ConfirmTitle", mainConfirmPanel.transform, "메인화면으로 돌아갑니까?", 25, new Color(.93f, .94f, .89f, 1), TextAnchor.MiddleCenter);
            Place(heading.rectTransform, new Vector2(490, 45), new Vector2(0, 86));
            heading.fontStyle = FontStyle.Bold;

            var body = Label("ConfirmBody", mainConfirmPanel.transform,
                "현재까지 자동 저장된 조사 기록은 유지됩니다.\n마지막 저장 이후의 미저장 상태는 복원되지 않을 수 있습니다.",
                16, new Color(.68f, .72f, .68f, 1), TextAnchor.MiddleCenter);
            Place(body.rectTransform, new Vector2(490, 76), new Vector2(0, 25));

            var cancel = SmallButton(mainConfirmPanel.transform, "취소", new Vector2(-120, -84), new Vector2(200, 48));
            var confirm = SmallButton(mainConfirmPanel.transform, "메인화면", new Vector2(120, -84), new Vector2(200, 48));
            cancel.onClick.AddListener(() =>
            {
                mainConfirmPanel.SetActive(false);
                FindPauseButton("계속")?.Select();
            });
            confirm.onClick.AddListener(ReturnToMainMenu);
            mainConfirmPanel.SetActive(false);
        }

        private void ConfirmMainMenu()
        {
            if (busy) return;
            settingsPanel.SetActive(false);
            mainConfirmPanel.SetActive(true);
            mainConfirmPanel.GetComponentsInChildren<Button>(true).FirstOrDefault()?.Select();
        }

        private void ShowSettings(bool visible)
        {
            if (busy) return;
            mainConfirmPanel.SetActive(false);
            settingsPanel.SetActive(visible);
            if (visible) settingsPanel.GetComponentInChildren<Slider>(true)?.Select();
            else FindPauseButton("계속")?.Select();
        }

        private void Resume()
        {
            if (busy) return;
            busy = true;
            InteractionSoundscape.PlayUIBack();
            StartCoroutine(ResumeRoutine());
        }

        private IEnumerator ResumeRoutine()
        {
            yield return Fade(rootGroup.alpha, 0f, .1f);
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            if (gameplayCanvas != null) gameplayCanvas.enabled = true;
            if (player != null) player.SetCapture(true);
            Destroy(gameObject);
        }

        private void ReturnToMainMenu()
        {
            if (busy) return;
            busy = true;
            PlayerPrefs.Save();
            StartCoroutine(ReturnToMainRoutine());
        }

        private IEnumerator ReturnToMainRoutine()
        {
            yield return Fade(rootGroup.alpha, 0f, .14f);
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            var operation = SceneManager.LoadSceneAsync("ArchiveRoom", LoadSceneMode.Single);
            while (operation != null && !operation.isDone) yield return null;
            yield return null;
            MainMenuController.PresentFromPause();
            Destroy(gameObject);
        }

        private void QuitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            Debug.Log("Quit requested from pause menu. Application.Quit is ignored in the Unity Editor.");
#else
            Application.Quit();
#endif
        }

        private Button FindPauseButton(string name)
        {
            return GetComponentsInChildren<Button>(true).FirstOrDefault(button => button.name == name);
        }

        private Button MenuButton(Transform parent, string caption, Vector2 position)
        {
            var button = SmallButton(parent, caption, position, new Vector2(390, 54));
            button.name = caption;
            return button;
        }

        private Button SmallButton(Transform parent, string caption, Vector2 position, Vector2 size)
        {
            var go = new GameObject("SmallButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), size, position);
            go.GetComponent<Image>().color = new Color(.095f, .12f, .116f, .98f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(InteractionSoundscape.PlayUIClick);

            var text = Label("Label", go.transform, caption, 20, new Color(.87f, .9f, .85f, 1), TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 10);
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        private Slider CreateSlider(Transform parent, Vector2 position, float min, float max, float value)
        {
            var root = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            Place(root.GetComponent<RectTransform>(), new Vector2(220, 32), position);

            var background = Panel("Background", root.transform, new Color(.14f, .17f, .16f, 1));
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, .5f); bgRect.anchorMax = new Vector2(1, .5f);
            bgRect.sizeDelta = new Vector2(0, 5); bgRect.anchoredPosition = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, .5f); fillAreaRect.anchorMax = new Vector2(1, .5f);
            fillAreaRect.offsetMin = new Vector2(0, -3); fillAreaRect.offsetMax = new Vector2(-12, 3);
            var fill = Panel("Fill", fillArea.transform, new Color(.52f, .62f, .55f, 1));
            Stretch(fill.GetComponent<RectTransform>());

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>(), 6);
            var handle = Panel("Handle", handleArea.transform, new Color(.88f, .9f, .84f, 1));
            Place(handle.GetComponent<RectTransform>(), new Vector2(12, 24), Vector2.zero);

            var slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = Mathf.Clamp(value, min, max);
            return slider;
        }

        private GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return go;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .9f);
            shadow.effectDistance = new Vector2(1, -1);
            return text;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                rootGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            rootGroup.alpha = to;
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private void OnDestroy()
        {
            if (!busy && gameplayCanvas != null) gameplayCanvas.enabled = true;
            if (instance == this) instance = null;
        }
    }
}
