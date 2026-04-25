using System;

namespace Hollow.Persistence
{
    [Serializable]
    public sealed class ProfileSlotSummary
    {
        public ProfileSlotSummary(int slotIndex)
            : this(slotIndex, string.Empty, string.Empty, 0, 0, 0, false)
        {
        }

        public ProfileSlotSummary(
            int slotIndex,
            string profileId,
            string displayName,
            long createdAtUtcTicks,
            long lastPlayedUtcTicks,
            int totalRuns,
            bool hasActiveRun)
        {
            SlotIndex = slotIndex;
            ProfileId = profileId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CreatedAtUtcTicks = createdAtUtcTicks;
            LastPlayedUtcTicks = lastPlayedUtcTicks;
            TotalRuns = totalRuns;
            HasActiveRun = hasActiveRun;
        }

        public int SlotIndex { get; }

        public string ProfileId { get; }

        public string DisplayName { get; }

        public long CreatedAtUtcTicks { get; }

        public long LastPlayedUtcTicks { get; }

        public int TotalRuns { get; }

        public bool HasActiveRun { get; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(ProfileId);

        public DateTime LastPlayedUtc => LastPlayedUtcTicks <= 0 ? DateTime.MinValue : new DateTime(LastPlayedUtcTicks, DateTimeKind.Utc);
    }
}
