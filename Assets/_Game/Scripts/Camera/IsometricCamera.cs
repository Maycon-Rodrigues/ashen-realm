using UnityEngine;

namespace AshenRealm
{
    public sealed class IsometricCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Range(15, 80)] private float pitch = 48f;
        [SerializeField] private float yaw = 45f;
        [SerializeField, Min(1)] private float distance = 17f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.16f;
        private Vector3 velocity;

        private void Start()
        {
            if (!target)
            {
                Debug.LogWarning("Isometric camera needs a target.", this);
                enabled = false;
            }
        }

        private void LateUpdate() => Place(false);

        public void Place(bool snap)
        {
            if (!target)
                return;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 goal = target.position + Vector3.up - rotation * Vector3.forward * distance;
            Vector3 position = goal;
            if (snap)
                velocity = Vector3.zero;
            else
                position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);

            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
