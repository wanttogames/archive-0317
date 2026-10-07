using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableDocument : Inspectable
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string heading;
        [SerializeField, TextArea(4, 14)] private string[] pages;
        [SerializeField] private string progressFlag;
        [SerializeField] private int evidencePage;
        [SerializeField] private string factKey;
        [SerializeField] private string factValue;
        [SerializeField] private string alternateCondition;
        [SerializeField, TextArea(4, 14)] private string[] alternatePages;
        [SerializeField] private string alternateViewedFlag;
        [SerializeField] private string alternateFactValue;
        [SerializeField] private Texture pageImage;
        [SerializeField] private int imagePage=-1;
        [SerializeField] private string imageCaption;
        [SerializeField] private Texture[] pageImages;
        [SerializeField] private string[] pageImageCaptions;
        [SerializeField] private string inspectionPrompt;
        [SerializeField] private bool keyEvidenceSound;
        public bool KeyEvidenceSound=>keyEvidenceSound;
        public void ConfigureKeyEvidenceSound(bool enabled=true)=>keyEvidenceSound=enabled;
        public override bool PlayInspectSound=>!keyEvidenceSound;
        public Texture Image(int index)=>pageImages!=null && index>=0 && index<pageImages.Length && pageImages[index]!=null?pageImages[index]:index==imagePage?pageImage:null;
        public string ImageCaption=>imageCaption;
        public string Caption(int index)=>pageImageCaptions!=null && index>=0 && index<pageImageCaptions.Length?pageImageCaptions[index]:imageCaption;
        public void ConfigureImages(Texture[] images,string[] captions,string prompt){pageImages=images;pageImageCaptions=captions;inspectionPrompt=prompt;}
        public void ConfigureImage(int page,Texture image,string caption){imagePage=page;pageImage=image;imageCaption=caption;}
        public string Title => heading;
        public CaseDefinition Definition => definition;
        private bool Alternate => definition != null && !string.IsNullOrEmpty(alternateCondition) && CaseProgressStore.Get(definition).Has(alternateCondition);
        public int PageCount { get { var contents=Alternate?alternatePages:pages; return contents?.Length ?? 0; } }
        public bool IsRecorded => definition != null && !string.IsNullOrEmpty(progressFlag) && CaseProgressStore.Get(definition).Has(progressFlag);
        public bool IsEvidencePage(int index) => index == evidencePage;
        public override string Prompt => !string.IsNullOrEmpty(inspectionPrompt)?inspectionPrompt:IsRecorded ? "E 다시 읽기" : "E 문서 읽기";
        public string Page(int index) { var contents=Alternate?alternatePages:pages; return contents!=null && index>=0 && index<contents.Length?contents[index]:""; }
        public void ConfigureVariant(string condition,string[] contents,string viewedFlag,string value=""){alternateCondition=condition;alternatePages=contents;alternateViewedFlag=viewedFlag;alternateFactValue=value;}
        public void Configure(CaseDefinition data, string title, string[] contents, string flag, int recordPage = 0, string key = "", string value = "")
        { definition = data; heading = title; pages = contents; progressFlag = flag; evidencePage = recordPage; factKey = key; factValue = value; }
        public override void Inspect(FirstPersonPlayer player) => player.HUD.ShowDocument(this);
        public bool Viewed(int page)
        {
            if (page != evidencePage || definition == null) return false;
            var progress = CaseProgressStore.Get(definition);
            bool newlyRecorded = !string.IsNullOrEmpty(progressFlag) && !progress.Has(progressFlag);
            bool alternateNew = Alternate && !string.IsNullOrEmpty(alternateViewedFlag) && !progress.Has(alternateViewedFlag);
            CaseProgressStore.Mark(definition, progressFlag);
            if(Alternate)CaseProgressStore.Mark(definition,alternateViewedFlag);
            CaseProgressStore.RecordFact(definition, factKey, Alternate && !string.IsNullOrEmpty(alternateFactValue)?alternateFactValue:factValue);
            return newlyRecorded || alternateNew;
        }
    }
}
