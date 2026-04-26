using Hollow.Rooms;

namespace Hollow.World
{
    public interface IBranchSessionController
    {
        void Initialize(ImportedRoomRuntimeAsset roomAsset, GameSessionState sessionState);
    }
}
