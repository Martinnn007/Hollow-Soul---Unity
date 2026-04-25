namespace Hollow.Platform
{
    public static class PlatformServiceFactory
    {
        public static IPlatformServices Create(HollowPlatformKind platformKind)
        {
            return platformKind switch
            {
                HollowPlatformKind.WindowsStandard3D => new WindowsPlatformServices(),
                HollowPlatformKind.VisionOSBoundedTabletop => new VisionOSBoundedPlatformServices(),
                HollowPlatformKind.VisionOSImmersive => new VisionOSImmersivePlatformServices(),
                _ => new WindowsPlatformServices()
            };
        }
    }
}
