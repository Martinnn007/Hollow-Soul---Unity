using Hollow.Core.App;
using Hollow.Persistence;
using Hollow.Platform;
using UnityEngine;

namespace Hollow.UI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind defaultPlatformKind = HollowPlatformKind.WindowsStandard3D;

        private MainMenuScreen screen;

        public MainMenuViewModel ViewModel { get; private set; }

        private void Awake()
        {
            var appStateMachine = HollowBootstrap.Instance != null ? HollowBootstrap.Instance.AppStateMachine : new AppStateMachine();
            var profileHost = ProfileSessionHost.Instance;
            var profileStore = profileHost?.ProfileStore ?? new JsonProfileStore();
            var selectedProfileContext = profileHost?.SelectedProfileContext ?? new SelectedProfileContext();
            ViewModel = new MainMenuViewModel(profileStore, selectedProfileContext, appStateMachine);
            screen = GetComponent<MainMenuScreen>() ?? gameObject.AddComponent<MainMenuScreen>();
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

        private void LaunchNewRun(HollowPlatformKind platformKind)
        {
            var route = ViewModel.LaunchNewRun(platformKind);
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
    }
}
