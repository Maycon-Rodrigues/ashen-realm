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
        [SerializeField, Min(0.05f)] private float cooldown = 1.15f;
        [SerializeField, Min(1)] private int damage = 12;
        private NavMeshAgent agent;
        private Health health;
        private float nextAttack;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
        }

        private void OnEnable() => health.Died += Die;
        private void OnDisable() => health.Died -= Die;

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
                agent.isStopped = true;
                return;
            }

            Vector3 direction = target.transform.position - transform.position;
            float distance = direction.magnitude;
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

            if (Time.time >= nextAttack)
            {
                nextAttack = Time.time + cooldown;
                target.TakeDamage(damage);
            }
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
