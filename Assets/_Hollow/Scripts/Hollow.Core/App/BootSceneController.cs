using System.Collections;
using Hollow.Core.Diagnostics;
using UnityEngine;

namespace Hollow.Core.App
{
    public sealed class BootSceneController : MonoBehaviour
    {
        [SerializeField] private bool loadMainMenuOnStart = true;
        [SerializeField] private AppShellRoute defaultStartupRoute = AppShellRoute.MainMenu;
        [SerializeField] private bool preferVisionOSStartupRoute = true;
        [SerializeField] private AppShellRoute visionOSStartupRoute = AppShellRoute.MainMenuVisionOS;
        [SerializeField] private bool showBootLoadingScreen = true;
        [SerializeField] private string studioName = "CineFit Studio";
        [SerializeField] private string gameTitle = "Hollow Soul";
        [SerializeField] private float minimumVisibleSeconds = 1.5f;
        [SerializeField] private bool allowEditorFastBoot = true;
        [SerializeField] private float editorMinimumVisibleSeconds = 0.25f;
        [SerializeField] private float fadeOutSeconds = 0.2f;
        [SerializeField] private BootPreloadSettings preloadSettings = new();

        private Coroutine bootRoutine;
        private BootLoadingScreenController loadingScreen;
        private BootPreloadReport lastPreloadReport;
        private string lastError = string.Empty;

        public bool IsBootLoading => bootRoutine != null;

        public BootLoadingScreenController LoadingScreen => loadingScreen;

        public BootPreloadReport LastPreloadReport => lastPreloadReport;

        public string LastError => lastError;

        private void Start()
        {
            if (!loadMainMenuOnStart)
            {
                return;
            }

            bootRoutine = StartCoroutine(BootRoutine());
        }

        private IEnumerator BootRoutine()
        {
            var startedRealtime = Time.realtimeSinceStartup;
            var route = ResolveStartupRoute(Application.platform, defaultStartupRoute, preferVisionOSStartupRoute, visionOSStartupRoute);
            lastError = string.Empty;
            lastPreloadReport = new BootPreloadReport();
            M136PerformanceOperationCounters.ReportBootLoadingStart();

            if (showBootLoadingScreen)
            {
                loadingScreen = BootLoadingScreenController.Create(transform);
                loadingScreen.Show(studioName, gameTitle, "Starting");
            }

            var routine = BootRoutineBody(startedRealtime, route);
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                    {
                        break;
                    }

                    current = routine.Current;
                }
                catch (System.Exception exception)
                {
                    lastError = $"{exception.GetType().Name}: {exception.Message}";
                    M136PerformanceOperationCounters.ReportBootLoadingFailure();
                    loadingScreen?.ShowFailure(lastError);
                    Debug.LogError($"Boot loading failed: {lastError}", this);
                    break;
                }

                yield return current;
            }

            bootRoutine = null;
        }

        private IEnumerator BootRoutineBody(float startedRealtime, AppShellRoute route)
        {
            var service = new BootPreloadService();
            yield return service.Run(
                preloadSettings ?? BootPreloadSettings.Default(),
                progress => loadingScreen?.SetStage(progress.Stage, progress.Progress01),
                lastPreloadReport);

            loadingScreen?.SetStage("Loading menu", 0.97f);
            var operation = SceneLoaderService.LoadRouteAsync(route);
            if (operation == null)
            {
                throw new MissingReferenceException($"Could not load startup scene for route {route}.");
            }

            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f)
            {
                loadingScreen?.SetStage("Loading menu", Mathf.Lerp(0.97f, 0.99f, Mathf.Clamp01(operation.progress / 0.9f)));
                yield return null;
            }

            var minimumVisible = EffectiveMinimumVisibleSeconds();
            while (Time.realtimeSinceStartup - startedRealtime < minimumVisible)
            {
                yield return null;
            }

            loadingScreen?.MarkReady("Opening menu");
            M136PerformanceOperationCounters.ReportBootLoadingCompletion((Time.realtimeSinceStartup - startedRealtime) * 1000f);
            HollowBootstrap.Instance?.AppStateMachine.TransitionTo(route);
            if (loadingScreen != null && fadeOutSeconds > 0f)
            {
                yield return loadingScreen.FadeOut(fadeOutSeconds);
            }

            operation.allowSceneActivation = true;
            while (!operation.isDone)
            {
                yield return null;
            }
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

        public void ConfigureBootLoading(
            bool showScreen,
            string nextStudioName,
            string nextGameTitle,
            float nextMinimumVisibleSeconds,
            bool nextAllowEditorFastBoot,
            float nextEditorMinimumVisibleSeconds,
            BootPreloadSettings nextPreloadSettings)
        {
            showBootLoadingScreen = showScreen;
            studioName = string.IsNullOrWhiteSpace(nextStudioName) ? studioName : nextStudioName;
            gameTitle = string.IsNullOrWhiteSpace(nextGameTitle) ? gameTitle : nextGameTitle;
            minimumVisibleSeconds = Mathf.Max(0f, nextMinimumVisibleSeconds);
            allowEditorFastBoot = nextAllowEditorFastBoot;
            editorMinimumVisibleSeconds = Mathf.Max(0f, nextEditorMinimumVisibleSeconds);
            preloadSettings = nextPreloadSettings ?? BootPreloadSettings.Default();
        }

        public float EffectiveMinimumVisibleSeconds()
        {
#if UNITY_EDITOR
            if (allowEditorFastBoot)
            {
                return Mathf.Max(0f, editorMinimumVisibleSeconds);
            }
#endif
            return Mathf.Max(0f, minimumVisibleSeconds);
        }

        private static bool IsVisionOSRuntime(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.VisionOS;
        }
    }
}
