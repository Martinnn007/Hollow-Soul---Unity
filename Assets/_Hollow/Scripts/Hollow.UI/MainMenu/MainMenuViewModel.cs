using System;
using System.Collections.Generic;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Persistence;
using Hollow.Platform;

namespace Hollow.UI.MainMenu
{
    public sealed class MainMenuViewModel
    {
        private readonly IProfileStore profileStore;
        private readonly SelectedProfileContext selectedProfileContext;
        private readonly AppStateMachine appStateMachine;
        private readonly List<ProfileSlotCardViewModel> profileCards = new();
        private HollowPlatformKind pendingNewRunPlatformKind = HollowPlatformKind.WindowsStandard3D;

        public MainMenuViewModel(IProfileStore profileStore, SelectedProfileContext selectedProfileContext, AppStateMachine appStateMachine)
        {
            this.profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
            this.selectedProfileContext = selectedProfileContext ?? throw new ArgumentNullException(nameof(selectedProfileContext));
            this.appStateMachine = appStateMachine ?? throw new ArgumentNullException(nameof(appStateMachine));
            Refresh();
        }

        public MainMenuState State { get; private set; } = MainMenuState.ProfileSelect;

        public IReadOnlyList<ProfileSlotCardViewModel> ProfileCards => profileCards;

        public ProfileSlotSummary SelectedProfile => selectedProfileContext.SelectedProfile;

        public string ErrorMessage { get; private set; }

        public HollowPlatformKind PendingNewRunPlatformKind => pendingNewRunPlatformKind;

        public string SelectedCharacterId => selectedProfileContext.SelectedCharacterId;

        public void Refresh()
        {
            profileCards.Clear();
            foreach (var summary in profileStore.LoadSlotSummaries())
            {
                profileCards.Add(new ProfileSlotCardViewModel(summary));
            }
        }

        public void SelectOrCreateSlot(int slotIndex)
        {
            try
            {
                var slotId = new ProfileSlotId(slotIndex);
                var summary = profileStore.CreateOrLoadProfile(slotId, $"Profile {slotIndex + 1}");
                selectedProfileContext.Select(summary);
                State = MainMenuState.SlotMain;
                ErrorMessage = string.Empty;
                Refresh();
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
            }
        }

        public void DeleteSlot(int slotIndex)
        {
            profileStore.DeleteProfile(new ProfileSlotId(slotIndex));
            if (selectedProfileContext.SelectedProfile?.SlotIndex == slotIndex)
            {
                selectedProfileContext.Clear();
                State = MainMenuState.ProfileSelect;
            }

            Refresh();
        }

        public void BackToProfiles()
        {
            selectedProfileContext.Clear();
            State = MainMenuState.ProfileSelect;
        }

        public AppShellRoute RouteForPlatform(HollowPlatformKind platformKind)
        {
            return PlatformPresentationModeResolver.RouteForPlatform(platformKind);
        }

        public AppShellRoute LaunchPlatform(HollowPlatformKind platformKind)
        {
            BeginNewRun(platformKind);
            return RouteForPlatform(platformKind);
        }

        public void BeginNewRun(HollowPlatformKind platformKind)
        {
            if (!selectedProfileContext.HasSelection)
            {
                SetError("Select or create a profile first.");
                return;
            }

            pendingNewRunPlatformKind = platformKind;
            selectedProfileContext.SetSelectedCharacterId("balanced");
            State = MainMenuState.CharacterSelect;
            ErrorMessage = string.Empty;
        }

        public AppShellRoute SelectCharacterAndLaunch(string characterId)
        {
            if (!selectedProfileContext.HasSelection)
            {
                SetError("Select or create a profile first.");
                return AppShellRoute.MainMenu;
            }

            State = MainMenuState.Launching;
            selectedProfileContext.SetSelectedCharacterId(characterId);
            var route = RouteForPlatform(pendingNewRunPlatformKind);
            var slotId = new ProfileSlotId(selectedProfileContext.SelectedProfile.SlotIndex);
            if (profileStore is IRunSaveStore runSaveStore)
            {
                runSaveStore.ClearActiveRun(slotId);
            }

            var updated = profileStore.MarkRunStarted(slotId);
            selectedProfileContext.UpdateSelectedProfile(updated);
            selectedProfileContext.SetLaunchMode(RunLaunchMode.NewRun);
            appStateMachine.TransitionTo(route);
            return route;
        }

        public void BackFromCharacterSelect()
        {
            if (State == MainMenuState.CharacterSelect)
            {
                State = MainMenuState.SlotMain;
                ErrorMessage = string.Empty;
            }
        }

        public AppShellRoute LaunchContinueRun(HollowPlatformKind platformKind)
        {
            if (!selectedProfileContext.HasSelection)
            {
                SetError("Select or create a profile first.");
                return AppShellRoute.MainMenu;
            }

            if (!selectedProfileContext.SelectedProfile.HasActiveRun)
            {
                SetError("This profile does not have an active run to continue.");
                return AppShellRoute.MainMenu;
            }

            State = MainMenuState.Launching;
            var route = RouteForPlatform(platformKind);
            selectedProfileContext.SetLaunchMode(RunLaunchMode.ContinueRun);
            var updated = profileStore.MarkLastPlayed(new ProfileSlotId(selectedProfileContext.SelectedProfile.SlotIndex));
            selectedProfileContext.UpdateSelectedProfile(updated);
            appStateMachine.TransitionTo(route);
            return route;
        }

        public AppShellRoute OpenRoomDesigner()
        {
            if (!selectedProfileContext.HasSelection)
            {
                SetError("Select or create a profile first.");
                return AppShellRoute.MainMenu;
            }

            State = MainMenuState.Launching;
            var route = AppShellRoute.RoomDesigner;
            appStateMachine.TransitionTo(route);
            return route;
        }

        private void SetError(string message)
        {
            State = MainMenuState.Error;
            ErrorMessage = message;
        }
    }
}
