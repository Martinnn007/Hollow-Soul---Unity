using UnityEngine;

namespace Hollow.Platform
{
    public sealed class WindowsPlatformServices : IPlatformServices
    {
        public HollowPlatformKind PlatformKind => HollowPlatformKind.WindowsStandard3D;

        public string PersistentDataRoot => Application.persistentDataPath;

        public bool SupportsSpatialTabletop => false;

        public bool SupportsImmersivePresentation => false;
    }
}
