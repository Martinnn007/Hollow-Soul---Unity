using Hollow.Core;
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
            var importedAsset = ImportRoomAssetIfAvailable();
            var spawnPosition = importedAsset?.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            var selectedProfileContext = ProfileSessionHost.Instance?.SelectedProfileContext;
            var selectedProfile = selectedProfileContext?.SelectedProfile;
            var launchMode = selectedProfileContext?.LaunchMode ?? RunLaunchMode.NewRun;
            SessionState = GameSessionState.Create(sessionMode, platformKind, launchMode, selectedProfile, spawnPosition);

            if (importedAsset != null && TryGetBranchSessionController(out var branchSessionController))
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

            if (!HollowRuntimeV2Importer.TryImport(sampleRoomRuntimeJson.text, out var importedAsset, out var error))
            {
                Debug.LogError($"Failed to load M3 sample room '{sampleRoomRuntimeJson.name}': {error}");
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
