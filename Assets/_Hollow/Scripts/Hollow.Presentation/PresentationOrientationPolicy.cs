using Hollow.Platform;

namespace Hollow.Presentation
{
    public static class PresentationOrientationPolicy
    {
        public const float DefaultWorldYawDegrees = 0f;
        public const float VisionOSGameplayWorldYawDegrees = 45f;

        public static float WorldYawDegreesFor(HollowPlatformKind platformKind)
        {
            return platformKind == HollowPlatformKind.WindowsStandard3D
                ? DefaultWorldYawDegrees
                : VisionOSGameplayWorldYawDegrees;
        }
    }
}
