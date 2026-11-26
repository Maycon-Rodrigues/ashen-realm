using UnityEngine;

namespace AshenRealm
{
    public sealed class ArenaCharacterMotion : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        private Vector3 lastPosition;
        private Vector3 restPosition;
        private float stride;
        private float weight;

        private void OnEnable()
        {
            lastPosition = transform.position;
            restPosition = visual.localPosition;
        }

        private void LateUpdate()
        {
            Vector3 offset = Vector3.ProjectOnPlane(transform.position - lastPosition, Vector3.up);
            lastPosition = transform.position;
            float distance = Mathf.Min(0.35f, offset.magnitude);
            float moving = Mathf.Clamp01(distance / Mathf.Max(0.001f, 2f * Time.deltaTime));
            weight = Mathf.MoveTowards(weight, moving, Time.deltaTime * 8f);
            stride += distance * 6.3f;
            float swing = Mathf.Sin(stride) * weight * 21f;
            leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            float rise = Mathf.Abs(Mathf.Sin(stride)) * weight * 0.048f;
            visual.localPosition = restPosition + Vector3.up * (rise + Mathf.Sin(Time.time * 3f) * 0.011f);
        }
    }
}
