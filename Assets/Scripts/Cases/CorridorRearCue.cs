using UnityEngine;

namespace Archive0317
{
    public sealed class CorridorRearCue : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private Transform figure;
        [SerializeField] private Transform[] doorways;
        [SerializeField] private AudioSource source;
        private AudioClip[] clips;
        private Vector3 previousPosition;
        private Quaternion observedRotation;
        private float cooldown = 18, distance, remaining;
        private int appearanceAt;
        private bool awaitingLook, observed;
        public int CueCount { get; private set; }
        public bool FigureVisible => figure != null && figure.gameObject.activeSelf;

        public void Configure(FirstPersonPlayer controller, CaseDefinition data, Transform silhouette, Transform[] anchors, AudioSource audio)
        { player=controller;definition=data;figure=silhouette;doorways=anchors;source=audio; }
        private void Start()
        {
            previousPosition=player.transform.position;appearanceAt=Random.Range(2,4);
            figure.gameObject.SetActive(false);
            clips=new[]{MakeClip(0),MakeClip(1),MakeClip(2)};
        }
        private void Update(){if(player==null)return;if(player.IsCaptured)Evaluate(Time.deltaTime);else{previousPosition=player.transform.position;Hide();}}
        public void Evaluate(float delta)
        {
            var position=player.transform.position;
            if(player.HUD.IsCaseOpen || MainMenuController.IsMenuOpen || CaseProgressStore.Get(definition).Has("Case001Completed"))
            { previousPosition=position;Hide();return; }
            bool inCorridor=position.y>8.8f && position.y<9.6f && position.x>-.95f && position.x<.95f && position.z>-.5f && position.z<8.8f;
            if(!inCorridor){previousPosition=position;distance=0;Hide();return;}
            if(awaitingLook)
            {
                remaining-=delta;
                var to=figure.position+Vector3.up-player.ViewCamera.transform.position;
                bool visible=Vector3.Angle(player.ViewCamera.transform.forward,to)<28 && !Physics.Linecast(player.ViewCamera.transform.position,figure.position+Vector3.up,~0,QueryTriggerInteraction.Ignore);
                if(visible && !observed){observed=true;observedRotation=player.ViewCamera.transform.rotation;}
                if(remaining<=0 || (observed && Quaternion.Angle(observedRotation,player.ViewCamera.transform.rotation)>7))Hide();
            }
            float moved=Vector3.Distance(position,previousPosition);previousPosition=position;
            if(moved>.7f)return; // Teleports are not walking.
            cooldown-=delta;distance+=moved;
            if(CueCount>=3 || cooldown>0 || distance<5 || moved<.001f)return;
            Transform rear=null;float best=0;
            foreach(var anchor in doorways)
            {
                var offset=anchor.position-position;float length=offset.magnitude;
                if(length>4 && length<10 && Vector3.Dot(player.ViewCamera.transform.forward,offset.normalized)<-.5f && length>best){rear=anchor;best=length;}
            }
            if(rear==null)return;
            CueCount++;distance=0;cooldown=Random.Range(38f,55f);
            source.transform.position=rear.position+Vector3.up*.3f;source.PlayOneShot(clips[(CueCount-1)%clips.Length]);
            if(CueCount==appearanceAt)
            {
                figure.position=rear.position;figure.LookAt(new Vector3(position.x,figure.position.y,position.z));
                figure.gameObject.SetActive(true);awaitingLook=true;observed=false;remaining=9;
            }
        }
        private void Hide(){if(figure!=null)figure.gameObject.SetActive(false);awaitingLook=false;observed=false;}
        private AudioClip MakeClip(int kind)
        {
            int rate=22050;float duration=kind==1?.65f:.26f;var samples=new float[(int)(rate*duration)];var rng=new System.Random(317+kind);
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate,noise=(float)rng.NextDouble()*2-1;
                float envelope=kind==1?Mathf.Sin(t/duration*Mathf.PI)*.16f:Mathf.Exp(-t*(kind==0?45:22))*.22f;
                samples[i]=(noise*(kind==1?.55f:.25f)+Mathf.Sin(t*(kind==0?2400:kind==1?650:430))*.4f)*envelope;
            }
            var clip=AudioClip.Create("RearCue_"+kind,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        private void OnDisable(){Hide();if(source!=null)source.Stop();}
        private void OnDestroy(){if(clips!=null)foreach(var clip in clips)if(clip!=null)Destroy(clip);}
    }
}
