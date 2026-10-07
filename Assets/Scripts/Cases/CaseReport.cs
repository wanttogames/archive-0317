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
            if(Completed)return CompletionSummary();
            var progress=CaseProgressStore.Get(definition);var text=new StringBuilder("확인된 주요 사실\n");
            foreach(var fact in confirmedFacts??Array.Empty<CaseNotebookEntry>())if(fact!=null && progress.Has(fact.progressFlag))text.AppendLine("· "+fact.text);
            text.AppendLine("\n수집 증거");foreach(var evidence in EvidenceCollection.Collect(definition))text.AppendLine("· "+evidence.title);
            return text.ToString().TrimEnd();
        }

        public string CompletionSummary()
        {
            var progress=CaseProgressStore.Get(definition);
            var collected=EvidenceCollection.Collect(definition);
            int totalEvidence=(definition.Evidence??Array.Empty<EvidenceDefinition>()).Length;
            int missing=Mathf.Max(0,totalEvidence-collected.Count);
            int contradictionCount=0;
            foreach(var entry in definition.ContradictionEntries??Array.Empty<CaseContradictionEntry>())
                if(entry!=null && progress.Has(entry.progressFlag))contradictionCount++;

            var text=new StringBuilder();
            text.AppendLine("보관 판정");
            text.AppendLine("  "+VerdictLabel(progress.verdict));
            text.AppendLine();
            text.AppendLine("조사 결과");
            text.AppendLine("  확보 증거     "+collected.Count+" / "+totalEvidence);
            text.AppendLine("  놓친 증거     "+missing);
            text.AppendLine("  확인한 모순   "+contradictionCount+" / "+(definition.ContradictionEntries??Array.Empty<CaseContradictionEntry>()).Length);
            text.AppendLine("  현장 조사     "+FormatDuration(progress.investigationSeconds));
            text.AppendLine();
            text.Append("사건 자료 보관 완료 / 원본 열람 가능");
            return text.ToString();
        }

        private static string FormatDuration(float seconds)
        {
            int total=Mathf.Max(0,Mathf.RoundToInt(seconds));
            int minutes=total/60;
            int remain=total%60;
            return minutes.ToString("00")+":"+remain.ToString("00");
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
