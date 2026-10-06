using System;
using UnityEngine;

namespace Archive0317
{
    [Serializable] public sealed class EnvironmentChange
    {
        public GameObject target;
        public bool changeActive; public bool active;
        public bool changePosition; public Vector3 localPosition;
        public bool changeRotation; public Vector3 localEulerAngles;
        public Light light; public bool lightEnabled;
        public TextMesh label; public string text;
        public void Apply()
        {
            if(target!=null){if(changePosition)target.transform.localPosition=localPosition;if(changeRotation)target.transform.localEulerAngles=localEulerAngles;if(changeActive)target.SetActive(active);}
            if(light!=null)light.enabled=lightEnabled;
            if(label!=null)label.text=text;
        }
    }
    [Serializable] public sealed class EnvironmentRule
    {
        public string[] requirements;
        public string completionFlag;
        public string[] flagsToMark;
        public Collider insideArea;
        public Collider outsideArea;
        public Renderer hiddenFromView;
        public EnvironmentChange[] changes;
        public AudioSource cue;
        public string observation;
        public float delayAfterReady;
    }
    public sealed class CaseEnvironmentState : MonoBehaviour
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private EnvironmentRule[] rules;
        private float nextCheck;
        private readonly System.Collections.Generic.Dictionary<EnvironmentRule,float> readySince=new System.Collections.Generic.Dictionary<EnvironmentRule,float>();
        public void Configure(CaseDefinition data,FirstPersonPlayer controller,EnvironmentRule[] states){definition=data;player=controller;rules=states;}
        private void Start()
        {
            if(definition==null)return;
            foreach(var rule in rules??Array.Empty<EnvironmentRule>())if(rule!=null && CaseProgressStore.Get(definition).Has(rule.completionFlag))Apply(rule,false);
        }
        private void Update()
        {
            if(Time.time<nextCheck || player==null || player.HUD.IsCaseOpen || definition==null)return;
            nextCheck=Time.time+.15f;
            Evaluate();
        }
        public void Evaluate()
        {
            if(player==null || player.HUD.IsCaseOpen || definition==null)return;
            var progress=CaseProgressStore.Get(definition);
            foreach(var rule in rules??Array.Empty<EnvironmentRule>())
            {
                if(rule==null || string.IsNullOrEmpty(rule.completionFlag))continue;
                if(progress.Has(rule.completionFlag))continue;
                bool ready=true;foreach(var flag in rule.requirements??Array.Empty<string>())if(!progress.Has(flag))ready=false;
                if(!ready){readySince.Remove(rule);continue;}
                if(rule.delayAfterReady>0)
                {
                    if(!readySince.TryGetValue(rule,out float since)){since=Time.time;readySince[rule]=since;}
                    if(Time.time-since<rule.delayAfterReady)continue;
                }
                if(rule.insideArea!=null && !rule.insideArea.bounds.Contains(player.transform.position+Vector3.up*.8f))continue;
                if(rule.outsideArea!=null && rule.outsideArea.bounds.Contains(player.transform.position+Vector3.up*.8f))continue;
                if(rule.hiddenFromView!=null && IsVisible(rule.hiddenFromView))continue;
                Apply(rule,true);
                foreach(var flag in rule.flagsToMark??Array.Empty<string>())CaseProgressStore.Mark(definition,flag);
                CaseProgressStore.Mark(definition,rule.completionFlag);
            }
        }
        private bool IsVisible(Renderer target)
        {
            var camera=player.ViewCamera;
            if(!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),target.bounds))return false;
            Vector3 destination=target.bounds.center;
            if(Physics.Linecast(camera.transform.position,destination,out var hit,~0,QueryTriggerInteraction.Ignore))
                return hit.collider.gameObject==target.gameObject || hit.collider.transform.IsChildOf(target.transform);
            return true;
        }
        private void Apply(EnvironmentRule rule,bool playCue)
        {
            foreach(var change in rule.changes??Array.Empty<EnvironmentChange>())change?.Apply();
            if(playCue && rule.cue!=null)rule.cue.Play();
            if(playCue && !string.IsNullOrEmpty(rule.observation))player.HUD.ShowToast(rule.observation,3);
        }
    }
}
