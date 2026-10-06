using UnityEngine;

namespace Archive0317
{
    public sealed class CaseProgressTrigger : MonoBehaviour
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string progressFlag;
        [SerializeField] private AudioSource quietCue;
        [SerializeField] private string repeatFlag;
        public void ConfigureRevisit(string flag){repeatFlag=flag;}
        public void Configure(CaseDefinition data, string flag, AudioSource cue = null) { definition = data; progressFlag = flag; quietCue = cue; }
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonPlayer>() == null || definition == null) return;
            bool first = !CaseProgressStore.Get(definition).Has(progressFlag);
            CaseProgressStore.Mark(definition, progressFlag);
            if(!first)CaseProgressStore.Mark(definition,repeatFlag);
            if (first && quietCue != null) quietCue.Play();
        }
    }
}
