using UnityEngine;

namespace Archive0317
{
    public sealed class CaseFile : MonoBehaviour
    {
        [SerializeField] private string caseTitle = "CASE 001 — 사라진 기록";
        [SerializeField, TextArea(5, 12)] private string description =
            "접수 시각  03:17\n분류  미해결 / 열람 제한\n\n야간 근무자의 실종 신고가 접수되었다. 마지막 출입 기록은 오전 3시 17분. 이후의 CCTV 기록은 비어 있다.\n\n현장에서 회수한 문서에는 같은 시각이 반복해서 적혀 있다. 담당 조사관은 원본 기록과 진술서를 대조해야 한다.\n\n현재 단계: 사건 파일 열람";
        public string Title => caseTitle;
        public string Description => description;
    }
}
