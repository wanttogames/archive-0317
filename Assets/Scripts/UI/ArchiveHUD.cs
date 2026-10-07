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
        private Button previousPageButton;
        private Text pageIndicator;
        private Text documentState;
        private Text documentText;
        private ScrollRect documentScroll;
        private GameObject documentViewport;
        private Text closeHintLabel;
        private string defaultCloseHint;
        public CaseDefinition ActiveDefinition { get; private set; }
        public bool IsCaseOpen => casePanel != null && casePanel.activeSelf;
        public bool IsPromptVisible => prompt != null && prompt.gameObject.activeSelf;
        public bool HasActiveDocument => activeDocument != null;
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

        public void SetDefinition(CaseDefinition definition) { ActiveDefinition = definition; }
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
            InteractionSoundscape.PlayDocumentOpen();
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
            if(previousPageButton!=null)previousPageButton.gameObject.SetActive(false);
            if(pageIndicator!=null)pageIndicator.gameObject.SetActive(false);
            if(documentState!=null)documentState.gameObject.SetActive(false);
            if(documentViewport!=null)documentViewport.SetActive(false);
            if(documentText!=null)documentText.text="";
            if(body!=null)body.gameObject.SetActive(true);
            if(closeHintLabel!=null)closeHintLabel.text=defaultCloseHint;
            if (compareButton != null) compareButton.gameObject.SetActive(false);
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
            if (toast != null && Time.unscaledTime >= toastUntil) toast.gameObject.SetActive(false);
        }
    }
}
