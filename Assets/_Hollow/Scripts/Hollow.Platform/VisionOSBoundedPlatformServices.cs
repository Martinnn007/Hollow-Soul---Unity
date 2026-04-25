using UnityEngine;

namespace Hollow.Platform
{
    public sealed class VisionOSBoundedPlatformServices : IPlatformServices
    {
        public HollowPlatformKind PlatformKind => HollowPlatformKind.VisionOSBoundedTabletop;

        public string PersistentDataRoot => Application.persistentDataPath;

        public bool SupportsSpatialTabletop => true;

        public bool SupportsImmersivePresentation => false;
    }
}
