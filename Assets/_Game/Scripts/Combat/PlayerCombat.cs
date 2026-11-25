using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshenRealm
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private LayerMask targets;
        [SerializeField, Min(0.1f)] private float reach = 2.3f;
        [SerializeField, Range(5, 180)] private float arc = 140f;
        [SerializeField, Min(1)] private int damage = 20;
        [SerializeField, Min(0.01f)] private float windup = 0.17f;
        [SerializeField, Min(0.05f)] private float cooldown = 0.45f;
        private readonly HashSet<Health> hit = new();
        private InputAction attack;
        private Health health;
        private Vector3 strikeDirection;
        private float impactAt;
        private float nextAttack;
        private bool pendingStrike;
        public bool IsAttacking => health.IsAlive && Time.time < nextAttack;
        public float WindupProgress => pendingStrike ? 1f - Mathf.Clamp01((impactAt - Time.time) / windup) : -1f;
        public event Action Attacked;

        private void Awake()
        {
            health = GetComponent<Health>();
            attack = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/buttonWest");
        }

        private void OnEnable() => attack.Enable();
        private void OnDisable()
        {
            attack.Disable();
            pendingStrike = false;
        }
        private void OnDestroy() => attack.Dispose();

        private void Update()
        {
            if (pendingStrike && (!health.IsAlive || Time.time >= impactAt))
            {
                pendingStrike = false;
                if (health.IsAlive)
                    Strike();
            }

            if (attack.WasPressedThisFrame())
                TryAttack();
        }

        public bool TryAttack()
        {
            if (!health.IsAlive || pendingStrike || IsAttacking)
                return false;

            strikeDirection = transform.forward;
            impactAt = Time.time + windup;
            nextAttack = Time.time + Mathf.Max(cooldown, windup);
            pendingStrike = true;
            return true;
        }

        private void Strike()
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            hit.Clear();
            foreach (Collider candidate in Physics.OverlapSphere(origin, reach, targets, QueryTriggerInteraction.Ignore))
            {
                Health target = candidate.GetComponentInParent<Health>();
                if (!target || target == health || !target.IsAlive)
                    continue;

                Vector3 direction = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
                if (Vector3.Angle(strikeDirection, direction) <= arc * 0.5f && hit.Add(target))
                    target.TakeDamage(damage);
            }

            Attacked?.Invoke();
        }
    }
}
