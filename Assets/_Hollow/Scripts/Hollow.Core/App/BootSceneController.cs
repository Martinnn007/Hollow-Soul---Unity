using UnityEngine;

namespace Hollow.Core.App
{
    public sealed class BootSceneController : MonoBehaviour
    {
        [SerializeField] private bool loadMainMenuOnStart = true;
        [SerializeField] private AppShellRoute defaultStartupRoute = AppShellRoute.MainMenu;
        [SerializeField] private bool preferVisionOSStartupRoute = true;
        [SerializeField] private AppShellRoute visionOSStartupRoute = AppShellRoute.MainMenuVisionOS;

        private void Start()
        {
            if (!loadMainMenuOnStart)
            {
                return;
            }

            var route = ResolveStartupRoute(Application.platform, defaultStartupRoute, preferVisionOSStartupRoute, visionOSStartupRoute);
            HollowBootstrap.Instance?.AppStateMachine.TransitionTo(route);
            SceneLoaderService.LoadRouteAsync(route);
        }

        public static AppShellRoute ResolveStartupRoute(
            RuntimePlatform platform,
            AppShellRoute defaultRoute,
            bool preferVisionOSRoute,
            AppShellRoute visionOSRoute)
        {
            return preferVisionOSRoute && IsVisionOSRuntime(platform)
                ? visionOSRoute
                : defaultRoute;
        }

        public void ConfigureStartup(
            bool loadOnStart,
            AppShellRoute nextDefaultStartupRoute,
            bool preferVisionOSRoute,
            AppShellRoute nextVisionOSStartupRoute)
        {
            loadMainMenuOnStart = loadOnStart;
            defaultStartupRoute = nextDefaultStartupRoute;
            preferVisionOSStartupRoute = preferVisionOSRoute;
            visionOSStartupRoute = nextVisionOSStartupRoute;
        }

        private static bool IsVisionOSRuntime(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.VisionOS;
        }
    }
}
