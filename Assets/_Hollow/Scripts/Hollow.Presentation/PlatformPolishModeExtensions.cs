using Hollow.Data.Definitions;
using Hollow.Platform;

namespace Hollow.Presentation
{
    public static class PlatformPolishModeExtensions
    {
        public static HollowPlatformKind ToPlatformKind(this PlatformPresentationMode mode)
        {
            return mode switch
            {
                PlatformPresentationMode.VisionOSBoundedTabletop => HollowPlatformKind.VisionOSBoundedTabletop,
                PlatformPresentationMode.VisionOSImmersive => HollowPlatformKind.VisionOSImmersive,
                _ => HollowPlatformKind.WindowsStandard3D
            };
        }

        public static PlatformPresentationMode ToPresentationMode(this HollowPlatformKind platformKind)
        {
            return platformKind switch
            {
                HollowPlatformKind.VisionOSBoundedTabletop => PlatformPresentationMode.VisionOSBoundedTabletop,
                HollowPlatformKind.VisionOSImmersive => PlatformPresentationMode.VisionOSImmersive,
                _ => PlatformPresentationMode.WindowsStandard3D
            };
        }
    }
}
