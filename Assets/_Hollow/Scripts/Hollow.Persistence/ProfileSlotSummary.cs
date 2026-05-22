using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Persistence
{
    [Serializable]
    public sealed class ProfileSlotSummary
    {
        public ProfileSlotSummary(int slotIndex)
            : this(slotIndex, string.Empty, string.Empty, 0, 0, 0, false, 0, 0, null)
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
            : this(slotIndex, profileId, displayName, createdAtUtcTicks, lastPlayedUtcTicks, totalRuns, hasActiveRun, 0, 0, null)
        {
        }

        public ProfileSlotSummary(
            int slotIndex,
            string profileId,
            string displayName,
            long createdAtUtcTicks,
            long lastPlayedUtcTicks,
            int totalRuns,
            bool hasActiveRun,
            int bankedSouls,
            int completedRuns)
            : this(slotIndex, profileId, displayName, createdAtUtcTicks, lastPlayedUtcTicks, totalRuns, hasActiveRun, bankedSouls, completedRuns, null)
        {
        }

        public ProfileSlotSummary(
            int slotIndex,
            string profileId,
            string displayName,
            long createdAtUtcTicks,
            long lastPlayedUtcTicks,
            int totalRuns,
            bool hasActiveRun,
            int bankedSouls,
            int completedRuns,
            IEnumerable<string> purchasedShipUpgradeIds)
        {
            SlotIndex = slotIndex;
            ProfileId = profileId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CreatedAtUtcTicks = createdAtUtcTicks;
            LastPlayedUtcTicks = lastPlayedUtcTicks;
            TotalRuns = totalRuns;
            HasActiveRun = hasActiveRun;
            BankedSouls = bankedSouls;
            CompletedRuns = completedRuns;
            PurchasedShipUpgradeIds = (purchasedShipUpgradeIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public int SlotIndex { get; }

        public string ProfileId { get; }

        public string DisplayName { get; }

        public long CreatedAtUtcTicks { get; }

        public long LastPlayedUtcTicks { get; }

        public int TotalRuns { get; }

        public bool HasActiveRun { get; }

        public int BankedSouls { get; }

        public int CompletedRuns { get; }

        public IReadOnlyList<string> PurchasedShipUpgradeIds { get; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(ProfileId);

        public DateTime LastPlayedUtc => LastPlayedUtcTicks <= 0 ? DateTime.MinValue : new DateTime(LastPlayedUtcTicks, DateTimeKind.Utc);
    }
}
