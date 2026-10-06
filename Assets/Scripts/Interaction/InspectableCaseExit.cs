using System;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableCaseExit : Inspectable
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string destination,returnFlag;
        [SerializeField] private string[] requirements;
        public void Configure(CaseDefinition data,string scene,string flag,string[] conditions){definition=data;destination=scene;returnFlag=flag;requirements=conditions;}
        public bool Ready {get{if(definition==null)return false;var progress=CaseProgressStore.Get(definition);foreach(var flag in requirements??Array.Empty<string>())if(!progress.Has(flag))return false;return true;}}
        public override void Inspect(FirstPersonPlayer player)
        {if(!Ready){player.HUD.ShowToast("아직 확인하지 못한 현장 기록이 남아 있다.");return;}player.HUD.ShowExit(this);}
        public bool ReturnToArchive(){if(!Ready)return false;EvidenceCollection.Collect(definition);return SceneTransitionManager.TravelTo(definition,destination,returnFlag);}
    }
}
