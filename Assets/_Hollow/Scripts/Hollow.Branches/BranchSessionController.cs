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
        [SerializeField] private BranchRoomTemplateCatalogDefinition branchRoomTemplateCatalog;
        [SerializeField] private BranchGenerationSettingsDefinition branchGenerationSettings;
        [SerializeField] private int macroBranchSeed = BranchGenerator.DefaultMacroFixtureSeed;

        private ImportedRoomRuntimeAsset roomAsset;
        private ImportedRoomRuntimeAsset currentRoomAsset;
        private BranchSessionContent branchContent;
        private GameSessionState gameSessionState;
        private RoomRewardPickup currentRewardPickup;
        private HubReturnPortal currentHubPortal;
        private readonly RuntimeRewardCounter rewardCounter = new();
        private RunEconomy runEconomy = new();
        private PlayerRunStats playerRunStats = new();
        private ProceduralRewardPlan proceduralRewardPlan = ProceduralRewardPlan.Empty;
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

        public ImportedRoomRuntimeAsset CurrentRoomAsset => currentRoomAsset ?? roomAsset;

        public BranchRoomTemplateCatalogDefinition BranchRoomTemplateCatalog => branchRoomTemplateCatalog;

        public BranchGenerationSettingsDefinition BranchGenerationSettings => branchGenerationSettings;

        public int MacroBranchSeed => macroBranchSeed;

        public void Configure(GameObject nextRewardPickupPrefab, GameObject nextHubReturnPortalPrefab)
        {
            rewardPickupPrefab = nextRewardPickupPrefab;
            hubReturnPortalPrefab = nextHubReturnPortalPrefab;
        }

        public void ConfigureTemplateCatalog(BranchRoomTemplateCatalogDefinition nextCatalog, int nextSeed)
        {
            branchRoomTemplateCatalog = nextCatalog;
            macroBranchSeed = nextSeed == 0 ? BranchGenerator.DefaultMacroFixtureSeed : nextSeed;
        }

        public void ConfigureGenerationSettings(BranchGenerationSettingsDefinition nextSettings)
        {
            branchGenerationSettings = nextSettings;
            if (branchGenerationSettings != null)
            {
                macroBranchSeed = branchGenerationSettings.DefaultSeed == 0 ? BranchGenerator.DefaultSeededMacroSeed : branchGenerationSettings.DefaultSeed;
            }
        }

        public void Initialize(ImportedRoomRuntimeAsset nextRoomAsset, GameSessionState nextSessionState)
        {
            roomAsset = nextRoomAsset;
            gameSessionState = nextSessionState;
            ResolveReferences();
            ResolvePersistence();
            ResolveBranchContent();

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
            ResolveBranchContent();

            runEconomy = new RunEconomy();
            playerRunStats = new PlayerRunStats();
            rewardCounter.SetClaimedRewards(0);
            activeRunCompletedOrFailed = false;
            State = BranchSessionState.Create(CreateFreshGraph());
            proceduralRewardPlan = State.Graph.BranchId == BranchGenerator.SeededMacroBranchId
                ? ProceduralRewardResolver.CreatePlan(State.Graph)
                : ProceduralRewardPlan.Empty;
            LoadCurrentRoom();
            CheckpointActiveRun();
        }

        public void InitializeFromSnapshot(ImportedRoomRuntimeAsset nextRoomAsset, GameSessionState nextSessionState, RunSaveSnapshot snapshot)
        {
            roomAsset = nextRoomAsset;
            gameSessionState = nextSessionState;
            ResolveReferences();
            ResolvePersistence();
            ResolveBranchContent();

            runEconomy = RunEconomy.FromSaveState(snapshot?.economy);
            playerRunStats = PlayerRunStats.FromSaveState(snapshot?.playerStats);
            proceduralRewardPlan = ProceduralRewardPlan.FromSaveState(snapshot?.proceduralRewardPlan);
            rewardCounter.SetClaimedRewards(runEconomy.CollectedRewards.Count);
            activeRunCompletedOrFailed = false;
            State = BranchSessionState.Create(CreateGraphForSnapshot(snapshot));
            if (State.Graph.BranchId == BranchGenerator.SeededMacroBranchId && !proceduralRewardPlan.Rewards.Any())
            {
                proceduralRewardPlan = ProceduralRewardResolver.CreatePlan(State.Graph);
            }
            RestoreBranchRooms(snapshot);
            State.RestoreCurrentRoom(new BranchRoomId(snapshot?.currentRoomId ?? BranchRoomId.Origin.Value));
            suppressCheckpoint = true;
            LoadCurrentRoom();
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

            return TryTraverse(connection);
        }

        private bool TryTraverse(BranchConnection connection)
        {
            if (State == null || connection == null || !State.CurrentRoom.IsCleared)
            {
                return false;
            }

            State.EnterRoom(connection.ToRoomId);
            LoadCurrentRoom(entryConnection: connection);
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
                .Select(connection =>
                {
                    var hasPort = connection.HasExplicitPorts
                        ? roomRuntimeRoot.TryGetDoorPortById(connection.FromPortId, out var port)
                        : roomRuntimeRoot.TryGetDoorPort(connection.FromDirection, out port);
                    return new
                    {
                        Connection = connection,
                        HasPort = hasPort,
                        Distance = hasPort ? Vector3.Distance(Flat(playerPosition), Flat(port.Position)) : float.MaxValue
                    };
                })
                .Where(candidate => candidate.HasPort)
                .OrderBy(candidate => candidate.Distance)
                .FirstOrDefault();

            if (nearest == null || nearest.Distance > BranchTraversalService.DoorInteractionRadiusMeters)
            {
                return false;
            }

            return TryTraverse(nearest.Connection);
        }

        private void LoadCurrentRoom(Vector3? requestedPlayerLocalPosition = null, BranchConnection entryConnection = null)
        {
            DestroyTransientInteractables();
            currentRoomAsset = ResolveCurrentRoomAsset();
            roomRuntimeRoot.BuildFrom(currentRoomAsset);
            var playerLocalPosition = entryConnection != null
                ? BranchTraversalService.EntryPositionFor(roomRuntimeRoot, entryConnection)
                : requestedPlayerLocalPosition ?? currentRoomAsset.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            playerController.transform.localPosition = playerLocalPosition;
            State.CurrentRoom.MarkVisited();
            roomCombatController.BeginRoom(
                roomRuntimeRoot,
                playerController,
                State.CurrentRoom.IsCleared,
                State.CurrentRoom.Role == BranchRoomRole.Boss ? RoomCombatEncounterKind.Boss : RoomCombatEncounterKind.Standard);
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
            var grant = ProceduralRewardResolver.Resolve(State.CurrentRoomId.Value, proceduralRewardPlan);
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
                var hasConnection = State.Graph.TryGetConnectionByPort(State.CurrentRoomId, port.Id, out _);
                if (!hasConnection)
                {
                    hasConnection = State.Graph.ConnectionsFrom(State.CurrentRoomId).All(connection => !connection.HasExplicitPorts) &&
                                    State.Graph.TryGetConnection(State.CurrentRoomId, port.Direction, out _);
                }

                var visualState = hasConnection
                    ? State.CurrentRoom.IsCleared ? RoomDoorVisualState.Cleared : RoomDoorVisualState.Locked
                    : RoomDoorVisualState.Unavailable;
                roomRuntimeRoot.SetDoorVisualStateById(port.Id, visualState);
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

        private void ResolveBranchContent()
        {
            branchContent = BranchSessionContent.Create(roomAsset, branchRoomTemplateCatalog, macroBranchSeed, out var error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning($"Branch template catalog import warning: {error}");
            }
        }

        private BranchFloorGraph CreateFreshGraph()
        {
            if (branchContent != null && branchContent.HasMacroFixturePool)
            {
                if (branchGenerationSettings != null)
                {
                    try
                    {
                        return BranchGenerator.CreateSeededMacroBranch(branchContent, branchGenerationSettings, macroBranchSeed);
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning($"M15 seeded macro branch generation failed; falling back to M14 fixed macro branch. {error.Message}");
                    }
                }

                return BranchGenerator.CreateMacroFixtureBranch(branchContent.MacroRoomPool, branchContent.BranchSeed);
            }

            return BranchGenerator.CreateFiveRoomCross(roomAsset);
        }

        private BranchFloorGraph CreateGraphForSnapshot(RunSaveSnapshot snapshot)
        {
            if (snapshot != null &&
                snapshot.branchId == BranchGenerator.SeededMacroBranchId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                return BranchGenerator.CreateSeededMacroBranch(
                    branchContent,
                    branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                    snapshot.branchSeed == 0 ? branchContent.BranchSeed : snapshot.branchSeed);
            }

            if (snapshot != null &&
                snapshot.branchId == BranchGenerator.MacroFixtureBranchId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                return BranchGenerator.CreateMacroFixtureBranch(
                    branchContent.MacroRoomPool,
                    snapshot.branchSeed == 0 ? branchContent.BranchSeed : snapshot.branchSeed);
            }

            return BranchGenerator.CreateFiveRoomCross(roomAsset);
        }

        private ImportedRoomRuntimeAsset ResolveCurrentRoomAsset()
        {
            if (State?.CurrentRoom != null &&
                branchContent != null &&
                branchContent.TryGetRoomAsset(State.CurrentRoom.RuntimeRoomAssetId, out var asset))
            {
                return asset;
            }

            return roomAsset;
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
                runId = $"{State?.Graph?.BranchId ?? BranchGenerator.LegacyFiveRoomBranchId}-{gameSessionState?.ProfileId ?? "transient"}",
                branchId = State?.Graph?.BranchId ?? BranchGenerator.LegacyFiveRoomBranchId,
                branchSeed = State?.Graph?.Seed ?? 0,
                currentRoomId = State?.CurrentRoomId.Value ?? BranchRoomId.Origin.Value,
                platformKind = gameSessionState?.PlatformKind.ToString() ?? string.Empty,
                playerCurrentHealth = roomCombatController?.PlayerHealth != null ? roomCombatController.PlayerHealth.CurrentHealth : RoomCombatController.PlayerMaxHealth,
                proceduralRewardPlan = proceduralRewardPlan.ToSaveState(),
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
