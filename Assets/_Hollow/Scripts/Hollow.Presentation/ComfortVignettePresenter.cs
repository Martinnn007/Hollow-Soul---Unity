using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class ComfortVignettePresenter : MonoBehaviour
    {
        [SerializeField] private bool vignetteEnabled;
        [SerializeField] private float radius = 0.82f;
        [SerializeField] private float opacity = 0.18f;

        public bool VignetteEnabled => vignetteEnabled;

        public float Radius => radius;

        public float Opacity => opacity;

        public void Configure(bool enabled, float nextRadius, float nextOpacity)
        {
            vignetteEnabled = enabled;
            radius = Mathf.Clamp01(nextRadius);
            opacity = Mathf.Clamp01(nextOpacity);
        }
    }
}
