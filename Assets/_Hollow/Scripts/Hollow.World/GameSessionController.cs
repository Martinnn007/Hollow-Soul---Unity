using Hollow.Core;
using Hollow.Core.App;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using System.Linq;
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

        public string CurrentChallengeId
        {
            get
            {
                var snapshot = CreateCurrentSnapshot();
                return !string.IsNullOrWhiteSpace(snapshot?.challengeId)
                    ? snapshot.challengeId
                    : SessionState?.SelectedChallengeId ?? string.Empty;
            }
        }

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
            var selectedCharacterId = selectedProfileContext?.SelectedCharacterId ?? "balanced";
            var selectedChallengeId = selectedProfileContext?.SelectedChallengeId ?? string.Empty;
            var developerLabRequested = selectedProfileContext?.DeveloperLabRequested ?? false;
            var effectiveSessionMode = isDesignerPlaytest
                ? playtestMode
                : developerLabRequested
                    ? RuntimeSessionMode.DeveloperLab
                    : !string.IsNullOrWhiteSpace(selectedChallengeId)
                    ? RuntimeSessionMode.TransientChallenge
                    : sessionMode;
            SessionState = GameSessionState.Create(effectiveSessionMode, platformKind, launchMode, selectedProfile, spawnPosition, selectedCharacterId, selectedChallengeId);

            if (importedAsset != null && !isDesignerPlaytest && TryGetBranchSessionController(out var branchSessionController))
            {
                branchSessionController.Initialize(importedAsset, SessionState);
                ResolveReferences();
                ConfigureGameplayCameraFollow();
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

            ConfigureGameplayCameraFollow();
        }

        public void RestartCurrentSession()
        {
            var route = PlatformPresentationModeResolver.RouteForPlatform(platformKind);
            var snapshot = CreateCurrentSnapshot();
            var challengeId = snapshot?.challengeId ?? SessionState?.SelectedChallengeId ?? string.Empty;
            var characterId = snapshot?.runBuild?.selectedCharacterId ?? SessionState?.SelectedCharacterId ?? "balanced";
            var profileHost = ProfileSessionHost.Instance;
            var context = profileHost?.SelectedProfileContext;
            var selectedProfile = context?.SelectedProfile;

            if (SessionState?.SessionMode == RuntimeSessionMode.DeveloperLab)
            {
                context?.SetLaunchMode(RunLaunchMode.NewRun);
                context?.SetSelectedCharacterId(characterId);
                context?.SetSelectedChallengeId(string.Empty);
                context?.SetDeveloperLabRequested(true);
                TransitionAndLoad(route);
                return;
            }

            if (context != null && selectedProfile != null && !selectedProfile.IsEmpty)
            {
                var slotId = new ProfileSlotId(selectedProfile.SlotIndex);
                profileHost?.RunSaveStore?.ClearActiveRun(slotId);
                context.SetLaunchMode(RunLaunchMode.NewRun);
                context.SetSelectedCharacterId(characterId);
                context.SetSelectedChallengeId(challengeId);

                if (!string.IsNullOrWhiteSpace(challengeId))
                {
                    var seed = snapshot != null && snapshot.runSeed != 0 ? snapshot.runSeed : snapshot?.branchSeed ?? 0;
                    profileHost?.ChallengeResultStore?.MarkChallengeAttemptStarted(slotId, challengeId, seed);
                    var updated = profileHost?.ProfileStore?.MarkLastPlayed(slotId);
                    if (updated != null)
                    {
                        context.UpdateSelectedProfile(updated);
                    }
                }
                else
                {
                    var updated = profileHost?.ProfileStore?.MarkRunStarted(slotId);
                    if (updated != null)
                    {
                        context.UpdateSelectedProfile(updated);
                    }
                }
            }

            TransitionAndLoad(route);
        }

        public void QuitCurrentSessionToProfileMenu()
        {
            var profileHost = ProfileSessionHost.Instance;
            var context = profileHost?.SelectedProfileContext;
            var selectedProfile = context?.SelectedProfile;
            var snapshot = CreateCurrentSnapshot();
            if (SessionState?.SessionMode != RuntimeSessionMode.DeveloperLab &&
                snapshot != null &&
                selectedProfile != null &&
                !selectedProfile.IsEmpty &&
                profileHost?.RunSaveStore != null)
            {
                var slotId = new ProfileSlotId(selectedProfile.SlotIndex);
                profileHost.RunSaveStore.SaveActiveRun(slotId, snapshot);
                var updated = profileHost.ProfileStore?.LoadSlotSummaries()
                    .FirstOrDefault(summary => summary.SlotIndex == selectedProfile.SlotIndex);
                if (updated != null)
                {
                    context.UpdateSelectedProfile(updated);
                }
            }

            context?.SetLaunchMode(RunLaunchMode.NewRun);
            context?.SetSelectedChallengeId(string.Empty);
            context?.SetDeveloperLabRequested(false);
            TransitionAndLoad(AppShellRoute.MainMenu);
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

        private RunSaveSnapshot CreateCurrentSnapshot()
        {
            return TryGetBranchSessionController(out var branchSessionController)
                ? branchSessionController.CreateSnapshot()
                : null;
        }

        private static void TransitionAndLoad(AppShellRoute route)
        {
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(route);
            }

            SceneLoaderService.LoadRouteAsync(route);
        }

        private void ConfigureGameplayCameraFollow()
        {
            if (playerController == null)
            {
                return;
            }

            var rigMetadata = FindPlatformCameraRig();
            var targetCamera = rigMetadata != null
                ? rigMetadata.GetComponentInChildren<Camera>(includeInactive: true)
                : Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (targetCamera == null)
            {
                return;
            }

            rigMetadata ??= targetCamera.GetComponentInParent<CameraRigMetadata>();
            var host = rigMetadata != null ? rigMetadata.gameObject : targetCamera.gameObject;
            var follow = host.GetComponent<GameplayCameraFollowController>() ?? host.AddComponent<GameplayCameraFollowController>();
            follow.Configure(playerController.transform, platformKind);
        }

        private CameraRigMetadata FindPlatformCameraRig()
        {
            var rigs = FindObjectsByType<CameraRigMetadata>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var rig in rigs)
            {
                if (rig.PlatformKind == platformKind)
                {
                    return rig;
                }
            }

            return rigs.Length > 0 ? rigs[0] : null;
        }
    }
}
