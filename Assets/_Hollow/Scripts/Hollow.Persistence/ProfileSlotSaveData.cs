using System;
using System.Collections.Generic;

namespace Hollow.Persistence
{
    [Serializable]
    public sealed class ProfileSlotSaveData
    {
        public int slotIndex;
        public string profileId;
        public string displayName;
        public long createdAtUtcTicks;
        public long lastPlayedUtcTicks;
        public int totalRuns;
        public bool hasActiveRun;

        public ProfileSlotSummary ToSummary()
        {
            return new ProfileSlotSummary(slotIndex, profileId, displayName, createdAtUtcTicks, lastPlayedUtcTicks, totalRuns, hasActiveRun);
        }

        public static ProfileSlotSaveData FromSummary(ProfileSlotSummary summary)
        {
            return new ProfileSlotSaveData
            {
                slotIndex = summary.SlotIndex,
                profileId = summary.ProfileId,
                displayName = summary.DisplayName,
                createdAtUtcTicks = summary.CreatedAtUtcTicks,
                lastPlayedUtcTicks = summary.LastPlayedUtcTicks,
                totalRuns = summary.TotalRuns,
                hasActiveRun = summary.HasActiveRun
            };
        }
    }

    [Serializable]
    public sealed class ProfileStoreSaveData
    {
        public int schemaVersion = 1;
        public List<ProfileSlotSaveData> slots = new();
    }
}
