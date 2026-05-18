using UnityEngine;

namespace Hollow.Data.Definitions
{
    public enum PlatformPresentationMode
    {
        WindowsStandard3D = 0,
        VisionOSBoundedTabletop = 1,
        VisionOSImmersive = 2
    }

    [CreateAssetMenu(menuName = "Hollow/Platform Presentation Profile", fileName = "PlatformPresentationProfile")]
    public sealed class PlatformPresentationProfile : HollowDefinition
    {
        [SerializeField] private PlatformPresentationMode mode;
        [SerializeField] private float worldScale = 1f;
        [SerializeField] private float worldYawDegrees;

        public PlatformPresentationMode Mode => mode;

        public float WorldScale => worldScale;

        public float WorldYawDegrees => worldYawDegrees;
    }
}
