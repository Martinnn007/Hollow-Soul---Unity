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
        [SerializeField] private PlatformPresentationRoot presentationRoot;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlayerSpawnPoint playerSpawnPoint;
        [SerializeField] private PlaceholderPlayerController playerController;

        public HollowPlatformKind PlatformKind => platformKind;

        public RuntimeSessionMode SessionMode => sessionMode;

        public PlatformPresentationRoot PresentationRoot => presentationRoot;

        public RoomRuntimeRoot RoomRuntimeRoot => roomRuntimeRoot;

        public PlayerSpawnPoint PlayerSpawnPoint => playerSpawnPoint;

        public PlaceholderPlayerController PlayerController => playerController;

        public GameSessionState SessionState { get; private set; }

        public void Configure(HollowPlatformKind nextPlatformKind)
        {
            platformKind = nextPlatformKind;
        }

        private void Awake()
        {
            InitializeSession();
        }

        public void InitializeSession()
        {
            ResolveReferences();
            presentationRoot?.Configure(platformKind);

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
            presentationRoot ??= GetComponentInChildren<PlatformPresentationRoot>(includeInactive: true);
            roomRuntimeRoot ??= GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true);
            playerSpawnPoint ??= GetComponentInChildren<PlayerSpawnPoint>(includeInactive: true);
            playerController ??= GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true);
        }
    }
}
