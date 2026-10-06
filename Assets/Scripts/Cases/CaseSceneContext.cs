using UnityEngine;

namespace Archive0317
{
    public sealed class CaseSceneContext : MonoBehaviour
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private ArchiveHUD hud;
        public CaseDefinition Definition => definition;
        public void Configure(CaseDefinition data, ArchiveHUD display) { definition = data; hud = display; }
        private void Awake()
        {
            if (definition == null || hud == null) return;
            hud.SetDefinition(definition);
            CaseProgressStore.Mark(definition, "CaseStarted");
            CaseProgressStore.RecordFact(definition, "OfficialRoom", definition.OfficialRoom);
        }
    }
}
