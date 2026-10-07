using System.Collections.Generic;
using UnityEngine;

namespace Archive0317
{
    public sealed class Room403TVWatcher : MonoBehaviour
    {
        public enum WatcherStage { Normal, Doorway, Approaching, Behind, Finished }
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private Collider roomArea;
        [SerializeField] private Renderer screen;
        [SerializeField] private Transform proxy, figure;
        [SerializeField] private Vector3 doorwayPosition;
        private InspectableCaseTelevision television;
        private float awaySeconds, seenSeconds, elapsed, stageAge;
        private bool stageObserved;
        private const string SnapshotFact="Room403TVWatcherSnapshot";
        [System.Serializable] private sealed class Snapshot
        {
            public WatcherStage stage;
            public float elapsed, stageAge;
            public bool observed;
            public Vector3 figurePosition, viewedPosition, viewedForward;
        }
        private Vector3 lastViewedPosition, lastViewedForward;
        private struct Pose { public float time;public Vector3 position;public Quaternion rotation; }
        private readonly Queue<Pose> poses=new Queue<Pose>();
        public WatcherStage Stage { get; private set; }
        public float StageAge=>stageAge;
        public bool LookingAtTV { get; private set; }
        public bool CanRender=>roomArea!=null && player!=null && roomArea.bounds.Contains(player.transform.position+Vector3.up*.8f);
        public Transform Figure=>figure;
        public Transform PlayerProxy=>proxy;
        public void Configure(CaseDefinition data,FirstPersonPlayer controller,Collider area,Renderer surface,Transform body,Transform watcher,Vector3 doorway)
        {definition=data;player=controller;roomArea=area;screen=surface;proxy=body;figure=watcher;doorwayPosition=doorway;}
        private void Start()
        {
            Stage=WatcherStage.Normal;awaySeconds=seenSeconds=elapsed=stageAge=0;stageObserved=false;poses.Clear();
            television=GetComponent<InspectableCaseTelevision>();figure.gameObject.SetActive(false);
            proxy.position=player.transform.position;proxy.rotation=player.transform.rotation;
            var progress=CaseProgressStore.Get(definition);
            if(progress.Has("TVWatcherFinished") || progress.Has("KeyTagVisible") || progress.Has("KeyEvidenceFound")){Finish(false);return;}
            var json=progress.Fact(SnapshotFact);
            if(string.IsNullOrEmpty(json))return;
            Snapshot saved;
            try{saved=JsonUtility.FromJson<Snapshot>(json);}catch(System.ArgumentException){return;}
            if(saved==null || saved.stage<WatcherStage.Normal || saved.stage>=WatcherStage.Finished)return;
            Stage=saved.stage;elapsed=Mathf.Clamp(saved.elapsed,0,40);stageAge=Mathf.Clamp(saved.stageAge,0,40);stageObserved=saved.observed;
            lastViewedPosition=saved.viewedPosition;lastViewedForward=saved.viewedForward;
            if(Stage!=WatcherStage.Normal){figure.position=saved.figurePosition;figure.rotation=Quaternion.LookRotation(lastViewedForward.sqrMagnitude>.1f?lastViewedForward:Vector3.forward);figure.gameObject.SetActive(true);}
        }
        private void Update()
        {
            if(television==null || player==null || PauseMenuController.IsOpen || MainMenuController.IsMenuOpen || SceneTransitionManager.IsTransitioning)return;
            Tick(Time.deltaTime);
        }
        public void Tick(float delta)
        {
            var progress=CaseProgressStore.Get(definition);
            if(Stage==WatcherStage.Finished)return;
            if(progress.Has("KeyEvidenceFound") || progress.Has("KeyTagVisible")){Finish(false);return;}
            if(!television.FootageVisible || !progress.Has("TelevisionInspected"))return;
            elapsed+=delta;stageAge+=delta;
            if(elapsed>=40){Finish(false);return;}
            if(!CanRender || player.HUD.IsCaseOpen)return;
            var offset=screen.bounds.center-player.ViewCamera.transform.position;
            LookingAtTV=offset.magnitude<=4 && Vector3.Angle(player.ViewCamera.transform.forward,offset)<24;
            if(LookingAtTV && Physics.Linecast(player.ViewCamera.transform.position,screen.bounds.center,out var hit,~0,QueryTriggerInteraction.Ignore))
                LookingAtTV=hit.collider.transform.IsChildOf(transform) || hit.collider.gameObject==screen.gameObject;
            if(LookingAtTV)
            {
                awaySeconds=0;seenSeconds+=delta;
                lastViewedPosition=player.transform.position;
                lastViewedForward=Vector3.ProjectOnPlane(player.ViewCamera.transform.forward,Vector3.up).normalized;
                if(seenSeconds>=.8f && !stageObserved){stageObserved=true;SaveSnapshot();}
                return; // Never move the figure while the player watches the screen.
            }
            seenSeconds=0;awaySeconds+=delta;
            if(!stageObserved || awaySeconds<(Stage==WatcherStage.Behind?1f:.65f) || (Stage!=WatcherStage.Behind && stageAge<5f))return;
            if(Stage==WatcherStage.Behind){Finish(true);return;}
            Stage=(WatcherStage)((int)Stage+1);stageObserved=false;awaySeconds=stageAge=0;
            Vector3 position=Stage==WatcherStage.Doorway?doorwayPosition:lastViewedPosition-lastViewedForward*(Stage==WatcherStage.Approaching?2f:.65f);
            position.x=Mathf.Clamp(position.x,-21.85f,-18.35f);position.z=Mathf.Clamp(position.z,2.7f,6.35f);position.y=9.05f;
            // Keep the distant figure on the narrow floor strip behind the bed.
            if(Stage==WatcherStage.Approaching && position.x<-20.15f && position.z>3.85f && position.z<6.15f)position.z=6.25f;
            figure.position=position;figure.rotation=Quaternion.LookRotation(lastViewedForward.sqrMagnitude>.1f?lastViewedForward:Vector3.forward);figure.gameObject.SetActive(true);
            SaveSnapshot();
        }
        public void BeforeFrame()
        {
            var forward=Vector3.ProjectOnPlane(player.ViewCamera.transform.forward,Vector3.up).normalized;
            var rotation=forward.sqrMagnitude>.1f?Quaternion.LookRotation(forward):player.transform.rotation;
            if(poses.Count==0 && !proxy.gameObject.activeSelf){proxy.position=player.transform.position;proxy.rotation=rotation;}
            poses.Enqueue(new Pose{time=Time.time,position=player.transform.position,rotation=rotation});
            while(poses.Count>1 && (poses.Peek().time<Time.time-.3f || poses.Count>8))
            {var pose=poses.Dequeue();proxy.position=pose.position;proxy.rotation=pose.rotation;}
            proxy.gameObject.SetActive(CanRender);
        }
        private void Finish(bool staticPulse)
        {
            Stage=WatcherStage.Finished;figure.gameObject.SetActive(false);
            CaseProgressStore.Mark(definition,"TVWatcherFinished");
            if((staticPulse || LookingAtTV) && television!=null)television.RequestStatic(.7f);
        }
        private void SaveSnapshot()
        {
            if(!Application.isPlaying || definition==null || figure==null || !CaseProgressStore.Get(definition).Has("TelevisionInspected") || Stage==WatcherStage.Finished)return;
            CaseProgressStore.RecordFact(definition,SnapshotFact,JsonUtility.ToJson(new Snapshot{stage=Stage,elapsed=elapsed,stageAge=stageAge,observed=stageObserved,figurePosition=figure.position,viewedPosition=lastViewedPosition,viewedForward=lastViewedForward}));
        }
        private void OnApplicationQuit()=>SaveSnapshot();
        private void OnDisable(){SaveSnapshot();if(figure!=null)figure.gameObject.SetActive(false);if(proxy!=null)proxy.gameObject.SetActive(false);poses.Clear();}
    }
}
