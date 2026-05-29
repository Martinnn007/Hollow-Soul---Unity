using System;
using System.Globalization;
using Hollow.Data.Definitions;

namespace Hollow.Branches
{
    public readonly struct RunLocationLabelContext
    {
        public RunLocationLabelContext(
            bool isSpaceshipHub,
            bool isDeveloperLab,
            bool isInterBranchHub,
            int worldIndex,
            int branchNumber,
            RunWorldPhase phase,
            RunFramingSnapshot framingSnapshot)
        {
            IsSpaceshipHub = isSpaceshipHub;
            IsDeveloperLab = isDeveloperLab;
            IsInterBranchHub = isInterBranchHub;
            WorldIndex = worldIndex <= 0 ? 1 : worldIndex;
            BranchNumber = branchNumber <= 0 ? 1 : branchNumber;
            Phase = phase;
            FramingSnapshot = framingSnapshot;
        }

        public bool IsSpaceshipHub { get; }
        public bool IsDeveloperLab { get; }
        public bool IsInterBranchHub { get; }
        public int WorldIndex { get; }
        public int BranchNumber { get; }
        public RunWorldPhase Phase { get; }
        public RunFramingSnapshot FramingSnapshot { get; }
    }

    public static class RunLocationLabelFormatter
    {
        public static string Format(RunLocationLabelContext context)
        {
            if (context.IsSpaceshipHub)
            {
                return "Spaceship";
            }

            if (context.IsDeveloperLab)
            {
                return "Developer Lab";
            }

            if (context.IsInterBranchHub || context.Phase == RunWorldPhase.Hub)
            {
                return $"World {context.WorldIndex} Hub";
            }

            var branchNumber = context.Phase == RunWorldPhase.Prologue ? 1 : Math.Max(1, context.BranchNumber);
            return $"{context.WorldIndex}-{branchNumber}: {ResolveWorldDisplayName(context.FramingSnapshot)}";
        }

        private static string ResolveWorldDisplayName(RunFramingSnapshot snapshot)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.WorldDisplayName))
            {
                return snapshot.WorldDisplayName.Trim();
            }

            var title = snapshot.Title ?? string.Empty;
            var separator = title.IndexOf(':');
            if (separator >= 0 && separator + 1 < title.Length)
            {
                var fromTitle = title[(separator + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(fromTitle))
                {
                    return fromTitle;
                }
            }

            return TitleCaseBiome(snapshot.BiomeId);
        }

        private static string TitleCaseBiome(string biomeId)
        {
            var normalized = RoomBiomeIds.Normalize(biomeId)
                .Replace('_', ' ')
                .Replace('-', ' ')
                .Trim();
            return string.IsNullOrWhiteSpace(normalized)
                ? "Hollow Threshold"
                : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized.ToLowerInvariant());
        }
    }
}
