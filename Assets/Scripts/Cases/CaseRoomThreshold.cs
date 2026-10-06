using UnityEngine;

namespace Archive0317
{
    public sealed class CaseRoomThreshold : MonoBehaviour
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private Collider area;
        [SerializeField] private InspectableCaseDoor door;
        [SerializeField] private Transform destination;
        [SerializeField] private string requiredFlag,blockedFlag,progressFlag;
        [SerializeField] private string completionRequirement;
        public void ConfigureCompletionCondition(string flag){completionRequirement=flag;}
        private float cooldown;
        public void Configure(CaseDefinition data,FirstPersonPlayer controller,Collider volume,InspectableCaseDoor panel,Transform point,string required,string blocked,string flag)
        {definition=data;player=controller;area=volume;door=panel;destination=point;requiredFlag=required;blockedFlag=blocked;progressFlag=flag;}
        private void Update(){Evaluate();}
        public void Evaluate()
        {
            if(player==null || Time.time<cooldown || player.HUD.IsCaseOpen || !door.IsOpen)return;
            var progress=CaseProgressStore.Get(definition);
            if(!progress.Has(requiredFlag) || (!string.IsNullOrEmpty(blockedFlag) && progress.Has(blockedFlag)))return;
            if(!area.bounds.Contains(player.transform.position+Vector3.up*.8f))return;
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=destination.position;controller.enabled=true;Physics.SyncTransforms();
            if(string.IsNullOrEmpty(completionRequirement) || progress.Has(completionRequirement))CaseProgressStore.Mark(definition,progressFlag);cooldown=Time.time+1;
        }
    }
}
