using PlanetHarvest.Core;
using PlanetHarvest.Planets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetHarvest.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class RadialPlayer : MonoBehaviour
    {
        [SerializeField] private HarvestSettings settings;
        [SerializeField] private Transform moon;
        [SerializeField] private Transform eyes;
        [SerializeField] private InputActionAsset input;
        private InputActionAsset actions;
        private InputAction move, look, jump, attack, interact;
        private CharacterController controller;
        private float pitch, verticalSpeed;
        public bool Captured { get; private set; }
        public bool MinePressed => Captured && attack.WasPressedThisFrame();
        public bool InteractPressed => Captured && interact.WasPressedThisFrame();

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            actions = Instantiate(input);
            move = actions.FindAction("Player/Move", true);
            look = actions.FindAction("Player/Look", true);
            jump = actions.FindAction("Player/Jump", true);
            attack = actions.FindAction("Player/Attack", true);
            interact = actions.FindAction("Player/Interact", true);
        }

        private void OnEnable() { actions.Enable(); SetCapture(true); }
        private void OnDisable() { actions.Disable(); SetCapture(false); }
        private void OnDestroy() { if (actions != null) Destroy(actions); }
        private void SetCapture(bool capture)
        {
            Captured = capture;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCapture(!Captured);
            Vector3 up = RadialGravity.Up(transform.position, moon.position);
            transform.rotation = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
            if (Captured)
            {
                Vector2 delta = look.ReadValue<Vector2>();
                float sensitivity = look.activeControl?.device is Gamepad ? 120f * Time.deltaTime : 0.12f;
                transform.Rotate(Vector3.up, delta.x * sensitivity, Space.Self);
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -85, 85);
                eyes.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (Captured && jump.WasPressedThisFrame() && controller.isGrounded) verticalSpeed = settings.JumpSpeed;
            verticalSpeed -= settings.Gravity * Time.deltaTime;
            Vector2 movement = Captured ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) : Vector2.zero;
            Vector3 tangent = transform.right * movement.x + transform.forward * movement.y;
            controller.Move((tangent * settings.MoveSpeed + up * verticalSpeed) * Time.deltaTime);
        }
    }
}
