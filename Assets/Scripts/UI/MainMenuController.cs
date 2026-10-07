using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Archive0317
{
    /// <summary>
    /// Runtime-built title screen. ArchiveRoom remains the first build scene and supplies the
    /// moving/atmospheric background, while this overlay owns first-launch navigation.
    /// Returning to ArchiveRoom from a case does not reopen the title screen in the same session.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public const string SaveKey = "Archive0317.Case.case001";
        public const string VolumeKey = "Archive0317.Settings.MasterVolume";
        public const string SensitivityKey = "Archive0317.Settings.MouseSensitivity";

        private static bool presentedThisSession;
        public static bool IsMenuOpen { get; private set; }
        private CanvasGroup rootGroup;
        private GameObject settingsPanel;
        private GameObject confirmPanel;
        private Button newGameButton;
        private Button continueButton;
        private FirstPersonPlayer player;
        private Canvas gameplayCanvas;
        private Font font;
        private Camera menuCamera;
        private Vector3 menuCameraBasePosition;
        private Quaternion menuCameraBaseRotation;
        private Text titleLabel;
        private Vector2 titleBasePosition;
        private Image accessStripeImage;
        private RectTransform accessStripeRect;
        private Vector2 stripeBasePosition;
        private Selectable lastSelected;
        private float selectionGlitchUntil;
        private float priorTimeScale = 1f;
        private bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { presentedThisSession = false; IsMenuOpen = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PresentOnFirstArchiveLoad()
        {
            if (presentedThisSession || SceneManager.GetActiveScene().name != "ArchiveRoom") return;
            presentedThisSession = true;
            var host = new GameObject("MainMenuController");
            DontDestroyOnLoad(host);
            host.AddComponent<MainMenuController>().Build();
        }

        private void Build()
        {
            IsMenuOpen = true;
            player = Object.FindFirstObjectByType<FirstPersonPlayer>();
            if (player != null)
            {
                gameplayCanvas = player.GetComponentInChildren<Canvas>(true);
                menuCamera = player.ViewCamera;
                if (menuCamera != null)
                {
                    menuCameraBasePosition = menuCamera.transform.localPosition;
                    menuCameraBaseRotation = menuCamera.transform.localRotation;
                }
                player.enabled = false;
            }
            if (gameplayCanvas != null) gameplayCanvas.enabled = false;

            priorTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            font = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.font).FirstOrDefault(f => f != null);
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans CJK KR", "Malgun Gothic", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            canvas.pixelPerfect = true;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            rootGroup = canvasObject.GetComponent<CanvasGroup>();
            rootGroup.alpha = 0f;

            Stretch(Panel("Backdrop", canvasObject.transform, new Color(.005f, .008f, .009f, .73f)).GetComponent<RectTransform>());
            var shade = Panel("LeftShade", canvasObject.transform, new Color(.018f, .024f, .023f, .94f));
            var shadeRect = shade.GetComponent<RectTransform>();
            shadeRect.anchorMin = new Vector2(0, 0);
            shadeRect.anchorMax = new Vector2(0, 1);
            shadeRect.pivot = new Vector2(0, .5f);
            shadeRect.sizeDelta = new Vector2(690, 0);
            shadeRect.anchoredPosition = Vector2.zero;

            // Restrained VHS texture: thin static scan lines live behind all readable menu text.
            for (int i = 0; i < 24; i++)
            {
                var line = Panel("Scanline", canvasObject.transform, new Color(.65f, .78f, .7f, .025f));
                var rect = line.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, .5f);
                rect.anchorMax = new Vector2(1, .5f);
                rect.sizeDelta = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(0, -430 + i * 38);
            }

            var accent = Panel("AccessStripe", canvasObject.transform, new Color(.55f, .15f, .12f, .82f));
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = accentRect.anchorMax = new Vector2(0, .5f);
            accentRect.pivot = new Vector2(0, .5f);
            accentRect.sizeDelta = new Vector2(5, 650);
            accentRect.anchoredPosition = new Vector2(92, 5);
            accessStripeImage = accent.GetComponent<Image>();
            accessStripeRect = accentRect;
            stripeBasePosition = accentRect.anchoredPosition;

            var terminal = Label("Terminal", canvasObject.transform, "RECORDS DIVISION // ACCESS TERMINAL", 17, new Color(.55f, .65f, .61f, 1), TextAnchor.MiddleLeft);
            Place(terminal.rectTransform, new Vector2(480, 32), new Vector2(-504, 300));
            terminal.fontStyle = FontStyle.Bold;

            var title = Label("Title", canvasObject.transform, "ARCHIVE 03:17", 54, new Color(.91f, .93f, .87f, 1), TextAnchor.MiddleLeft);
            Place(title.rectTransform, new Vector2(520, 70), new Vector2(-484, 235));
            title.fontStyle = FontStyle.Bold;
            titleLabel = title;
            titleBasePosition = title.rectTransform.anchoredPosition;

            var korean = Label("KoreanTitle", canvasObject.transform, "기록보관실", 27, new Color(.72f, .76f, .7f, 1), TextAnchor.MiddleLeft);
            Place(korean.rectTransform, new Vector2(420, 42), new Vector2(-484, 184));

            var rule = Panel("Rule", canvasObject.transform, new Color(.45f, .53f, .48f, .5f));
            Place(rule.GetComponent<RectTransform>(), new Vector2(410, 1), new Vector2(-539, 146));

            newGameButton = MenuButton(canvasObject.transform, "새 게임", new Vector2(-476, 76));
            continueButton = MenuButton(canvasObject.transform, "이어하기", new Vector2(-476, 10));
            var settings = MenuButton(canvasObject.transform, "설정", new Vector2(-476, -56));
            var quit = MenuButton(canvasObject.transform, "종료", new Vector2(-476, -122));

            newGameButton.onClick.AddListener(RequestNewGame);
            continueButton.onClick.AddListener(ContinueGame);
            settings.onClick.AddListener(() => ShowSettings(true));
            quit.onClick.AddListener(QuitGame);

            bool hasSave = PlayerPrefs.HasKey(SaveKey);
            var savedProgress = ReadSavedProgress();
            continueButton.interactable = hasSave;
            if (!hasSave)
            {
                var label = continueButton.GetComponentInChildren<Text>();
                label.color = new Color(.38f, .42f, .4f, 1);
            }

            var note = Label("Hint", canvasObject.transform,
                hasSave ? DescribeProgress(savedProgress) : "보관된 조사 기록이 없습니다.",
                16, new Color(.5f, .58f, .54f, 1), TextAnchor.MiddleLeft);
            Place(note.rectTransform, new Vector2(440, 28), new Vector2(-476, -181));

            BuildCaseStatus(canvasObject.transform, savedProgress, hasSave);

            var warning = Label("Warning", canvasObject.transform,
                "본 게임은 저조도 화면과 순간적인 시각·청각 연출을 포함합니다.",
                14, new Color(.43f, .47f, .44f, 1), TextAnchor.MiddleLeft);
            Place(warning.rectTransform, new Vector2(540, 28), new Vector2(-426, -335));

            var version = Label("Version", canvasObject.transform, "CASE ARCHIVE BUILD 001", 13, new Color(.35f, .4f, .37f, 1), TextAnchor.MiddleLeft);
            Place(version.rectTransform, new Vector2(300, 24), new Vector2(-546, -404));

            BuildSettings(canvasObject.transform);
            BuildConfirmation(canvasObject.transform);
            EnsureEventSystem();

            (continueButton.interactable ? continueButton : newGameButton).Select();
            StartCoroutine(FadeMenu(0f, 1f, .3f));
        }

        private void Update()
        {
            if (!IsMenuOpen || busy) return;

            if (menuCamera != null)
            {
                float t = Time.unscaledTime;
                menuCamera.transform.localPosition = menuCameraBasePosition + new Vector3(
                    Mathf.Sin(t * .19f) * .012f,
                    Mathf.Sin(t * .13f + 1.1f) * .006f,
                    0f);
                menuCamera.transform.localRotation = menuCameraBaseRotation * Quaternion.Euler(
                    Mathf.Sin(t * .17f) * .12f,
                    Mathf.Sin(t * .11f + .7f) * .22f,
                    0f);
            }

            var selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>() : null;
            if (selected != null && selected != lastSelected)
            {
                lastSelected = selected;
                selectionGlitchUntil = Time.unscaledTime + .08f;
            }

            if (titleLabel != null)
            {
                bool glitching = Time.unscaledTime < selectionGlitchUntil;
                float offset = glitching ? Mathf.Sin(Time.unscaledTime * 210f) * 2.1f : 0f;
                titleLabel.rectTransform.anchoredPosition = titleBasePosition + new Vector2(offset, 0f);
                if (accessStripeRect != null) accessStripeRect.anchoredPosition = stripeBasePosition + new Vector2(glitching ? -offset * .6f : 0f, 0f);
                if (accessStripeImage != null)
                {
                    var color = accessStripeImage.color;
                    color.a = glitching ? .98f : .82f;
                    accessStripeImage.color = color;
                }
            }
        }

        private void BuildCaseStatus(Transform parent, CaseProgress progress, bool hasSave)
        {
            var card = Panel("CaseStatusCard", parent, new Color(.025f, .033f, .031f, .84f));
            Place(card.GetComponent<RectTransform>(), new Vector2(430, 220), new Vector2(430, -205));

            var overline = Label("StatusOverline", card.transform, "CURRENT RECORD", 13, new Color(.48f, .57f, .52f, 1), TextAnchor.MiddleLeft);
            Place(overline.rectTransform, new Vector2(350, 24), new Vector2(0, 76));
            overline.fontStyle = FontStyle.Bold;

            var caseName = Label("CaseName", card.transform, hasSave ? "CASE 001" : "NO ACTIVE CASE", 25,
                new Color(.88f, .9f, .84f, 1), TextAnchor.MiddleLeft);
            Place(caseName.rectTransform, new Vector2(350, 38), new Vector2(0, 42));
            caseName.fontStyle = FontStyle.Bold;

            var status = Label("CaseStatus", card.transform,
                hasSave ? DescribeProgress(progress) : "새 기록을 시작할 수 있습니다.",
                16, new Color(.66f, .72f, .67f, 1), TextAnchor.MiddleLeft);
            Place(status.rectTransform, new Vector2(350, 32), new Vector2(0, 6));

            int evidenceCount = progress != null && progress.evidenceIds != null ? progress.evidenceIds.Count : 0;
            var evidence = Label("EvidenceStatus", card.transform,
                hasSave ? "증거 기록  " + evidenceCount + "개" : "증거 기록  —",
                14, new Color(.48f, .54f, .5f, 1), TextAnchor.MiddleLeft);
            Place(evidence.rectTransform, new Vector2(350, 26), new Vector2(0, -33));

            string saveStamp = progress != null && !string.IsNullOrEmpty(progress.lastSavedLocal)
                ? progress.lastSavedLocal : (hasSave ? "이전 버전 기록" : "—");
            var saved = Label("SavedAt", card.transform, "LAST SAVE  " + saveStamp, 13,
                new Color(.4f, .46f, .42f, 1), TextAnchor.MiddleLeft);
            Place(saved.rectTransform, new Vector2(350, 24), new Vector2(0, -69));

            var corner = Panel("StatusAccent", card.transform, new Color(.5f, .14f, .11f, .9f));
            var cornerRect = corner.GetComponent<RectTransform>();
            cornerRect.anchorMin = cornerRect.anchorMax = new Vector2(0, 1);
            cornerRect.pivot = new Vector2(0, 1);
            cornerRect.sizeDelta = new Vector2(58, 3);
            cornerRect.anchoredPosition = new Vector2(0, 0);
        }

        private static CaseProgress ReadSavedProgress()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return null;
            try
            {
                var progress = JsonUtility.FromJson<CaseProgress>(PlayerPrefs.GetString(SaveKey, ""));
                if (progress != null)
                {
                    if (progress.flags == null) progress.flags = new System.Collections.Generic.List<string>();
                    if (progress.evidenceIds == null) progress.evidenceIds = new System.Collections.Generic.List<string>();
                }
                return progress;
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        private static string DescribeProgress(CaseProgress progress)
        {
            if (progress == null || progress.flags == null) return "조사 기록을 확인할 수 없습니다.";
            if (progress.flags.Contains("CaseCompleted")) return "CASE 001 / 보관 완료";
            if (progress.flags.Contains("Room403Completed")) return "CASE 001 / 403호 조사 완료";
            if (progress.flags.Contains("Room403Entered")) return "CASE 001 / 403호 조사 중";
            if (progress.flags.Contains("RoomNumberMismatchFound")) return "CASE 001 / 기록 불일치 확인";
            if (progress.flags.Contains("CaseStarted")) return "CASE 001 / 현장 조사 중";
            if (progress.flags.Contains("OfficialRecordSeen")) return "CASE 001 / 사건 파일 열람";
            return "CASE 001 / 조사 기록";
        }

        private void BuildSettings(Transform parent)
        {
            settingsPanel = Panel("SettingsPanel", parent, new Color(.025f, .032f, .031f, .985f));
            Place(settingsPanel.GetComponent<RectTransform>(), new Vector2(520, 430), new Vector2(310, 0));

            var heading = Label("SettingsTitle", settingsPanel.transform, "설정", 32, new Color(.92f, .94f, .89f, 1), TextAnchor.MiddleLeft);
            Place(heading.rectTransform, new Vector2(420, 50), new Vector2(0, 150));
            heading.fontStyle = FontStyle.Bold;

            var volumeLabel = Label("VolumeLabel", settingsPanel.transform, "전체 음량", 19, new Color(.74f, .79f, .74f, 1), TextAnchor.MiddleLeft);
            Place(volumeLabel.rectTransform, new Vector2(180, 32), new Vector2(-120, 75));
            var volume = CreateSlider(settingsPanel.transform, new Vector2(82, 75), 0f, 1f, PlayerPrefs.GetFloat(VolumeKey, 1f));
            volume.onValueChanged.AddListener(value =>
            {
                AudioListener.volume = value;
                PlayerPrefs.SetFloat(VolumeKey, value);
                PlayerPrefs.Save();
            });

            var sensitivityLabel = Label("SensitivityLabel", settingsPanel.transform, "마우스 감도", 19, new Color(.74f, .79f, .74f, 1), TextAnchor.MiddleLeft);
            Place(sensitivityLabel.rectTransform, new Vector2(180, 32), new Vector2(-120, 5));
            var sensitivity = CreateSlider(settingsPanel.transform, new Vector2(82, 5), .04f, .2f, PlayerPrefs.GetFloat(SensitivityKey, .09f));
            sensitivity.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetFloat(SensitivityKey, value);
                PlayerPrefs.Save();
                if (player != null) player.ApplyPreferences();
            });

            var settingsHint = Label("SettingsHint", settingsPanel.transform,
                "설정은 즉시 저장됩니다. 화면의 VHS 질감은 게임 연출에만 적용됩니다.",
                15, new Color(.5f, .57f, .53f, 1), TextAnchor.UpperLeft);
            Place(settingsHint.rectTransform, new Vector2(420, 58), new Vector2(0, -65));

            var back = SmallButton(settingsPanel.transform, "돌아가기", new Vector2(0, -150), new Vector2(220, 48));
            back.onClick.AddListener(() => ShowSettings(false));
            settingsPanel.SetActive(false);
        }

        private void BuildConfirmation(Transform parent)
        {
            confirmPanel = Panel("NewGameConfirm", parent, new Color(.018f, .023f, .022f, .99f));
            Place(confirmPanel.GetComponent<RectTransform>(), new Vector2(560, 300), new Vector2(310, 0));

            var heading = Label("ConfirmTitle", confirmPanel.transform, "새 기록을 시작하시겠습니까?", 26, new Color(.93f, .94f, .89f, 1), TextAnchor.MiddleCenter);
            Place(heading.rectTransform, new Vector2(480, 45), new Vector2(0, 82));
            heading.fontStyle = FontStyle.Bold;

            var body = Label("ConfirmBody", confirmPanel.transform,
                "기존 CASE 001 조사 기록이 삭제됩니다.\n이 작업은 되돌릴 수 없습니다.",
                17, new Color(.68f, .72f, .68f, 1), TextAnchor.MiddleCenter);
            Place(body.rectTransform, new Vector2(470, 70), new Vector2(0, 25));

            var cancel = SmallButton(confirmPanel.transform, "취소", new Vector2(-120, -82), new Vector2(200, 48));
            var start = SmallButton(confirmPanel.transform, "새로 시작", new Vector2(120, -82), new Vector2(200, 48));
            cancel.onClick.AddListener(() => confirmPanel.SetActive(false));
            start.onClick.AddListener(StartNewGame);
            confirmPanel.SetActive(false);
        }

        private void RequestNewGame()
        {
            if (busy) return;
            if (!PlayerPrefs.HasKey(SaveKey)) StartNewGame();
            else
            {
                settingsPanel.SetActive(false);
                confirmPanel.SetActive(true);
                confirmPanel.GetComponentsInChildren<Button>(true).First(b => b.name == "SmallButton").Select();
            }
        }

        private void StartNewGame()
        {
            if (busy) return;
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            CaseProgressStore.ClearCache();
            StartCoroutine(LoadFromMenu("ArchiveRoom"));
        }

        private void ContinueGame()
        {
            if (busy || !PlayerPrefs.HasKey(SaveKey)) return;
            string destination = "ArchiveRoom";
            var progress = ReadSavedProgress();
            if (progress != null && progress.flags != null)
            {
                bool completed = progress.flags.Contains("Case001Completed") || progress.flags.Contains("ReturnedToArchive") || progress.flags.Contains("CaseCompleted");
                if (!completed && progress.flags.Contains("CaseStarted")) destination = "Case001_Motel";
            }

            if (SceneManager.GetActiveScene().name == destination) StartCoroutine(CloseMenu());
            else StartCoroutine(LoadFromMenu(destination));
        }

        private void ShowSettings(bool visible)
        {
            if (busy) return;
            confirmPanel.SetActive(false);
            settingsPanel.SetActive(visible);
            if (visible)
            {
                var first = settingsPanel.GetComponentInChildren<Slider>(true);
                if (first != null) first.Select();
            }
            else (continueButton.interactable ? continueButton : newGameButton).Select();
        }

        private IEnumerator LoadFromMenu(string sceneName)
        {
            busy = true;
            confirmPanel.SetActive(false);
            settingsPanel.SetActive(false);
            yield return FadeMenu(rootGroup.alpha, 0f, .22f);
            Time.timeScale = priorTimeScale <= 0f ? 1f : priorTimeScale;
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (operation != null && !operation.isDone) yield return null;
            Destroy(gameObject);
        }

        private IEnumerator CloseMenu()
        {
            busy = true;
            yield return FadeMenu(rootGroup.alpha, 0f, .22f);
            Time.timeScale = priorTimeScale <= 0f ? 1f : priorTimeScale;
            if (gameplayCanvas != null) gameplayCanvas.enabled = true;
            if (player != null)
            {
                player.enabled = true;
                player.ApplyPreferences();
            }
            Destroy(gameObject);
        }

        private IEnumerator FadeMenu(float from, float to, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                rootGroup.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }
            rootGroup.alpha = to;
        }

        private void OnDestroy()
        {
            if (menuCamera != null)
            {
                menuCamera.transform.localPosition = menuCameraBasePosition;
                menuCamera.transform.localRotation = menuCameraBaseRotation;
            }
            IsMenuOpen = false;
        }

        private void QuitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            Debug.Log("Quit requested from the title screen. Application.Quit is ignored in the Unity Editor.");
#else
            Application.Quit();
#endif
        }

        private Button MenuButton(Transform parent, string caption, Vector2 position)
        {
            var button = SmallButton(parent, caption, position, new Vector2(390, 54));
            button.name = caption;
            var image = button.GetComponent<Image>();
            image.color = new Color(.08f, .105f, .1f, .94f);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.12f, 1);
            colors.selectedColor = new Color(1.12f, 1.12f, 1.08f, 1);
            colors.pressedColor = new Color(.82f, .86f, .82f, 1);
            button.colors = colors;
            return button;
        }

        private Button SmallButton(Transform parent, string caption, Vector2 position, Vector2 size)
        {
            var go = new GameObject("SmallButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), size, position);
            go.GetComponent<Image>().color = new Color(.1f, .13f, .125f, .98f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(InteractionSoundscape.PlayUIClick);

            var text = Label("Label", go.transform, caption, 20, new Color(.86f, .89f, .84f, 1), TextAnchor.MiddleCenter);
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
            image.raycastTarget = name == "Backdrop" || name == "LeftShade" || name == "SettingsPanel" || name == "NewGameConfirm";
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
    }
}
