using UnityEngine;

namespace Archive0317
{
    public sealed class CaseSceneContext : MonoBehaviour
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private ArchiveHUD hud;
        private float pendingInvestigationSeconds;
        private float nextTimeFlush;
        public CaseDefinition Definition => definition;
        public void Configure(CaseDefinition data, ArchiveHUD display) { definition = data; hud = display; }
        private void Awake()
        {
            if (definition == null || hud == null) return;
            hud.SetDefinition(definition);
            CaseProgressStore.Mark(definition, "CaseStarted");
            CaseProgressStore.RecordFact(definition, "OfficialRoom", definition.OfficialRoom);
            nextTimeFlush = Time.unscaledTime + 10f;
        }

        private void Update()
        {
            if (definition == null || CaseProgressStore.Get(definition).Has("CaseCompleted")) return;
            if (PauseMenuController.IsOpen || MainMenuController.IsMenuOpen || SceneTransitionManager.IsTransitioning) return;
            pendingInvestigationSeconds += Time.unscaledDeltaTime;
            if (Time.unscaledTime >= nextTimeFlush) FlushInvestigationTime();
        }

        private void OnDisable() => FlushInvestigationTime();

        private void FlushInvestigationTime()
        {
            if (definition == null || pendingInvestigationSeconds <= 0f) return;
            CaseProgressStore.AddInvestigationTime(definition, pendingInvestigationSeconds);
            pendingInvestigationSeconds = 0f;
            nextTimeFlush = Time.unscaledTime + 10f;
        }
    }
}
