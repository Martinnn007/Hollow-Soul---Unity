using Hollow.Platform;
using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class CameraRigMetadata : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;

        public HollowPlatformKind PlatformKind => platformKind;

        public void Configure(HollowPlatformKind nextPlatformKind)
        {
            platformKind = nextPlatformKind;
        }
    }
}
