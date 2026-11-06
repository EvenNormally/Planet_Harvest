using PlanetHarvest.Core;
using PlanetHarvest.Planets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetHarvest.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class RadialPlayer : MonoBehaviour
    {
        [SerializeField] private HarvestSettings settings;
        [SerializeField] private Transform moon;
        [SerializeField] private Transform eyes;
        [SerializeField] private InputActionAsset input;
        private InputActionAsset actions;
        private InputAction move, look, jump, attack, interact;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private PhysicsMaterial contactMaterial;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];
        private Vector2 movement;
        private float pitch, pendingYaw;
        private bool jumpQueued;
        public bool IsGrounded { get; private set; }
        public bool Captured { get; private set; }
        public bool MinePressed => Captured && attack.WasPressedThisFrame();
        public bool InteractPressed => Captured && interact.WasPressedThisFrame();

        private void Awake()
        {
            // Old scenes still contain a world-Y CharacterController. Never run both colliders.
            var legacyController = GetComponent<CharacterController>();
            if (legacyController != null) legacyController.enabled = false;
            body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2.2f;
            capsule.radius = 0.3f;
            capsule.direction = 1;
            capsule.center = Vector3.zero;
            contactMaterial = new PhysicsMaterial("Radial Player Contact")
            {
                dynamicFriction = 0, staticFriction = 0, bounciness = 0,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            capsule.sharedMaterial = contactMaterial;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.mass = 80;
            body.linearDamping = 0;
            actions = Instantiate(input);
            move = actions.FindAction("Player/Move", true);
            look = actions.FindAction("Player/Look", true);
            jump = actions.FindAction("Player/Jump", true);
            attack = actions.FindAction("Player/Attack", true);
            interact = actions.FindAction("Player/Interact", true);
        }

        private void OnEnable() { actions.Enable(); SetCapture(true); }
        private void OnDisable()
        {
            actions.Disable(); SetCapture(false);
            movement = Vector2.zero; jumpQueued = false; pendingYaw = 0;
            if (body != null) body.linearVelocity = Vector3.zero;
        }
        private void OnDestroy()
        {
            if (actions != null) Destroy(actions);
            if (contactMaterial != null) Destroy(contactMaterial);
        }
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
            if (Captured)
            {
                Vector2 delta = look.ReadValue<Vector2>();
                float sensitivity = look.activeControl?.device is Gamepad ? 120f * Time.deltaTime : 0.12f;
                pendingYaw += delta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -85, 85);
                eyes.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            movement = Captured ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) : Vector2.zero;
            if (Captured && jump.WasPressedThisFrame()) jumpQueued = true;
        }

        private void FixedUpdate()
        {
            Vector3 up = RadialGravity.Up(body.position, moon.position);
            Quaternion rotation = Quaternion.FromToRotation(body.rotation * Vector3.up, up) * body.rotation;
            rotation = Quaternion.AngleAxis(pendingYaw, up) * rotation;
            pendingYaw = 0;
            body.MoveRotation(rotation);

            Vector3 groundNormal = up;
            Vector3 footSphere = body.position - up * (capsule.height * 0.5f - capsule.radius);
            int count = Physics.SphereCastNonAlloc(footSphere + up * 0.05f, capsule.radius - 0.03f,
                -up, groundHits, 0.2f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            IsGrounded = false;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider.attachedRigidbody == body || Vector3.Dot(hit.normal, up) < 0.5f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                groundNormal = hit.normal;
                IsGrounded = Vector3.Dot(body.linearVelocity, up) <= 1;
            }

            Vector3 tangent = rotation * new Vector3(movement.x, 0, movement.y);
            if (IsGrounded && tangent.sqrMagnitude > 0)
                tangent = Vector3.ProjectOnPlane(tangent, groundNormal).normalized * movement.magnitude;
            Vector3 target = tangent * settings.MoveSpeed;
            Vector3 current = Vector3.ProjectOnPlane(body.linearVelocity, up);
            body.AddForce(Vector3.ClampMagnitude(target - current, 50 * Time.fixedDeltaTime), ForceMode.VelocityChange);
            if (jumpQueued && IsGrounded)
            {
                body.AddForce(up * (settings.JumpSpeed - Vector3.Dot(body.linearVelocity, up)), ForceMode.VelocityChange);
                IsGrounded = false;
            }
            jumpQueued = false;
            // Constant magnitude on every hemisphere; Unity's global gravity is disabled above.
            body.AddForce(RadialGravity.Acceleration(body.position, moon.position, settings.Gravity), ForceMode.Acceleration);
        }
    }
}
