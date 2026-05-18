using Hollow.Platform;
using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class PlatformPresentationRoot : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;
        [SerializeField] private float worldScale = PresentationScalePolicy.FullScale;
        [SerializeField] private float worldYawDegrees;

        public HollowPlatformKind PlatformKind => platformKind;

        public float WorldScale => worldScale;

        public float WorldYawDegrees => worldYawDegrees;

        public void Configure(HollowPlatformKind nextPlatformKind)
        {
            platformKind = nextPlatformKind;
            worldScale = PresentationScalePolicy.WorldScaleFor(platformKind);
            worldYawDegrees = PresentationOrientationPolicy.WorldYawDegreesFor(platformKind);
            ApplyTransform();
        }

        public void Configure(HollowPlatformKind nextPlatformKind, float nextWorldScale)
        {
            Configure(
                nextPlatformKind,
                nextWorldScale,
                PresentationOrientationPolicy.WorldYawDegreesFor(nextPlatformKind));
        }

        public void Configure(HollowPlatformKind nextPlatformKind, float nextWorldScale, float nextWorldYawDegrees)
        {
            platformKind = nextPlatformKind;
            worldScale = nextWorldScale > 0f ? nextWorldScale : PresentationScalePolicy.WorldScaleFor(platformKind);
            worldYawDegrees = nextWorldYawDegrees;
            ApplyTransform();
        }

        private void Awake()
        {
            ApplyTransform();
        }

        private void OnValidate()
        {
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            if (worldScale <= 0f)
            {
                worldScale = PresentationScalePolicy.WorldScaleFor(platformKind);
            }

            transform.localScale = Vector3.one * worldScale;
            transform.localRotation = Quaternion.Euler(0f, worldYawDegrees, 0f);
        }
    }
}
