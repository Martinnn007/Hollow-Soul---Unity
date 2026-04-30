using System;
using Hollow.Presentation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using Hollow.UI.MainMenu;
using Hollow.UI.Shell;
using Hollow.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Diagnostics
{
    public sealed class PlatformRuntimeQaProbe : MonoBehaviour
    {
        [SerializeField] private PlatformRuntimeQaSnapshot lastSnapshot = new();

        public PlatformRuntimeQaSnapshot LastSnapshot => lastSnapshot;

        private void Start()
        {
            Capture();
        }

        public PlatformRuntimeQaSnapshot Capture()
        {
            lastSnapshot = CaptureCurrentScene();
            return lastSnapshot;
        }

        public static PlatformRuntimeQaSnapshot CaptureCurrentScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            var presentationRoot = FindFirst<PlatformPresentationRoot>();
            var shell = FindFirst<PlatformShellController>();
            var shellCanvas = shell != null ? shell.GetComponent<Canvas>() : GameObject.Find("PlatformShellCanvas")?.GetComponent<Canvas>();
            var roomRuntime = FindFirst<RoomRuntimeRoot>();
            var gameSession = FindFirst<GameSessionController>();
            var mainMenu = FindFirst<MainMenuController>();
            var roomDesigner = FindFirst<RoomDesignerController>();
            var catalog = PresentationContentProvider.ActiveCatalog;

            return new PlatformRuntimeQaSnapshot
            {
                sceneName = activeScene.name,
                platformKind = presentationRoot != null ? presentationRoot.PlatformKind.ToString() : gameSession != null ? gameSession.PlatformKind.ToString() : "Unknown",
                worldScale = presentationRoot != null ? presentationRoot.WorldScale : 1f,
                hasPresentationRoot = presentationRoot != null,
                hasPlatformShellCanvas = shellCanvas != null,
                hudOutsideWorldRoot = presentationRoot == null || shellCanvas == null || !shellCanvas.transform.IsChildOf(presentationRoot.transform),
                hasGameSessionController = gameSession != null,
                hasRoomRuntimeRoot = roomRuntime != null,
                hasMainMenuController = mainMenu != null,
                hasRoomDesignerController = roomDesigner != null,
                hasPresentationCatalog = catalog != null,
                frameTimeMs = Time.deltaTime > 0f ? Time.deltaTime * 1000f : 0f,
                realtimeSinceStartup = Time.realtimeSinceStartup
            };
        }

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        }
    }

    [Serializable]
    public sealed class PlatformRuntimeQaSnapshot
    {
        public string sceneName = string.Empty;
        public string platformKind = string.Empty;
        public float worldScale;
        public bool hasPresentationRoot;
        public bool hasPlatformShellCanvas;
        public bool hudOutsideWorldRoot;
        public bool hasGameSessionController;
        public bool hasRoomRuntimeRoot;
        public bool hasMainMenuController;
        public bool hasRoomDesignerController;
        public bool hasPresentationCatalog;
        public float frameTimeMs;
        public float realtimeSinceStartup;
    }
}
