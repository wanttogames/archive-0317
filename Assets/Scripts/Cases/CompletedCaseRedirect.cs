using UnityEngine;

namespace Archive0317
{
    public sealed class CompletedCaseRedirect : MonoBehaviour
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string completedFlag,destination;
        public void Configure(CaseDefinition data,string flag,string scene){definition=data;completedFlag=flag;destination=scene;}
        private void Start(){if(definition!=null && CaseProgressStore.Get(definition).Has(completedFlag))SceneTransitionManager.TravelTo(definition,destination,"");}
    }
}
