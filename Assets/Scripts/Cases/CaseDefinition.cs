using UnityEngine;

namespace Archive0317
{
    [System.Serializable] public sealed class CaseNotebookEntry
    {
        public string progressFlag;
        public string text;
        public string factKey;
    }
    [System.Serializable] public sealed class CaseContradictionEntry
    {
        public string progressFlag;
        public string text;
    }
    [CreateAssetMenu(menuName = "Archive 03:17/Case Definition")]
    public sealed class CaseDefinition : ScriptableObject
    {
        [SerializeField] private string caseId;
        [SerializeField] private string title;
        [SerializeField] private string fieldScene;
        [SerializeField, TextArea(5, 12)] private string officialRecord;
        [SerializeField] private string officialRoom;
        [SerializeField] private string[] comparisonRequirements;
        [SerializeField] private CaseNotebookEntry[] notebookEntries;
        [SerializeField] private CaseContradictionEntry[] contradictionEntries;
        [SerializeField] private EvidenceDefinition[] evidence;
        public EvidenceDefinition[] Evidence=>evidence;
        public void ConfigureEvidence(EvidenceDefinition[] entries){evidence=entries;}
        public string Id => caseId;
        public string Title => title;
        public string FieldScene => fieldScene;
        public string OfficialRecord => officialRecord;
        public string OfficialRoom => officialRoom;
        public string[] ComparisonRequirements => comparisonRequirements;
        public CaseNotebookEntry[] NotebookEntries => notebookEntries;
        public CaseContradictionEntry[] ContradictionEntries => contradictionEntries;
        public void ConfigureNotebook(CaseNotebookEntry[] entries) { notebookEntries=entries; }
        public void ConfigureContradictions(CaseContradictionEntry[] entries) { contradictionEntries=entries; }
        public void Configure(string id, string heading, string scene, string record, string room, string[] requirements)
        { caseId = id; title = heading; fieldScene = scene; officialRecord = record; officialRoom = room; comparisonRequirements = requirements; }
    }
}
