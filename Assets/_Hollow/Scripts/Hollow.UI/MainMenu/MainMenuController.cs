using Hollow.Core.App;
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

        private MainMenuScreen screen;

        public MainMenuViewModel ViewModel { get; private set; }

        private void Awake()
        {
            var appStateMachine = HollowBootstrap.Instance != null ? HollowBootstrap.Instance.AppStateMachine : new AppStateMachine();
            var profileHost = ProfileSessionHost.Instance;
            var profileStore = profileHost?.ProfileStore ?? new JsonProfileStore();
            var selectedProfileContext = profileHost?.SelectedProfileContext ?? new SelectedProfileContext();
            ViewModel = new MainMenuViewModel(profileStore, selectedProfileContext, appStateMachine, challengeCatalog);
            screen = GetComponent<MainMenuScreen>() ?? gameObject.AddComponent<MainMenuScreen>();
        }

        public ChallengeCatalogDefinition ChallengeCatalog => challengeCatalog;

        public void ConfigureChallengeCatalog(ChallengeCatalogDefinition nextChallengeCatalog)
        {
            challengeCatalog = nextChallengeCatalog;
        }

        private void Start()
        {
            screen.Build(this);
        }

        public void SelectSlot(int slotIndex)
        {
            ViewModel.SelectOrCreateSlot(slotIndex);
            screen.Rebuild();
        }

        public void DeleteSlot(int slotIndex)
        {
            ViewModel.DeleteSlot(slotIndex);
            screen.Rebuild();
        }

        public void BackToProfiles()
        {
            ViewModel.BackToProfiles();
            screen.Rebuild();
        }

        public void LaunchDefaultPlatform()
        {
            LaunchNewRun(defaultPlatformKind);
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
                screen.Rebuild();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }

        public void LaunchDeveloperLab()
        {
            var route = ViewModel.LaunchDeveloperLab();
            if (ViewModel.State == MainMenuState.Error)
            {
                screen.Rebuild();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }

        public void LaunchArenaMode()
        {
            var route = ViewModel.LaunchArenaMode();
            if (ViewModel.State == MainMenuState.Error)
            {
                screen.Rebuild();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }

        public void OpenChallenges()
        {
            ViewModel.OpenChallenges();
            screen.Rebuild();
        }

        public void BackFromChallenges()
        {
            ViewModel.BackFromChallenges();
            screen.Rebuild();
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
                screen.Rebuild();
                return;
            }

            ViewModel.BeginNewRun(platformKind);
            screen.Rebuild();
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
            screen.Rebuild();
        }

        private void SelectCharacterAndLaunch(string characterId)
        {
            var route = ViewModel.SelectCharacterAndLaunch(characterId);
            if (ViewModel.State == MainMenuState.Error)
            {
                screen.Rebuild();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }

        private void LaunchContinueRun(HollowPlatformKind platformKind)
        {
            var route = ViewModel.LaunchContinueRun(platformKind);
            if (ViewModel.State == MainMenuState.Error)
            {
                screen.Rebuild();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }

        private void LaunchChallenge(string challengeId, HollowPlatformKind platformKind)
        {
            var route = ViewModel.LaunchChallenge(challengeId, platformKind);
            if (ViewModel.State == MainMenuState.Error)
            {
                screen.Rebuild();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }
    }
}
