using UnityEngine;

namespace Archive0317
{
    public sealed class CaseFile : Inspectable
    {
        [SerializeField] private string caseTitle = "CASE 001 — 사라진 기록";
        [SerializeField, TextArea(5, 12)] private string description =
            "접수 시각  03:17\n분류  미해결 / 열람 제한\n\n야간 근무자의 실종 신고가 접수되었다. 마지막 출입 기록은 오전 3시 17분. 이후의 CCTV 기록은 비어 있다.\n\n현장에서 회수한 문서에는 같은 시각이 반복해서 적혀 있다. 담당 조사관은 원본 기록과 진술서를 대조해야 한다.\n\n현재 단계: 사건 파일 열람";
        [SerializeField] private CaseDefinition definition;
        public CaseDefinition Definition => definition;
        public string Title => definition != null ? definition.Title : caseTitle;
        public string Description => definition != null ? definition.OfficialRecord : description;
        public void Configure(CaseDefinition data) { definition = data; }
        public override void Inspect(FirstPersonPlayer player) => player.HUD.ShowCase(this);
    }
}
