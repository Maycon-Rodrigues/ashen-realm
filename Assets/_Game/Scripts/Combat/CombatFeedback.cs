using UnityEngine;

namespace AshenRealm
{
    [RequireComponent(typeof(Health))]
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private Renderer body;
        [SerializeField] private Transform pose;
        [SerializeField] private Transform strikingArm;
        [SerializeField] private LineRenderer warning;
        [SerializeField] private AudioClip swing;
        [SerializeField] private AudioClip hit;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private readonly Color hurtColor = new(1f, 0.79f, 0.57f);
        private MaterialPropertyBlock properties;
        private Health health;
        private PlayerCombat player;
        private EnemyAI enemy;
        private Color bodyColor;
        private Quaternion restPose;
        private Quaternion restArm;
        private Vector3 strikeAxis;
        private float lastImpact = float.NegativeInfinity;
        private float lastHurt = float.NegativeInfinity;

        private void Awake()
        {
            health = GetComponent<Health>();
            player = GetComponent<PlayerCombat>();
            enemy = GetComponent<EnemyAI>();
            if (body)
            {
                properties = new MaterialPropertyBlock();
                bodyColor = body.sharedMaterial.GetColor(BaseColor);
            }
            if (pose)
                restPose = pose.localRotation;
            if (strikingArm)
            {
                restArm = strikingArm.localRotation;
                strikeAxis = strikingArm.parent.InverseTransformDirection(transform.right);
            }
            if (warning && enemy)
                BuildWarning();
        }

        private void OnEnable()
        {
            health.Damaged += OnHurt;
            if (player)
                player.Attacked += OnImpact;
            if (enemy)
                enemy.Attacked += OnImpact;
        }

        private void OnDisable()
        {
            health.Damaged -= OnHurt;
            if (player)
                player.Attacked -= OnImpact;
            if (enemy)
                enemy.Attacked -= OnImpact;
            if (warning)
                warning.enabled = false;
        }

        private void LateUpdate()
        {
            float preparation = player ? player.WindupProgress : enemy ? enemy.WindupProgress : -1f;
            float recovery = (Time.time - lastImpact) / 0.22f;
            if (pose)
            {
                float impactAngle = player ? 36f : -26f;
                float windupAngle = player ? -76f : 24f;
                float angle = preparation < 0f
                    ? Mathf.Lerp(impactAngle, 0f, recovery)
                    : preparation < 0.7f
                        ? Mathf.Lerp(0f, windupAngle, preparation / 0.7f)
                        : Mathf.Lerp(windupAngle, impactAngle, (preparation - 0.7f) / 0.3f);
                pose.localRotation = restPose * Quaternion.AngleAxis(angle, player ? Vector3.up : Vector3.right);
            }
            if (strikingArm)
            {
                float armAngle = Mathf.Lerp(-31f, 0f, recovery);
                if (preparation >= 0f)
                    armAngle = preparation < 0.78f
                        ? Mathf.Lerp(0f, 53f, preparation / 0.78f)
                        : Mathf.Lerp(53f, -31f, (preparation - 0.78f) / 0.22f);
                strikingArm.localRotation = Quaternion.AngleAxis(armAngle, strikeAxis) * restArm;
            }

            if (body)
            {
                float flash = 1f - Mathf.Clamp01((Time.time - lastHurt) / 0.16f);
                Color tint = enemy && preparation >= 0f
                    ? Color.Lerp(bodyColor, new Color(1f, 0.41f, 0.13f), 0.25f + preparation * 0.45f)
                    : bodyColor;
                properties.SetColor(BaseColor, Color.Lerp(tint, hurtColor, flash));
                body.SetPropertyBlock(properties);
            }

            if (warning)
            {
                warning.enabled = preparation >= 0f;
                warning.widthMultiplier = Mathf.Lerp(0.045f, 0.11f, preparation);
            }
        }

        private void BuildWarning()
        {
            const int segments = 24;
            warning.positionCount = segments + 2;
            warning.SetPosition(0, Vector3.zero);
            for (int index = 0; index <= segments; index++)
            {
                float angle = Mathf.Lerp(-enemy.AttackArc * 0.5f, enemy.AttackArc * 0.5f,
                    (float)index / segments) * Mathf.Deg2Rad;
                warning.SetPosition(index + 1, new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * enemy.AttackDistance);
            }
        }

        private void OnImpact()
        {
            lastImpact = Time.time;
            if (swing)
                AudioSource.PlayClipAtPoint(swing, transform.position, 0.35f);
        }

        private void OnHurt()
        {
            lastHurt = Time.time;
            if (hit)
                AudioSource.PlayClipAtPoint(hit, transform.position, 0.48f);
        }
    }
}
