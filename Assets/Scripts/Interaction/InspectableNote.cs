using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableNote : Inspectable
    {
        [SerializeField, TextArea] private string observation;
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string progressFlag;
        [SerializeField] private bool keyPickupSound;
        public void ConfigureKeyPickupSound(bool enabled=true)=>keyPickupSound=enabled;
        public override bool PlayInspectSound=>!keyPickupSound;
        public void Configure(string text, CaseDefinition data = null, string flag = "") { observation = text; definition = data; progressFlag = flag; }
        public override void Inspect(FirstPersonPlayer player)
        {
            bool first=definition!=null && !string.IsNullOrEmpty(progressFlag) && !CaseProgressStore.Get(definition).Has(progressFlag);
            player.HUD.ShowToast(observation, 5); CaseProgressStore.Mark(definition, progressFlag);
            if(keyPickupSound && first)InteractionSoundscape.PlaySpareKey(transform.position);
        }
    }
}
