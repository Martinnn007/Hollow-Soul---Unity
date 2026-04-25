using UnityEngine;

namespace Hollow.Core.App
{
    public sealed class BootSceneController : MonoBehaviour
    {
        [SerializeField] private bool loadMainMenuOnStart = true;

        private void Start()
        {
            if (!loadMainMenuOnStart)
            {
                return;
            }

            HollowBootstrap.Instance?.AppStateMachine.TransitionTo(AppShellRoute.MainMenu);
            SceneLoaderService.LoadRouteAsync(AppShellRoute.MainMenu);
        }
    }
}
