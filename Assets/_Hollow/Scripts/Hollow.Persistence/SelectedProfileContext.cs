using Hollow.Core;

namespace Hollow.Persistence
{
    public sealed class SelectedProfileContext
    {
        public ProfileSlotSummary SelectedProfile { get; private set; }

        public RunLaunchMode LaunchMode { get; private set; } = RunLaunchMode.NewRun;

        public string SelectedCharacterId { get; private set; } = "balanced";

        public string SelectedChallengeId { get; private set; } = string.Empty;

        public bool DeveloperLabRequested { get; private set; }

        public bool HasSelection => SelectedProfile != null && !SelectedProfile.IsEmpty;

        public void Select(ProfileSlotSummary profile)
        {
            SelectedProfile = profile;
            LaunchMode = RunLaunchMode.NewRun;
            DeveloperLabRequested = false;
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

        public void SetSelectedChallengeId(string challengeId)
        {
            SelectedChallengeId = challengeId ?? string.Empty;
        }

        public void SetDeveloperLabRequested(bool requested)
        {
            DeveloperLabRequested = requested;
            if (requested)
            {
                SelectedChallengeId = string.Empty;
                LaunchMode = RunLaunchMode.NewRun;
            }
        }

        public void Clear()
        {
            SelectedProfile = null;
            LaunchMode = RunLaunchMode.NewRun;
            SelectedCharacterId = "balanced";
            SelectedChallengeId = string.Empty;
            DeveloperLabRequested = false;
        }
    }
}
