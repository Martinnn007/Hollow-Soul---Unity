using Hollow.Rooms;
using Hollow.Persistence;

namespace Hollow.World
{
    public interface IBranchSessionController
    {
        void Initialize(ImportedRoomRuntimeAsset roomAsset, GameSessionState sessionState);

        RunSaveSnapshot CreateSnapshot();
    }
}
