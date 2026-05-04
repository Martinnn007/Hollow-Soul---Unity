namespace Hollow.Core.App
{
    using UnityEngine.SceneManagement;
    using UnityEngine;

    public static class SceneLoaderService
    {
        public static string SceneNameForRoute(AppShellRoute route)
        {
            return route switch
            {
                AppShellRoute.Boot => "Boot",
                AppShellRoute.MainMenu => "MainMenu",
                AppShellRoute.GameWindows => "Game_Windows",
                AppShellRoute.GameVisionOSBounded => "Game_VisionOS_Bounded",
                AppShellRoute.GameVisionOSImmersive => "Game_VisionOS_Immersive",
                AppShellRoute.RoomDesigner => "RoomDesigner",
                AppShellRoute.DeveloperSandbox => "DeveloperSandbox",
                AppShellRoute.ArenaMode => "ArenaMode",
                _ => "Boot"
            };
        }

        public static AsyncOperation LoadRouteAsync(AppShellRoute route, LoadSceneMode mode = LoadSceneMode.Single)
        {
            return SceneManager.LoadSceneAsync(SceneNameForRoute(route), mode);
        }
    }
}
