using Hollow.Persistence;

namespace Hollow.UI.MainMenu
{
    public sealed class ProfileSlotCardViewModel
    {
        public ProfileSlotCardViewModel(ProfileSlotSummary summary)
        {
            SlotIndex = summary.SlotIndex;
            IsEmpty = summary.IsEmpty;
            Title = summary.IsEmpty ? $"Empty Slot {summary.SlotIndex + 1}" : summary.DisplayName;
            Subtitle = summary.IsEmpty
                ? "Create new profile"
                : $"Runs: {summary.TotalRuns} | Active Run: {(summary.HasActiveRun ? "Yes" : "No")}";
        }

        public int SlotIndex { get; }

        public bool IsEmpty { get; }

        public string Title { get; }

        public string Subtitle { get; }
    }
}
