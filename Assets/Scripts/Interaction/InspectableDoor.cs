using System.Collections;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableDoor : Inspectable
    {
        private enum DoorState { Closed, Opening, Open }

        [SerializeField] private Transform leaf;
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private string requiredFlag;
        [SerializeField] private string lockedMessage = "문이 잠겨 있다.";
        [SerializeField] private bool permanentlyLocked;
        [SerializeField] private float openAngle = -95;

        private DoorState state = DoorState.Closed;
        private Coroutine transitionRoutine;
        private Quaternion closedRotation;
        private float nextLockedFeedbackAt;

        public bool IsOpen => state == DoorState.Open;
        public override bool IsInteractionAvailable => state == DoorState.Closed && isActiveAndEnabled;
        public override float InteractionCooldown => .24f;
        public override bool PlayInspectSound => false;
        public override string Prompt => state == DoorState.Open ? "열림" : state == DoorState.Opening ? "여는 중…" : "E 열기";

        public void Configure(Transform panel, CaseDefinition data, string requirement, string message, bool locked = false)
        {
            leaf = panel;
            if (leaf != null) closedRotation = leaf.localRotation;
            definition = data;
            requiredFlag = requirement;
            lockedMessage = message;
            permanentlyLocked = locked;
        }

        private void Awake()
        {
            if (leaf != null) closedRotation = leaf.localRotation;
        }

        public override void Inspect(FirstPersonPlayer player)
        {
            if (!IsInteractionAvailable || player == null) return;

            if (IsLocked())
            {
                if (Time.unscaledTime >= nextLockedFeedbackAt)
                {
                    nextLockedFeedbackAt = Time.unscaledTime + .65f;
                    InteractionSoundscape.PlayLockedDoor(transform.position);
                    player.HUD.ShowToast(lockedMessage);
                }
                return;
            }

            transitionRoutine = StartCoroutine(OpenRoutine());
        }

        private bool IsLocked()
        {
            if (permanentlyLocked) return true;
            if (string.IsNullOrEmpty(requiredFlag)) return false;
            if (definition == null) return true;
            return !CaseProgressStore.Get(definition).Has(requiredFlag);
        }

        private IEnumerator OpenRoutine()
        {
            state = DoorState.Opening;
            InteractionSoundscape.PlayDoorLatch(transform.position);

            foreach (var collider in leaf.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            var start = leaf.localRotation;
            var end = closedRotation * Quaternion.Euler(0, openAngle, 0);
            InteractionSoundscape.PlayDoorCreak(leaf.position);

            const float duration = .4f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                leaf.localRotation = Quaternion.Slerp(start, end, elapsed / duration);
                yield return null;
            }

            leaf.localRotation = end;
            state = DoorState.Open;
            transitionRoutine = null;
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }
            if (state == DoorState.Opening)
            {
                if (leaf != null)
                {
                    leaf.localRotation = closedRotation;
                    foreach (var collider in leaf.GetComponentsInChildren<Collider>())
                        collider.enabled = true;
                }
                state = DoorState.Closed;
            }
        }
    }
}
