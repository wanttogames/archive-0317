using System.Collections;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableCaseDoor : Inspectable
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private Transform leaf;
        [SerializeField] private string[] requirements;
        [SerializeField] private string firstFlag,openFlag,enteredFlag,exitFlag;
        [SerializeField] private AudioSource receiver,latch;
        private Quaternion closed;
        private bool moving;
        public bool IsOpen {get;private set;}
        public bool CuePlayed {get;private set;}
        public override string Prompt=>"E 열기";
        public void Configure(CaseDefinition data,Transform panel,string[] conditions,string first,string opened,string entered,string exit,AudioSource sound,AudioSource click)
        {definition=data;leaf=panel;requirements=conditions;firstFlag=first;openFlag=opened;enteredFlag=entered;exitFlag=exit;receiver=sound;latch=click;}
        private void Awake(){closed=leaf.localRotation;}
        private void Start(){if(CaseProgressStore.Get(definition).Has(openFlag))SetOpen(!CaseProgressStore.Get(definition).Has("RoomAltered") || CaseProgressStore.Get(definition).Has(exitFlag));}
        public override void Inspect(FirstPersonPlayer player)
        {
            if(moving || IsOpen)return;
            var progress=CaseProgressStore.Get(definition);
            if(!progress.Has(firstFlag))
            {player.HUD.ShowToast("문이 잠겨 있다.");CaseProgressStore.Mark(definition,firstFlag);StartCoroutine(Receiver());return;}
            foreach(var flag in requirements)if(!progress.Has(flag)){player.HUD.ShowToast("문이 잠겨 있다.");return;}
            if(progress.Has(enteredFlag) && !progress.Has(exitFlag)){player.HUD.ShowToast("손잡이가 움직이지 않는다.");return;}
            StartCoroutine(Open());
        }
        private IEnumerator Receiver(){yield return new WaitForSeconds(1.4f);if(receiver!=null)receiver.Play();CuePlayed=true;}
        private IEnumerator Open()
        {
            moving=true;yield return new WaitForSeconds(1);if(latch!=null)latch.Play();
            foreach(var c in leaf.GetComponentsInChildren<Collider>())c.enabled=false;
            var start=leaf.localRotation;var end=closed*Quaternion.Euler(0,-95,0);
            for(float t=0;t<2.4f;t+=Time.deltaTime){leaf.localRotation=Quaternion.Slerp(start,end,t/2.4f);yield return null;}
            SetOpen(true);CaseProgressStore.Mark(definition,openFlag);moving=false;
        }
        public void SetOpen(bool open)
        {if(leaf==null)return;leaf.localRotation=closed*Quaternion.Euler(0,open?-95:0,0);foreach(var c in leaf.GetComponentsInChildren<Collider>())c.enabled=!open;IsOpen=open;}
    }
}
