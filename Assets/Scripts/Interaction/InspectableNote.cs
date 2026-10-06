using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableNote : Inspectable
    {
        [SerializeField, TextArea] private string observation;
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string progressFlag;
        public void Configure(string text, CaseDefinition data = null, string flag = "") { observation = text; definition = data; progressFlag = flag; }
        public override void Inspect(FirstPersonPlayer player)
        { player.HUD.ShowToast(observation, 5); CaseProgressStore.Mark(definition, progressFlag); }
    }
}
