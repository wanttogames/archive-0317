using UnityEngine;
using UnityEngine.UI;

namespace Archive0317
{
    public sealed class ArchiveHUD : MonoBehaviour
    {
        [SerializeField] private GameObject casePanel;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private Text prompt;
        [SerializeField] private Text title;
        [SerializeField] private Text body;
        [SerializeField] private Text cursorHint;
        [SerializeField] private FirstPersonPlayer player;
        public bool IsCaseOpen => casePanel != null && casePanel.activeSelf;
        public bool IsPromptVisible => prompt != null && prompt.gameObject.activeSelf;
        public void Configure(GameObject panel, GameObject aim, Text interaction, Text heading, Text description, Text hint, FirstPersonPlayer controller)
        { casePanel = panel; crosshair = aim; prompt = interaction; title = heading; body = description; cursorHint = hint; player = controller; }
        public void SetPrompt(bool visible) { prompt.gameObject.SetActive(visible && !IsCaseOpen); }
        public void ShowCase(CaseFile file)
        { title.text = file.Title; body.text = file.Description; casePanel.SetActive(true); SetPrompt(false); }
        public void CloseCase() { casePanel.SetActive(false); }
        private void LateUpdate()
        {
            crosshair.SetActive(!IsCaseOpen && player.IsCaptured);
            cursorHint.gameObject.SetActive(!IsCaseOpen && !player.IsCaptured);
        }
    }
}
