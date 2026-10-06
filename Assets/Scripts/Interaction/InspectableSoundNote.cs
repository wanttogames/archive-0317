using System.Collections;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableSoundNote : Inspectable
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string progressFlag;
        [SerializeField] private string observation;
        [SerializeField] private AudioSource cue;
        [SerializeField] private float delay=1.4f;
        public bool CuePlayed { get; private set; }
        public void Configure(CaseDefinition data,string flag,string message,AudioSource sound){definition=data;progressFlag=flag;observation=message;cue=sound;}
        public override void Inspect(FirstPersonPlayer player)
        {
            player.HUD.ShowToast(observation,3);
            bool first=!CaseProgressStore.Get(definition).Has(progressFlag);
            CaseProgressStore.Mark(definition,progressFlag);
            if(first && cue!=null)StartCoroutine(DelayedCue());
        }
        private IEnumerator DelayedCue(){yield return new WaitForSeconds(delay);cue.Play();CuePlayed=true;}
    }
}
