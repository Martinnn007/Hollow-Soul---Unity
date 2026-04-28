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
        {
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            PhaseLabel = phaseLabel ?? string.Empty;
            Message = message ?? string.Empty;
            SeedSummary = seedSummary ?? string.Empty;
        }

        public string Title { get; }

        public string Subtitle { get; }

        public string PhaseLabel { get; }

        public string Message { get; }

        public string SeedSummary { get; }

        public string SummaryKey => $"{Title}|{Subtitle}|{PhaseLabel}|{Message}|{SeedSummary}";
    }
}
