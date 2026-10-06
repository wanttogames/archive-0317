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
        public string Title => heading;
        public int PageCount => pages?.Length ?? 0;
        public string Page(int index) => pages != null && index >= 0 && index < pages.Length ? pages[index] : "";
        public void Configure(CaseDefinition data, string title, string[] contents, string flag, int recordPage = 0, string key = "", string value = "")
        { definition = data; heading = title; pages = contents; progressFlag = flag; evidencePage = recordPage; factKey = key; factValue = value; }
        public override void Inspect(FirstPersonPlayer player) => player.HUD.ShowDocument(this);
        public void Viewed(int page)
        {
            if (page != evidencePage) return;
            CaseProgressStore.Mark(definition, progressFlag);
            CaseProgressStore.RecordFact(definition, factKey, factValue);
        }
    }
}
