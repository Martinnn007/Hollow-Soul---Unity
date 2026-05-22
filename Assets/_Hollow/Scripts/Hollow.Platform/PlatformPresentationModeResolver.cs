using Hollow.Core.App;

namespace Hollow.Platform
{
    public static class PlatformPresentationModeResolver
    {
        public static AppShellRoute RouteForPlatform(HollowPlatformKind platformKind)
        {
            return platformKind switch
            {
                HollowPlatformKind.WindowsStandard3D => AppShellRoute.GameWindows,
                HollowPlatformKind.VisionOSBoundedTabletop => AppShellRoute.GameVisionOSBounded,
                HollowPlatformKind.VisionOSImmersive => AppShellRoute.GameVisionOSImmersive,
                _ => AppShellRoute.GameWindows
            };
        }

        public static AppShellRoute SpaceshipRouteForPlatform(HollowPlatformKind platformKind)
        {
            return platformKind switch
            {
                HollowPlatformKind.WindowsStandard3D => AppShellRoute.SpaceshipWindows,
                HollowPlatformKind.VisionOSBoundedTabletop => AppShellRoute.SpaceshipVisionOSBounded,
                HollowPlatformKind.VisionOSImmersive => AppShellRoute.SpaceshipVisionOSImmersive,
                _ => AppShellRoute.SpaceshipWindows
            };
        }
    }
}
