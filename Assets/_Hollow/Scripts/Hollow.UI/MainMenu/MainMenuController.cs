using Hollow.Core.App;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Persistence;
using Hollow.Platform;
using UnityEngine;

namespace Hollow.UI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind defaultPlatformKind = HollowPlatformKind.WindowsStandard3D;
        [SerializeField] private ChallengeCatalogDefinition challengeCatalog;
        [SerializeField] private CharacterCatalogDefinition characterCatalog;
        [SerializeField] private AppShellRoute defaultReturnRoute = AppShellRoute.MainMenu;

        private MainMenuScreen screen;
        private VisionOSMainMenuScreen visionOSScreen;

        public MainMenuViewModel ViewModel { get; private set; }

        private void Awake()
        {
            defaultPlatformKind = PlatformRuntimeDetector.DefaultPlatformKindForRuntime(defaultPlatformKind);
            GetComponent<MainMenuPlatformPresenter>()?.Apply(defaultPlatformKind);
            var appStateMachine = HollowBootstrap.Instance != null ? HollowBootstrap.Instance.AppStateMachine : new AppStateMachine();
            var profileHost = ProfileSessionHost.Instance;
            var profileStore = profileHost?.ProfileStore ?? new JsonProfileStore();
            var selectedProfileContext = profileHost?.SelectedProfileContext ?? new SelectedProfileContext();
            ViewModel = new MainMenuViewModel(profileStore, selectedProfileContext, appStateMachine, challengeCatalog, characterCatalog);
            visionOSScreen = GetComponent<VisionOSMainMenuScreen>();
            if (visionOSScreen == null)
            {
                screen = GetComponent<MainMenuScreen>() ?? gameObject.AddComponent<MainMenuScreen>();
            }
        }

        public ChallengeCatalogDefinition ChallengeCatalog => challengeCatalog;

        public CharacterCatalogDefinition CharacterCatalog => characterCatalog;

        public HollowPlatformKind DefaultPlatformKind => defaultPlatformKind;

        public AppShellRoute DefaultReturnRoute => defaultReturnRoute;

        public void ConfigureChallengeCatalog(ChallengeCatalogDefinition nextChallengeCatalog)
        {
            challengeCatalog = nextChallengeCatalog;
        }

        public void ConfigureCharacterCatalog(CharacterCatalogDefinition nextCharacterCatalog)
        {
            characterCatalog = nextCharacterCatalog;
        }

        public void ConfigureDefaults(HollowPlatformKind nextDefaultPlatformKind, AppShellRoute nextDefaultReturnRoute)
        {
            defaultPlatformKind = nextDefaultPlatformKind;
            defaultReturnRoute = nextDefaultReturnRoute;
        }

        private void Start()
        {
            if (visionOSScreen != null)
            {
                visionOSScreen.Build(this);
                return;
            }

            screen.Build(this);
        }

        public void SelectSlot(int slotIndex)
        {
            ViewModel.SelectOrCreateSlot(slotIndex);
            RebuildActiveScreen();
        }

        public void DeleteSlot(int slotIndex)
        {
            ViewModel.DeleteSlot(slotIndex);
            RebuildActiveScreen();
        }

        public void BackToProfiles()
        {
            ViewModel.BackToProfiles();
            RebuildActiveScreen();
        }

        public void LaunchDefaultPlatform()
        {
            LaunchNewRun(defaultPlatformKind);
        }

        public void LaunchDefaultContinue()
        {
            LaunchContinueRun(defaultPlatformKind);
        }

        public void LaunchWindows()
        {
            LaunchNewRun(HollowPlatformKind.WindowsStandard3D);
        }

        public void LaunchVisionOSBounded()
        {
            LaunchNewRun(HollowPlatformKind.VisionOSBoundedTabletop);
        }

        public void LaunchVisionOSImmersive()
        {
            LaunchNewRun(HollowPlatformKind.VisionOSImmersive);
        }

        public void LaunchContinueWindows()
        {
            LaunchContinueRun(HollowPlatformKind.WindowsStandard3D);
        }

        public void LaunchContinueVisionOSBounded()
        {
            LaunchContinueRun(HollowPlatformKind.VisionOSBoundedTabletop);
        }

        public void LaunchContinueVisionOSImmersive()
        {
            LaunchContinueRun(HollowPlatformKind.VisionOSImmersive);
        }

        public void OpenRoomDesigner()
        {
            var route = ViewModel.OpenRoomDesigner();
            if (ViewModel.State == MainMenuState.Error)
            {
                RebuildActiveScreen();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }

        public void LaunchDeveloperLab()
        {
            var route = ViewModel.LaunchDeveloperLab();
            if (ViewModel.State == MainMenuState.Error)
            {
                RebuildActiveScreen();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }

        public void LaunchArenaMode()
        {
            LaunchArenaModeWithCharacter("balanced");
        }

        public void LaunchArenaModeWithCharacter(string characterId)
        {
            var route = ViewModel.LaunchArenaMode(characterId);
            if (ViewModel.State == MainMenuState.Error)
            {
                LogVisionOSLaunchBlocked("Arena", ViewModel.ErrorMessage);
                RebuildActiveScreen();
                return;
            }

            LogVisionOSLaunchDiagnostic("Arena", defaultPlatformKind, ViewModel.SelectedCharacterId, challengeId: string.Empty, route: route);
            ArenaModeHandoff.Set(string.Empty, false, defaultReturnRoute, defaultPlatformKind, ViewModel.SelectedCharacterId);
            SceneLoaderService.LoadRouteAsync(route);
        }

        public void OpenChallenges()
        {
            ViewModel.OpenChallenges();
            RebuildActiveScreen();
        }

        public void BackFromChallenges()
        {
            ViewModel.BackFromChallenges();
            RebuildActiveScreen();
        }

        public void LaunchDefaultChallenge(string challengeId)
        {
            LaunchChallenge(challengeId, defaultPlatformKind);
        }

        public void LaunchChallengeWindows(string challengeId)
        {
            LaunchChallenge(challengeId, HollowPlatformKind.WindowsStandard3D);
        }

        public void LaunchChallengeVisionOSBounded(string challengeId)
        {
            LaunchChallenge(challengeId, HollowPlatformKind.VisionOSBoundedTabletop);
        }

        public void LaunchChallengeVisionOSImmersive(string challengeId)
        {
            LaunchChallenge(challengeId, HollowPlatformKind.VisionOSImmersive);
        }

        private void LaunchNewRun(HollowPlatformKind platformKind)
        {
            if (ViewModel.State == MainMenuState.Error)
            {
                RebuildActiveScreen();
                return;
            }

            ViewModel.BeginNewRun(platformKind);
            RebuildActiveScreen();
        }

        public void SelectBalancedCharacter()
        {
            SelectCharacterAndLaunch("balanced");
        }

        public void SelectHeavyCharacter()
        {
            SelectCharacterAndLaunch("heavy");
        }

        public void BackFromCharacterSelect()
        {
            ViewModel.BackFromCharacterSelect();
            RebuildActiveScreen();
        }

        public void SelectCharacterAndLaunch(string characterId)
        {
            var route = ViewModel.SelectCharacterAndLaunch(characterId);
            if (ViewModel.State == MainMenuState.Error)
            {
                LogVisionOSLaunchBlocked("Normal Run", ViewModel.ErrorMessage);
                RebuildActiveScreen();
                return;
            }

            LogVisionOSLaunchDiagnostic("Normal Run", ViewModel.PendingNewRunPlatformKind, ViewModel.SelectedCharacterId, challengeId: string.Empty, route: route);
            SceneLoaderService.LoadRouteAsync(route);
        }

        private void LaunchContinueRun(HollowPlatformKind platformKind)
        {
            var route = ViewModel.LaunchContinueRun(platformKind);
            if (ViewModel.State == MainMenuState.Error)
            {
                LogVisionOSLaunchBlocked("Continue Run", ViewModel.ErrorMessage);
                RebuildActiveScreen();
                return;
            }

            LogVisionOSLaunchDiagnostic("Continue Run", platformKind, ViewModel.SelectedCharacterId, challengeId: string.Empty, route: route);
            SceneLoaderService.LoadRouteAsync(route);
        }

        private void LaunchChallenge(string challengeId, HollowPlatformKind platformKind)
        {
            var route = ViewModel.LaunchChallenge(challengeId, platformKind);
            if (ViewModel.State == MainMenuState.Error)
            {
                LogVisionOSLaunchBlocked("Challenge", ViewModel.ErrorMessage);
                RebuildActiveScreen();
                return;
            }

            LogVisionOSLaunchDiagnostic("Challenge", platformKind, ViewModel.SelectedCharacterId, challengeId, route);
            SceneLoaderService.LoadRouteAsync(route);
        }

        private void LogVisionOSLaunchDiagnostic(
            string mode,
            HollowPlatformKind platformKind,
            string characterId = "",
            string challengeId = "",
            AppShellRoute route = AppShellRoute.MainMenu)
        {
            if (defaultReturnRoute != AppShellRoute.MainMenuVisionOS && platformKind == HollowPlatformKind.WindowsStandard3D)
            {
                return;
            }

            var selectedProfile = ViewModel.SelectedProfile;
            var profileName = selectedProfile != null ? selectedProfile.DisplayName : "<none>";
            Debug.Log(
                $"VisionOS menu launch: mode={mode} profile={profileName} platform={platformKind} character={characterId} challenge={challengeId} route={route}");
        }

        private void LogVisionOSLaunchBlocked(string mode, string errorMessage)
        {
            if (defaultReturnRoute != AppShellRoute.MainMenuVisionOS)
            {
                return;
            }

            Debug.LogWarning($"VisionOS menu launch blocked: mode={mode} error={errorMessage}");
        }

        private void RebuildActiveScreen()
        {
            if (visionOSScreen != null)
            {
                visionOSScreen.Rebuild();
                return;
            }

            screen?.Rebuild();
        }
    }
}
