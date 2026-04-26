using System;
using System.Linq;
using Hollow.Combat;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Persistence;
using Hollow.Presentation;
using Hollow.Rewards;
using Hollow.Rooms;
using Hollow.World;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchSessionController : MonoBehaviour, IBranchSessionController
    {
        private const float RewardInteractionRadiusMeters = 1.25f;
        private const float PortalInteractionRadiusMeters = 1.5f;

        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlaceholderPlayerController playerController;
        [SerializeField] private RoomCombatController roomCombatController;
        [SerializeField] private GameObject rewardPickupPrefab;
        [SerializeField] private GameObject hubReturnPortalPrefab;

        private ImportedRoomRuntimeAsset roomAsset;
        private GameSessionState gameSessionState;
        private RoomRewardPickup currentRewardPickup;
        private HubReturnPortal currentHubPortal;
        private readonly RuntimeRewardCounter rewardCounter = new();
        private RunEconomy runEconomy = new();
        private PlayerRunStats playerRunStats = new();
        private IRunSaveStore runSaveStore;
        private ProfileSlotId activeProfileSlotId;
        private bool canPersist;
        private bool activeRunCompletedOrFailed;
        private bool suppressCheckpoint;

        public BranchSessionState State { get; private set; }

        public RuntimeRewardCounter RewardCounter => rewardCounter;

        public RunEconomy RunEconomy => runEconomy;

        public PlayerRunStats PlayerRunStats => playerRunStats;

        public int BankedSouls { get; private set; }

        public string SaveStatus { get; private set; } = "Transient";

        public GameObject RewardPickupPrefab => rewardPickupPrefab;

        public GameObject HubReturnPortalPrefab => hubReturnPortalPrefab;

        public RoomRewardPickup CurrentRewardPickup => currentRewardPickup;

        public HubReturnPortal CurrentHubPortal => currentHubPortal;

        public bool HubReturnRequested { get; private set; }

        public ImportedRoomRuntimeAsset RoomAsset => roomAsset;

        public void Configure(GameObject nextRewardPickupPrefab, GameObject nextHubReturnPortalPrefab)
        {
            rewardPickupPrefab = nextRewardPickupPrefab;
            hubReturnPortalPrefab = nextHubReturnPortalPrefab;
        }

        public void Initialize(ImportedRoomRuntimeAsset nextRoomAsset, GameSessionState nextSessionState)
        {
            roomAsset = nextRoomAsset;
            gameSessionState = nextSessionState;
            ResolveReferences();
            ResolvePersistence();

            if (roomAsset == null || roomRuntimeRoot == null || playerController == null || roomCombatController == null)
            {
                Debug.LogError("Branch session could not initialize because a required runtime reference is missing.");
                return;
            }

            roomCombatController.RoomCleared -= OnRoomCleared;
            roomCombatController.RoomCleared += OnRoomCleared;

            if (canPersist && gameSessionState.LaunchMode == Hollow.Core.RunLaunchMode.ContinueRun && runSaveStore.TryLoadActiveRun(activeProfileSlotId, out var snapshot))
            {
                InitializeFromSnapshot(roomAsset, gameSessionState, snapshot);
                return;
            }

            InitializeFresh(roomAsset, gameSessionState);
        }

        public void InitializeFresh(ImportedRoomRuntimeAsset nextRoomAsset, GameSessionState nextSessionState)
        {
            roomAsset = nextRoomAsset;
            gameSessionState = nextSessionState;
            ResolveReferences();
            ResolvePersistence();

            runEconomy = new RunEconomy();
            playerRunStats = new PlayerRunStats();
            rewardCounter.SetClaimedRewards(0);
            activeRunCompletedOrFailed = false;
            State = BranchSessionState.Create(BranchGenerator.CreateFiveRoomCross(roomAsset));
            LoadCurrentRoom(roomAsset.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero);
            CheckpointActiveRun();
        }

        public void InitializeFromSnapshot(ImportedRoomRuntimeAsset nextRoomAsset, GameSessionState nextSessionState, RunSaveSnapshot snapshot)
        {
            roomAsset = nextRoomAsset;
            gameSessionState = nextSessionState;
            ResolveReferences();
            ResolvePersistence();

            runEconomy = RunEconomy.FromSaveState(snapshot?.economy);
            playerRunStats = PlayerRunStats.FromSaveState(snapshot?.playerStats);
            rewardCounter.SetClaimedRewards(runEconomy.CollectedRewards.Count);
            activeRunCompletedOrFailed = false;
            State = BranchSessionState.Create(BranchGenerator.CreateFiveRoomCross(roomAsset));
            RestoreBranchRooms(snapshot);
            State.RestoreCurrentRoom(new BranchRoomId(snapshot?.currentRoomId ?? BranchRoomId.Origin.Value));
            suppressCheckpoint = true;
            LoadCurrentRoom(roomAsset.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero);
            RestorePlayerHealth(snapshot);
            ApplyRunStatsToPlayer(healAmount: 0);
            suppressCheckpoint = false;
            CheckpointActiveRun();
        }

        private void OnDestroy()
        {
            if (roomCombatController != null)
            {
                roomCombatController.RoomCleared -= OnRoomCleared;
                if (roomCombatController.PlayerHealth != null)
                {
                    roomCombatController.PlayerHealth.Died -= OnPlayerDied;
                }
            }
        }

        private void Update()
        {
            if (State == null || !GameplayInputReader.ReadCurrent().InteractPressed)
            {
                return;
            }

            TryInteract();
        }

        public bool TryInteract()
        {
            return TryClaimReward() || TryUseHubReturnPortal() || TryTraverseNearestDoor();
        }

        public bool TryTraverse(string direction)
        {
            if (!BranchTraversalService.CanTraverse(State, direction, out var connection))
            {
                return false;
            }

            State.EnterRoom(connection.ToRoomId);
            LoadCurrentRoom(BranchTraversalService.EntryPositionFor(roomRuntimeRoot, connection.ToDirection));
            CheckpointActiveRun();
            return true;
        }

        public BranchMiniMapModel CreateMiniMapModel()
        {
            return new BranchMiniMapModel(State);
        }

        private bool TryTraverseNearestDoor()
        {
            if (State == null || playerController == null || roomRuntimeRoot == null)
            {
                return false;
            }

            var playerPosition = playerController.transform.localPosition;
            var nearest = State.Graph.ConnectionsFrom(State.CurrentRoomId)
                .Where(connection => roomRuntimeRoot.TryGetDoorPort(connection.FromDirection, out _))
                .Select(connection =>
                {
                    roomRuntimeRoot.TryGetDoorPort(connection.FromDirection, out var port);
                    return new
                    {
                        Connection = connection,
                        Distance = Vector3.Distance(Flat(playerPosition), Flat(port.Position))
                    };
                })
                .OrderBy(candidate => candidate.Distance)
                .FirstOrDefault();

            if (nearest == null || nearest.Distance > BranchTraversalService.DoorInteractionRadiusMeters)
            {
                return false;
            }

            return TryTraverse(nearest.Connection.FromDirection);
        }

        private void LoadCurrentRoom(Vector3 playerLocalPosition)
        {
            DestroyTransientInteractables();
            roomRuntimeRoot.BuildFrom(roomAsset);
            playerController.transform.localPosition = playerLocalPosition;
            State.CurrentRoom.MarkVisited();
            roomCombatController.BeginRoom(roomRuntimeRoot, playerController, State.CurrentRoom.IsCleared);
            ApplyRunStatsToPlayer(healAmount: 0);
            SubscribePlayerDeath();
            UpdateDoorVisuals();
            VfxPresenter.Play(VfxCueId.DoorUnlock, roomRuntimeRoot.transform.position, roomRuntimeRoot.transform);
            AudioPresenter.Play(AudioCueId.DoorUnlock, roomRuntimeRoot.transform.position);
            SpawnRewardIfNeeded();
            SpawnHubPortalIfReady();
            CheckpointActiveRun();
        }

        private void OnRoomCleared(RoomCombatController _)
        {
            if (State == null)
            {
                return;
            }

            State.CurrentRoom.MarkCleared();
            if (State.CurrentRoom.Id != BranchRoomId.Origin)
            {
                State.CurrentRoom.MarkRewardPending();
            }

            UpdateDoorVisuals();
            SpawnRewardIfNeeded();
            SpawnHubPortalIfReady();
        }

        private bool TryClaimReward()
        {
            if (currentRewardPickup == null || playerController == null || State == null)
            {
                return false;
            }

            if (Vector3.Distance(Flat(playerController.transform.localPosition), Flat(currentRewardPickup.transform.localPosition)) > RewardInteractionRadiusMeters)
            {
                return false;
            }

            if (!currentRewardPickup.Claim())
            {
                return false;
            }

            State.CurrentRoom.MarkRewardClaimed();
            var grant = RewardResolver.Resolve(State.CurrentRoomId.Value);
            if (runEconomy.ApplyReward(grant))
            {
                var healAmount = playerRunStats.ApplyReward(grant);
                ApplyRunStatsToPlayer(healAmount);
                rewardCounter.SetClaimedRewards(runEconomy.CollectedRewards.Count);
            }

            DestroyRuntimeObject(currentRewardPickup.gameObject);
            currentRewardPickup = null;
            VfxPresenter.Play(VfxCueId.RewardClaim, playerController.transform.position, playerController.transform.parent);
            AudioPresenter.Play(AudioCueId.RewardClaim, playerController.transform.position);
            SpawnHubPortalIfReady();
            CheckpointActiveRun();
            return true;
        }

        private bool TryUseHubReturnPortal()
        {
            if (currentHubPortal == null || playerController == null)
            {
                return false;
            }

            if (Vector3.Distance(Flat(playerController.transform.localPosition), Flat(currentHubPortal.transform.localPosition)) > PortalInteractionRadiusMeters)
            {
                return false;
            }

            HubReturnRequested = true;
            VfxPresenter.Play(VfxCueId.PortalComplete, currentHubPortal.transform.position, currentHubPortal.transform.parent);
            AudioPresenter.Play(AudioCueId.PortalComplete, currentHubPortal.transform.position);
            CompleteActiveRunIfPersistent();
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(AppShellRoute.MainMenu);
                SceneLoaderService.LoadRouteAsync(AppShellRoute.MainMenu);
            }

            return true;
        }

        private void SpawnRewardIfNeeded()
        {
            if (State.CurrentRoom.RewardState != RoomRewardState.Pending || currentRewardPickup != null)
            {
                return;
            }

            var rewardObject = InstantiateOrCreate(rewardPickupPrefab, "RoomRewardPickup", PrimitiveType.Sphere, MaterialRole.RewardPickup);
            rewardObject.transform.SetParent(playerController.transform.parent, false);
            rewardObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            rewardObject.transform.localScale = Vector3.one * 0.35f;
            currentRewardPickup = rewardObject.GetComponent<RoomRewardPickup>() ?? rewardObject.AddComponent<RoomRewardPickup>();
            currentRewardPickup.Configure(State.CurrentRoomId.Value);
        }

        private void SpawnHubPortalIfReady()
        {
            if (currentHubPortal != null || State == null || !State.AreAllRoomsCleared() || !State.AreAllRewardsClaimed())
            {
                return;
            }

            var portalObject = InstantiateOrCreate(hubReturnPortalPrefab, "HubReturnPortal", PrimitiveType.Cylinder, MaterialRole.HubReturnPortal);
            portalObject.transform.SetParent(playerController.transform.parent, false);
            portalObject.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            portalObject.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            currentHubPortal = portalObject.GetComponent<HubReturnPortal>() ?? portalObject.AddComponent<HubReturnPortal>();
        }

        private void UpdateDoorVisuals()
        {
            if (State == null || roomRuntimeRoot == null)
            {
                return;
            }

            foreach (var port in roomRuntimeRoot.DoorPorts)
            {
                var hasConnection = State.Graph.TryGetConnection(State.CurrentRoomId, port.Direction, out _);
                var visualState = hasConnection
                    ? State.CurrentRoom.IsCleared ? RoomDoorVisualState.Cleared : RoomDoorVisualState.Locked
                    : RoomDoorVisualState.Unavailable;
                roomRuntimeRoot.SetDoorVisualState(port.Direction, visualState);
            }
        }

        private void DestroyTransientInteractables()
        {
            if (currentRewardPickup != null)
            {
                DestroyRuntimeObject(currentRewardPickup.gameObject);
                currentRewardPickup = null;
            }

            if (currentHubPortal != null)
            {
                DestroyRuntimeObject(currentHubPortal.gameObject);
                currentHubPortal = null;
            }
        }

        private void ResolveReferences()
        {
            roomRuntimeRoot = roomRuntimeRoot != null ? roomRuntimeRoot : GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true) ?? FindFirstObjectByType<RoomRuntimeRoot>();
            playerController = playerController != null ? playerController : GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true) ?? FindFirstObjectByType<PlaceholderPlayerController>();
            roomCombatController = roomCombatController != null ? roomCombatController : GetComponent<RoomCombatController>() ?? FindFirstObjectByType<RoomCombatController>();
        }

        private void ResolvePersistence()
        {
            canPersist = false;
            SaveStatus = "Transient";
            BankedSouls = 0;

            var profileHost = ProfileSessionHost.Instance;
            var selectedProfile = profileHost?.SelectedProfileContext?.SelectedProfile;
            if (selectedProfile != null)
            {
                BankedSouls = selectedProfile.BankedSouls;
            }

            if (gameSessionState == null || gameSessionState.ProfileSlotIndex < 0 || profileHost?.RunSaveStore == null)
            {
                return;
            }

            canPersist = TransientSessionGuard.CanPersist(gameSessionState.SessionMode, gameSessionState.HasProfile);
            if (!canPersist)
            {
                return;
            }

            activeProfileSlotId = new ProfileSlotId(gameSessionState.ProfileSlotIndex);
            runSaveStore = profileHost.RunSaveStore;
            SaveStatus = "Active";
        }

        private void RestoreBranchRooms(RunSaveSnapshot snapshot)
        {
            if (snapshot?.rooms == null || State == null)
            {
                return;
            }

            foreach (var roomSave in snapshot.rooms)
            {
                var roomId = new BranchRoomId(roomSave.roomId);
                if (!State.Graph.TryGetRoom(roomId, out var room))
                {
                    continue;
                }

                if (!Enum.TryParse(roomSave.rewardState, out RoomRewardState rewardState))
                {
                    rewardState = roomId == BranchRoomId.Origin ? RoomRewardState.Unavailable : RoomRewardState.None;
                }

                room.Restore(roomSave.isVisited, roomSave.isCleared, rewardState);
            }
        }

        private void RestorePlayerHealth(RunSaveSnapshot snapshot)
        {
            if (snapshot == null || roomCombatController?.PlayerHealth == null)
            {
                return;
            }

            roomCombatController.PlayerHealth.Restore(RoomCombatController.PlayerMaxHealth + playerRunStats.MaxHealthBonus, snapshot.playerCurrentHealth);
        }

        private void SubscribePlayerDeath()
        {
            if (roomCombatController?.PlayerHealth == null)
            {
                return;
            }

            roomCombatController.PlayerHealth.Died -= OnPlayerDied;
            roomCombatController.PlayerHealth.Died += OnPlayerDied;
        }

        private void OnPlayerDied(CombatantHealth _)
        {
            if (activeRunCompletedOrFailed)
            {
                return;
            }

            activeRunCompletedOrFailed = true;
            if (canPersist)
            {
                runSaveStore.ClearActiveRun(activeProfileSlotId);
                RefreshSelectedProfileSummary();
            }

            SaveStatus = "Run Lost";
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(AppShellRoute.MainMenu);
                SceneLoaderService.LoadRouteAsync(AppShellRoute.MainMenu);
            }
        }

        private void ApplyRunStatsToPlayer(int healAmount)
        {
            ItemEffectApplier.ApplyToPlayer(playerController != null ? playerController.gameObject : null, playerRunStats, healAmount);
        }

        private void CheckpointActiveRun()
        {
            if (suppressCheckpoint || !canPersist || activeRunCompletedOrFailed || runSaveStore == null)
            {
                return;
            }

            runSaveStore.SaveActiveRun(activeProfileSlotId, CreateSnapshot());
            SaveStatus = "Saved";
            RefreshSelectedProfileSummary();
        }

        public RunSaveSnapshot CreateSnapshot()
        {
            var snapshot = new RunSaveSnapshot
            {
                runId = $"m7-{gameSessionState?.ProfileId ?? "transient"}",
                branchId = "m7_five_room_cross",
                currentRoomId = State?.CurrentRoomId.Value ?? BranchRoomId.Origin.Value,
                platformKind = gameSessionState?.PlatformKind.ToString() ?? string.Empty,
                playerCurrentHealth = roomCombatController?.PlayerHealth != null ? roomCombatController.PlayerHealth.CurrentHealth : RoomCombatController.PlayerMaxHealth,
                economy = runEconomy.ToSaveState(),
                playerStats = playerRunStats.ToSaveState()
            };

            if (State?.Graph?.Rooms != null)
            {
                snapshot.rooms = State.Graph.Rooms.Select(room => new BranchRoomSaveState
                {
                    roomId = room.Id.Value,
                    coordinateX = room.Coordinate.x,
                    coordinateZ = room.Coordinate.y,
                    isVisited = room.IsVisited,
                    isCleared = room.IsCleared,
                    rewardState = room.RewardState.ToString()
                }).ToList();
            }

            return snapshot;
        }

        private void CompleteActiveRunIfPersistent()
        {
            if (activeRunCompletedOrFailed)
            {
                return;
            }

            activeRunCompletedOrFailed = true;
            if (canPersist)
            {
                MetaProgressionService.CompleteRun(runSaveStore, activeProfileSlotId, runEconomy);
                RefreshSelectedProfileSummary();
                BankedSouls = ProfileSessionHost.Instance?.SelectedProfileContext?.SelectedProfile?.BankedSouls ?? BankedSouls + runEconomy.RunSouls;
            }

            SaveStatus = "Completed";
        }

        private void RefreshSelectedProfileSummary()
        {
            var profileHost = ProfileSessionHost.Instance;
            if (profileHost?.SelectedProfileContext == null || profileHost.ProfileStore == null || gameSessionState == null)
            {
                return;
            }

            var updated = profileHost.ProfileStore.LoadSlotSummaries()
                .FirstOrDefault(summary => summary.SlotIndex == gameSessionState.ProfileSlotIndex);
            if (updated != null)
            {
                profileHost.SelectedProfileContext.UpdateSelectedProfile(updated);
                BankedSouls = updated.BankedSouls;
            }
        }

        private static GameObject InstantiateOrCreate(GameObject prefab, string objectName, PrimitiveType primitiveType, MaterialRole role)
        {
            var instance = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(primitiveType);
            instance.name = objectName;
            MaterialResolver.ApplyTo(instance, role);
            var collider = instance.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return instance;
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }

        private static void DestroyRuntimeObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
