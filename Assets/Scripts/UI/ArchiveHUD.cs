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
        [SerializeField] private GameObject reportActions;
        [SerializeField] private Button[] verdictButtons;
        [SerializeField] private Button confirmVerdictButton,returnButton,continueButton;
        private CaseReport activeReport;
        private InspectableCaseExit activeExit;
        private int selectedVerdict=-1;
        private Vector2 bodySize,bodyPosition;
        private int bodyFontSize;
        private RectTransform reportCard,reportHeader,reportCloseHint;
        private Vector2 cardSize,titlePosition,headerPosition,closeHintPosition,confirmPosition;
        private Vector2[] verdictPositions;
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
        public void ConfigureClosure(GameObject actions,Button[] verdicts,Button confirm,Button back,Button resume)
        {reportActions=actions;verdictButtons=verdicts;confirmVerdictButton=confirm;returnButton=back;continueButton=resume;}
        public void ShowCCTV(InspectableCCTV recording)
        {
            ResetActions();title.text=recording.Title;body.text="";
            cctvFrame.texture=recording.Frame;cctvFrame.gameObject.SetActive(true);
            cctvTimestamp.text=recording.Timestamp;cctvTimestamp.gameObject.SetActive(true);
            casePanel.SetActive(true);SetPrompt(false);
        }
        private void Awake()
        {
            bodySize=body.rectTransform.sizeDelta;bodyPosition=body.rectTransform.anchoredPosition;bodyFontSize=body.fontSize;
            reportCard=title.rectTransform.parent as RectTransform;reportHeader=reportCard.Find("DocumentHeader") as RectTransform;reportCloseHint=reportCard.Find("CloseHint") as RectTransform;
            cardSize=reportCard.sizeDelta;titlePosition=title.rectTransform.anchoredPosition;
            if(reportHeader!=null)headerPosition=reportHeader.anchoredPosition;if(reportCloseHint!=null)closeHintPosition=reportCloseHint.anchoredPosition;
            if(confirmVerdictButton!=null)confirmPosition=confirmVerdictButton.GetComponent<RectTransform>().anchoredPosition;
            if(verdictButtons!=null){verdictPositions=new Vector2[verdictButtons.Length];for(int i=0;i<verdictButtons.Length;i++)verdictPositions[i]=verdictButtons[i].GetComponent<RectTransform>().anchoredPosition;}
            if (startFieldButton != null) startFieldButton.onClick.AddListener(StartField);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
            if (compareButton != null) compareButton.onClick.AddListener(CompareRecords);
            if(verdictButtons!=null)for(int i=0;i<verdictButtons.Length;i++){int choice=i;verdictButtons[i].onClick.AddListener(()=>ChooseVerdict(choice));}
            if(confirmVerdictButton!=null)confirmVerdictButton.onClick.AddListener(ConfirmVerdict);
            if(returnButton!=null)returnButton.onClick.AddListener(ReturnFromField);
            if(continueButton!=null)continueButton.onClick.AddListener(()=>player.CloseCase());
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
                if (startFieldButton != null) startFieldButton.gameObject.SetActive(!CaseProgressStore.Get(ActiveDefinition).Has("CaseCompleted"));
            }
        }
        public void ShowDocument(InspectableDocument document)
        { ResetActions(); activeDocument = document; documentPage = 0; DisplayPage(); casePanel.SetActive(true); SetPrompt(false); }
        private void DisplayPage()
        {
            if (activeDocument == null) return;
            title.text = activeDocument.Title;
            body.text = activeDocument.Page(documentPage);
            var photograph=activeDocument.Image(documentPage);
            if(cctvFrame!=null){cctvFrame.texture=photograph;cctvFrame.gameObject.SetActive(photograph!=null);}
            if(cctvTimestamp!=null){cctvTimestamp.text=activeDocument.ImageCaption;cctvTimestamp.gameObject.SetActive(photograph!=null);}
            if(photograph!=null)body.text="";
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
        public void ShowReport(CaseReport report)
        {
            ResetActions();activeReport=report;ActiveDefinition=report.Definition;selectedVerdict=-1;
            title.text=report.Definition.Title+" / 사건 정리";body.text=report.Summary();body.fontSize=19;
            reportCard.sizeDelta=new Vector2(850,880);title.rectTransform.anchoredPosition=new Vector2(0,280);
            if(reportHeader!=null)reportHeader.anchoredPosition=new Vector2(0,350);if(reportCloseHint!=null)reportCloseHint.anchoredPosition=new Vector2(0,-395);
            body.rectTransform.sizeDelta=new Vector2(730,report.Completed?510:440);body.rectTransform.anchoredPosition=new Vector2(0,report.Completed?-25:10);
            for(int i=0;i<verdictButtons.Length;i++)verdictButtons[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(i%2==0?-184:184,i<2?-238:-285);
            confirmVerdictButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-337);
            reportActions.SetActive(!report.Completed);foreach(var button in verdictButtons){button.gameObject.SetActive(!report.Completed);button.image.color=new Color(.19f,.22f,.22f);}
            confirmVerdictButton.gameObject.SetActive(false);returnButton.gameObject.SetActive(false);continueButton.gameObject.SetActive(false);
            casePanel.SetActive(true);SetPrompt(false);
        }
        public void ChooseVerdict(int index)
        {
            if(activeReport==null || activeReport.Completed || index<0 || index>=4)return;
            selectedVerdict=index;for(int i=0;i<verdictButtons.Length;i++)verdictButtons[i].image.color=i==index?new Color(.31f,.34f,.31f):new Color(.19f,.22f,.22f);
            confirmVerdictButton.GetComponentInChildren<Text>().text="이 판정으로 보관 — "+CaseReport.VerdictLabel(((CaseVerdict)index).ToString());confirmVerdictButton.gameObject.SetActive(true);
        }
        public void ConfirmVerdict()
        {
            if(activeReport==null || selectedVerdict<0 || !activeReport.Confirm((CaseVerdict)selectedVerdict))return;
            string label=activeReport.Definition.Title.Split('—')[0].Trim();player.CloseCase();ShowToast(label+"\nARCHIVED",2.3f);
        }
        public void ShowExit(InspectableCaseExit exit)
        {
            ResetActions();activeExit=exit;title.text="현장 조사 종료";body.text="조사를 마치고 돌아간다.";
            reportActions.SetActive(true);foreach(var button in verdictButtons)button.gameObject.SetActive(false);confirmVerdictButton.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(true);continueButton.gameObject.SetActive(true);casePanel.SetActive(true);SetPrompt(false);
        }
        public void ReturnFromField()
        {if(activeExit==null || !activeExit.ReturnToArchive())return;player.CloseCase();}
        public void ShowToast(string text, float duration = 4)
        { if (toast == null) return; toast.text = text; toast.gameObject.SetActive(true); toastUntil = Time.unscaledTime + duration; }
        private void ResetActions()
        {
            activeDocument = null;
            activeReport=null;activeExit=null;
            if(bodyFontSize>0){body.fontSize=bodyFontSize;body.rectTransform.sizeDelta=bodySize;body.rectTransform.anchoredPosition=bodyPosition;}
            if(reportCard!=null){reportCard.sizeDelta=cardSize;title.rectTransform.anchoredPosition=titlePosition;if(reportHeader!=null)reportHeader.anchoredPosition=headerPosition;if(reportCloseHint!=null)reportCloseHint.anchoredPosition=closeHintPosition;}
            if(verdictPositions!=null)for(int i=0;i<verdictPositions.Length;i++)verdictButtons[i].GetComponent<RectTransform>().anchoredPosition=verdictPositions[i];
            if(confirmVerdictButton!=null)confirmVerdictButton.GetComponent<RectTransform>().anchoredPosition=confirmPosition;
            if(reportActions!=null)reportActions.SetActive(false);
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
