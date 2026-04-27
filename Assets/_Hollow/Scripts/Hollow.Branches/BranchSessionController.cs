using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Core;
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
        private const float ShopCardInteractionRadiusMeters = 0.85f;
        private const int FinalWorldIndex = 3;

        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlaceholderPlayerController playerController;
        [SerializeField] private RoomCombatController roomCombatController;
        [SerializeField] private GameObject rewardPickupPrefab;
        [SerializeField] private GameObject hubReturnPortalPrefab;
        [SerializeField] private GameObject bossKeyPickupPrefab;
        [SerializeField] private GameObject hubShopPrefab;
        [SerializeField] private GameObject nextBranchPortalPrefab;
        [SerializeField] private BranchRoomTemplateCatalogDefinition branchRoomTemplateCatalog;
        [SerializeField] private BranchGenerationSettingsDefinition branchGenerationSettings;
        [SerializeField] private RewardPoolDefinition standardRewardPool;
        [SerializeField] private RewardPoolDefinition treasureRewardPool;
        [SerializeField] private RewardPoolDefinition bossRewardPool;
        [SerializeField] private RewardPoolDefinition weaponRewardPool;
        [SerializeField] private WeaponCatalogDefinition weaponCatalog;
        [SerializeField] private EncounterCatalogDefinition encounterCatalog;
        [SerializeField] private int macroBranchSeed = BranchGenerator.DefaultMacroFixtureSeed;

        private ImportedRoomRuntimeAsset roomAsset;
        private ImportedRoomRuntimeAsset currentRoomAsset;
        private BranchSessionContent branchContent;
        private GameSessionState gameSessionState;
        private RoomRewardPickup currentRewardPickup;
        private HubReturnPortal currentHubPortal;
        private BossKeyPickup currentBossKeyPickup;
        private HubShopController currentHubShop;
        private readonly List<NextBranchPortal> currentNextBranchPortals = new();
        private readonly RuntimeRewardCounter rewardCounter = new();
        private RunEconomy runEconomy = new();
        private PlayerRunStats playerRunStats = new();
        private PlayerRunBuild playerRunBuild = new();
        private ProceduralRewardPlan proceduralRewardPlan = ProceduralRewardPlan.Empty;
        private EncounterPlan encounterPlan = EncounterPlan.Empty;
        private BranchFeaturePlan branchFeaturePlan = BranchFeaturePlan.Empty;
        private InterBranchHubState interBranchHubState = InterBranchHubState.Inactive;
        private BossKeyState bossKeyState = BossKeyState.None;
        private bool bossDoorUnlocked;
        private int branchDepth;
        private int currentBranchSeed;
        private int runSeed;
        private int worldIndex = 1;
        private RunWorldPhase worldPhase = RunWorldPhase.Legacy;
        private string activeHubPortalId = string.Empty;
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

        public string LastRewardMessage { get; private set; } = "None";

        public GameObject RewardPickupPrefab => rewardPickupPrefab;

        public GameObject HubReturnPortalPrefab => hubReturnPortalPrefab;

        public GameObject BossKeyPickupPrefab => bossKeyPickupPrefab;

        public GameObject HubShopPrefab => hubShopPrefab;

        public GameObject NextBranchPortalPrefab => nextBranchPortalPrefab;

        public RoomRewardPickup CurrentRewardPickup => currentRewardPickup;

        public HubReturnPortal CurrentHubPortal => currentHubPortal;

        public bool HubReturnRequested { get; private set; }

        public BossKeyPickup CurrentBossKeyPickup => currentBossKeyPickup;

        public HubShopController CurrentHubShop => currentHubShop;

        public IReadOnlyList<NextBranchPortal> CurrentNextBranchPortals => currentNextBranchPortals;

        public BossKeyState BossKeyState => bossKeyState;

        public bool BossDoorUnlocked => bossDoorUnlocked;

        public int BranchDepth => branchDepth;

        public int CurrentBranchSeed => currentBranchSeed;

        public int RunSeed => runSeed == 0 ? currentBranchSeed : runSeed;

        public int WorldIndex => worldIndex <= 0 ? 1 : worldIndex;

        public RunWorldPhase WorldPhase => worldPhase;

        public string ActiveHubPortalId => activeHubPortalId;

        public BranchFeaturePlan BranchFeaturePlan => branchFeaturePlan;

        public InterBranchHubState InterBranchHubState => interBranchHubState;

        public bool IsInInterBranchHub => interBranchHubState.IsActive && (worldPhase == RunWorldPhase.Hub || worldPhase == RunWorldPhase.Legacy);

        public string PlayerBuildHudSummary
        {
            get
            {
                var weapon = playerController != null ? playerController.GetComponent<PlayerWeaponController>() : null;
                if (weapon == null)
                {
                    return $"Character: {playerRunBuild.SelectedCharacterId}\nWeapon: Ranged\nStamina: --";
                }

                return $"Character: {playerRunBuild.SelectedCharacterId}\nWeapon: {weapon.ActiveWeaponSlot} - {weapon.ActiveWeaponDisplayName}\nStamina: {weapon.CurrentStamina:0}/{weapon.MaxStamina:0}";
            }
        }

        public ImportedRoomRuntimeAsset RoomAsset => roomAsset;

        public ImportedRoomRuntimeAsset CurrentRoomAsset => currentRoomAsset ?? roomAsset;

        public BranchRoomTemplateCatalogDefinition BranchRoomTemplateCatalog => branchRoomTemplateCatalog;

        public BranchGenerationSettingsDefinition BranchGenerationSettings => branchGenerationSettings;

        public RewardPoolDefinition StandardRewardPool => standardRewardPool;

        public RewardPoolDefinition TreasureRewardPool => treasureRewardPool;

        public RewardPoolDefinition BossRewardPool => bossRewardPool;

        public RewardPoolDefinition WeaponRewardPool => weaponRewardPool;

        public EncounterCatalogDefinition EncounterCatalog => encounterCatalog;

        public WeaponCatalogDefinition WeaponCatalog => weaponCatalog;

        public int MacroBranchSeed => macroBranchSeed;

        public void Configure(GameObject nextRewardPickupPrefab, GameObject nextHubReturnPortalPrefab)
        {
            rewardPickupPrefab = nextRewardPickupPrefab;
            hubReturnPortalPrefab = nextHubReturnPortalPrefab;
        }

        public void ConfigureBranchFeaturePrefabs(GameObject nextBossKeyPickupPrefab, GameObject nextHubShopPrefab, GameObject nextNextBranchPortalPrefab)
        {
            bossKeyPickupPrefab = nextBossKeyPickupPrefab;
            hubShopPrefab = nextHubShopPrefab;
            nextBranchPortalPrefab = nextNextBranchPortalPrefab;
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

        public void ConfigureRewardPools(RewardPoolDefinition standardPool, RewardPoolDefinition treasurePool, RewardPoolDefinition bossPool)
        {
            standardRewardPool = standardPool;
            treasureRewardPool = treasurePool;
            bossRewardPool = bossPool;
        }

        public void ConfigureWeaponRewardPool(RewardPoolDefinition nextWeaponRewardPool)
        {
            weaponRewardPool = nextWeaponRewardPool;
        }

        public void ConfigureWeaponCatalog(WeaponCatalogDefinition nextWeaponCatalog)
        {
            weaponCatalog = nextWeaponCatalog;
        }

        public void ConfigureEncounterCatalog(EncounterCatalogDefinition nextEncounterCatalog)
        {
            encounterCatalog = nextEncounterCatalog;
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
            playerRunBuild = new PlayerRunBuild();
            rewardCounter.SetClaimedRewards(0);
            activeRunCompletedOrFailed = false;
            branchDepth = 0;
            runSeed = ShouldUseRandomFreshRunSeed() ? RunSeedProvider.CreateSeed() : macroBranchSeed;
            worldIndex = 1;
            worldPhase = IsWorldLoopRuntime() ? RunWorldPhase.Prologue : RunWorldPhase.Legacy;
            activeHubPortalId = string.Empty;
            currentBranchSeed = worldPhase == RunWorldPhase.Prologue
                ? RunSeedDeriver.PrologueBranchSeed(runSeed, worldIndex)
                : runSeed;
            bossKeyState = BossKeyState.None;
            bossDoorUnlocked = false;
            interBranchHubState = InterBranchHubState.Inactive;
            State = BranchSessionState.Create(CreateFreshGraph());
            branchFeaturePlan = BranchFeaturePlan.Create(State.Graph);
            proceduralRewardPlan = CreateRewardPlanForGraph(State.Graph, legacyFallback: false);
            encounterPlan = CreateEncounterPlanForGraph(State.Graph);
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
            playerRunBuild = PlayerRunBuild.FromSaveState(snapshot?.runBuild);
            proceduralRewardPlan = ProceduralRewardPlan.FromSaveState(snapshot?.proceduralRewardPlan);
            encounterPlan = EncounterPlan.FromSaveState(snapshot?.encounterPlan);
            rewardCounter.SetClaimedRewards(runEconomy.CollectedRewards.Count);
            activeRunCompletedOrFailed = false;
            branchDepth = Math.Max(0, snapshot?.branchDepth ?? 0);
            runSeed = snapshot != null && snapshot.runSeed != 0
                ? snapshot.runSeed
                : snapshot != null && snapshot.currentBranchSeed != 0
                    ? snapshot.currentBranchSeed
                    : snapshot != null && snapshot.branchSeed != 0 ? snapshot.branchSeed : macroBranchSeed;
            worldIndex = Math.Max(1, snapshot?.worldIndex ?? 1);
            worldPhase = Enum.TryParse(snapshot?.worldPhase, out RunWorldPhase parsedWorldPhase)
                ? parsedWorldPhase
                : RunWorldPhase.Legacy;
            activeHubPortalId = snapshot?.activeHubPortalId ?? string.Empty;
            currentBranchSeed = snapshot != null && snapshot.currentBranchSeed != 0
                ? snapshot.currentBranchSeed
                : worldPhase == RunWorldPhase.Prologue
                    ? RunSeedDeriver.PrologueBranchSeed(runSeed, worldIndex)
                    : snapshot != null && snapshot.branchSeed != 0 ? snapshot.branchSeed : runSeed;
            bossKeyState = Enum.TryParse(snapshot?.bossKeyState, out BossKeyState parsedBossKeyState) ? parsedBossKeyState : BossKeyState.None;
            bossDoorUnlocked = snapshot?.bossDoorUnlocked ?? false;
            State = BranchSessionState.Create(CreateGraphForSnapshot(snapshot));
            branchFeaturePlan = BranchFeaturePlan.Create(State.Graph);
            interBranchHubState = InterBranchHubState.FromSaveState(snapshot?.interBranchHub, currentBranchSeed, branchDepth, standardRewardPool, weaponRewardPool);
            if (IsProceduralRewardBranch(State.Graph.BranchId) && !proceduralRewardPlan.Rewards.Any())
            {
                proceduralRewardPlan = CreateRewardPlanForGraph(
                    State.Graph,
                    legacyFallback: snapshot?.branchId == BranchGenerator.SeededMacroBranchId);
            }
            if ((State.Graph.BranchId == BranchGenerator.EnemyEncounterBranchId || State.Graph.BranchId == BranchGenerator.BranchFeaturesId) && !encounterPlan.Assignments.Any())
            {
                encounterPlan = CreateEncounterPlanForGraph(State.Graph);
            }
            RestoreBranchRooms(snapshot);
            State.RestoreCurrentRoom(new BranchRoomId(snapshot?.currentRoomId ?? BranchRoomId.Origin.Value));
            suppressCheckpoint = true;
            if (interBranchHubState.IsActive && (worldPhase == RunWorldPhase.Hub || worldPhase == RunWorldPhase.Legacy))
            {
                EnterInterBranchHub(restoreExistingState: true);
            }
            else
            {
                LoadCurrentRoom();
            }
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
            return TryClaimBossKey() ||
                   TryClaimReward() ||
                   TryHandleNearestShopCard() ||
                   TryUseNextBranchPortal() ||
                   TryUseHubReturnPortal() ||
                   TryTraverseNearestDoor();
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

            if (!TryResolveConnectionLock(connection))
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
            if (State.CurrentRoom.Role is BranchRoomRole.Treasure or BranchRoomRole.Secret && !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardPending();
            }

            roomCombatController.BeginRoom(
                roomRuntimeRoot,
                playerController,
                State.CurrentRoom.IsCleared,
                State.CurrentRoom.Role == BranchRoomRole.Boss ? RoomCombatEncounterKind.Boss : RoomCombatEncounterKind.Standard,
                CreateEncounterContextForCurrentRoom());
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

        private bool TryClaimBossKey()
        {
            if (currentBossKeyPickup == null || playerController == null || State == null)
            {
                return false;
            }

            if (Vector3.Distance(Flat(playerController.transform.localPosition), Flat(currentBossKeyPickup.transform.localPosition)) > RewardInteractionRadiusMeters)
            {
                return false;
            }

            if (!currentBossKeyPickup.Claim())
            {
                return false;
            }

            bossKeyState = BossKeyState.Held;
            State.CurrentRoom.MarkRewardClaimed();
            LastRewardMessage = "Boss Key acquired";
            DestroyRuntimeObject(currentBossKeyPickup.gameObject);
            currentBossKeyPickup = null;
            VfxPresenter.Play(VfxCueId.RewardClaim, playerController.transform.position, playerController.transform.parent);
            AudioPresenter.Play(AudioCueId.RewardClaim, playerController.transform.position);
            UpdateDoorVisuals();
            CheckpointActiveRun();
            return true;
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
                ApplyEquipmentReward(grant);
                ApplyRunStatsToPlayer(healAmount);
                rewardCounter.SetClaimedRewards(runEconomy.CollectedRewards.Count);
                LastRewardMessage = RewardMessage(grant);
            }

            DestroyRuntimeObject(currentRewardPickup.gameObject);
            currentRewardPickup = null;
            VfxPresenter.Play(VfxCueId.RewardClaim, playerController.transform.position, playerController.transform.parent);
            AudioPresenter.Play(AudioCueId.RewardClaim, playerController.transform.position);
            SpawnHubPortalIfReady();
            CheckpointActiveRun();
            return true;
        }

        public bool TryBuyShopOffer(string offerId)
        {
            if (!IsInInterBranchHub || string.IsNullOrWhiteSpace(offerId))
            {
                return false;
            }

            var offer = interBranchHubState.ShopOffers.FirstOrDefault(candidate => candidate.OfferId == offerId);
            if (offer == null)
            {
                return false;
            }

            if (offer.IsPurchased)
            {
                LastRewardMessage = $"{offer.DisplayName} is sold out";
                currentHubShop?.RefreshCards(runEconomy.RunSouls);
                return false;
            }

            if (runEconomy.RunSouls < offer.Price)
            {
                LastRewardMessage = $"Need {offer.Price - runEconomy.RunSouls} more souls";
                currentHubShop?.RefreshCards(runEconomy.RunSouls);
                return false;
            }

            if (!offer.TryPurchase(runEconomy, out var grant, out var healAmount))
            {
                return false;
            }

            if (!grant.IsEmpty && runEconomy.ApplyReward(grant))
            {
                healAmount += playerRunStats.ApplyReward(grant);
                ApplyEquipmentReward(grant);
                rewardCounter.SetClaimedRewards(runEconomy.CollectedRewards.Count);
                LastRewardMessage = $"Purchased: {grant.DisplayName}";
            }
            else if (healAmount > 0)
            {
                LastRewardMessage = "Purchased: Heal 2 HP";
            }

            ApplyRunStatsToPlayer(healAmount);
            currentHubShop?.Configure(interBranchHubState);
            currentHubShop?.RefreshCards(runEconomy.RunSouls);
            CheckpointActiveRun();
            return true;
        }

        public bool TryBuyNearestShopCard()
        {
            return TryFindNearestShopCard(out var card) && TryBuyShopOffer(card.OfferId);
        }

        private bool TryHandleNearestShopCard()
        {
            if (!TryFindNearestShopCard(out var card))
            {
                return false;
            }

            TryBuyShopOffer(card.OfferId);
            return true;
        }

        private bool TryFindNearestShopCard(out HubShopCard card)
        {
            card = null;
            if (currentHubShop == null || playerController == null || !IsInInterBranchHub)
            {
                return false;
            }

            return currentHubShop.TryGetNearestCard(
                playerController.transform.localPosition,
                playerController.transform.parent,
                ShopCardInteractionRadiusMeters,
                out card);
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
            if (IsM20Branch())
            {
                if (worldPhase == RunWorldPhase.Branch && !string.IsNullOrWhiteSpace(activeHubPortalId))
                {
                    interBranchHubState = interBranchHubState.IsActive
                        ? interBranchHubState.MarkBranchPortalDefeated(activeHubPortalId, standardRewardPool)
                        : InterBranchHubState.CreateWorldHub(runSeed, worldIndex, 0, standardRewardPool, weaponRewardPool)
                            .MarkBranchPortalDefeated(activeHubPortalId, standardRewardPool, weaponRewardPool);
                    activeHubPortalId = string.Empty;
                }

                EnterInterBranchHub();
                return true;
            }

            CompleteActiveRunIfPersistent();
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(AppShellRoute.MainMenu);
                SceneLoaderService.LoadRouteAsync(AppShellRoute.MainMenu);
            }

            return true;
        }

        private bool TryUseNextBranchPortal()
        {
            if (playerController == null || !IsInInterBranchHub)
            {
                return false;
            }

            var portal = currentNextBranchPortals
                .Where(candidate => candidate != null && candidate.Choice != null && candidate.IsInteractable)
                .OrderBy(candidate => Vector3.Distance(Flat(playerController.transform.localPosition), Flat(candidate.transform.localPosition)))
                .FirstOrDefault();
            if (portal == null || Vector3.Distance(Flat(playerController.transform.localPosition), Flat(portal.transform.localPosition)) > PortalInteractionRadiusMeters)
            {
                return false;
            }

            StartNextBranch(portal.Choice);
            return true;
        }

        public void EnterInterBranchHub()
        {
            EnterInterBranchHub(restoreExistingState: false);
        }

        private void EnterInterBranchHub(bool restoreExistingState)
        {
            if (!interBranchHubState.IsActive || (!restoreExistingState && !IsWorldLoopRuntime()))
            {
                interBranchHubState = IsWorldLoopRuntime()
                    ? InterBranchHubState.CreateWorldHub(runSeed == 0 ? currentBranchSeed : runSeed, worldIndex, interBranchHubState.ShopRefreshIndex, standardRewardPool, weaponRewardPool)
                    : InterBranchHubState.Create(currentBranchSeed == 0 ? State?.Graph?.Seed ?? macroBranchSeed : currentBranchSeed, branchDepth, standardRewardPool, weaponRewardPool);
            }

            worldPhase = IsWorldLoopRuntime() ? RunWorldPhase.Hub : worldPhase;
            DestroyTransientInteractables();
            roomRuntimeRoot?.ClearRuntime();
            if (playerController != null)
            {
                playerController.transform.localPosition = Vector3.zero;
            }

            SpawnHubShopAndPortals();
            SaveStatus = "Inter-Branch Hub";
            CheckpointActiveRun();
        }

        public void StartNextBranch(NextBranchChoice choice)
        {
            if (choice == null)
            {
                return;
            }

            if (choice.Kind == HubPortalKind.FinalExtraction)
            {
                worldPhase = RunWorldPhase.Completed;
                CompleteActiveRunIfPersistent();
                if (HollowBootstrap.Instance != null)
                {
                    HollowBootstrap.Instance.AppStateMachine.TransitionTo(AppShellRoute.MainMenu);
                    SceneLoaderService.LoadRouteAsync(AppShellRoute.MainMenu);
                }

                return;
            }

            var preservedHealth = roomCombatController?.PlayerHealth != null
                ? roomCombatController.PlayerHealth.CurrentHealth
                : CreateCurrentRunBuild().DerivedStats.MaxHealth;
            branchDepth++;
            if (choice.Kind == HubPortalKind.NextWorld)
            {
                worldIndex = Math.Min(FinalWorldIndex, choice.WorldIndex);
                worldPhase = RunWorldPhase.Prologue;
                activeHubPortalId = string.Empty;
                interBranchHubState = InterBranchHubState.Inactive;
            }
            else
            {
                worldIndex = Math.Max(1, choice.WorldIndex);
                worldPhase = IsWorldLoopRuntime() ? RunWorldPhase.Branch : worldPhase;
                activeHubPortalId = choice.ChoiceId;
            }

            currentBranchSeed = choice.Seed;
            bossKeyState = BossKeyState.None;
            bossDoorUnlocked = false;
            if (choice.Kind == HubPortalKind.NextWorld)
            {
                interBranchHubState = InterBranchHubState.Inactive;
            }
            HubReturnRequested = false;
            State = BranchSessionState.Create(CreateM20Graph(currentBranchSeed));
            branchFeaturePlan = BranchFeaturePlan.Create(State.Graph);
            proceduralRewardPlan = CreateRewardPlanForGraph(State.Graph, legacyFallback: false);
            encounterPlan = CreateEncounterPlanForGraph(State.Graph);
            LoadCurrentRoom();
            if (roomCombatController?.PlayerHealth != null)
            {
                roomCombatController.PlayerHealth.Restore(CreateCurrentRunBuild().DerivedStats.MaxHealth, preservedHealth);
            }

            ApplyRunStatsToPlayer(healAmount: 0);
            CheckpointActiveRun();
        }

        private void SpawnRewardIfNeeded()
        {
            if (State.CurrentRoom.RewardState != RoomRewardState.Pending || currentRewardPickup != null)
            {
                return;
            }

            if (IsM20Branch() && branchFeaturePlan.IsBossKeyRoom(State.CurrentRoomId))
            {
                SpawnBossKeyIfNeeded();
                return;
            }

            var rewardObject = InstantiateOrCreate(rewardPickupPrefab, "RoomRewardPickup", PrimitiveType.Sphere, MaterialRole.RewardPickup);
            rewardObject.transform.SetParent(playerController.transform.parent, false);
            rewardObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            rewardObject.transform.localScale = Vector3.one * 0.35f;
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.RewardPickup, rewardObject.transform, Vector3.zero, Vector3.one);
            currentRewardPickup = rewardObject.GetComponent<RoomRewardPickup>() ?? rewardObject.AddComponent<RoomRewardPickup>();
            currentRewardPickup.Configure(State.CurrentRoomId.Value);
        }

        private void SpawnBossKeyIfNeeded()
        {
            if (currentBossKeyPickup != null || State.CurrentRoom.RewardState != RoomRewardState.Pending)
            {
                return;
            }

            var keyObject = InstantiateOrCreate(bossKeyPickupPrefab, "BossKeyPickup", PrimitiveType.Cube, MaterialRole.BossKeyPickup);
            keyObject.transform.SetParent(playerController.transform.parent, false);
            keyObject.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            keyObject.transform.localScale = Vector3.one * 0.32f;
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.BossKeyPickup, keyObject.transform, Vector3.zero, Vector3.one);
            currentBossKeyPickup = keyObject.GetComponent<BossKeyPickup>() ?? keyObject.AddComponent<BossKeyPickup>();
            currentBossKeyPickup.Configure(State.CurrentRoomId.Value);
        }

        private void SpawnHubPortalIfReady()
        {
            if (currentHubPortal != null || State == null)
            {
                return;
            }

            if (IsM20Branch())
            {
                if (State.CurrentRoom.Role != BranchRoomRole.Boss ||
                    !State.CurrentRoom.IsCleared ||
                    State.CurrentRoom.RewardState != RoomRewardState.Claimed)
                {
                    return;
                }
            }
            else if (!State.AreAllRoomsCleared() || !State.AreAllRewardsClaimed())
            {
                return;
            }

            var portalObject = InstantiateOrCreate(hubReturnPortalPrefab, "HubReturnPortal", PrimitiveType.Cylinder, MaterialRole.HubReturnPortal);
            portalObject.transform.SetParent(playerController.transform.parent, false);
            portalObject.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            portalObject.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.HubReturnPortal, portalObject.transform, Vector3.zero, Vector3.one);
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
                var hasConnection = State.Graph.TryGetConnectionByPort(State.CurrentRoomId, port.Id, out var connectedConnection);
                if (!hasConnection)
                {
                    hasConnection = State.Graph.ConnectionsFrom(State.CurrentRoomId).All(connection => !connection.HasExplicitPorts) &&
                                    State.Graph.TryGetConnection(State.CurrentRoomId, port.Direction, out connectedConnection);
                }

                var visualState = RoomDoorVisualState.Unavailable;
                if (hasConnection)
                {
                    var connectedRoom = State.Graph.TryGetRoom(connectedConnection.ToRoomId, out var room) ? room : null;
                    if (connectedConnection.LockKind == BranchConnectionLockKind.BossKey && !bossDoorUnlocked)
                    {
                        visualState = bossKeyState == BossKeyState.Held && State.CurrentRoom.IsCleared
                            ? RoomDoorVisualState.Active
                            : RoomDoorVisualState.Locked;
                    }
                    else if (connectedRoom?.Role == BranchRoomRole.Secret)
                    {
                        visualState = State.CurrentRoom.IsCleared ? RoomDoorVisualState.Active : RoomDoorVisualState.Locked;
                    }
                    else
                    {
                        visualState = State.CurrentRoom.IsCleared ? RoomDoorVisualState.Cleared : RoomDoorVisualState.Locked;
                    }
                }

                roomRuntimeRoot.SetDoorVisualStateById(port.Id, visualState);
            }
        }

        private bool TryResolveConnectionLock(BranchConnection connection)
        {
            if (connection.LockKind != BranchConnectionLockKind.BossKey || bossDoorUnlocked)
            {
                return true;
            }

            if (bossKeyState != BossKeyState.Held)
            {
                LastRewardMessage = "Boss door requires a boss key";
                return false;
            }

            bossKeyState = BossKeyState.Consumed;
            bossDoorUnlocked = true;
            LastRewardMessage = "Boss door unlocked";
            UpdateDoorVisuals();
            CheckpointActiveRun();
            return true;
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

            if (currentBossKeyPickup != null)
            {
                DestroyRuntimeObject(currentBossKeyPickup.gameObject);
                currentBossKeyPickup = null;
            }

            if (currentHubShop != null)
            {
                DestroyRuntimeObject(currentHubShop.gameObject);
                currentHubShop = null;
            }

            foreach (var portal in currentNextBranchPortals)
            {
                if (portal != null)
                {
                    DestroyRuntimeObject(portal.gameObject);
                }
            }

            currentNextBranchPortals.Clear();
        }

        private void SpawnHubShopAndPortals()
        {
            if (playerController == null)
            {
                return;
            }

            var parent = playerController.transform.parent;
            var shopObject = InstantiateOrCreate(hubShopPrefab, "HubShop", PrimitiveType.Cube, MaterialRole.HubShop);
            shopObject.transform.SetParent(parent, false);
            shopObject.transform.localPosition = new Vector3(-1.5f, 0.4f, 0.9f);
            shopObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.HubShop, shopObject.transform, Vector3.zero, Vector3.one);
            currentHubShop = shopObject.GetComponent<HubShopController>() ?? shopObject.AddComponent<HubShopController>();
            currentHubShop.Configure(interBranchHubState);
            currentHubShop.BuildCards(runEconomy.RunSouls);

            for (var index = 0; index < interBranchHubState.NextBranchChoices.Count; index++)
            {
                var choice = interBranchHubState.NextBranchChoices[index];
                var portalObject = InstantiateOrCreate(nextBranchPortalPrefab, $"NextBranchPortal_{index}", PrimitiveType.Cylinder, MaterialRole.NextBranchPortal);
                portalObject.transform.SetParent(parent, false);
                portalObject.transform.localPosition = choice.Kind == HubPortalKind.Branch
                    ? new Vector3(-0.8f + choice.SlotIndex * 0.8f, 0.18f, -1.1f)
                    : new Vector3(1.8f, 0.2f, -0.9f);
                portalObject.transform.localScale = choice.Kind == HubPortalKind.Branch && choice.State == HubBranchPortalState.Defeated
                    ? new Vector3(0.36f, 0.035f, 0.36f)
                    : choice.Kind == HubPortalKind.Branch ? new Vector3(0.42f, 0.08f, 0.42f) : new Vector3(0.55f, 0.1f, 0.55f);
                PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.NextBranchPortal, portalObject.transform, Vector3.zero, Vector3.one);
                var portal = portalObject.GetComponent<NextBranchPortal>() ?? portalObject.AddComponent<NextBranchPortal>();
                portal.Configure(choice);
                currentNextBranchPortals.Add(portal);
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
            var seed = currentBranchSeed == 0 ? macroBranchSeed : currentBranchSeed;
            if (branchContent != null && branchContent.HasMacroFixturePool)
            {
                if (branchGenerationSettings != null)
                {
                    try
                    {
                        if (encounterCatalog != null)
                        {
                            return CreateM20Graph(seed);
                        }

                        return branchGenerationSettings.EnableTreasureLeaf
                            ? BranchGenerator.CreateSeededFeatureBranch(branchContent, branchGenerationSettings, seed)
                            : BranchGenerator.CreateSeededMacroBranch(branchContent, branchGenerationSettings, seed);
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning($"Seeded macro branch generation failed; falling back to M14 fixed macro branch. {error.Message}");
                    }
                }

                return BranchGenerator.CreateMacroFixtureBranch(branchContent.MacroRoomPool, seed == 0 ? branchContent.BranchSeed : seed);
            }

            return BranchGenerator.CreateFiveRoomCross(roomAsset);
        }

        private bool ShouldUseRandomFreshRunSeed()
        {
            return gameSessionState != null &&
                   gameSessionState.SessionMode == RuntimeSessionMode.ProfileBacked &&
                   gameSessionState.LaunchMode == RunLaunchMode.NewRun &&
                   gameSessionState.HasProfile;
        }

        private bool IsWorldLoopRuntime()
        {
            return branchContent != null &&
                   branchContent.HasMacroFixturePool &&
                   branchGenerationSettings != null &&
                   encounterCatalog != null;
        }

        private BranchFloorGraph CreateGraphForSnapshot(RunSaveSnapshot snapshot)
        {
            if (snapshot != null &&
                snapshot.branchId == BranchGenerator.BranchFeaturesId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                return CreateM20Graph(currentBranchSeed == 0 ? snapshot.branchSeed : currentBranchSeed);
            }

            if (snapshot != null &&
                snapshot.branchId == BranchGenerator.EnemyEncounterBranchId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                return BranchGenerator.CreateSeededEncounterBranch(
                    branchContent,
                    branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                    snapshot.branchSeed == 0 ? branchContent.BranchSeed : snapshot.branchSeed);
            }

            if (snapshot != null &&
                snapshot.branchId == BranchGenerator.FeatureBranchId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                return BranchGenerator.CreateSeededFeatureBranch(
                    branchContent,
                    branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                    snapshot.branchSeed == 0 ? branchContent.BranchSeed : snapshot.branchSeed);
            }

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

        private BranchFloorGraph CreateM20Graph(int seed)
        {
            return BranchGenerator.CreateSeededBranchFeatures(
                branchContent,
                branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                seed == 0 ? macroBranchSeed : seed);
        }

        private static bool IsProceduralRewardBranch(string branchId)
        {
            return branchId == BranchGenerator.SeededMacroBranchId ||
                   branchId == BranchGenerator.FeatureBranchId ||
                   branchId == BranchGenerator.EnemyEncounterBranchId ||
                   branchId == BranchGenerator.BranchFeaturesId;
        }

        private ProceduralRewardPlan CreateRewardPlanForGraph(BranchFloorGraph graph, bool legacyFallback)
        {
            if (graph == null || !IsProceduralRewardBranch(graph.BranchId))
            {
                return ProceduralRewardPlan.Empty;
            }

            return legacyFallback
                ? ProceduralRewardResolver.CreatePlan(graph)
                : ProceduralRewardResolver.CreateSeededPlan(graph, standardRewardPool, treasureRewardPool, bossRewardPool, weaponRewardPool);
        }

        private static string RewardMessage(RewardGrant grant)
        {
            if (grant.IsEmpty)
            {
                return "No reward";
            }

            if (grant.Souls > 0 && grant.Coins > 0)
            {
                return $"Received: {grant.DisplayName} (+{grant.Souls} souls, +{grant.Coins} coins)";
            }

            if (grant.Coins > 0)
            {
                return $"Received: {grant.DisplayName} (+{grant.Coins} coins)";
            }

            return grant.Souls > 0
                ? $"Received: {grant.DisplayName} (+{grant.Souls} souls)"
                : $"Received: {grant.DisplayName}";
        }

        private EncounterPlan CreateEncounterPlanForGraph(BranchFloorGraph graph)
        {
            return graph != null && (graph.BranchId == BranchGenerator.EnemyEncounterBranchId || graph.BranchId == BranchGenerator.BranchFeaturesId)
                ? EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed)
                : EncounterPlan.Empty;
        }

        private bool IsM20Branch()
        {
            return State?.Graph?.BranchId == BranchGenerator.BranchFeaturesId;
        }

        private RoomCombatEncounterContext CreateEncounterContextForCurrentRoom()
        {
            if (State?.CurrentRoomId == null ||
                !encounterPlan.TryResolve(State.CurrentRoomId.Value, out var assignment))
            {
                return RoomCombatEncounterContext.Empty;
            }

            return new RoomCombatEncounterContext(assignment.EncounterId, assignment.EnemySpawnKinds);
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

            roomCombatController.PlayerHealth.Restore(CreateCurrentRunBuild().DerivedStats.MaxHealth, snapshot.playerCurrentHealth);
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
            playerRunBuild = CreateCurrentRunBuild();
            PlayerBuildApplier.Apply(playerRunBuild, playerController != null ? playerController.gameObject : null, weaponCatalog, healAmount);
        }

        private void ApplyEquipmentReward(RewardGrant grant)
        {
            if (grant.RewardKind != RewardKind.Weapon || string.IsNullOrWhiteSpace(grant.RewardId))
            {
                return;
            }

            playerRunBuild ??= CreateCurrentRunBuild();
            if (weaponCatalog != null && weaponCatalog.TryGetWeapon(grant.RewardId, out var weapon))
            {
                if (weapon.Slot == WeaponSlot.Melee)
                {
                    playerRunBuild.Equipment.EquipMeleeWeapon(weapon.WeaponId);
                    playerRunBuild.Equipment.SetActiveWeaponSlot(WeaponSlot.Melee);
                }
                else
                {
                    playerRunBuild.Equipment.EquipRangedWeapon(weapon.WeaponId);
                    playerRunBuild.Equipment.SetActiveWeaponSlot(WeaponSlot.Ranged);
                }
            }
            else if (grant.RewardId.Contains("blade") || grant.RewardId.Contains("cleaver") || grant.RewardId.Contains("sword"))
            {
                playerRunBuild.Equipment.EquipMeleeWeapon(grant.RewardId);
                playerRunBuild.Equipment.SetActiveWeaponSlot(WeaponSlot.Melee);
            }
            else
            {
                playerRunBuild.Equipment.EquipRangedWeapon(grant.RewardId);
                playerRunBuild.Equipment.SetActiveWeaponSlot(WeaponSlot.Ranged);
            }
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
                branchDepth = branchDepth,
                currentBranchSeed = currentBranchSeed == 0 ? State?.Graph?.Seed ?? 0 : currentBranchSeed,
                runSeed = runSeed == 0 ? currentBranchSeed : runSeed,
                worldIndex = worldIndex <= 0 ? 1 : worldIndex,
                worldPhase = worldPhase.ToString(),
                activeHubPortalId = activeHubPortalId ?? string.Empty,
                hubShopRefreshIndex = interBranchHubState.ShopRefreshIndex,
                bossKeyState = bossKeyState.ToString(),
                bossKeyRoomId = branchFeaturePlan.BossKeyRoomId,
                secretRoomId = branchFeaturePlan.SecretRoomId,
                bossDoorUnlocked = bossDoorUnlocked,
                proceduralRewardPlan = proceduralRewardPlan.ToSaveState(),
                encounterPlan = encounterPlan.ToSaveState(),
                interBranchHub = interBranchHubState.ToSaveState(),
                economy = runEconomy.ToSaveState(),
                playerStats = playerRunStats.ToSaveState(),
                runBuild = CreateCurrentRunBuild(captureRuntimeStamina: true).ToSaveState()
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

        private PlayerRunBuild CreateCurrentRunBuild(bool captureRuntimeStamina = false)
        {
            var legacySave = PlayerRunBuild.FromLegacy(playerRunStats, runEconomy).ToSaveState();
            var preservedSave = playerRunBuild?.ToSaveState();
            if (preservedSave != null)
            {
                legacySave.selectedCharacterId = preservedSave.selectedCharacterId;
                legacySave.baseMaxHealth = preservedSave.baseMaxHealth;
                legacySave.baseSpeed = preservedSave.baseSpeed;
                legacySave.baseStrength = preservedSave.baseStrength;
                legacySave.baseMaxStamina = preservedSave.baseMaxStamina;
                legacySave.baseStaminaRegen = preservedSave.baseStaminaRegen;
                legacySave.baseDefense = preservedSave.baseDefense;
                legacySave.baseMeleeDamageBonus = preservedSave.baseMeleeDamageBonus;
                legacySave.baseRangedDamageBonus = preservedSave.baseRangedDamageBonus;
                legacySave.baseAttackCooldownMultiplier = preservedSave.baseAttackCooldownMultiplier;
                legacySave.currentStamina = preservedSave.currentStamina;
                legacySave.equipment = preservedSave.equipment;
                legacySave.inventory = preservedSave.inventory;
            }

            if (captureRuntimeStamina)
            {
                var weapon = playerController != null ? playerController.GetComponent<PlayerWeaponController>() : null;
                if (weapon != null)
                {
                    legacySave.currentStamina = weapon.CurrentStamina;
                    legacySave.equipment.activeWeaponSlot = weapon.ActiveWeaponSlot.ToString();
                }
            }

            return PlayerRunBuild.FromSaveState(legacySave);
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
