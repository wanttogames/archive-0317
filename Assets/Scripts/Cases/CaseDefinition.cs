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
    [System.Serializable] public sealed class CaseObjectiveEntry
    {
        public string id;
        public string[] requirements;
        public string completionFlag;
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
        [SerializeField] private CaseObjectiveEntry[] objectives;
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
        public CaseObjectiveEntry[] Objectives => objectives;
        public void ConfigureNotebook(CaseNotebookEntry[] entries) { notebookEntries=entries; }
        public void ConfigureContradictions(CaseContradictionEntry[] entries) { contradictionEntries=entries; }
        public void ConfigureObjectives(CaseObjectiveEntry[] entries) { objectives=entries; }
        public CaseObjectiveEntry CurrentObjective(CaseProgress progress)
        {
            if(progress==null || objectives==null)return null;
            foreach(var objective in objectives)
            {
                if(objective==null || string.IsNullOrEmpty(objective.completionFlag) || progress.Has(objective.completionFlag))continue;
                bool ready=true;
                foreach(var flag in objective.requirements??System.Array.Empty<string>())
                    if(!progress.Has(flag)){ready=false;break;}
                if(ready)return objective;
            }
            return null;
        }
        public void Configure(string id, string heading, string scene, string record, string room, string[] requirements)
        { caseId = id; title = heading; fieldScene = scene; officialRecord = record; officialRoom = room; comparisonRequirements = requirements; }
    }
}
