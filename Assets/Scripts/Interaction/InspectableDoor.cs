using System.Collections;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableDoor : Inspectable
    {
        [SerializeField] private Transform leaf;
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string requiredFlag;
        [SerializeField] private string lockedMessage = "문이 잠겨 있다.";
        [SerializeField] private bool permanentlyLocked;
        [SerializeField] private float openAngle = -95;
        private bool moving;
        public bool IsOpen { get; private set; }
        public override string Prompt => "E 열기";
        public void Configure(Transform panel, CaseDefinition data, string requirement, string message, bool locked = false)
        { leaf = panel; definition = data; requiredFlag = requirement; lockedMessage = message; permanentlyLocked = locked; }
        public override void Inspect(FirstPersonPlayer player)
        {
            if (moving || IsOpen) return;
            if (permanentlyLocked || (!string.IsNullOrEmpty(requiredFlag) && !CaseProgressStore.Get(definition).Has(requiredFlag)))
            { InteractionSoundscape.PlayLockedDoor(transform.position); player.HUD.ShowToast(lockedMessage); return; }
            InteractionSoundscape.PlayDoorLatch(transform.position);
            StartCoroutine(Open());
        }
        private IEnumerator Open()
        {
            moving = true;
            // Disable only the swinging leaf collider; frame and room boundaries remain solid.
            foreach (var collider in leaf.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var start = leaf.localRotation;
            var end = start * Quaternion.Euler(0, openAngle, 0);
            InteractionSoundscape.PlayDoorCreak(leaf.position);
            for (float elapsed = 0; elapsed < .4f; elapsed += Time.deltaTime)
            { leaf.localRotation = Quaternion.Slerp(start, end, elapsed / .4f); yield return null; }
            leaf.localRotation = end; IsOpen = true; moving = false;
        }
    }
}
