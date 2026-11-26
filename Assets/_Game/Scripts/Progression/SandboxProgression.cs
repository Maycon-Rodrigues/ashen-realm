using UnityEngine;
using UnityEngine.InputSystem;

namespace AshenRealm
{
    public enum SwordState { Locked, Dropped, Collected, Equipped }

    public sealed class SandboxProgression : MonoBehaviour
    {
        [SerializeField] private PlayerCombat player;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health enemy;
        [SerializeField] private Transform drop;
        [SerializeField] private Renderer blade;
        [SerializeField] private Material upgradedBlade;
        [SerializeField, Min(0.1f)] private float pickupRange = 1.8f;
        [SerializeField, Min(1)] private int damageBonus = 5;

        private InputAction collect;
        private InputAction equip;
        private Vector3 dropPosition;
        public SwordState State { get; private set; }
        public int Damage => player.Damage;
        public Vector3 PickupPoint => drop.position;
        public bool CanCollect => State == SwordState.Dropped && playerHealth.IsAlive &&
            Vector3.ProjectOnPlane(player.transform.position - dropPosition, Vector3.up).sqrMagnitude <= pickupRange * pickupRange;

        private void Awake()
        {
            collect = new InputAction("Collect", InputActionType.Button, "<Keyboard>/e");
            collect.AddBinding("<Gamepad>/buttonSouth");
            equip = new InputAction("Equip", InputActionType.Button, "<Keyboard>/q");
            equip.AddBinding("<Gamepad>/buttonNorth");
            drop.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            enemy.Died += DropSword;
            collect.Enable();
            equip.Enable();
        }

        private void OnDisable()
        {
            enemy.Died -= DropSword;
            collect.Disable();
            equip.Disable();
        }

        private void OnDestroy()
        {
            collect.Dispose();
            equip.Dispose();
        }

        private void Update()
        {
            if (State == SwordState.Dropped)
            {
                drop.position = dropPosition + Vector3.up * (0.45f + 0.10f * Mathf.Sin(Time.time * 4f));
                drop.Rotate(Vector3.up, 75f * Time.deltaTime, Space.World);

                if (CanCollect && collect.WasPressedThisFrame())
                {
                    State = SwordState.Collected;
                    drop.gameObject.SetActive(false);
                }
            }

            if (State == SwordState.Collected && playerHealth.IsAlive && equip.WasPressedThisFrame())
                Restore(SwordState.Equipped);
        }

        private void DropSword()
        {
            if (State != SwordState.Locked)
                return;

            State = SwordState.Dropped;
            dropPosition = enemy.transform.position;
            drop.position = dropPosition + Vector3.up * 0.45f;
            drop.gameObject.SetActive(true);
        }

        internal void Restore(SwordState carriedSword)
        {
            if (carriedSword == SwordState.Collected)
                State = SwordState.Collected;
            else if (carriedSword == SwordState.Equipped)
            {
                State = SwordState.Equipped;
                player.DamageBonus = damageBonus;
                blade.sharedMaterial = upgradedBlade;
            }
        }
    }
}
