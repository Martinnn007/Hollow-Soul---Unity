using System.Collections.Generic;

namespace Hollow.Persistence
{
    public interface IProfileStore
    {
        IReadOnlyList<ProfileSlotSummary> LoadSlotSummaries();

        ProfileSlotSummary CreateOrLoadProfile(ProfileSlotId slotId, string displayName);

        ProfileSlotSummary MarkLastPlayed(ProfileSlotId slotId);

        ProfileSlotSummary MarkRunStarted(ProfileSlotId slotId);

        void DeleteProfile(ProfileSlotId slotId);
    }
}
