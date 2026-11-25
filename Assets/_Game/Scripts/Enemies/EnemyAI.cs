using System;
using UnityEngine;
using UnityEngine.AI;

namespace AshenRealm
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(CapsuleCollider))]
    public sealed class EnemyAI : MonoBehaviour
    {
        [SerializeField] private Health target;
        [SerializeField, Min(0.1f)] private float detectionRadius = 9f;
        [SerializeField, Min(0.1f)] private float attackDistance = 1.55f;
        [SerializeField, Range(15, 180)] private float attackArc = 110f;
        [SerializeField, Min(0.05f)] private float windup = 0.65f;
        [SerializeField, Min(0.05f)] private float cooldown = 1.15f;
        [SerializeField, Min(1)] private int damage = 12;
        private NavMeshAgent agent;
        private Health health;
        private float impactAt;
        private float nextAttack;
        public bool IsWindingUp { get; private set; }
        public float WindupProgress => IsWindingUp ? 1f - Mathf.Clamp01((impactAt - Time.time) / windup) : -1f;
        public float AttackDistance => attackDistance;
        public float AttackArc => attackArc;
        public event Action Attacked;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
        }

        private void OnEnable() => health.Died += Die;
        private void OnDisable()
        {
            health.Died -= Die;
            IsWindingUp = false;
        }

        private void Start()
        {
            agent.updateRotation = false;
            agent.stoppingDistance = attackDistance * 0.85f;
            if (!target || !agent.isOnNavMesh)
                Debug.LogWarning("Enemy needs a target and a spawn point on the baked NavMesh.", this);
        }

        private void Update()
        {
            if (!agent.isOnNavMesh)
                return;
            if (!target || !target.IsAlive)
            {
                IsWindingUp = false;
                agent.isStopped = true;
                return;
            }

            Vector3 direction = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
            float distance = direction.magnitude;
            if (IsWindingUp)
            {
                if (Time.time >= impactAt)
                {
                    IsWindingUp = false;
                    if (distance <= attackDistance && Vector3.Angle(transform.forward, direction) <= attackArc * 0.5f)
                        target.TakeDamage(damage);
                    Attacked?.Invoke();
                }
                return;
            }

            if (Time.time < nextAttack)
                return;

            if (distance > detectionRadius)
            {
                agent.isStopped = true;
                return;
            }

            Vector3 facing = distance > attackDistance ? agent.desiredVelocity : direction;
            facing.y = 0;
            if (facing.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(facing), agent.angularSpeed * Time.deltaTime);

            if (distance > attackDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(target.transform.position);
                return;
            }

            agent.isStopped = true;

            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
            IsWindingUp = true;
            impactAt = Time.time + windup;
            nextAttack = impactAt + cooldown;
        }

        private void Die()
        {
            agent.enabled = false;
            GetComponent<CapsuleCollider>().enabled = false;
            enabled = false;
            gameObject.SetActive(false);
        }
    }
}
