using System;

namespace Hollow.Core.App
{
    public enum AppShellRoute
    {
        Boot = 0,
        MainMenu = 1,
        GameWindows = 2,
        GameVisionOSBounded = 3,
        GameVisionOSImmersive = 4,
        RoomDesigner = 5,
        DeveloperSandbox = 6,
        ArenaMode = 7,
        MainMenuVisionOS = 8
    }

    public sealed class AppStateMachine
    {
        public AppShellRoute CurrentRoute { get; private set; } = AppShellRoute.Boot;

        public event Action<AppShellRoute> RouteChanged;

        public void TransitionTo(AppShellRoute nextRoute)
        {
            if (CurrentRoute == nextRoute)
            {
                return;
            }

            CurrentRoute = nextRoute;
            RouteChanged?.Invoke(CurrentRoute);
        }
    }
}
