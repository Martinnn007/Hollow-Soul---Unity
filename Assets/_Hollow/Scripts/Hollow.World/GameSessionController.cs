using Hollow.Core;
using Hollow.Core.App;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.World
{
    public sealed class GameSessionController : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;
        [SerializeField] private RuntimeSessionMode sessionMode = RuntimeSessionMode.ProfileBacked;
        [SerializeField] private TextAsset sampleRoomRuntimeJson;
        [SerializeField] private PlatformPresentationRoot presentationRoot;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlayerSpawnPoint playerSpawnPoint;
        [SerializeField] private PlaceholderPlayerController playerController;
        private AppShellRoute transientReturnRoute = AppShellRoute.MainMenu;

        public HollowPlatformKind PlatformKind => platformKind;

        public RuntimeSessionMode SessionMode => sessionMode;

        public TextAsset SampleRoomRuntimeJson => sampleRoomRuntimeJson;

        public PlatformPresentationRoot PresentationRoot => presentationRoot;

        public RoomRuntimeRoot RoomRuntimeRoot => roomRuntimeRoot;

        public PlayerSpawnPoint PlayerSpawnPoint => playerSpawnPoint;

        public PlaceholderPlayerController PlayerController => playerController;

        public GameSessionState SessionState { get; private set; }

        public void Configure(HollowPlatformKind nextPlatformKind)
        {
            platformKind = nextPlatformKind;
        }

        public void Configure(HollowPlatformKind nextPlatformKind, TextAsset roomRuntimeJson)
        {
            platformKind = nextPlatformKind;
            sampleRoomRuntimeJson = roomRuntimeJson;
        }

        private void Awake()
        {
            InitializeSession();
        }

        public void InitializeSession()
        {
            ResolveReferences();
            presentationRoot?.Configure(platformKind);
            var isDesignerPlaytest = RoomPlaytestHandoff.TryConsume(out var playtestJson, out var playtestMode, out var playtestReturnRoute);
            transientReturnRoute = isDesignerPlaytest ? playtestReturnRoute : AppShellRoute.MainMenu;
            var importedAsset = isDesignerPlaytest ? ImportRoomAsset(playtestJson, "Room Designer Playtest") : ImportRoomAssetIfAvailable();
            var spawnPosition = importedAsset?.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            var selectedProfileContext = ProfileSessionHost.Instance?.SelectedProfileContext;
            var selectedProfile = selectedProfileContext?.SelectedProfile;
            var launchMode = selectedProfileContext?.LaunchMode ?? RunLaunchMode.NewRun;
            var effectiveSessionMode = isDesignerPlaytest ? playtestMode : sessionMode;
            SessionState = GameSessionState.Create(effectiveSessionMode, platformKind, launchMode, selectedProfile, spawnPosition);

            if (importedAsset != null && !isDesignerPlaytest && TryGetBranchSessionController(out var branchSessionController))
            {
                branchSessionController.Initialize(importedAsset, SessionState);
                ResolveReferences();
                return;
            }

            if (importedAsset != null && roomRuntimeRoot != null)
            {
                roomRuntimeRoot.BuildFrom(importedAsset);
                ResolveReferences();
            }

            if (playerController != null)
            {
                playerController.transform.localPosition = spawnPosition;
                playerController.ConfigureDefault();
            }
        }

        private void Update()
        {
            if (SessionState?.SessionMode == RuntimeSessionMode.TransientRoomDesignerPlaytest && Input.GetKeyDown(KeyCode.Escape))
            {
                if (HollowBootstrap.Instance != null)
                {
                    HollowBootstrap.Instance.AppStateMachine.TransitionTo(transientReturnRoute);
                    SceneLoaderService.LoadRouteAsync(transientReturnRoute);
                }
            }
        }

        private void ResolveReferences()
        {
            if (presentationRoot == null)
            {
                presentationRoot = GetComponentInChildren<PlatformPresentationRoot>(includeInactive: true);
            }

            if (roomRuntimeRoot == null)
            {
                roomRuntimeRoot = GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true);
            }

            if (playerSpawnPoint == null)
            {
                playerSpawnPoint = GetComponentInChildren<PlayerSpawnPoint>(includeInactive: true);
            }

            if (playerController == null)
            {
                playerController = GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true);
            }
        }

        private ImportedRoomRuntimeAsset ImportRoomAssetIfAvailable()
        {
            if (sampleRoomRuntimeJson == null)
            {
                return null;
            }

            return ImportRoomAsset(sampleRoomRuntimeJson.text, sampleRoomRuntimeJson.name);
        }

        private static ImportedRoomRuntimeAsset ImportRoomAsset(string json, string displayName)
        {
            if (!HollowRuntimeV2Importer.TryImport(json, out var importedAsset, out var error))
            {
                Debug.LogError($"Failed to load room runtime '{displayName}': {error}");
                return null;
            }

            return importedAsset;
        }

        private bool TryGetBranchSessionController(out IBranchSessionController branchSessionController)
        {
            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour is IBranchSessionController controller)
                {
                    branchSessionController = controller;
                    return true;
                }
            }

            branchSessionController = null;
            return false;
        }
    }
}
