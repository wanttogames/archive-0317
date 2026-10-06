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
        public string Title => heading;
        public int PageCount => pages?.Length ?? 0;
        private bool Alternate => definition != null && !string.IsNullOrEmpty(alternateCondition) && CaseProgressStore.Get(definition).Has(alternateCondition);
        public string Page(int index) { var contents=Alternate?alternatePages:pages; return contents!=null && index>=0 && index<contents.Length?contents[index]:""; }
        public void ConfigureVariant(string condition,string[] contents,string viewedFlag,string value=""){alternateCondition=condition;alternatePages=contents;alternateViewedFlag=viewedFlag;alternateFactValue=value;}
        public void Configure(CaseDefinition data, string title, string[] contents, string flag, int recordPage = 0, string key = "", string value = "")
        { definition = data; heading = title; pages = contents; progressFlag = flag; evidencePage = recordPage; factKey = key; factValue = value; }
        public override void Inspect(FirstPersonPlayer player) => player.HUD.ShowDocument(this);
        public void Viewed(int page)
        {
            if (page != evidencePage) return;
            CaseProgressStore.Mark(definition, progressFlag);
            if(Alternate)CaseProgressStore.Mark(definition,alternateViewedFlag);
            CaseProgressStore.RecordFact(definition, factKey, Alternate && !string.IsNullOrEmpty(alternateFactValue)?alternateFactValue:factValue);
        }
    }
}
