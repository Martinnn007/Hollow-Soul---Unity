using UnityEngine;

namespace Hollow.Platform
{
    public sealed class VisionOSImmersivePlatformServices : IPlatformServices
    {
        public HollowPlatformKind PlatformKind => HollowPlatformKind.VisionOSImmersive;

        public string PersistentDataRoot => Application.persistentDataPath;

        public bool SupportsSpatialTabletop => true;

        public bool SupportsImmersivePresentation => true;
    }
}
