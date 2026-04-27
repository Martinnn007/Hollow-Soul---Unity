using Hollow.Core;

namespace Hollow.Persistence
{
    public sealed class SelectedProfileContext
    {
        public ProfileSlotSummary SelectedProfile { get; private set; }

        public RunLaunchMode LaunchMode { get; private set; } = RunLaunchMode.NewRun;

        public string SelectedCharacterId { get; private set; } = "balanced";

        public bool HasSelection => SelectedProfile != null && !SelectedProfile.IsEmpty;

        public void Select(ProfileSlotSummary profile)
        {
            SelectedProfile = profile;
            LaunchMode = RunLaunchMode.NewRun;
        }

        public void UpdateSelectedProfile(ProfileSlotSummary profile)
        {
            SelectedProfile = profile;
        }

        public void SetLaunchMode(RunLaunchMode launchMode)
        {
            LaunchMode = launchMode;
        }

        public void SetSelectedCharacterId(string characterId)
        {
            SelectedCharacterId = string.IsNullOrWhiteSpace(characterId) ? "balanced" : characterId;
        }

        public void Clear()
        {
            SelectedProfile = null;
            LaunchMode = RunLaunchMode.NewRun;
            SelectedCharacterId = "balanced";
        }
    }
}
