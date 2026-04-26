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
            BuildImportedRoomIfAvailable();
            ResolveReferences();

            var spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.WorldPosition : Vector3.zero;
            var selectedProfile = ProfileSessionHost.Instance?.SelectedProfileContext?.SelectedProfile;
            SessionState = GameSessionState.Create(sessionMode, platformKind, selectedProfile, spawnPosition);

            if (playerController != null)
            {
                playerController.transform.position = spawnPosition;
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

        private void BuildImportedRoomIfAvailable()
        {
            if (sampleRoomRuntimeJson == null || roomRuntimeRoot == null)
            {
                return;
            }

            if (!HollowRuntimeV2Importer.TryImport(sampleRoomRuntimeJson.text, out var importedAsset, out var error))
            {
                Debug.LogError($"Failed to load M3 sample room '{sampleRoomRuntimeJson.name}': {error}");
                return;
            }

            roomRuntimeRoot.BuildFrom(importedAsset);
        }
    }
}
