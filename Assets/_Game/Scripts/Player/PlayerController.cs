using UnityEngine;
using UnityEngine.InputSystem;

namespace AshenRealm
{
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField, Min(0)] private float speed = 4.8f;
        [SerializeField, Min(0)] private float turnSpeed = 1080f;
        [SerializeField, Min(0)] private float gravity = 22f;
        private CharacterController motor;
        private Health health;
        private PlayerCombat combat;
        private InputAction move;
        private float verticalSpeed;
        public Vector3 MoveDirection { get; private set; }

        private void Awake()
        {
            motor = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            combat = GetComponent<PlayerCombat>();
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            if (!view && UnityEngine.Camera.main)
                view = UnityEngine.Camera.main.transform;
            if (!view)
                Debug.LogWarning("Player view is missing. Movement uses world axes.", this);
        }

        private void OnEnable() => move.Enable();
        private void OnDisable() => move.Disable();
        private void OnDestroy() => move.Dispose();

        private void Update()
        {
            Vector2 input = health.IsAlive ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            Vector3 forward = view ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : Vector3.forward;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            MoveDirection = forward * input.y + right * input.x;

            if (MoveDirection.sqrMagnitude > 0.001f && (!combat || !combat.IsAttacking))
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(MoveDirection), turnSpeed * Time.deltaTime);

            verticalSpeed = motor.isGrounded && verticalSpeed < 0f ? -2f : verticalSpeed - gravity * Time.deltaTime;
            motor.Move((MoveDirection * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
    }
}
