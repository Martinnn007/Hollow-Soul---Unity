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
            LaunchPlatform(defaultPlatformKind);
        }

        public void LaunchWindows()
        {
            LaunchPlatform(HollowPlatformKind.WindowsStandard3D);
        }

        public void LaunchVisionOSBounded()
        {
            LaunchPlatform(HollowPlatformKind.VisionOSBoundedTabletop);
        }

        public void LaunchVisionOSImmersive()
        {
            LaunchPlatform(HollowPlatformKind.VisionOSImmersive);
        }

        private void LaunchPlatform(HollowPlatformKind platformKind)
        {
            var route = ViewModel.LaunchPlatform(platformKind);
            if (ViewModel.State == MainMenuState.Error)
            {
                screen.Rebuild();
                return;
            }

            SceneLoaderService.LoadRouteAsync(route);
        }
    }
}
