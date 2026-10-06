using System;
using System.Text;
using UnityEngine;

namespace Archive0317
{
    public enum CaseVerdict { HumanCause, RecordErrorOrTampering, Unexplained, Deferred }
    public sealed class CaseReport : MonoBehaviour
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string returnedFlag,completedFlag,verdictKey;
        [SerializeField] private string[] requirements;
        [SerializeField] private CaseNotebookEntry[] confirmedFacts;
        public void ConfigureFacts(CaseNotebookEntry[] facts){confirmedFacts=facts;}
        public CaseDefinition Definition=>definition;
        public bool Completed=>CaseProgressStore.Get(definition).Has(completedFlag);
        public bool CanReview=>Completed || CaseProgressStore.Get(definition).Has(returnedFlag);
        public void Configure(CaseDefinition data,string returned,string completed,string verdict,string[] conditions)
        {definition=data;returnedFlag=returned;completedFlag=completed;verdictKey=verdict;requirements=conditions;}
        public string Summary()
        {
            var progress=CaseProgressStore.Get(definition);var text=new StringBuilder("확인된 주요 사실\n");
            foreach(var fact in confirmedFacts??Array.Empty<CaseNotebookEntry>())if(fact!=null && progress.Has(fact.progressFlag))text.AppendLine("· "+fact.text);
            text.AppendLine("\n수집 증거");foreach(var evidence in EvidenceCollection.Collect(definition))text.AppendLine("· "+evidence.title);
            if(Completed){text.AppendLine("\n보관 판정: "+VerdictLabel(progress.verdict));text.Append("사건 자료 보관 완료 / 원본 열람");}
            return text.ToString().TrimEnd();
        }
        public bool Confirm(CaseVerdict verdict)
        {
            if(Completed || !CanReview || !Enum.IsDefined(typeof(CaseVerdict),verdict))return false;
            var progress=CaseProgressStore.Get(definition);foreach(var flag in requirements??Array.Empty<string>())if(!progress.Has(flag))return false;
            EvidenceCollection.Collect(definition);CaseProgressStore.SaveVerdict(definition,verdict.ToString(),verdictKey,completedFlag);return true;
        }
        public static string VerdictLabel(string value)
        {switch(value){case "HumanCause":return "범죄/인위적 사건";case "RecordErrorOrTampering":return "기록 오류 또는 조작";case "Unexplained":return "설명 불가";case "Deferred":return "판단 보류";default:return "미분류";}}
    }
}
