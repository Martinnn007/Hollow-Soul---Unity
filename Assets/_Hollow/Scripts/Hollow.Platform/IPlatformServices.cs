namespace Hollow.Platform
{
    public interface IPlatformServices
    {
        HollowPlatformKind PlatformKind { get; }

        string PersistentDataRoot { get; }

        bool SupportsSpatialTabletop { get; }

        bool SupportsImmersivePresentation { get; }
    }
}
