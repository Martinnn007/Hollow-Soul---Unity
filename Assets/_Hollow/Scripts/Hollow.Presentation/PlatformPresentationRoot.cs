using Hollow.Platform;
using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class PlatformPresentationRoot : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;
        [SerializeField] private float worldScale = PresentationScalePolicy.FullScale;

        public HollowPlatformKind PlatformKind => platformKind;

        public float WorldScale => worldScale;

        public void Configure(HollowPlatformKind nextPlatformKind)
        {
            platformKind = nextPlatformKind;
            worldScale = PresentationScalePolicy.WorldScaleFor(platformKind);
            ApplyScale();
        }

        private void Awake()
        {
            ApplyScale();
        }

        private void OnValidate()
        {
            ApplyScale();
        }

        private void ApplyScale()
        {
            if (worldScale <= 0f)
            {
                worldScale = PresentationScalePolicy.WorldScaleFor(platformKind);
            }

            transform.localScale = Vector3.one * worldScale;
        }
    }
}
