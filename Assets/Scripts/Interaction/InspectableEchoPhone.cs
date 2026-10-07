using System.Collections;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableEchoPhone : Inspectable
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string[] requirements;
        [SerializeField] private AudioSource ringing,receiver;
        [SerializeField] private AudioClip noise;
        [SerializeField] private AudioSource[] roomSounds;
        private bool answering;
        private float nextCheck;
        public bool Ringing=>ringing!=null && ringing.isPlaying;
        public bool Answering=>answering;
        public override bool IsInteractionAvailable=>!answering && isActiveAndEnabled;
        public override float InteractionCooldown=>.3f;
        public override string Prompt=>answering?"통화 중…":"E 조사";
        public bool EchoPlayed {get;private set;}
        public void Configure(CaseDefinition data,string[] conditions,AudioSource bell,AudioSource handset,AudioClip staticNoise,AudioSource[] ambience)
        {definition=data;requirements=conditions;ringing=bell;receiver=handset;noise=staticNoise;roomSounds=ambience;}
        private void Update()
        {
            if(Time.time<nextCheck)return;nextCheck=Time.time+.25f;var p=CaseProgressStore.Get(definition);
            if(p.Has("PhoneEventTriggered"))return;
            foreach(var flag in requirements)if(!p.Has(flag))return;
            ringing.Play();CaseProgressStore.Mark(definition,"PhoneEventTriggered");
        }
        public override void Inspect(FirstPersonPlayer player)
        {
            if(answering)return;var p=CaseProgressStore.Get(definition);
            if(!p.Has("PhoneEventTriggered")){player.HUD.ShowToast("수화기에서는 아무 소리도 들리지 않는다.");CaseProgressStore.Mark(definition,"PhoneInspected");return;}
            if(p.Has("PhoneEventAnswered")){player.HUD.ShowToast("연결음이 끊어져 있다.");return;}
            StartCoroutine(Answer());
        }
        private IEnumerator Answer()
        {
            answering=true;ringing.Stop();receiver.clip=noise;receiver.loop=false;receiver.Play();yield return new WaitForSeconds(2);receiver.Stop();
            foreach(var source in roomSounds)if(source!=null && source.clip!=null)receiver.PlayOneShot(source.clip,.55f);
            EchoPlayed=true;yield return new WaitForSeconds(4);receiver.Stop();answering=false;CaseProgressStore.Mark(definition,"PhoneEventAnswered");
        }
        private void OnDisable(){if(receiver!=null)receiver.Stop();if(ringing!=null)ringing.Stop();answering=false;}
    }
}
