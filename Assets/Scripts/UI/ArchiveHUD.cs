using UnityEngine;
using UnityEngine.UI;

namespace Archive0317
{
    public sealed class ArchiveHUD : MonoBehaviour
    {
        [SerializeField] private GameObject casePanel;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private Text prompt;
        [SerializeField] private Text title;
        [SerializeField] private Text body;
        [SerializeField] private Text cursorHint;
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private Button startFieldButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button compareButton;
        [SerializeField] private Text toast;
        [SerializeField] private RawImage cctvFrame;
        [SerializeField] private Text cctvTimestamp;
        private float toastUntil;
        private InspectableDocument activeDocument;
        private int documentPage;
        public CaseDefinition ActiveDefinition { get; private set; }
        public bool IsCaseOpen => casePanel != null && casePanel.activeSelf;
        public bool IsPromptVisible => prompt != null && prompt.gameObject.activeSelf;
        public void Configure(GameObject panel, GameObject aim, Text interaction, Text heading, Text description, Text hint, FirstPersonPlayer controller)
        { casePanel = panel; crosshair = aim; prompt = interaction; title = heading; body = description; cursorHint = hint; player = controller; }
        public void ConfigureCaseUI(Button start, Button next, Button compare, Text notification)
        { startFieldButton = start; nextPageButton = next; compareButton = compare; toast = notification; }
        public void ConfigureCCTV(RawImage frame,Text timestamp){cctvFrame=frame;cctvTimestamp=timestamp;}
        public void ShowCCTV(InspectableCCTV recording)
        {
            ResetActions();title.text=recording.Title;body.text="";
            cctvFrame.texture=recording.Frame;cctvFrame.gameObject.SetActive(true);
            cctvTimestamp.text=recording.Timestamp;cctvTimestamp.gameObject.SetActive(true);
            casePanel.SetActive(true);SetPrompt(false);
        }
        private void Awake()
        {
            if (startFieldButton != null) startFieldButton.onClick.AddListener(StartField);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
            if (compareButton != null) compareButton.onClick.AddListener(CompareRecords);
        }
        public void SetDefinition(CaseDefinition definition) { ActiveDefinition = definition; }
        public void SetPrompt(bool visible, string text = "E 조사") { prompt.text = text; prompt.gameObject.SetActive(visible && !IsCaseOpen); }
        public void ShowCase(CaseFile file)
        {
            ResetActions(); ActiveDefinition = file.Definition;
            title.text = file.Title; body.text = file.Description; casePanel.SetActive(true); SetPrompt(false);
            if (ActiveDefinition != null)
            {
                CaseProgressStore.Mark(ActiveDefinition, "OfficialRecordSeen");
                CaseProgressStore.RecordFact(ActiveDefinition, "OfficialRoom", ActiveDefinition.OfficialRoom);
                if (startFieldButton != null) startFieldButton.gameObject.SetActive(true);
            }
        }
        public void ShowDocument(InspectableDocument document)
        { ResetActions(); activeDocument = document; documentPage = 0; DisplayPage(); casePanel.SetActive(true); SetPrompt(false); }
        private void DisplayPage()
        {
            if (activeDocument == null) return;
            title.text = activeDocument.Title;
            body.text = activeDocument.Page(documentPage);
            activeDocument.Viewed(documentPage);
            if (nextPageButton != null) nextPageButton.gameObject.SetActive(documentPage + 1 < activeDocument.PageCount);
        }
        public void NextPage() { if (activeDocument != null && documentPage + 1 < activeDocument.PageCount) { documentPage++; DisplayPage(); } }
        public void ShowNotebook()
        {
            if (ActiveDefinition == null) return;
            ResetActions(); var progress = CaseProgressStore.Get(ActiveDefinition);
            title.text = "사건 기록 — " + ActiveDefinition.Title;
            body.text = ActiveDefinition.OfficialRecord + "\n\n현장 메모\n";
            foreach (var entry in ActiveDefinition.NotebookEntries ?? System.Array.Empty<CaseNotebookEntry>())
                if (entry != null && progress.Has(entry.progressFlag))
                    body.text += (entry.text ?? "").Replace("{value}", progress.Fact(entry.factKey) ?? "") + "\n";
            bool ready = true;
            foreach (var flag in ActiveDefinition.ComparisonRequirements ?? System.Array.Empty<string>()) if (!progress.Has(flag)) ready = false;
            if (compareButton != null) compareButton.gameObject.SetActive(ready && !progress.Has("RoomNumberMismatchFound"));
            casePanel.SetActive(true); SetPrompt(false);
        }
        public void CompareRecords()
        {
            if (CaseProgressStore.CompareRooms(ActiveDefinition))
            { ShowToast("기록이 일치하지 않는다.", 6); if (compareButton != null) compareButton.gameObject.SetActive(false); }
        }
        public void StartField()
        {
            if (!SceneTransitionManager.Begin(ActiveDefinition)) { ShowToast("현장으로 이동할 수 없다."); return; }
            if (startFieldButton != null) startFieldButton.interactable = false;
        }
        public void ShowToast(string text, float duration = 4)
        { if (toast == null) return; toast.text = text; toast.gameObject.SetActive(true); toastUntil = Time.unscaledTime + duration; }
        private void ResetActions()
        {
            activeDocument = null;
            if(cctvFrame!=null){cctvFrame.gameObject.SetActive(false);cctvFrame.texture=null;}
            if(cctvTimestamp!=null)cctvTimestamp.gameObject.SetActive(false);
            if (startFieldButton != null) { startFieldButton.gameObject.SetActive(false); startFieldButton.interactable = true; }
            if (nextPageButton != null) nextPageButton.gameObject.SetActive(false);
            if (compareButton != null) compareButton.gameObject.SetActive(false);
        }
        public void CloseCase() { casePanel.SetActive(false); ResetActions(); }
        private void LateUpdate()
        {
            crosshair.SetActive(!IsCaseOpen && player.IsCaptured);
            cursorHint.gameObject.SetActive(!IsCaseOpen && !player.IsCaptured);
            if (toast != null && Time.unscaledTime >= toastUntil) toast.gameObject.SetActive(false);
        }
    }
}
