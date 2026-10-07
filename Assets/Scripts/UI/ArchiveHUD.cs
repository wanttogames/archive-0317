using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Archive0317
{
    public sealed class ArchiveHUD : MonoBehaviour
    {
        private enum NotebookTab { Facts, Contradictions, Evidence }
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
        private Button previousPageButton;
        private Text pageIndicator;
        private Text documentState;
        private Text documentText;
        private ScrollRect documentScroll;
        private GameObject documentViewport;
        private Text closeHintLabel;
        private string defaultCloseHint;
        private GameObject notebookTabsRoot;
        private Button[] notebookTabButtons;
        private GameObject completionStampRoot;
        private CanvasGroup completionStampGroup;
        private Text completionStampLabel;
        private Vector3 completionStampBaseScale;
        private GameObject objectiveRoot;
        private Text objectiveLabel;
        private Text objectiveOverline;
        private string currentObjectiveId;
        private float nextObjectiveRefresh;
        private bool notebookOpen;
        private NotebookTab activeNotebookTab;
        public CaseDefinition ActiveDefinition { get; private set; }
        public bool IsCaseOpen => casePanel != null && casePanel.activeSelf;
        public bool IsPromptVisible => prompt != null && prompt.gameObject.activeSelf;
        public bool HasActiveDocument => activeDocument != null;
        public bool IsNotebookOpen => notebookOpen;
        public bool CanGoNextDocumentPage => activeDocument != null && documentPage + 1 < activeDocument.PageCount;
        public bool CanGoPreviousDocumentPage => activeDocument != null && documentPage > 0;
        public void Configure(GameObject panel, GameObject aim, Text interaction, Text heading, Text description, Text hint, FirstPersonPlayer controller)
        { casePanel = panel; crosshair = aim; prompt = interaction; title = heading; body = description; cursorHint = hint; player = controller; }
        public void ConfigureCaseUI(Button start, Button next, Button compare, Text notification)
        { startFieldButton = start; nextPageButton = next; compareButton = compare; toast = notification; }
        public void ConfigureCCTV(RawImage frame,Text timestamp){cctvFrame=frame;cctvTimestamp=timestamp;}
        public void ConfigureClosure(GameObject actions,Button[] verdicts,Button confirm,Button back,Button resume)
        {reportActions=actions;verdictButtons=verdicts;confirmVerdictButton=confirm;returnButton=back;continueButton=resume;}
        public void ShowCCTV(InspectableCCTV recording)
        {
            InteractionSoundscape.PlayUIClick();
            ResetActions();title.text=recording.Title;body.text="";
            cctvFrame.texture=recording.Frame;cctvFrame.gameObject.SetActive(true);
            cctvTimestamp.text=recording.Timestamp;cctvTimestamp.gameObject.SetActive(true);
            casePanel.SetActive(true);SetPrompt(false);
        }
        private void Awake()
        {
            ApplyReadabilityProfile();
            bodySize=body.rectTransform.sizeDelta;bodyPosition=body.rectTransform.anchoredPosition;bodyFontSize=body.fontSize;
            reportCard=title.rectTransform.parent as RectTransform;reportHeader=reportCard.Find("DocumentHeader") as RectTransform;reportCloseHint=reportCard.Find("CloseHint") as RectTransform;
            closeHintLabel=reportCloseHint!=null?reportCloseHint.GetComponent<Text>():null;defaultCloseHint=closeHintLabel!=null?closeHintLabel.text:"";
            cardSize=reportCard.sizeDelta;titlePosition=title.rectTransform.anchoredPosition;
            if(reportHeader!=null)headerPosition=reportHeader.anchoredPosition;if(reportCloseHint!=null)closeHintPosition=reportCloseHint.anchoredPosition;
            if(confirmVerdictButton!=null)confirmPosition=confirmVerdictButton.GetComponent<RectTransform>().anchoredPosition;
            if(verdictButtons!=null){verdictPositions=new Vector2[verdictButtons.Length];for(int i=0;i<verdictButtons.Length;i++)verdictPositions[i]=verdictButtons[i].GetComponent<RectTransform>().anchoredPosition;}
            if (startFieldButton != null) { startFieldButton.onClick.AddListener(InteractionSoundscape.PlayUIClick); startFieldButton.onClick.AddListener(StartField); }
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
            if (compareButton != null) { compareButton.onClick.AddListener(InteractionSoundscape.PlayUIClick); compareButton.onClick.AddListener(CompareRecords); }
            if(verdictButtons!=null)for(int i=0;i<verdictButtons.Length;i++){int choice=i;verdictButtons[i].onClick.AddListener(InteractionSoundscape.PlayUIClick);verdictButtons[i].onClick.AddListener(()=>ChooseVerdict(choice));}
            if(confirmVerdictButton!=null){confirmVerdictButton.onClick.AddListener(InteractionSoundscape.PlayUIClick);confirmVerdictButton.onClick.AddListener(ConfirmVerdict);}
            if(returnButton!=null){returnButton.onClick.AddListener(InteractionSoundscape.PlayUIClick);returnButton.onClick.AddListener(ReturnFromField);}
            if(continueButton!=null){continueButton.onClick.AddListener(InteractionSoundscape.PlayUIBack);continueButton.onClick.AddListener(()=>player.CloseCase());}
            CreateDocumentReader();
            CreateNotebookTabs();
            CreateCompletionPresentation();
            CreateObjectiveUI();
        }
        private void ApplyReadabilityProfile()
        {
            // Keep the PS1/VHS treatment on the 3D scene, but render investigation UI as a crisp overlay.
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = true;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600, 900);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = .5f;
            }

            foreach (var label in GetComponentsInChildren<Text>(true))
            {
                label.resizeTextForBestFit = false;
                label.alignByGeometry = false;
                if (label.GetComponent<Shadow>() == null)
                {
                    var shadow = label.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0, 0, 0, .88f);
                    shadow.effectDistance = new Vector2(1, -1);
                    shadow.useGraphicAlpha = true;
                }
            }

            if (title != null)
            {
                title.fontStyle = FontStyle.Bold;
                title.color = new Color(.95f, .96f, .92f, 1);
            }
            if (body != null) body.color = new Color(.9f, .92f, .88f, 1);
            if (prompt != null)
            {
                prompt.fontStyle = FontStyle.Bold;
                prompt.color = new Color(.96f, .97f, .92f, 1);
            }
            if (cursorHint != null) cursorHint.color = new Color(.92f, .94f, .9f, 1);
            if (toast != null)
            {
                toast.fontStyle = FontStyle.Bold;
                toast.color = new Color(.96f, .97f, .92f, 1);
            }
            if (cctvTimestamp != null) cctvTimestamp.color = new Color(.92f, .95f, .9f, 1);
        }

        public void SetDefinition(CaseDefinition definition)
        {
            ActiveDefinition = definition;
            currentObjectiveId = null;
            RefreshObjectiveUI(true);
        }
        public void SetPrompt(bool visible, string text = "E 조사") { prompt.text = text; prompt.gameObject.SetActive(visible && !IsCaseOpen); }
        public void ShowCase(CaseFile file)
        {
            InteractionSoundscape.PlayDocumentOpen();
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
        {
            InteractionSoundscape.PlayDocumentOpen();
            ResetActions();
            activeDocument = document;
            documentPage = 0;
            if(documentViewport!=null)documentViewport.SetActive(true);
            if(body!=null)body.gameObject.SetActive(false);
            casePanel.SetActive(true);
            DisplayPage();
            SetPrompt(false);
        }
        private void DisplayPage()
        {
            if (activeDocument == null) return;
            title.text = activeDocument.Title;
            string pageText = activeDocument.Page(documentPage);
            var photograph=activeDocument.Image(documentPage);

            if(documentText!=null)
            {
                documentText.text=photograph!=null?"":pageText;
                documentText.gameObject.SetActive(photograph==null);
                Canvas.ForceUpdateCanvases();
                var textRect=documentText.rectTransform;
                float viewportHeight=documentScroll!=null?documentScroll.viewport.rect.height:bodySize.y;
                textRect.sizeDelta=new Vector2(textRect.sizeDelta.x,Mathf.Max(viewportHeight,documentText.preferredHeight+28f));
                textRect.anchoredPosition=Vector2.zero;
                if(documentScroll!=null){documentScroll.verticalNormalizedPosition=1f;documentScroll.enabled=photograph==null && textRect.sizeDelta.y>viewportHeight+2f;}
            }

            if(cctvFrame!=null){cctvFrame.texture=photograph;cctvFrame.gameObject.SetActive(photograph!=null);}
            if(cctvTimestamp!=null){cctvTimestamp.text=activeDocument.ImageCaption;cctvTimestamp.gameObject.SetActive(photograph!=null);}

            bool newlyRecorded=activeDocument.Viewed(documentPage);
            if(pageIndicator!=null)
            {
                pageIndicator.text=(documentPage+1)+" / "+Mathf.Max(1,activeDocument.PageCount);
                pageIndicator.gameObject.SetActive(true);
            }
            if(documentState!=null)
            {
                documentState.text=newlyRecorded?"EVIDENCE RECORDED":activeDocument.IsRecorded?"기록됨":"열람 중";
                documentState.color=newlyRecorded?new Color(.78f,.34f,.28f,1):new Color(.5f,.58f,.54f,1);
                documentState.gameObject.SetActive(true);
            }

            if(previousPageButton!=null)previousPageButton.gameObject.SetActive(CanGoPreviousDocumentPage);
            if(nextPageButton!=null)
            {
                nextPageButton.gameObject.SetActive(CanGoNextDocumentPage);
                var label=nextPageButton.GetComponentInChildren<Text>();
                if(label!=null)label.text="다음 페이지  E";
            }
            if(closeHintLabel!=null)
            {
                bool scrollable=documentScroll!=null && documentScroll.enabled;
                if(activeDocument.PageCount>1)
                    closeHintLabel.text="Q 이전 · E 다음"+(scrollable?" · 휠 스크롤":"")+" · ESC 닫기";
                else
                    closeHintLabel.text=(scrollable?"휠 스크롤 · ":"")+"E 또는 ESC — 파일 닫기";
            }
        }
        public void NextPage()
        {
            if (!CanGoNextDocumentPage) return;
            InteractionSoundscape.PlayDocumentPage();
            documentPage++;
            DisplayPage();
        }
        public void PreviousPage()
        {
            if (!CanGoPreviousDocumentPage) return;
            InteractionSoundscape.PlayDocumentPage();
            documentPage--;
            DisplayPage();
        }
        public void ShowNotebook()
        {
            if (ActiveDefinition == null) return;
            InteractionSoundscape.PlayDocumentOpen();
            ResetActions();
            notebookOpen = true;
            activeNotebookTab = NotebookTab.Facts;
            if(notebookTabsRoot!=null)notebookTabsRoot.SetActive(true);
            title.text = "사건 기록 — " + ActiveDefinition.Title;
            body.rectTransform.sizeDelta = new Vector2(bodySize.x, 335);
            body.rectTransform.anchoredPosition = new Vector2(bodyPosition.x, bodyPosition.y - 38);
            if(closeHintLabel!=null)closeHintLabel.text="1 확인된 사실 · 2 모순 · 3 증거 · TAB/ESC 닫기";
            casePanel.SetActive(true);
            SetPrompt(false);
            RefreshNotebook();
        }

        public void SelectNotebookTab(int index)
        {
            if(!notebookOpen || index<0 || index>2)return;
            activeNotebookTab=(NotebookTab)index;
            InteractionSoundscape.PlayUIClick();
            RefreshNotebook();
        }

        private void RefreshNotebook()
        {
            if(!notebookOpen || ActiveDefinition==null)return;
            var progress=CaseProgressStore.Get(ActiveDefinition);
            UpdateNotebookTabVisuals();

            string objectivePrefix=BuildObjectiveNotebookHeader(progress);
            if(compareButton!=null)compareButton.gameObject.SetActive(false);
            switch(activeNotebookTab)
            {
                case NotebookTab.Facts:
                    body.text=objectivePrefix+BuildFactsNotebook(progress);
                    break;
                case NotebookTab.Contradictions:
                    body.text=objectivePrefix+BuildContradictionsNotebook(progress);
                    bool ready=true;
                    foreach(var flag in ActiveDefinition.ComparisonRequirements??System.Array.Empty<string>())
                        if(!progress.Has(flag))ready=false;
                    if(compareButton!=null)
                    {
                        compareButton.gameObject.SetActive(ready && !progress.Has("RoomNumberMismatchFound"));
                        if(compareButton.gameObject.activeSelf)
                        {
                            var label=compareButton.GetComponentInChildren<Text>();
                            if(label!=null)label.text="기록 대조";
                        }
                    }
                    break;
                case NotebookTab.Evidence:
                    body.text=objectivePrefix+BuildEvidenceNotebook(progress);
                    break;
            }
        }

        private string BuildFactsNotebook(CaseProgress progress)
        {
            var entries=ActiveDefinition.NotebookEntries??System.Array.Empty<CaseNotebookEntry>();
            int found=0;
            foreach(var entry in entries)if(entry!=null && progress.Has(entry.progressFlag))found++;
            var text=new System.Text.StringBuilder();
            text.AppendLine("확인된 사실  "+found+" / "+entries.Length);
            if(!string.IsNullOrEmpty(ActiveDefinition.OfficialRoom))
                text.AppendLine("\n■ 공식 사건 기록의 투숙 객실: "+ActiveDefinition.OfficialRoom+"호");
            foreach(var entry in entries)
            {
                if(entry==null)continue;
                if(progress.Has(entry.progressFlag))
                    text.AppendLine("■ "+(entry.text??"").Replace("{value}",progress.Fact(entry.factKey)??""));
                else text.AppendLine("□ ???");
            }
            return text.ToString().TrimEnd();
        }

        private string BuildContradictionsNotebook(CaseProgress progress)
        {
            var entries=ActiveDefinition.ContradictionEntries??System.Array.Empty<CaseContradictionEntry>();
            int found=0;
            foreach(var entry in entries)if(entry!=null && progress.Has(entry.progressFlag))found++;
            var text=new System.Text.StringBuilder();
            text.AppendLine("확인된 모순  "+found+" / "+entries.Length);
            if(entries.Length==0){text.Append("\n아직 분류된 모순 항목이 없다.");return text.ToString();}
            foreach(var entry in entries)
            {
                if(entry==null)continue;
                text.AppendLine(progress.Has(entry.progressFlag)?"\n! "+entry.text:"\n· ???");
            }
            return text.ToString().TrimEnd();
        }

        private string BuildEvidenceNotebook(CaseProgress progress)
        {
            EvidenceCollection.Collect(ActiveDefinition);
            var entries=ActiveDefinition.Evidence??System.Array.Empty<EvidenceDefinition>();
            int found=0;
            foreach(var entry in entries)if(entry!=null && progress.evidenceIds.Contains(entry.id))found++;
            var text=new System.Text.StringBuilder();
            text.AppendLine("수집 증거  "+found+" / "+entries.Length);
            foreach(var entry in entries)
            {
                if(entry==null)continue;
                if(progress.evidenceIds.Contains(entry.id))text.AppendLine("\n[REC] "+entry.title);
                else text.AppendLine("\n[---] ???");
            }
            return text.ToString().TrimEnd();
        }
        public void CompareRecords()
        {
            if (CaseProgressStore.CompareRooms(ActiveDefinition))
            {
                ShowToast("기록이 일치하지 않는다.", 6);
                if (compareButton != null) compareButton.gameObject.SetActive(false);
                if(notebookOpen)RefreshNotebook();
            }
        }
        public void StartField()
        {
            if (!SceneTransitionManager.Begin(ActiveDefinition)) { ShowToast("현장으로 이동할 수 없다."); return; }
            if (startFieldButton != null) startFieldButton.interactable = false;
        }
        public void ShowReport(CaseReport report)
        {
            InteractionSoundscape.PlayDocumentOpen();
            ResetActions();activeReport=report;ActiveDefinition=report.Definition;selectedVerdict=-1;
            bool completed=report.Completed;
            title.text=report.Definition.Title+(completed?" / 보관 완료":" / 사건 정리");
            body.text=report.Summary();body.fontSize=completed?21:19;
            reportCard.sizeDelta=new Vector2(850,880);title.rectTransform.anchoredPosition=new Vector2(0,280);
            if(reportHeader!=null)reportHeader.anchoredPosition=new Vector2(0,350);
            if(reportCloseHint!=null)reportCloseHint.anchoredPosition=new Vector2(0,-395);
            body.rectTransform.sizeDelta=new Vector2(730,completed?455:440);
            body.rectTransform.anchoredPosition=new Vector2(0,completed?-35:10);

            for(int i=0;i<verdictButtons.Length;i++)
                verdictButtons[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(i%2==0?-184:184,i<2?-238:-285);
            confirmVerdictButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-337);

            reportActions.SetActive(true);
            foreach(var button in verdictButtons)
            {
                button.gameObject.SetActive(!completed);
                button.image.color=new Color(.19f,.22f,.22f);
            }
            confirmVerdictButton.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(completed);
            if(completed)
            {
                var continueLabel=continueButton.GetComponentInChildren<Text>();
                if(continueLabel!=null)continueLabel.text="기록실로 돌아가기";
                continueButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-337);
                if(closeHintLabel!=null)closeHintLabel.text="ARCHIVE RECORD CLOSED";
                if(completionStampRoot!=null)
                {
                    completionStampRoot.SetActive(true);
                    StartCoroutine(AnimateCompletionStamp());
                }
            }
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
            if(activeReport==null || selectedVerdict<0)return;
            var report=activeReport;
            if(!report.Confirm((CaseVerdict)selectedVerdict))return;
            ShowReport(report);
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
            notebookOpen=false;
            if(notebookTabsRoot!=null)notebookTabsRoot.SetActive(false);
            if(completionStampRoot!=null)completionStampRoot.SetActive(false);
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
            if(previousPageButton!=null)previousPageButton.gameObject.SetActive(false);
            if(pageIndicator!=null)pageIndicator.gameObject.SetActive(false);
            if(documentState!=null)documentState.gameObject.SetActive(false);
            if(documentViewport!=null)documentViewport.SetActive(false);
            if(documentText!=null)documentText.text="";
            if(body!=null)body.gameObject.SetActive(true);
            if(closeHintLabel!=null)closeHintLabel.text=defaultCloseHint;
            if (compareButton != null) compareButton.gameObject.SetActive(false);
        }
        private void CreateObjectiveUI()
        {
            if(objectiveRoot!=null)return;
            var canvas=GetComponent<Canvas>();
            if(canvas==null)return;

            objectiveRoot=new GameObject("CurrentObjective",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));
            objectiveRoot.transform.SetParent(transform,false);
            var rect=objectiveRoot.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);
            rect.pivot=new Vector2(0,1);
            rect.sizeDelta=new Vector2(520,86);
            rect.anchoredPosition=new Vector2(28,-28);

            var image=objectiveRoot.GetComponent<Image>();
            image.color=new Color(.018f,.026f,.024f,.72f);
            image.raycastTarget=false;

            objectiveOverline=CreateObjectiveLabel("ObjectiveOverline","CURRENT OBJECTIVE",12,new Vector2(20,-13),new Vector2(470,22),
                new Color(.48f,.58f,.53f,1),FontStyle.Bold);
            objectiveLabel=CreateObjectiveLabel("ObjectiveText","",17,new Vector2(20,-37),new Vector2(470,38),
                new Color(.92f,.94f,.89f,1),FontStyle.Normal);

            var accent=new GameObject("ObjectiveAccent",typeof(RectTransform),typeof(Image));
            accent.transform.SetParent(objectiveRoot.transform,false);
            var accentRect=accent.GetComponent<RectTransform>();
            accentRect.anchorMin=new Vector2(0,0);
            accentRect.anchorMax=new Vector2(0,1);
            accentRect.pivot=new Vector2(0,.5f);
            accentRect.sizeDelta=new Vector2(3,0);
            accentRect.anchoredPosition=Vector2.zero;
            var accentImage=accent.GetComponent<Image>();
            accentImage.color=new Color(.48f,.14f,.11f,.9f);
            accentImage.raycastTarget=false;

            objectiveRoot.SetActive(false);
        }

        private Text CreateObjectiveLabel(string name,string value,int size,Vector2 position,Vector2 dimensions,Color color,FontStyle style)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow));
            go.transform.SetParent(objectiveRoot.transform,false);
            var rect=go.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);
            rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=position;
            rect.sizeDelta=dimensions;
            var label=go.GetComponent<Text>();
            label.font=body!=null?body.font:null;
            label.fontSize=size;
            label.fontStyle=style;
            label.text=value;
            label.color=color;
            label.alignment=TextAnchor.UpperLeft;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;
            label.verticalOverflow=VerticalWrapMode.Truncate;
            label.raycastTarget=false;
            var shadow=go.GetComponent<Shadow>();
            shadow.effectColor=new Color(0,0,0,.88f);
            shadow.effectDistance=new Vector2(1,-1);
            return label;
        }

        private string BuildObjectiveNotebookHeader(CaseProgress progress)
        {
            if(ActiveDefinition==null)return "";
            var objective=ActiveDefinition.CurrentObjective(progress);
            if(objective==null)return "현재 조사 목표\n✓ 현장 조사 목표 완료\n\n";
            return "현재 조사 목표\n→ "+objective.text+"\n\n";
        }

        private void RefreshObjectiveUI(bool force=false)
        {
            if(objectiveRoot==null)return;
            if(ActiveDefinition==null)
            {
                objectiveRoot.SetActive(false);
                return;
            }

            bool inField=SceneManager.GetActiveScene().name==ActiveDefinition.FieldScene;
            if(!inField)
            {
                objectiveRoot.SetActive(false);
                return;
            }

            var progress=CaseProgressStore.Get(ActiveDefinition);
            var objective=ActiveDefinition.CurrentObjective(progress);
            bool visible=objective!=null && !IsCaseOpen && player!=null && player.IsCaptured
                && !PauseMenuController.IsOpen && !MainMenuController.IsMenuOpen && !SceneTransitionManager.IsTransitioning;
            objectiveRoot.SetActive(visible);
            if(objective==null)return;

            if(force || currentObjectiveId!=objective.id)
            {
                currentObjectiveId=objective.id;
                objectiveLabel.text=objective.text;
                objectiveOverline.text="CURRENT OBJECTIVE";
            }
        }

        private void CreateCompletionPresentation()
        {
            if(reportCard==null || completionStampRoot!=null)return;
            completionStampRoot=new GameObject("ArchivedStamp",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));
            completionStampRoot.transform.SetParent(reportCard,false);
            var rect=completionStampRoot.GetComponent<RectTransform>();
            rect.sizeDelta=new Vector2(210,72);
            rect.anchoredPosition=new Vector2(245,208);
            rect.localRotation=Quaternion.Euler(0,0,-7f);

            var image=completionStampRoot.GetComponent<Image>();
            image.color=new Color(.32f,.055f,.045f,.16f);
            image.raycastTarget=false;

            completionStampLabel=CreateDocumentLabel("StampLabel","ARCHIVED",29,Vector2.zero,new Vector2(200,64),
                TextAnchor.MiddleCenter,new Color(.72f,.18f,.14f,1),completionStampRoot.transform);
            completionStampLabel.fontStyle=FontStyle.Bold;

            var outline=completionStampLabel.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(.25f,.035f,.03f,.95f);
            outline.effectDistance=new Vector2(2,-2);

            completionStampGroup=completionStampRoot.GetComponent<CanvasGroup>();
            completionStampBaseScale=Vector3.one;
            completionStampRoot.SetActive(false);
        }

        private System.Collections.IEnumerator AnimateCompletionStamp()
        {
            if(completionStampRoot==null || completionStampGroup==null)yield break;
            completionStampRoot.SetActive(true);
            completionStampGroup.alpha=0f;
            completionStampRoot.transform.localScale=completionStampBaseScale*1.55f;
            float duration=.22f;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/duration);
                float eased=1f-Mathf.Pow(1f-t,3f);
                completionStampGroup.alpha=eased;
                completionStampRoot.transform.localScale=Vector3.Lerp(completionStampBaseScale*1.55f,completionStampBaseScale,eased);
                yield return null;
            }
            completionStampGroup.alpha=1f;
            completionStampRoot.transform.localScale=completionStampBaseScale;
        }

        private void CreateNotebookTabs()
        {
            if(reportCard==null || notebookTabsRoot!=null)return;
            notebookTabsRoot=new GameObject("NotebookTabs",typeof(RectTransform));
            notebookTabsRoot.transform.SetParent(reportCard,false);
            var rootRect=notebookTabsRoot.GetComponent<RectTransform>();
            rootRect.sizeDelta=new Vector2(700,48);
            rootRect.anchoredPosition=new Vector2(0,190);

            notebookTabButtons=new Button[3];
            string[] labels={"1  확인된 사실","2  모순","3  증거"};
            float[] positions={-235f,0f,235f};
            for(int i=0;i<3;i++)
            {
                int tab=i;
                var go=new GameObject("NotebookTab"+i,typeof(RectTransform),typeof(Image),typeof(Button));
                go.transform.SetParent(notebookTabsRoot.transform,false);
                var rect=go.GetComponent<RectTransform>();
                rect.sizeDelta=new Vector2(215,42);
                rect.anchoredPosition=new Vector2(positions[i],0);
                var image=go.GetComponent<Image>();
                image.color=new Color(.08f,.105f,.1f,.94f);
                var button=go.GetComponent<Button>();
                button.onClick.AddListener(()=>SelectNotebookTab(tab));
                var label=CreateDocumentLabel("Label",labels[i],16,Vector2.zero,new Vector2(205,38),TextAnchor.MiddleCenter,new Color(.76f,.8f,.75f,1),go.transform);
                label.fontStyle=FontStyle.Bold;
                notebookTabButtons[i]=button;
            }
            notebookTabsRoot.SetActive(false);
        }

        private void UpdateNotebookTabVisuals()
        {
            if(notebookTabButtons==null)return;
            for(int i=0;i<notebookTabButtons.Length;i++)
            {
                var image=notebookTabButtons[i].GetComponent<Image>();
                if(image!=null)image.color=i==(int)activeNotebookTab?new Color(.26f,.31f,.29f,.98f):new Color(.08f,.105f,.1f,.94f);
                var label=notebookTabButtons[i].GetComponentInChildren<Text>();
                if(label!=null)label.color=i==(int)activeNotebookTab?new Color(.96f,.97f,.92f,1):new Color(.7f,.75f,.7f,1);
            }
        }

        private void CreateDocumentReader()
        {
            if(reportCard==null || body==null || documentViewport!=null)return;

            documentViewport=new GameObject("DocumentViewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
            documentViewport.transform.SetParent(reportCard,false);
            var viewportRect=documentViewport.GetComponent<RectTransform>();
            viewportRect.sizeDelta=bodySize;
            viewportRect.anchoredPosition=bodyPosition;
            var viewportImage=documentViewport.GetComponent<Image>();
            viewportImage.color=new Color(0,0,0,.001f);
            viewportImage.raycastTarget=true;

            var textObject=new GameObject("DocumentText",typeof(RectTransform),typeof(Text),typeof(Shadow));
            textObject.transform.SetParent(documentViewport.transform,false);
            documentText=textObject.GetComponent<Text>();
            documentText.font=body.font;
            documentText.fontSize=bodyFontSize;
            documentText.fontStyle=body.fontStyle;
            documentText.color=body.color;
            documentText.alignment=TextAnchor.UpperLeft;
            documentText.horizontalOverflow=HorizontalWrapMode.Wrap;
            documentText.verticalOverflow=VerticalWrapMode.Overflow;
            documentText.raycastTarget=false;
            var textRect=documentText.rectTransform;
            textRect.anchorMin=new Vector2(0,1);
            textRect.anchorMax=new Vector2(1,1);
            textRect.pivot=new Vector2(.5f,1);
            textRect.offsetMin=new Vector2(0,0);
            textRect.offsetMax=new Vector2(0,0);
            textRect.sizeDelta=new Vector2(0,bodySize.y);
            textRect.anchoredPosition=Vector2.zero;
            var shadow=textObject.GetComponent<Shadow>();
            shadow.effectColor=new Color(0,0,0,.88f);
            shadow.effectDistance=new Vector2(1,-1);

            documentScroll=documentViewport.GetComponent<ScrollRect>();
            documentScroll.viewport=viewportRect;
            documentScroll.content=textRect;
            documentScroll.horizontal=false;
            documentScroll.vertical=true;
            documentScroll.movementType=ScrollRect.MovementType.Clamped;
            documentScroll.scrollSensitivity=32f;
            documentScroll.inertia=true;
            documentScroll.decelerationRate=.14f;

            previousPageButton=CreateDocumentButton("PreviousPage","Q  이전 페이지",new Vector2(-190,-240));
            previousPageButton.onClick.AddListener(PreviousPage);

            pageIndicator=CreateDocumentLabel("PageIndicator","1 / 1",15,new Vector2(-10,-240),new Vector2(70,42),TextAnchor.MiddleCenter,new Color(.58f,.65f,.6f,1));
            documentState=CreateDocumentLabel("DocumentState","",13,new Vector2(270,280),new Vector2(190,26),TextAnchor.MiddleRight,new Color(.5f,.58f,.54f,1));

            documentViewport.SetActive(false);
            previousPageButton.gameObject.SetActive(false);
            pageIndicator.gameObject.SetActive(false);
            documentState.gameObject.SetActive(false);
        }

        private Button CreateDocumentButton(string name,string caption,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));
            go.transform.SetParent(reportCard,false);
            var rect=go.GetComponent<RectTransform>();
            rect.sizeDelta=new Vector2(270,48);
            rect.anchoredPosition=position;
            go.GetComponent<Image>().color=new Color(.1f,.13f,.125f,.98f);
            var button=go.GetComponent<Button>();
            var label=CreateDocumentLabel("Label",caption,18,Vector2.zero,new Vector2(250,42),TextAnchor.MiddleCenter,new Color(.86f,.89f,.84f,1),go.transform);
            label.fontStyle=FontStyle.Bold;
            return button;
        }

        private Text CreateDocumentLabel(string name,string value,int size,Vector2 position,Vector2 dimensions,TextAnchor anchor,Color color,Transform parent=null)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow));
            go.transform.SetParent(parent??reportCard,false);
            var rect=go.GetComponent<RectTransform>();
            rect.sizeDelta=dimensions;
            rect.anchoredPosition=position;
            var label=go.GetComponent<Text>();
            label.font=body.font;
            label.fontSize=size;
            label.text=value;
            label.color=color;
            label.alignment=anchor;
            label.raycastTarget=false;
            var shadow=go.GetComponent<Shadow>();
            shadow.effectColor=new Color(0,0,0,.9f);
            shadow.effectDistance=new Vector2(1,-1);
            return label;
        }

        public void CloseCase()
        {
            if (casePanel != null && casePanel.activeSelf) InteractionSoundscape.PlayDocumentClose();
            casePanel.SetActive(false);
            ResetActions();
        }
        private void LateUpdate()
        {
            crosshair.SetActive(!IsCaseOpen && player.IsCaptured);
            cursorHint.gameObject.SetActive(!IsCaseOpen && !player.IsCaptured);
            if(Time.unscaledTime>=nextObjectiveRefresh)
            {
                nextObjectiveRefresh=Time.unscaledTime+.2f;
                RefreshObjectiveUI();
            }
            if (toast != null && Time.unscaledTime >= toastUntil) toast.gameObject.SetActive(false);
        }
    }
}
