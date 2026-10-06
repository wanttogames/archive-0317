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
        public CaseFile CurrentTarget { get; private set; }
        public Camera ViewCamera => viewCamera;
        public bool IsCaptured => captureRequested && Cursor.lockState == CursorLockMode.Locked;

        public void Configure(Camera camera, ArchiveHUD display) { viewCamera = camera; hud = display; }
        private void Awake() { controller = GetComponent<CharacterController>(); }
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
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (hud.IsCaseOpen) CloseCase();
                else SetCapture(!IsCaptured);
            }
            if (hud.IsCaseOpen)
            {
                CurrentTarget = null;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame) CloseCase();
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
            controller.Move((velocity + Vector3.up * verticalSpeed) * deltaTime);
        }
        public void UpdateTarget()
        {
            CurrentTarget = null;
            if (!hud.IsCaseOpen && Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                out RaycastHit hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore))
                CurrentTarget = hit.collider.GetComponentInParent<CaseFile>();
            hud.SetPrompt(CurrentTarget != null);
        }
        public bool TryInteract()
        {
            UpdateTarget();
            if (CurrentTarget == null || hud.IsCaseOpen) return false;
            hud.ShowCase(CurrentTarget);
            CurrentTarget = null;
            SetCapture(false);
            return true;
        }
        public void CloseCase() { hud.CloseCase(); SetCapture(true); }
    }
}
