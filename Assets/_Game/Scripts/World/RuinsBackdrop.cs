using UnityEngine;

namespace AshenRealm
{
    [ExecuteAlways, RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class RuinsBackdrop : MonoBehaviour
    {
        [SerializeField] private Transform canvas;
        [SerializeField] private float imageAspect = 16f / 9f;

        private UnityEngine.Camera view;

        private void OnEnable()
        {
            view = GetComponent<UnityEngine.Camera>();
            Fit();
        }

        private void LateUpdate() => Fit();

        public void Fit()
        {
            if (!view || !canvas)
                return;

            float height = 2f * view.orthographicSize;
            float width = height * view.aspect;
            height = Mathf.Max(height, width / imageAspect);
            var size = new Vector3(height * imageAspect * 1.01f, height * 1.01f, 1f);
            if (canvas.localScale != size)
                canvas.localScale = size;
        }
    }
}
