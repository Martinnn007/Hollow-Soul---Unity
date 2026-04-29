namespace Hollow.Branches
{
    public readonly struct RunFramingSnapshot
    {
        public RunFramingSnapshot(
            string title,
            string subtitle,
            string phaseLabel,
            string message,
            string seedSummary)
            : this(title, subtitle, phaseLabel, message, seedSummary, string.Empty, string.Empty)
        {
        }

        public RunFramingSnapshot(
            string title,
            string subtitle,
            string phaseLabel,
            string message,
            string seedSummary,
            string worldIdentityId,
            string worldDisplayName)
        {
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            PhaseLabel = phaseLabel ?? string.Empty;
            Message = message ?? string.Empty;
            SeedSummary = seedSummary ?? string.Empty;
            WorldIdentityId = worldIdentityId ?? string.Empty;
            WorldDisplayName = worldDisplayName ?? string.Empty;
        }

        public string Title { get; }

        public string Subtitle { get; }

        public string PhaseLabel { get; }

        public string Message { get; }

        public string SeedSummary { get; }

        public string WorldIdentityId { get; }

        public string WorldDisplayName { get; }

        public string SummaryKey => $"{Title}|{Subtitle}|{PhaseLabel}|{Message}|{SeedSummary}|{WorldIdentityId}|{WorldDisplayName}";
    }
}
