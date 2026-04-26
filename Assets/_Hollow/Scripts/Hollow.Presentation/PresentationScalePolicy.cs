using Hollow.Platform;

namespace Hollow.Presentation
{
    public static class PresentationScalePolicy
    {
        public const float FullScale = 1f;
        public const float VisionOSBoundedTabletopScale = 0.1f;

        public static float WorldScaleFor(HollowPlatformKind platformKind)
        {
            return platformKind == HollowPlatformKind.VisionOSBoundedTabletop
                ? VisionOSBoundedTabletopScale
                : FullScale;
        }
    }
}
