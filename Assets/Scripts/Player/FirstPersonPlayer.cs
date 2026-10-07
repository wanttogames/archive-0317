using UnityEngine;
using UnityEngine.InputSystem;

namespace Archive0317
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private ArchiveHUD hud;
        [SerializeField] private float walkSpeed = 2.6f;
        [SerializeField] private float runSpeed = 4.6f;
        [SerializeField] private float mouseSensitivity = 0.09f;
        [SerializeField] private float interactionDistance = 2.5f;
        private CharacterController controller;
        private float verticalSpeed;
        private float pitch;
        private bool captureRequested;
        private float footstepDistance;
        public Inspectable CurrentTarget { get; private set; }
        public Camera ViewCamera => viewCamera;
        public ArchiveHUD HUD => hud;
        public bool IsCaptured => captureRequested && Cursor.lockState == CursorLockMode.Locked;

        public void Configure(Camera camera, ArchiveHUD display) { viewCamera = camera; hud = display; }
        public void ApplyPreferences()
        {
            mouseSensitivity = PlayerPrefs.GetFloat(MainMenuController.SensitivityKey, mouseSensitivity);
            AudioListener.volume = PlayerPrefs.GetFloat(MainMenuController.VolumeKey, 1f);
        }
        private void Awake() { controller = GetComponent<CharacterController>(); ApplyPreferences(); }
        private void OnEnable() { ApplyPreferences(); }
        private void Start() { SetCapture(true); }
        private void OnDisable() { SetCapture(false); }
        private void OnApplicationFocus(bool focused) { if (!focused) SetCapture(false); }
        public void SetCapture(bool captured)
        {
            captureRequested = captured;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
        private void Update()
        {
            if (SceneTransitionManager.IsTransitioning || PauseMenuController.IsOpen) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                if (hud.IsNotebookOpen) { CloseCase(); return; }
                if (!hud.IsCaseOpen && hud.ActiveDefinition != null) { hud.ShowNotebook(); SetCapture(false); }
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (hud.IsCaseOpen) CloseCase();
                else if (PauseMenuController.Open(this)) return;
            }
            if (hud.IsCaseOpen)
            {
                CurrentTarget = null;
                if (keyboard != null)
                {
                    if (hud.HasActiveDocument)
                    {
                        if (keyboard.qKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                            hud.PreviousPage();
                        else if (keyboard.eKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                        {
                            if (hud.CanGoNextDocumentPage) hud.NextPage();
                            else CloseCase();
                        }
                    }
                    else if (hud.IsNotebookOpen)
                    {
                        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) hud.SelectNotebookTab(0);
                        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) hud.SelectNotebookTab(1);
                        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) hud.SelectNotebookTab(2);
                    }
                    else if (keyboard.eKey.wasPressedThisFrame) CloseCase();
                }
                hud.SetPrompt(false);
                return;
            }
            if (!IsCaptured)
            {
                CurrentTarget = null;
                hud.SetPrompt(false);
                if (mouse != null && mouse.leftButton.wasPressedThisFrame) SetCapture(true);
                return;
            }
            if (mouse != null) ApplyLook(mouse.delta.ReadValue());
            Vector2 movement = Vector2.zero;
            bool sprint = false;
            if (keyboard != null)
            {
                movement = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }
            Move(movement, sprint, Time.deltaTime);
            UpdateTarget();
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) TryInteract();
        }
        public void ApplyLook(Vector2 delta)
        {
            transform.Rotate(0, delta.x * mouseSensitivity, 0);
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -80, 80);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        public void Move(Vector2 input, bool sprint, float deltaTime)
        {
            input = Vector2.ClampMagnitude(input, 1);
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * deltaTime;
            Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * (sprint ? runSpeed : walkSpeed);
            Vector3 before = transform.position;
            controller.Move((velocity + Vector3.up * verticalSpeed) * deltaTime);
            Vector3 moved = transform.position - before;
            moved.y = 0;
            if (controller.isGrounded && input.sqrMagnitude > .04f)
            {
                footstepDistance += moved.magnitude;
                float spacing = sprint ? 1.55f : 1.35f;
                if (footstepDistance >= spacing)
                {
                    footstepDistance = 0f;
                    InteractionSoundscape.PlayFootstep(transform.position + Vector3.up * .05f, sprint);
                }
            }
            else if (input.sqrMagnitude <= .04f) footstepDistance = Mathf.Min(footstepDistance, .45f);
        }
        public void UpdateTarget()
        {
            CurrentTarget = null;
            if (!hud.IsCaseOpen && Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                out RaycastHit hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore))
                CurrentTarget = hit.collider.GetComponentInParent<Inspectable>();
            hud.SetPrompt(CurrentTarget != null, CurrentTarget != null ? CurrentTarget.Prompt : "E 조사");
        }
        public bool TryInteract()
        {
            UpdateTarget();
            if (CurrentTarget == null || hud.IsCaseOpen) return false;
            InteractionSoundscape.PlayInspect(CurrentTarget.transform.position);
            CurrentTarget.Inspect(this);
            CurrentTarget = null;
            if (hud.IsCaseOpen) SetCapture(false);
            return true;
        }
        public void CloseCase() { hud.CloseCase(); SetCapture(true); }
    }
}
