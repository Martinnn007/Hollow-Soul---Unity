using UnityEngine;

namespace Hollow.Platform
{
    public static class PlatformRuntimeDetector
    {
        public static bool IsVisionOSRuntime()
        {
            return IsVisionOSRuntime(Application.platform);
        }

        public static bool IsVisionOSRuntime(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.VisionOS;
        }

        public static HollowPlatformKind DefaultPlatformKindForRuntime(HollowPlatformKind fallback)
        {
            return IsVisionOSRuntime() && fallback == HollowPlatformKind.WindowsStandard3D
                ? HollowPlatformKind.VisionOSBoundedTabletop
                : fallback;
        }
    }
}
