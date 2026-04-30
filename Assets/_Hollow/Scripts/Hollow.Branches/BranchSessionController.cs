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
        private const float CoinPickupRadiusMeters = 0.55f;
        private const float PortalInteractionRadiusMeters = 1.5f;
        private const float ShopCardInteractionRadiusMeters = 0.85f;
        private const int FinalWorldIndex = 3;
        private const string HubReplacementContextId = "__hub__";
        private const string RuntimeRewardMarkerKind = "spawn_point_roomReward";
        private const string RuntimeChestMarkerKind = "spawn_point_chest";

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
        [SerializeField] private UsableItemCatalogDefinition usableItemCatalog;
        [SerializeField] private CharacterCatalogDefinition characterCatalog;
        [SerializeField] private ArmorCatalogDefinition armorCatalog;
        [SerializeField] private SynergyCatalogDefinition synergyCatalog;
        [SerializeField] private ChallengeCatalogDefinition challengeCatalog;
        [SerializeField] private EncounterCatalogDefinition encounterCatalog;
        [SerializeField] private BossCatalogDefinition bossCatalog;
        [SerializeField] private EncounterDirectorProfileDefinition encounterDirectorProfile;
        [SerializeField] private RunFramingCatalogDefinition runFramingCatalog;
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
        private readonly List<ReplacementPickup> currentReplacementPickups = new();
        private readonly List<ReplacementPickupState> droppedReplacementPickups = new();
        private readonly List<HazardCoinPickup> currentHazardCoinPickups = new();
        private readonly List<RoomChestController> currentRoomChests = new();
        private readonly List<CoinPickupController> currentCoinPickups = new();
        private readonly List<RunRoomHazardStateSave> roomHazardStates = new();
        private readonly List<RunChestStateSave> roomChestStates = new();
        private readonly List<RunCoinPickupSaveState> looseCoinPickupStates = new();
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
        private IChallengeResultStore challengeResultStore;
        private ProfileSlotId activeProfileSlotId;
        private bool canPersist;
        private bool activeRunCompletedOrFailed;
        private bool suppressCheckpoint;
        private string activeSynergyId = string.Empty;
        private string activeSynergyDisplayName = "None";
        private string synergyAcquisitionMessage = string.Empty;
        private float synergyAcquisitionMessageExpiresAt;
        private ChallengeDefinition activeChallenge;
        private float challengeStartedRealtime;
        private bool challengeCompletionRecorded;
        private PickupRevealModel latestPickupReveal = PickupRevealModel.Empty;
        private int pickupRevealSequence;

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

        public IReadOnlyList<RoomChestController> CurrentRoomChests => currentRoomChests;

        public IReadOnlyList<CoinPickupController> CurrentCoinPickups => currentCoinPickups;

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
                    return $"Character: {playerRunBuild.SelectedCharacterId}\n{ChallengeSummaryLine()}Weapon: Ranged\nStamina: --\nArmor: {ArmorSummary()}\nSet: {ActiveSynergyDisplayName}\nActive: {ActiveItemSummary()}\nCard: {CardSummary()}";
                }

                var message = SynergyAcquisitionMessage;
                var setLine = string.IsNullOrWhiteSpace(message) ? $"Set: {ActiveSynergyDisplayName}" : $"Set: {ActiveSynergyDisplayName}\n{message}";
                return $"Character: {playerRunBuild.SelectedCharacterId}\n{ChallengeSummaryLine()}Weapon: {weapon.ActiveWeaponSlot} - {weapon.ActiveWeaponDisplayName}\nStamina: {weapon.CurrentStamina:0}/{weapon.MaxStamina:0}\nArmor: {ArmorSummary()}\n{setLine}\nActive: {ActiveItemSummary()}\nCard: {CardSummary()}";
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

        public BossCatalogDefinition BossCatalog => bossCatalog;

        public EncounterDirectorProfileDefinition EncounterDirectorProfile => encounterDirectorProfile;

        public WeaponCatalogDefinition WeaponCatalog => weaponCatalog;

        public UsableItemCatalogDefinition UsableItemCatalog => usableItemCatalog;

        public CharacterCatalogDefinition CharacterCatalog => characterCatalog;

        public ArmorCatalogDefinition ArmorCatalog => armorCatalog;

        public SynergyCatalogDefinition SynergyCatalog => synergyCatalog;

        public ChallengeCatalogDefinition ChallengeCatalog => challengeCatalog;

        public RunFramingCatalogDefinition RunFramingCatalog => runFramingCatalog;

        public ChallengeDefinition ActiveChallenge => activeChallenge;

        public string ActiveSynergyDisplayName => string.IsNullOrWhiteSpace(activeSynergyDisplayName) ? "None" : activeSynergyDisplayName;

        public string SynergyAcquisitionMessage => Time.time <= synergyAcquisitionMessageExpiresAt ? synergyAcquisitionMessage : string.Empty;

        public PickupRevealModel LatestPickupReveal => latestPickupReveal;

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

        public void ConfigureUsableItemCatalog(UsableItemCatalogDefinition nextUsableItemCatalog)
        {
            usableItemCatalog = nextUsableItemCatalog;
        }

        public void ConfigureCharacterCatalog(CharacterCatalogDefinition nextCharacterCatalog)
        {
            characterCatalog = nextCharacterCatalog;
        }

        public void ConfigureArmorCatalog(ArmorCatalogDefinition nextArmorCatalog)
        {
            armorCatalog = nextArmorCatalog;
        }

        public void ConfigureSynergyCatalog(SynergyCatalogDefinition nextSynergyCatalog)
        {
            synergyCatalog = nextSynergyCatalog;
        }

        public void ConfigureChallengeCatalog(ChallengeCatalogDefinition nextChallengeCatalog)
        {
            challengeCatalog = nextChallengeCatalog;
        }

        public void ConfigureEncounterCatalog(EncounterCatalogDefinition nextEncounterCatalog)
        {
            encounterCatalog = nextEncounterCatalog;
        }

        public void ConfigureBossCatalog(BossCatalogDefinition nextBossCatalog)
        {
            bossCatalog = nextBossCatalog;
        }

        public void ConfigureEncounterDirectorProfile(EncounterDirectorProfileDefinition nextEncounterDirectorProfile)
        {
            encounterDirectorProfile = nextEncounterDirectorProfile;
        }

        public void ConfigureRunFramingCatalog(RunFramingCatalogDefinition nextRunFramingCatalog)
        {
            runFramingCatalog = nextRunFramingCatalog;
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
            roomCombatController.InteractiveObjectDestroyed -= OnInteractiveObjectDestroyed;
            roomCombatController.InteractiveObjectDestroyed += OnInteractiveObjectDestroyed;

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
            droppedReplacementPickups.Clear();
            currentReplacementPickups.Clear();
            currentHazardCoinPickups.Clear();
            roomHazardStates.Clear();
            currentRoomChests.Clear();
            currentCoinPickups.Clear();
            roomChestStates.Clear();
            looseCoinPickupStates.Clear();
            latestPickupReveal = PickupRevealModel.Empty;
            pickupRevealSequence = 0;
            activeChallenge = ResolveActiveChallenge();
            ApplySelectedCharacterForFreshRun();
            ApplyChallengeRulesForFreshRun();
            rewardCounter.SetClaimedRewards(0);
            activeRunCompletedOrFailed = false;
            branchDepth = 0;
            challengeStartedRealtime = activeChallenge != null ? Time.realtimeSinceStartup : 0f;
            challengeCompletionRecorded = false;
            runSeed = activeChallenge != null
                ? activeChallenge.FixedRunSeed
                : ShouldUseRandomFreshRunSeed() ? RunSeedProvider.CreateSeed() : macroBranchSeed;
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
            droppedReplacementPickups.Clear();
            if (snapshot?.droppedReplacementPickups != null)
            {
                foreach (var saveState in snapshot.droppedReplacementPickups)
                {
                    var pickup = ReplacementPickupState.FromSaveState(saveState);
                    if (pickup != null)
                    {
                        droppedReplacementPickups.Add(pickup);
                    }
                }
            }
            roomHazardStates.Clear();
            if (snapshot?.roomHazardStates != null)
            {
                roomHazardStates.AddRange(snapshot.roomHazardStates);
            }
            roomChestStates.Clear();
            if (snapshot?.roomChestStates != null)
            {
                roomChestStates.AddRange(snapshot.roomChestStates);
            }

            looseCoinPickupStates.Clear();
            if (snapshot?.looseCoinPickups != null)
            {
                looseCoinPickupStates.AddRange(snapshot.looseCoinPickups);
            }
            activeChallenge = ResolveActiveChallenge(snapshot?.challengeId);
            proceduralRewardPlan = ProceduralRewardPlan.FromSaveState(snapshot?.proceduralRewardPlan);
            encounterPlan = EncounterPlan.FromSaveState(snapshot?.encounterPlan);
            rewardCounter.SetClaimedRewards(runEconomy.CollectedRewards.Count);
            activeRunCompletedOrFailed = false;
            challengeStartedRealtime = activeChallenge != null ? Time.realtimeSinceStartup : 0f;
            challengeCompletionRecorded = false;
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
            interBranchHubState = InterBranchHubState.FromSaveState(snapshot?.interBranchHub, currentBranchSeed, branchDepth, standardRewardPool, weaponRewardPool, treasureRewardPool);
            if (IsProceduralRewardBranch(State.Graph.BranchId) && !proceduralRewardPlan.Rewards.Any())
            {
                proceduralRewardPlan = CreateRewardPlanForGraph(
                    State.Graph,
                    legacyFallback: snapshot?.branchId == BranchGenerator.SeededMacroBranchId);
            }
            if ((State.Graph.BranchId == BranchGenerator.EnemyEncounterBranchId ||
                 State.Graph.BranchId == BranchGenerator.BranchFeaturesId ||
                 State.Graph.BranchId == BranchGenerator.DirectedEncounterBranchId) &&
                !encounterPlan.Assignments.Any())
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
                roomCombatController.InteractiveObjectDestroyed -= OnInteractiveObjectDestroyed;
                if (roomCombatController.PlayerHealth != null)
                {
                    roomCombatController.PlayerHealth.Died -= OnPlayerDied;
                }
            }
        }

        private void Update()
        {
            if (State == null || GameplayPauseState.IsPaused)
            {
                return;
            }

            TryCollectCoinPickupsByProximity();
            var input = GameplayInputReader.ReadCurrent();
            if (input.UseActiveItemPressed)
            {
                TryUseActiveItem();
            }

            if (input.UseConsumableCardPressed)
            {
                TryUseConsumableCard();
            }

            if (input.InteractPressed)
            {
                TryInteract();
            }
        }

        public bool TryInteract()
        {
            return TryClaimBossKey() ||
                   TryOpenNearestChest() ||
                   TryClaimReward() ||
                   TryClaimHazardCoinPickup() ||
                   TryClaimReplacementPickup() ||
                   TryHandleNearestShopCard() ||
                   TryUseNextBranchPortal() ||
                   TryUseHubReturnPortal() ||
                   TryTraverseNearestDoor();
        }

        public RunFramingSnapshot CreateRunFramingSnapshot(RunFramingCatalogDefinition overrideCatalog = null)
        {
            var graphSeed = State?.Graph?.Seed ?? 0;
            var branchSeed = currentBranchSeed != 0 ? currentBranchSeed : graphSeed;
            var bossRoomActive = State?.CurrentRoom?.Role == BranchRoomRole.Boss;
            return RunFramingService.Create(
                overrideCatalog != null ? overrideCatalog : runFramingCatalog,
                WorldIndex,
                worldPhase,
                RunSeed,
                branchSeed,
                bossRoomActive);
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

            playerRunBuild = CreateCurrentRunBuild(captureRuntimeStamina: true);
            State.EnterRoom(connection.ToRoomId);
            LoadCurrentRoom(entryConnection: connection);
            CheckpointActiveRun();
            return true;
        }

        public BranchMiniMapModel CreateMiniMapModel()
        {
            return new BranchMiniMapModel(State);
        }

        public PlayerBuildHudModel CreatePlayerBuildHudModel()
        {
            var appliedBuild = CreateAppliedCurrentRunBuild(captureRuntimeStamina: true);
            var derived = appliedBuild.DerivedStats;
            var weapon = playerController != null ? playerController.GetComponent<PlayerWeaponController>() : null;
            var defense = playerController != null ? playerController.GetComponent<PlayerDefenseController>() : null;
            var health = roomCombatController?.PlayerHealth;
            return new PlayerBuildHudModel(
                CharacterDisplayName(appliedBuild.SelectedCharacterId),
                health != null ? health.CurrentHealth : Mathf.RoundToInt(derived.MaxHealth),
                Mathf.RoundToInt(derived.MaxHealth),
                derived.Defense,
                defense != null && defense.IsGuarding,
                derived.SpeedMetersPerSecond,
                derived.Strength,
                weapon != null ? weapon.CurrentStamina : appliedBuild.CurrentStamina,
                weapon != null ? weapon.MaxStamina : derived.MaxStamina,
                derived.StaminaRegenPerSecond,
                derived.MeleeDamageBonus,
                derived.RangedDamageBonus,
                derived.MeleeRangeBonusMeters,
                derived.RangedRangeBonusMeters,
                derived.AttackCooldownMultiplier,
                runEconomy.RunCoins,
                runEconomy.RunSouls,
                weapon != null ? $"{weapon.ActiveWeaponSlot} - {weapon.ActiveWeaponDisplayName}" : appliedBuild.Equipment.ActiveWeaponSlot.ToString(),
                ResolveRewardName(RewardKind.Weapon, appliedBuild.Equipment.MeleeWeaponId),
                ResolveRewardName(RewardKind.Weapon, appliedBuild.Equipment.RangedWeaponId),
                ResolveRewardName(RewardKind.Armor, appliedBuild.Equipment.ArmorId),
                ActiveItemSummary(appliedBuild),
                CardSummary(appliedBuild),
                ActiveSynergyDisplayName);
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
            if (State.CurrentRoom.Role == BranchRoomRole.Origin)
            {
                roomRuntimeRoot.ClearHazardsAndInteractiveObjects();
            }

            roomRuntimeRoot.ApplyInteractiveObjectState(DestroyedObjectIdsForCurrentRoom());
            var playerLocalPosition = entryConnection != null
                ? BranchTraversalService.EntryPositionFor(roomRuntimeRoot, entryConnection)
                : requestedPlayerLocalPosition ?? currentRoomAsset.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            playerController.transform.localPosition = playerLocalPosition;
            State.CurrentRoom.MarkVisited();
            if (State.CurrentRoom.Role == BranchRoomRole.Origin && !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
            }

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
            RewardApplicationService.RechargeActiveItem(playerRunBuild, usableItemCatalog);
            ApplyRunStatsToPlayer(healAmount: 0);
            VfxPresenter.Play(VfxCueId.DoorUnlock, roomRuntimeRoot.transform.position, roomRuntimeRoot.transform);
            AudioPresenter.Play(AudioCueId.DoorUnlock, roomRuntimeRoot.transform.position);
            SpawnRewardIfNeeded();
            SpawnHazardCoinPickupsForCurrentRoom();
            SpawnSavedChestsForCurrentRoom();
            SpawnLooseCoinPickupsForCurrentRoom();
            SpawnReplacementPickupsForCurrentContext();
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
            RewardApplicationService.RechargeActiveItem(playerRunBuild, usableItemCatalog);
            ApplyRunStatsToPlayer(healAmount: 0);
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
            ShowStatusReveal("Boss Key", "Boss Key acquired", new Color(1f, 0.78f, 0.24f, 1f));
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
            var result = ApplyRewardGrant(grant);
            if (result.Applied)
            {
                LastRewardMessage = result.Message;
            }

            DestroyRuntimeObject(currentRewardPickup.gameObject);
            currentRewardPickup = null;
            VfxPresenter.Play(VfxCueId.RewardClaim, playerController.transform.position, playerController.transform.parent);
            AudioPresenter.Play(AudioCueId.RewardClaim, playerController.transform.position);
            SpawnHubPortalIfReady();
            CheckpointActiveRun();
            return true;
        }

        private bool TryOpenNearestChest()
        {
            if (playerController == null || State == null || currentRoomChests.Count == 0)
            {
                return false;
            }

            var nearest = currentRoomChests
                .Where(chest => chest != null && !chest.IsOpened)
                .OrderBy(chest => Vector3.Distance(Flat(playerController.transform.localPosition), Flat(chest.transform.localPosition)))
                .FirstOrDefault();
            if (nearest == null ||
                Vector3.Distance(Flat(playerController.transform.localPosition), Flat(nearest.transform.localPosition)) > RewardInteractionRadiusMeters ||
                !nearest.Open())
            {
                return false;
            }

            var state = FindChestState(nearest.RoomId, nearest.ChestId);
            if (state == null)
            {
                return false;
            }

            state.state = ChestState.Opened.ToString();
            state.contentsClaimed = true;
            State.CurrentRoom.MarkRewardClaimed();
            ApplyChestContents(state, nearest.transform.localPosition);
            VfxPresenter.Play(VfxCueId.ChestOpen, nearest.transform.position, nearest.transform.parent);
            AudioPresenter.Play(AudioCueId.ChestOpen, nearest.transform.position);
            SpawnHubPortalIfReady();
            CheckpointActiveRun();
            return true;
        }

        private bool TryCollectCoinPickupsByProximity()
        {
            if (playerController == null || currentCoinPickups.Count == 0)
            {
                return false;
            }

            var collectedAny = false;
            foreach (var pickup in currentCoinPickups.Where(pickup => pickup != null && !pickup.IsCollected).ToArray())
            {
                if (Vector3.Distance(Flat(playerController.transform.localPosition), Flat(pickup.transform.localPosition)) > CoinPickupRadiusMeters ||
                    !pickup.Collect())
                {
                    continue;
                }

                MarkCoinPickupCollected(pickup.RoomId, pickup.PickupId);
                var grant = new RewardGrant(
                    pickup.PickupId,
                    CoinDenominationResolver.RewardIdFor(pickup.Denomination),
                    CoinDenominationResolver.DisplayNameFor(pickup.Denomination),
                    RewardKind.Currency,
                    0,
                    pickup.Value,
                    Array.Empty<RewardEffect>());
                if (runEconomy.ApplyReward(grant))
                {
                    LastRewardMessage = $"+{pickup.Value} coin";
                    ShowPickupReveal(grant, null);
                }

                currentCoinPickups.Remove(pickup);
                DestroyRuntimeObject(pickup.gameObject);
                VfxPresenter.Play(VfxCueId.CoinPickup, playerController.transform.position, playerController.transform.parent);
                AudioPresenter.Play(AudioCueId.CoinPickup, playerController.transform.position);
                collectedAny = true;
            }

            if (collectedAny)
            {
                CheckpointActiveRun();
            }

            return collectedAny;
        }

        private bool TryClaimReplacementPickup()
        {
            if (playerController == null || currentReplacementPickups.Count == 0)
            {
                return false;
            }

            var nearest = currentReplacementPickups
                .Where(pickup => pickup != null)
                .OrderBy(pickup => Vector3.Distance(Flat(playerController.transform.localPosition), Flat(pickup.transform.localPosition)))
                .FirstOrDefault();
            if (nearest == null ||
                Vector3.Distance(Flat(playerController.transform.localPosition), Flat(nearest.transform.localPosition)) > RewardInteractionRadiusMeters ||
                !nearest.Claim())
            {
                return false;
            }

            var state = droppedReplacementPickups.FirstOrDefault(candidate => candidate.PickupId == nearest.PickupId);
            if (state == null)
            {
                return false;
            }

            droppedReplacementPickups.Remove(state);
            currentReplacementPickups.Remove(nearest);
            var dropPosition = nearest.transform.localPosition;
            DestroyRuntimeObject(nearest.gameObject);

            ApplyReplacementPickupState(state, dropPosition);
            LastRewardMessage = $"Swapped back: {state.DisplayName}";
            VfxPresenter.Play(VfxCueId.RewardClaim, playerController.transform.position, playerController.transform.parent);
            AudioPresenter.Play(AudioCueId.RewardClaim, playerController.transform.position);
            CheckpointActiveRun();
            return true;
        }

        private bool TryClaimHazardCoinPickup()
        {
            if (playerController == null || currentHazardCoinPickups.Count == 0)
            {
                return false;
            }

            var nearest = currentHazardCoinPickups
                .Where(pickup => pickup != null && !pickup.IsClaimed)
                .OrderBy(pickup => Vector3.Distance(Flat(playerController.transform.localPosition), Flat(pickup.transform.localPosition)))
                .FirstOrDefault();
            if (nearest == null ||
                Vector3.Distance(Flat(playerController.transform.localPosition), Flat(nearest.transform.localPosition)) > RewardInteractionRadiusMeters ||
                !nearest.Claim())
            {
                return false;
            }

            MarkHazardCoinCollected(nearest.RoomId, nearest.ObjectId);
            var grant = new RewardGrant(
                $"{nearest.RoomId}:hazard_coin:{nearest.ObjectId}",
                "hazard_coin",
                "Loose Coin",
                RewardKind.Currency,
                0,
                nearest.CoinAmount,
                Array.Empty<RewardEffect>());
            if (runEconomy.ApplyReward(grant))
            {
                LastRewardMessage = $"+{nearest.CoinAmount} coin";
                ShowPickupReveal(grant, null);
            }

            currentHazardCoinPickups.Remove(nearest);
            DestroyRuntimeObject(nearest.gameObject);
            VfxPresenter.Play(VfxCueId.HazardCoinDrop, playerController.transform.position, playerController.transform.parent);
            AudioPresenter.Play(AudioCueId.HazardCoinDrop, playerController.transform.position);
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

            if (ChallengeHasRule(ChallengeRuleKind.BlockShops))
            {
                LastRewardMessage = "Challenge rule blocks shop purchases";
                currentHubShop?.RefreshCards(runEconomy.RunSouls, runEconomy.RunCoins);
                return false;
            }

            if (ChallengeHasRule(ChallengeRuleKind.BlockHealingRewards) &&
                (offer.HealAmount > 0 || GrantHasHeal(offer.RewardGrant)))
            {
                LastRewardMessage = "Challenge rule blocks healing";
                currentHubShop?.RefreshCards(runEconomy.RunSouls, runEconomy.RunCoins);
                return false;
            }

            if (offer.IsPurchased)
            {
                LastRewardMessage = $"{offer.DisplayName} is sold out";
                currentHubShop?.RefreshCards(runEconomy.RunSouls, runEconomy.RunCoins);
                return false;
            }

            if (!CanAfford(offer))
            {
                LastRewardMessage = NeedCurrencyMessage(offer);
                currentHubShop?.RefreshCards(runEconomy.RunSouls, runEconomy.RunCoins);
                return false;
            }

            if (!offer.TryPurchase(runEconomy, out var grant, out var healAmount))
            {
                return false;
            }

            if (!grant.IsEmpty)
            {
                var result = ApplyRewardGrant(grant, extraHeal: healAmount);
                LastRewardMessage = result.Applied ? $"Purchased: {grant.DisplayName}" : LastRewardMessage;
                healAmount = 0;
            }
            else if (healAmount > 0)
            {
                LastRewardMessage = "Purchased: Heal 2 HP";
                ShowPickupReveal(new RewardGrant($"heal_{offer.OfferId}_{Time.frameCount}", "heal", "Heal 2 HP", RewardKind.Heal, 0, 0, new[] { new RewardEffect(RewardEffectKind.Heal, healAmount) }), null);
            }

            ApplyRunStatsToPlayer(healAmount);
            currentHubShop?.Configure(interBranchHubState);
            currentHubShop?.RefreshCards(runEconomy.RunSouls, runEconomy.RunCoins);
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
                        ? interBranchHubState.MarkBranchPortalDefeated(activeHubPortalId, standardRewardPool, weaponRewardPool, treasureRewardPool)
                        : InterBranchHubState.CreateWorldHub(runSeed, worldIndex, 0, standardRewardPool, weaponRewardPool, treasureRewardPool)
                            .MarkBranchPortalDefeated(activeHubPortalId, standardRewardPool, weaponRewardPool, treasureRewardPool);
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
                    ? InterBranchHubState.CreateWorldHub(runSeed == 0 ? currentBranchSeed : runSeed, worldIndex, interBranchHubState.ShopRefreshIndex, standardRewardPool, weaponRewardPool, treasureRewardPool)
                    : InterBranchHubState.Create(currentBranchSeed == 0 ? State?.Graph?.Seed ?? macroBranchSeed : currentBranchSeed, branchDepth, standardRewardPool, weaponRewardPool, treasureRewardPool);
            }

            worldPhase = IsWorldLoopRuntime() ? RunWorldPhase.Hub : worldPhase;
            DestroyTransientInteractables();
            roomRuntimeRoot?.ClearRuntime();
            if (playerController != null)
            {
                playerController.transform.localPosition = Vector3.zero;
            }

            droppedReplacementPickups.RemoveAll(pickup => pickup.RoomId != HubReplacementContextId);
            roomChestStates.Clear();
            looseCoinPickupStates.Clear();
            SpawnHubShopAndPortals();
            SpawnReplacementPickupsForCurrentContext();
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
                : CreateAppliedCurrentRunBuild().DerivedStats.MaxHealth;
            branchDepth++;
            droppedReplacementPickups.Clear();
            roomChestStates.Clear();
            looseCoinPickupStates.Clear();
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
            State = BranchSessionState.Create(CreateWorldLoopGraph(currentBranchSeed));
            branchFeaturePlan = BranchFeaturePlan.Create(State.Graph);
            proceduralRewardPlan = CreateRewardPlanForGraph(State.Graph, legacyFallback: false);
            encounterPlan = CreateEncounterPlanForGraph(State.Graph);
            LoadCurrentRoom();
            if (roomCombatController?.PlayerHealth != null)
            {
                roomCombatController.PlayerHealth.Restore(CreateAppliedCurrentRunBuild().DerivedStats.MaxHealth, preservedHealth);
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

            var grant = ProceduralRewardResolver.Resolve(State.CurrentRoomId.Value, proceduralRewardPlan);
            if (grant.IsEmpty)
            {
                State.CurrentRoom.MarkRewardUnavailable();
                LastRewardMessage = "Nothing found";
                SpawnHubPortalIfReady();
                CheckpointActiveRun();
                return;
            }

            if (ChestRewardResolver.IsChestReward(grant))
            {
                SpawnChestForReward(grant);
                return;
            }

            if (grant.RewardKind == RewardKind.Currency && grant.RewardId == ChestRewardResolver.SmallCoinPouchRewardId && grant.Coins > 0)
            {
                SpawnCoinsForValue(
                    State.CurrentRoomId.Value,
                    $"{State.CurrentRoomId.Value}_loose_reward",
                    grant.Coins,
                    CurrentRewardSpawnPosition(preferChestMarker: false),
                    StableHash($"{State.Graph.BranchId}|{State.Graph.Seed}|{State.CurrentRoomId.Value}|loose_reward"));
                State.CurrentRoom.MarkRewardClaimed();
                LastRewardMessage = $"{grant.Coins} coins spilled";
                SpawnHubPortalIfReady();
                CheckpointActiveRun();
                return;
            }

            var rewardObject = InstantiateOrCreate(rewardPickupPrefab, "RoomRewardPickup", PrimitiveType.Sphere, MaterialRole.RewardPickup);
            rewardObject.transform.SetParent(playerController.transform.parent, false);
            rewardObject.transform.localPosition = CurrentRewardSpawnPosition(preferChestMarker: false);
            rewardObject.transform.localScale = Vector3.one * 0.35f;
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.RewardPickup, rewardObject.transform, Vector3.zero, Vector3.one);
            currentRewardPickup = rewardObject.GetComponent<RoomRewardPickup>() ?? rewardObject.AddComponent<RoomRewardPickup>();
            currentRewardPickup.Configure(State.CurrentRoomId.Value);
        }

        private void SpawnChestForReward(RewardGrant grant)
        {
            if (State == null || playerController == null)
            {
                return;
            }

            var roomId = State.CurrentRoomId.Value;
            var chestId = $"{roomId}_{grant.RewardId}";
            var state = FindChestState(roomId, chestId);
            if (state == null)
            {
                state = CreateChestState(grant, chestId);
                roomChestStates.Add(state);
            }

            SpawnChest(state);
            LastRewardMessage = state.kind == ChestKind.Golden.ToString() ? "Golden chest appeared" : "Chest appeared";
            CheckpointActiveRun();
        }

        private RunChestStateSave CreateChestState(RewardGrant grant, string chestId)
        {
            var roomId = State.CurrentRoomId.Value;
            var kind = ChestRewardResolver.KindForGrant(grant);
            var contents = ChestRewardResolver.ResolveContents(State.Graph.BranchId, State.Graph.Seed, roomId, kind);
            var contentGrant = contents.RewardGrant;
            var position = CurrentRewardSpawnPosition(preferChestMarker: true);
            return new RunChestStateSave
            {
                roomId = roomId,
                chestId = chestId,
                kind = kind.ToString(),
                state = ChestState.Unopened.ToString(),
                contentsClaimed = false,
                contentRewardId = contentGrant.RewardId,
                contentDisplayName = contentGrant.DisplayName,
                contentRewardKind = contentGrant.IsEmpty ? string.Empty : contentGrant.RewardKind.ToString(),
                contentSouls = contentGrant.Souls,
                contentCoins = contents.CoinValue,
                contentEffects = contentGrant.Effects?.Select(effect => effect.ToSaveState()).ToList() ?? new List<RunRewardEffectSaveState>(),
                localX = position.x,
                localY = position.y,
                localZ = position.z
            };
        }

        private void SpawnSavedChestsForCurrentRoom()
        {
            var roomId = State?.CurrentRoomId.Value ?? string.Empty;
            foreach (var state in roomChestStates.Where(candidate => candidate != null && candidate.roomId == roomId))
            {
                SpawnChest(state);
            }
        }

        private void SpawnChest(RunChestStateSave state)
        {
            if (state == null ||
                playerController == null ||
                currentRoomChests.Any(chest => chest != null && chest.RoomId == state.roomId && chest.ChestId == state.chestId))
            {
                return;
            }

            var kind = Enum.TryParse(state.kind, out ChestKind parsedKind) ? parsedKind : ChestKind.Normal;
            var chestState = Enum.TryParse(state.state, out ChestState parsedState) ? parsedState : ChestState.Unopened;
            var role = kind == ChestKind.Golden ? PresentationPrefabRole.ChestGolden : PresentationPrefabRole.ChestNormal;
            var materialRole = kind == ChestKind.Golden ? MaterialRole.ChestGolden : MaterialRole.ChestNormal;
            var chestObject = InstantiateOrCreate(rewardPickupPrefab, $"Chest_{kind}_{state.chestId}", PrimitiveType.Cube, materialRole);
            chestObject.transform.SetParent(playerController.transform.parent, false);
            chestObject.transform.localPosition = new Vector3(state.localX, state.localY <= 0f ? 0.35f : state.localY, state.localZ);
            PresentationPrefabResolver.InstantiateVisual(role, chestObject.transform, Vector3.zero, Vector3.one);
            var chest = chestObject.GetComponent<RoomChestController>() ?? chestObject.AddComponent<RoomChestController>();
            chest.Configure(state.roomId, state.chestId, kind, chestState);
            currentRoomChests.Add(chest);
        }

        private void ApplyChestContents(RunChestStateSave state, Vector3 chestPosition)
        {
            if (state == null)
            {
                return;
            }

            if (state.contentCoins > 0)
            {
                SpawnCoinsForValue(
                    state.roomId,
                    $"{state.chestId}_contents",
                    state.contentCoins,
                    chestPosition,
                    StableHash($"{State?.Graph?.BranchId}|{State?.Graph?.Seed ?? 0}|{state.roomId}|{state.chestId}|coins"));
            }

            var grant = RewardGrantFromChestState(state);
            if (!grant.IsEmpty)
            {
                var result = ApplyRewardGrant(grant);
                if (result.Applied)
                {
                    LastRewardMessage = result.Message;
                }
            }
            else if (state.contentCoins > 0)
            {
                LastRewardMessage = $"{state.contentCoins} coins spilled";
                ShowStatusReveal("Chest Opened", $"{state.contentCoins} coins spilled", new Color(1f, 0.82f, 0.18f, 1f));
            }
            else
            {
                LastRewardMessage = "Chest was empty";
            }
        }

        private RewardGrant RewardGrantFromChestState(RunChestStateSave state)
        {
            if (state == null ||
                string.IsNullOrWhiteSpace(state.contentRewardId) ||
                !Enum.TryParse(state.contentRewardKind, out RewardKind rewardKind))
            {
                return default;
            }

            var effects = state.contentEffects != null
                ? state.contentEffects.Select(RewardEffect.FromSaveState).ToArray()
                : Array.Empty<RewardEffect>();

            return new RewardGrant(
                $"{state.roomId}:chest:{state.chestId}:reward",
                state.contentRewardId,
                state.contentDisplayName,
                rewardKind,
                state.contentSouls,
                0,
                effects);
        }

        private RunChestStateSave FindChestState(string roomId, string chestId)
        {
            return roomChestStates.FirstOrDefault(candidate => candidate != null && candidate.roomId == roomId && candidate.chestId == chestId);
        }

        private Vector3 CurrentRewardSpawnPosition(bool preferChestMarker)
        {
            if (currentRoomAsset?.ItemSpawns != null)
            {
                var marker = preferChestMarker
                    ? currentRoomAsset.ItemSpawns.FirstOrDefault(spawn => spawn?.kind == RuntimeChestMarkerKind)
                    : null;
                marker ??= currentRoomAsset.ItemSpawns.FirstOrDefault(spawn => spawn?.kind == RuntimeRewardMarkerKind);
                if (marker?.position != null)
                {
                    return new Vector3(marker.position.x, 0.35f, marker.position.z);
                }
            }

            var safeStart = currentRoomAsset?.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            return new Vector3(Mathf.Clamp(safeStart.x + 1.2f, -2.5f, 2.5f), 0.35f, Mathf.Clamp(safeStart.z + 0.8f, -1.5f, 1.5f));
        }

        private void SpawnReplacementPickupsForCurrentContext()
        {
            if (playerController == null)
            {
                return;
            }

            var contextId = CurrentReplacementContextId();
            foreach (var state in droppedReplacementPickups.Where(candidate => candidate.RoomId == contextId).ToArray())
            {
                if (currentReplacementPickups.Any(pickup => pickup != null && pickup.PickupId == state.PickupId))
                {
                    continue;
                }

                SpawnReplacementPickup(state);
            }
        }

        private void SpawnReplacementPickup(ReplacementPickupState state)
        {
            if (state == null || playerController == null)
            {
                return;
            }

            var pickupObject = InstantiateOrCreate(rewardPickupPrefab, $"ReplacementPickup_{state.RewardKind}_{state.RewardId}", PrimitiveType.Sphere, MaterialRole.RewardPickup);
            pickupObject.transform.SetParent(playerController.transform.parent, false);
            pickupObject.transform.localPosition = state.LocalPosition;
            pickupObject.transform.localScale = Vector3.one * 0.3f;
            PresentationPrefabResolver.InstantiateVisual(VisualRoleForReplacement(state.RewardKind), pickupObject.transform, Vector3.zero, Vector3.one);
            var pickup = pickupObject.GetComponent<ReplacementPickup>() ?? pickupObject.AddComponent<ReplacementPickup>();
            pickup.Configure(state);
            currentReplacementPickups.Add(pickup);
        }

        private void AddDroppedReplacement(ReplacementPickupState replacement)
        {
            if (replacement == null)
            {
                return;
            }

            droppedReplacementPickups.RemoveAll(candidate => candidate.PickupId == replacement.PickupId);
            droppedReplacementPickups.Add(replacement);
            if (replacement.RoomId == CurrentReplacementContextId())
            {
                SpawnReplacementPickup(replacement);
            }
        }

        private IEnumerable<string> DestroyedObjectIdsForCurrentRoom()
        {
            var roomId = State?.CurrentRoomId.Value ?? string.Empty;
            return roomHazardStates
                .Where(state => state != null && state.roomId == roomId && state.isDestroyed)
                .Select(state => state.objectId)
                .Where(id => !string.IsNullOrWhiteSpace(id));
        }

        private void SpawnCoinsForValue(string roomId, string sourceId, int totalValue, Vector3 origin, int seed)
        {
            if (totalValue <= 0 || string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            var denominations = CoinDenominationResolver.ResolveExactValue(totalValue, seed);
            for (var index = 0; index < denominations.Count; index++)
            {
                var pickupId = $"{sourceId}_{index:00}";
                if (looseCoinPickupStates.Any(state => state.pickupId == pickupId))
                {
                    continue;
                }

                var denomination = denominations[index];
                var offset = CoinScatterOffset(seed, index, denominations.Count);
                var state = new RunCoinPickupSaveState
                {
                    roomId = roomId,
                    pickupId = pickupId,
                    denomination = denomination.ToString(),
                    value = CoinDenominationResolver.ValueFor(denomination),
                    isCollected = false,
                    localX = origin.x + offset.x,
                    localY = 0.24f,
                    localZ = origin.z + offset.z
                };
                looseCoinPickupStates.Add(state);
                SpawnCoinPickup(state);
            }
        }

        private void SpawnLooseCoinPickupsForCurrentRoom()
        {
            var roomId = State?.CurrentRoomId.Value ?? string.Empty;
            foreach (var state in looseCoinPickupStates.Where(candidate => candidate != null && candidate.roomId == roomId && !candidate.isCollected))
            {
                SpawnCoinPickup(state);
            }
        }

        private void SpawnCoinPickup(RunCoinPickupSaveState state)
        {
            if (state == null ||
                playerController == null ||
                state.isCollected ||
                currentCoinPickups.Any(pickup => pickup != null && pickup.PickupId == state.pickupId))
            {
                return;
            }

            var denomination = Enum.TryParse(state.denomination, out CoinDenomination parsedDenomination)
                ? parsedDenomination
                : CoinDenomination.Copper;
            var pickupObject = InstantiateOrCreate(rewardPickupPrefab, $"Coin_{denomination}_{state.pickupId}", PrimitiveType.Sphere, MaterialRoleForCoin(denomination));
            pickupObject.transform.SetParent(playerController.transform.parent, false);
            pickupObject.transform.localPosition = new Vector3(state.localX, state.localY <= 0f ? 0.24f : state.localY, state.localZ);
            pickupObject.transform.localScale = Vector3.one * ScaleForCoin(denomination);
            PresentationPrefabResolver.InstantiateVisual(PrefabRoleForCoin(denomination), pickupObject.transform, Vector3.zero, Vector3.one);
            var pickup = pickupObject.GetComponent<CoinPickupController>() ?? pickupObject.AddComponent<CoinPickupController>();
            pickup.Configure(state.roomId, state.pickupId, denomination, state.value <= 0 ? CoinDenominationResolver.ValueFor(denomination) : state.value, state.isCollected);
            currentCoinPickups.Add(pickup);
        }

        private void MarkCoinPickupCollected(string roomId, string pickupId)
        {
            var state = looseCoinPickupStates.FirstOrDefault(candidate => candidate.roomId == roomId && candidate.pickupId == pickupId);
            if (state != null)
            {
                state.isCollected = true;
            }
        }

        private static Vector3 CoinScatterOffset(int seed, int index, int count)
        {
            if (count <= 1)
            {
                return Vector3.zero;
            }

            var angle = (StableHash($"{seed}|coin_angle|{index}") % 628) / 100f;
            var radius = 0.22f + 0.08f * (index % 4);
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static float ScaleForCoin(CoinDenomination denomination)
        {
            return denomination switch
            {
                CoinDenomination.Gold => 0.28f,
                CoinDenomination.Silver => 0.24f,
                _ => 0.2f
            };
        }

        private static PresentationPrefabRole PrefabRoleForCoin(CoinDenomination denomination)
        {
            return denomination switch
            {
                CoinDenomination.Gold => PresentationPrefabRole.CoinGold,
                CoinDenomination.Silver => PresentationPrefabRole.CoinSilver,
                _ => PresentationPrefabRole.CoinCopper
            };
        }

        private static MaterialRole MaterialRoleForCoin(CoinDenomination denomination)
        {
            return denomination switch
            {
                CoinDenomination.Gold => MaterialRole.CoinGold,
                CoinDenomination.Silver => MaterialRole.CoinSilver,
                _ => MaterialRole.CoinCopper
            };
        }

        private void OnInteractiveObjectDestroyed(RoomInteractiveObjectDestroyedContext context)
        {
            var roomId = State?.CurrentRoomId.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(context.ObjectId))
            {
                return;
            }

            var state = roomHazardStates.FirstOrDefault(candidate => candidate.roomId == roomId && candidate.objectId == context.ObjectId);
            if (state == null)
            {
                state = new RunRoomHazardStateSave
                {
                    roomId = roomId,
                    objectId = context.ObjectId,
                    objectKind = context.ObjectKind
                };
                roomHazardStates.Add(state);
            }

            state.isDestroyed = true;
            state.objectKind = context.ObjectKind;
            if (context.CoinDropAmount > 0)
            {
                state.coinDropAmount = context.CoinDropAmount;
                state.coinCollected = false;
                state.localX = context.LocalPosition.x;
                state.localY = 0.25f;
                state.localZ = context.LocalPosition.z;
                SpawnHazardCoinPickup(state);
            }

            CheckpointActiveRun();
        }

        private void SpawnHazardCoinPickupsForCurrentRoom()
        {
            var roomId = State?.CurrentRoomId.Value ?? string.Empty;
            foreach (var state in roomHazardStates.Where(candidate =>
                         candidate != null &&
                         candidate.roomId == roomId &&
                         candidate.isDestroyed &&
                         candidate.coinDropAmount > 0 &&
                         !candidate.coinCollected))
            {
                SpawnHazardCoinPickup(state);
            }
        }

        private void SpawnHazardCoinPickup(RunRoomHazardStateSave state)
        {
            if (state == null ||
                playerController == null ||
                currentHazardCoinPickups.Any(pickup => pickup != null && pickup.RoomId == state.roomId && pickup.ObjectId == state.objectId))
            {
                return;
            }

            var pickupObject = InstantiateOrCreate(rewardPickupPrefab, $"HazardCoin_{state.objectId}", PrimitiveType.Sphere, MaterialRole.HazardCoinDrop);
            pickupObject.transform.SetParent(playerController.transform.parent, false);
            pickupObject.transform.localPosition = new Vector3(state.localX, state.localY <= 0f ? 0.25f : state.localY, state.localZ);
            pickupObject.transform.localScale = Vector3.one * 0.22f;
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.HazardCoinDrop, pickupObject.transform, Vector3.zero, Vector3.one);
            var pickup = pickupObject.GetComponent<HazardCoinPickup>() ?? pickupObject.AddComponent<HazardCoinPickup>();
            pickup.Configure(state.roomId, state.objectId, state.coinDropAmount);
            currentHazardCoinPickups.Add(pickup);
        }

        private void MarkHazardCoinCollected(string roomId, string objectId)
        {
            var state = roomHazardStates.FirstOrDefault(candidate => candidate.roomId == roomId && candidate.objectId == objectId);
            if (state != null)
            {
                state.coinCollected = true;
            }
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

            foreach (var pickup in currentReplacementPickups)
            {
                if (pickup != null)
                {
                    DestroyRuntimeObject(pickup.gameObject);
                }
            }

            currentReplacementPickups.Clear();

            foreach (var pickup in currentHazardCoinPickups)
            {
                if (pickup != null)
                {
                    DestroyRuntimeObject(pickup.gameObject);
                }
            }

            currentHazardCoinPickups.Clear();

            foreach (var chest in currentRoomChests)
            {
                if (chest != null)
                {
                    DestroyRuntimeObject(chest.gameObject);
                }
            }

            currentRoomChests.Clear();

            foreach (var pickup in currentCoinPickups)
            {
                if (pickup != null)
                {
                    DestroyRuntimeObject(pickup.gameObject);
                }
            }

            currentCoinPickups.Clear();
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
            currentHubShop.BuildCards(runEconomy.RunSouls, runEconomy.RunCoins);

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
                portal.Configure(choice, DisplayNameForHubChoice(choice));
                currentNextBranchPortals.Add(portal);
            }
        }

        private string DisplayNameForHubChoice(NextBranchChoice choice)
        {
            if (choice == null)
            {
                return string.Empty;
            }

            if (choice.Kind == HubPortalKind.Branch)
            {
                var echo = RunWorldItineraryService.ResolveBranchEcho(runFramingCatalog, RunSeed, choice.WorldIndex, choice.SlotIndex);
                return string.IsNullOrWhiteSpace(echo) ? choice.DisplayName : echo;
            }

            if (choice.Kind == HubPortalKind.NextWorld)
            {
                var nextWorld = RunWorldItineraryService.Resolve(runFramingCatalog, RunSeed, choice.WorldIndex);
                return nextWorld != null ? $"Descend: {nextWorld.DisplayName}" : choice.DisplayName;
            }

            if (choice.Kind == HubPortalKind.FinalExtraction)
            {
                return "Temporary Extraction";
            }

            return choice.DisplayName;
        }

        private void ResolveReferences()
        {
            roomRuntimeRoot = roomRuntimeRoot != null ? roomRuntimeRoot : GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true) ?? FindAnyObjectByType<RoomRuntimeRoot>();
            playerController = playerController != null ? playerController : GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true) ?? FindAnyObjectByType<PlaceholderPlayerController>();
            roomCombatController = roomCombatController != null ? roomCombatController : GetComponent<RoomCombatController>() ?? FindAnyObjectByType<RoomCombatController>();
            if (roomCombatController != null)
            {
                roomCombatController.ConfigureBossCatalog(bossCatalog);
            }
        }

        private void ResolvePersistence()
        {
            canPersist = false;
            challengeResultStore = null;
            SaveStatus = "Transient";
            BankedSouls = 0;

            var profileHost = ProfileSessionHost.Instance;
            if (profileHost != null)
            {
                challengeResultStore = profileHost.ChallengeResultStore ?? profileHost.ProfileStore as IChallengeResultStore;
            }

            var selectedProfile = profileHost?.SelectedProfileContext?.SelectedProfile;
            if (selectedProfile != null)
            {
                BankedSouls = selectedProfile.BankedSouls;
            }

            if (gameSessionState == null || gameSessionState.ProfileSlotIndex < 0)
            {
                return;
            }

            activeProfileSlotId = new ProfileSlotId(gameSessionState.ProfileSlotIndex);
            if (profileHost?.RunSaveStore == null)
            {
                return;
            }

            canPersist = TransientSessionGuard.CanPersist(gameSessionState.SessionMode, gameSessionState.HasProfile);
            if (!canPersist)
            {
                return;
            }

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
                            return CreateM46Graph(seed, worldIndex);
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

        private ChallengeDefinition ResolveActiveChallenge(string overrideChallengeId = null)
        {
            var challengeId = !string.IsNullOrWhiteSpace(overrideChallengeId)
                ? overrideChallengeId
                : gameSessionState != null && gameSessionState.SessionMode == RuntimeSessionMode.TransientChallenge
                    ? gameSessionState.SelectedChallengeId
                    : string.Empty;
            if (string.IsNullOrWhiteSpace(challengeId))
            {
                return null;
            }

            var catalog = challengeCatalog != null ? challengeCatalog : ChallengeCatalogDefinition.CreateRuntimeDefault();
            return catalog.Resolve(challengeId);
        }

        private void ApplyChallengeRulesForFreshRun()
        {
            if (activeChallenge == null)
            {
                return;
            }

            if (!activeChallenge.StatModifier.IsEmpty)
            {
                playerRunBuild.AddModifier(CreateClampedChallengeModifier(activeChallenge));
            }

            if (activeChallenge.StartingCoins > 0)
            {
                runEconomy.ApplyReward(new RewardGrant("challenge_start_coins", "challenge_coins", activeChallenge.DisplayName, RewardKind.Currency, 0, activeChallenge.StartingCoins, Array.Empty<RewardEffect>()));
            }

            if (activeChallenge.StartingSouls > 0)
            {
                runEconomy.ApplyReward(new RewardGrant("challenge_start_souls", "challenge_souls", activeChallenge.DisplayName, RewardKind.Currency, activeChallenge.StartingSouls, 0, Array.Empty<RewardEffect>()));
            }

            ApplyChallengeLoadoutForFreshRun();
            ApplyEquipmentAndSynergyModifiers(playerRunBuild, announceActivation: false);
            LastRewardMessage = $"Challenge: {activeChallenge.DisplayName}";
            SaveStatus = "Challenge";
        }

        private PlayerStatModifier CreateClampedChallengeModifier(ChallengeDefinition challenge)
        {
            var modifier = PlayerStatModifier.FromCharacterStatModifier($"challenge:{challenge.ChallengeId}", challenge.StatModifier);
            const int minimumChallengeHealth = 2;
            var projectedHealth = playerRunBuild.DerivedStats.MaxHealth + modifier.maxHealth;
            if (projectedHealth < minimumChallengeHealth)
            {
                modifier.maxHealth += minimumChallengeHealth - projectedHealth;
            }

            return modifier;
        }

        private void ApplyChallengeLoadoutForFreshRun()
        {
            var loadout = activeChallenge?.Loadout;
            if (loadout == null || loadout.IsEmpty)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(loadout.MeleeWeaponId))
            {
                playerRunBuild.Equipment.EquipMeleeWeapon(loadout.MeleeWeaponId);
            }

            if (!string.IsNullOrWhiteSpace(loadout.RangedWeaponId))
            {
                playerRunBuild.Equipment.EquipRangedWeapon(loadout.RangedWeaponId);
            }

            if (!string.IsNullOrWhiteSpace(loadout.ArmorId))
            {
                playerRunBuild.Equipment.EquipArmor(loadout.ArmorId);
            }

            if (!string.IsNullOrWhiteSpace(loadout.ActiveItemId))
            {
                playerRunBuild.Equipment.EquipActiveItem(loadout.ActiveItemId);
                var maxCharges = usableItemCatalog != null && usableItemCatalog.TryGet(loadout.ActiveItemId, out var activeItem)
                    ? Math.Max(1, activeItem.MaxCharges)
                    : 3;
                playerRunBuild.Equipment.SetActiveItemCharges(maxCharges);
            }

            if (!string.IsNullOrWhiteSpace(loadout.ConsumableCardId))
            {
                playerRunBuild.Equipment.EquipConsumableCard(loadout.ConsumableCardId);
            }
        }

        private string ChallengeSummaryLine()
        {
            if (activeChallenge == null)
            {
                return string.Empty;
            }

            var elapsedSeconds = challengeStartedRealtime > 0f ? Mathf.Max(0f, Time.realtimeSinceStartup - challengeStartedRealtime) : 0f;
            return $"Challenge: {activeChallenge.DisplayName} | Seed {activeChallenge.FixedRunSeed} | {FormatElapsed(elapsedSeconds)}\nRules: {CompactChallengeRules(activeChallenge.RulesSummary)}\n";
        }

        private bool ChallengeHasRule(ChallengeRuleKind kind)
        {
            return activeChallenge != null && activeChallenge.HasRule(kind);
        }

        private int ChallengeRuleIntValue(ChallengeRuleKind kind, int fallback = 0)
        {
            return activeChallenge != null ? activeChallenge.RuleIntValue(kind, fallback) : fallback;
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
                snapshot.branchId == BranchGenerator.DirectedEncounterBranchId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                return CreateM46Graph(
                    currentBranchSeed == 0 ? snapshot.branchSeed : currentBranchSeed,
                    snapshot.worldIndex <= 0 ? worldIndex : snapshot.worldIndex);
            }

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

        private BranchFloorGraph CreateM46Graph(int seed, int nextWorldIndex)
        {
            var resolvedSeed = seed == 0 ? macroBranchSeed : seed;
            var resolvedWorldIndex = nextWorldIndex <= 0 ? 1 : nextWorldIndex;
            var selectedBoss = BossSelectionResolver.Resolve(
                bossCatalog,
                resolvedSeed,
                resolvedSeed,
                resolvedWorldIndex,
                "boss_01",
                BranchGenerator.DirectedEncounterBranchId);
            return BranchGenerator.CreateDirectedEncounterBranch(
                branchContent,
                branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                encounterDirectorProfile,
                resolvedWorldIndex,
                resolvedSeed,
                selectedBoss != null ? selectedBoss.Arena.arenaId : string.Empty);
        }

        private BranchFloorGraph CreateWorldLoopGraph(int seed)
        {
            return encounterCatalog != null
                ? CreateM46Graph(seed, worldIndex)
                : CreateM20Graph(seed);
        }

        private static bool IsProceduralRewardBranch(string branchId)
        {
            return branchId == BranchGenerator.SeededMacroBranchId ||
                   branchId == BranchGenerator.FeatureBranchId ||
                   branchId == BranchGenerator.EnemyEncounterBranchId ||
                   branchId == BranchGenerator.BranchFeaturesId ||
                   branchId == BranchGenerator.DirectedEncounterBranchId;
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

        private EncounterPlan CreateEncounterPlanForGraph(BranchFloorGraph graph)
        {
            if (graph == null)
            {
                return EncounterPlan.Empty;
            }

            if (graph.BranchId == BranchGenerator.DirectedEncounterBranchId)
            {
                return EncounterResolver.CreateDirectedSeededPlan(
                    graph,
                    encounterCatalog,
                    graph.Seed,
                    worldIndex,
                    encounterDirectorProfile,
                    ChallengeRuleIntValue(ChallengeRuleKind.EncounterPressureBonus),
                    bossCatalog);
            }

            return graph.BranchId == BranchGenerator.EnemyEncounterBranchId || graph.BranchId == BranchGenerator.BranchFeaturesId
                ? EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed)
                : EncounterPlan.Empty;
        }

        private bool IsM20Branch()
        {
            return State?.Graph?.BranchId == BranchGenerator.BranchFeaturesId ||
                   State?.Graph?.BranchId == BranchGenerator.DirectedEncounterBranchId;
        }

        private RoomCombatEncounterContext CreateEncounterContextForCurrentRoom()
        {
            if (State?.CurrentRoomId == null ||
                !encounterPlan.TryResolve(State.CurrentRoomId.Value, out var assignment))
            {
                return RoomCombatEncounterContext.Empty;
            }

            return new RoomCombatEncounterContext(
                assignment.EncounterId,
                assignment.EnemySpawnKinds,
                assignment.WorldIndex,
                assignment.DifficultyBand,
                assignment.DirectorPressure,
                assignment.BossId,
                assignment.BossArenaId,
                assignment.BossWorldBand,
                assignment.BossPhaseState);
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

            roomCombatController.PlayerHealth.Restore(CreateAppliedCurrentRunBuild().DerivedStats.MaxHealth, snapshot.playerCurrentHealth);
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
            playerRunBuild = CreateAppliedCurrentRunBuild(announceActivation: true);
            PlayerBuildApplier.Apply(playerRunBuild, playerController != null ? playerController.gameObject : null, weaponCatalog, healAmount);
        }

        private void ApplySelectedCharacterForFreshRun()
        {
            var selectedCharacterId = activeChallenge?.SelectedCharacterId ?? gameSessionState?.SelectedCharacterId ?? "balanced";
            var character = characterCatalog != null ? characterCatalog.Resolve(selectedCharacterId) : null;
            playerRunBuild.ConfigureCharacter(character);
            ApplyEquipmentAndSynergyModifiers(playerRunBuild, announceActivation: false);
        }

        private void ApplyReplacementPickupState(ReplacementPickupState state, Vector3 dropPosition)
        {
            if (state == null)
            {
                return;
            }

            playerRunBuild = CreateCurrentRunBuild(captureRuntimeStamina: true);
            var grant = WithRoomId(state.ToGrant(), CurrentReplacementContextId());
            var replacement = RewardReplacementDetector.CaptureBeforeApply(
                grant,
                playerRunBuild,
                weaponCatalog,
                armorCatalog,
                usableItemCatalog,
                dropPosition);

            switch (state.RewardKind)
            {
                case RewardKind.Weapon:
                    var resolvedSlot = WeaponSlot.Ranged;
                    if (weaponCatalog != null && weaponCatalog.TryGetWeapon(state.RewardId, out var weapon))
                    {
                        resolvedSlot = weapon.Slot;
                    }
                    else if (state.RewardId.Contains("blade") || state.RewardId.Contains("cleaver") || state.RewardId.Contains("sword") || state.RewardId.Contains("fang"))
                    {
                        resolvedSlot = WeaponSlot.Melee;
                    }

                    if (resolvedSlot == WeaponSlot.Melee)
                    {
                        playerRunBuild.Equipment.EquipMeleeWeapon(state.RewardId);
                        playerRunBuild.Equipment.SetActiveWeaponSlot(WeaponSlot.Melee);
                    }
                    else
                    {
                        playerRunBuild.Equipment.EquipRangedWeapon(state.RewardId);
                        playerRunBuild.Equipment.SetActiveWeaponSlot(WeaponSlot.Ranged);
                    }
                    break;
                case RewardKind.Armor:
                    playerRunBuild.Equipment.EquipArmor(state.RewardId);
                    break;
                case RewardKind.ActiveItem:
                    playerRunBuild.Equipment.EquipActiveItem(state.RewardId);
                    playerRunBuild.Equipment.SetActiveItemCharges(state.ActiveItemCharges);
                    break;
                case RewardKind.ConsumableCard:
                    playerRunBuild.Equipment.EquipConsumableCard(state.RewardId);
                    break;
            }

            ApplyRunStatsToPlayer(0);
            AddDroppedReplacement(replacement);
            ShowPickupReveal(state.ToGrant(), replacement);
        }

        private RewardApplicationResult ApplyRewardGrant(RewardGrant grant, int extraHeal = 0, Vector3? replacementDropPosition = null, int restoredActiveItemCharges = -1)
        {
            if (ChallengeHasRule(ChallengeRuleKind.BlockHealingRewards) && GrantHasHeal(grant))
            {
                grant = WithoutHealEffects(grant);
                extraHeal = 0;
                LastRewardMessage = "Challenge rule blocks healing";
                if ((grant.Effects == null || grant.Effects.Count == 0) && grant.Souls <= 0 && grant.Coins <= 0 && grant.RewardKind == RewardKind.Currency)
                {
                    return new RewardApplicationResult(false, 0, LastRewardMessage);
                }
            }

            playerRunBuild ??= CreateCurrentRunBuild();
            var dropPosition = replacementDropPosition ?? CurrentReplacementDropPosition();
            var replacementGrant = WithRoomId(grant, CurrentReplacementContextId());
            var replacement = RewardReplacementDetector.CaptureBeforeApply(
                replacementGrant,
                playerRunBuild,
                weaponCatalog,
                armorCatalog,
                usableItemCatalog,
                dropPosition);
            var result = RewardApplicationService.Apply(grant, runEconomy, playerRunStats, playerRunBuild, weaponCatalog, usableItemCatalog);
            if (result.Applied)
            {
                if (restoredActiveItemCharges >= 0 && grant.RewardKind == RewardKind.ActiveItem)
                {
                    playerRunBuild.Equipment.SetActiveItemCharges(restoredActiveItemCharges);
                }

                ApplyRunStatsToPlayer(result.HealAmount + Mathf.Max(0, extraHeal));
                rewardCounter.SetClaimedRewards(runEconomy.CollectedRewards.Count);
                AddDroppedReplacement(replacement);
                ShowPickupReveal(grant, replacement);
            }

            return result;
        }

        public bool TryUseActiveItem()
        {
            playerRunBuild = CreateAppliedCurrentRunBuild(captureRuntimeStamina: true);
            var itemId = playerRunBuild.Equipment.ActiveItemId;
            if (string.IsNullOrWhiteSpace(itemId) || usableItemCatalog == null || !usableItemCatalog.TryGet(itemId, out var item))
            {
                LastRewardMessage = "No active item equipped";
                return false;
            }

            if (!playerRunBuild.Equipment.SpendActiveItemCharge())
            {
                LastRewardMessage = $"{item.DisplayName} has no charges";
                return false;
            }

            ApplyUsableEffects(item, consumeAfterUse: false);
            LastRewardMessage = $"Used: {item.DisplayName}";
            ApplyRunStatsToPlayer(0);
            CheckpointActiveRun();
            return true;
        }

        public bool TryUseConsumableCard()
        {
            playerRunBuild = CreateAppliedCurrentRunBuild(captureRuntimeStamina: true);
            var cardId = playerRunBuild.Equipment.ConsumableCardId;
            if (string.IsNullOrWhiteSpace(cardId) || usableItemCatalog == null || !usableItemCatalog.TryGet(cardId, out var card))
            {
                LastRewardMessage = "No card equipped";
                return false;
            }

            ApplyUsableEffects(card, consumeAfterUse: true);
            playerRunBuild.Equipment.EquipConsumableCard(string.Empty);
            LastRewardMessage = $"Used: {card.DisplayName}";
            ApplyRunStatsToPlayer(0);
            CheckpointActiveRun();
            return true;
        }

        private void ApplyUsableEffects(UsableItemDefinition usable, bool consumeAfterUse)
        {
            var healAmount = 0;
            foreach (var effect in usable.Effects)
            {
                switch (effect.Kind)
                {
                    case RewardEffectKind.Heal:
                        healAmount += Mathf.Max(0, effect.IntValue);
                        break;
                    case RewardEffectKind.ProjectileDamageBonus:
                    case RewardEffectKind.MeleeDamageBonus:
                    case RewardEffectKind.RangedDamageBonus:
                        playerController?.GetComponent<PlayerWeaponController>()?.ApplyTemporaryDamageBonus(Mathf.Max(1, effect.IntValue), 8f);
                        break;
                    case RewardEffectKind.MoveSpeedBonus:
                        playerController?.GetComponent<PlayerMovementController>()?.ApplyTemporarySpeedBonus(Mathf.Max(0f, effect.FloatValue), 8f);
                        break;
                    case RewardEffectKind.MaxStaminaBonus:
                        playerRunBuild.RegenerateStamina(999f);
                        break;
                    case RewardEffectKind.Coins:
                        runEconomy.ApplyReward(new RewardGrant($"usable_{usable.ItemId}_{Time.frameCount}", usable.ItemId, usable.DisplayName, RewardKind.Currency, 0, Mathf.Max(0, effect.IntValue), Array.Empty<RewardEffect>()));
                        break;
                }
            }

            if (usable.ItemId == "echo_burst")
            {
                ApplyEchoBurstDamage();
            }

            ApplyRunStatsToPlayer(healAmount);
            if (consumeAfterUse)
            {
                playerRunBuild.Equipment.EquipConsumableCard(string.Empty);
            }
        }

        private void ApplyEchoBurstDamage()
        {
            if (roomCombatController == null || playerController == null)
            {
                return;
            }

            var center = playerController.transform.localPosition;
            foreach (var enemy in roomCombatController.Enemies)
            {
                if (enemy == null || !enemy.IsAlive || Vector3.Distance(Flat(enemy.transform.localPosition), Flat(center)) > 2.15f)
                {
                    continue;
                }

                DamageSystem.ApplyDamage(enemy.Health, new DamageRequest(2, playerController.gameObject));
            }
        }

        private bool CanAfford(HubShopOffer offer)
        {
            return offer.PriceCurrency == ShopPriceCurrency.Coins
                ? runEconomy.RunCoins >= offer.Price
                : runEconomy.RunSouls >= offer.Price;
        }

        private string NeedCurrencyMessage(HubShopOffer offer)
        {
            var current = offer.PriceCurrency == ShopPriceCurrency.Coins ? runEconomy.RunCoins : runEconomy.RunSouls;
            var currency = offer.PriceCurrency == ShopPriceCurrency.Coins ? "coins" : "souls";
            return $"Need {offer.Price - current} more {currency}";
        }

        private static bool GrantHasHeal(RewardGrant grant)
        {
            return grant.RewardKind == RewardKind.Heal ||
                   (grant.Effects != null && grant.Effects.Any(effect => effect.Kind == RewardEffectKind.Heal));
        }

        private static RewardGrant WithoutHealEffects(RewardGrant grant)
        {
            return new RewardGrant(
                grant.RoomId,
                grant.RewardId,
                grant.DisplayName,
                grant.RewardKind == RewardKind.Heal ? RewardKind.Currency : grant.RewardKind,
                grant.Souls,
                grant.Coins,
                grant.Effects?.Where(effect => effect.Kind != RewardEffectKind.Heal) ?? Array.Empty<RewardEffect>());
        }

        private static string CompactChallengeRules(string rules)
        {
            if (string.IsNullOrWhiteSpace(rules))
            {
                return "Fixed seed.";
            }

            rules = rules.Replace("\n", " ");
            return rules.Length <= 64 ? rules : rules.Substring(0, 61) + "...";
        }

        private static string FormatElapsed(float seconds)
        {
            var safeSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{safeSeconds / 60:00}:{safeSeconds % 60:00}";
        }

        private string ActiveItemSummary()
        {
            return ActiveItemSummary(playerRunBuild);
        }

        private string ActiveItemSummary(PlayerRunBuild build)
        {
            var itemId = build?.Equipment.ActiveItemId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return "None";
            }

            var displayName = usableItemCatalog != null && usableItemCatalog.TryGet(itemId, out var item) ? item.DisplayName : itemId;
            return $"{displayName} ({build.Equipment.ActiveItemCharges}/3)";
        }

        private string CardSummary()
        {
            return CardSummary(playerRunBuild);
        }

        private string CardSummary(PlayerRunBuild build)
        {
            var cardId = build?.Equipment.ConsumableCardId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return "None";
            }

            return usableItemCatalog != null && usableItemCatalog.TryGet(cardId, out var card) ? card.DisplayName : cardId;
        }

        private string ResolveRewardName(RewardKind kind, string id)
        {
            return RewardPresentationResolver.ResolveName(kind, id, weaponCatalog, armorCatalog, usableItemCatalog, ActiveRewardPoolsForSynergies());
        }

        private string CharacterDisplayName(string characterId)
        {
            return characterCatalog != null && characterCatalog.TryGetCharacter(characterId, out var character)
                ? character.DisplayName
                : string.IsNullOrWhiteSpace(characterId) ? "Balanced" : characterId;
        }

        private void ShowPickupReveal(RewardGrant grant, ReplacementPickupState replacement)
        {
            if (grant.IsEmpty)
            {
                return;
            }

            var replacementText = replacement == null
                ? string.Empty
                : $"Dropped old: {replacement.DisplayName}";
            latestPickupReveal = RewardPresentationResolver.CreateReveal(
                ++pickupRevealSequence,
                grant,
                runEconomy,
                weaponCatalog,
                armorCatalog,
                usableItemCatalog,
                ActiveRewardPoolsForSynergies(),
                replacementText);
        }

        private void ShowStatusReveal(string title, string message, Color color)
        {
            latestPickupReveal = PickupRevealModel.Message(++pickupRevealSequence, title, message, color);
        }

        private Vector3 CurrentReplacementDropPosition()
        {
            if (playerController == null)
            {
                return Vector3.zero;
            }

            var basePosition = playerController.transform.localPosition + new Vector3(0.75f, 0.35f, 0.35f);
            return new Vector3(basePosition.x, 0.35f, basePosition.z);
        }

        private string CurrentReplacementContextId()
        {
            return IsInInterBranchHub ? HubReplacementContextId : State?.CurrentRoomId.Value ?? BranchRoomId.Origin.Value;
        }

        private static RewardGrant WithRoomId(RewardGrant grant, string roomId)
        {
            return grant.IsEmpty
                ? grant
                : new RewardGrant(roomId, grant.RewardId, grant.DisplayName, grant.RewardKind, grant.Souls, grant.Coins, grant.Effects, grant.MaxStacks);
        }

        private static PresentationPrefabRole VisualRoleForReplacement(RewardKind kind)
        {
            return kind switch
            {
                RewardKind.Weapon => PresentationPrefabRole.WeaponMelee,
                RewardKind.Armor => PresentationPrefabRole.Armor,
                RewardKind.ActiveItem => PresentationPrefabRole.ActiveItemPickup,
                RewardKind.ConsumableCard => PresentationPrefabRole.ConsumableCardPickup,
                _ => PresentationPrefabRole.RewardPickup
            };
        }

        private string ArmorSummary()
        {
            var armorId = playerRunBuild?.Equipment.ArmorId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(armorId))
            {
                return "None";
            }

            return armorCatalog != null && armorCatalog.TryGetArmor(armorId, out var armor) ? armor.DisplayName : armorId;
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
                challengeId = activeChallenge?.ChallengeId ?? gameSessionState?.SelectedChallengeId ?? string.Empty,
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
                runBuild = CreateCurrentRunBuild(captureRuntimeStamina: true).ToSaveState(),
                droppedReplacementPickups = droppedReplacementPickups.Select(pickup => pickup.ToSaveState()).ToList(),
                roomHazardStates = roomHazardStates.Select(state => new RunRoomHazardStateSave
                {
                    roomId = state.roomId,
                    objectId = state.objectId,
                    objectKind = state.objectKind,
                    isDestroyed = state.isDestroyed,
                    coinDropAmount = state.coinDropAmount,
                    coinCollected = state.coinCollected,
                    localX = state.localX,
                    localY = state.localY,
                    localZ = state.localZ
                }).ToList(),
                roomChestStates = roomChestStates.Select(state => new RunChestStateSave
                {
                    roomId = state.roomId,
                    chestId = state.chestId,
                    kind = state.kind,
                    state = state.state,
                    contentsClaimed = state.contentsClaimed,
                    contentRewardId = state.contentRewardId,
                    contentDisplayName = state.contentDisplayName,
                    contentRewardKind = state.contentRewardKind,
                    contentSouls = state.contentSouls,
                    contentCoins = state.contentCoins,
                    contentEffects = state.contentEffects?.Select(effect => new RunRewardEffectSaveState
                    {
                        kind = effect.kind,
                        intValue = effect.intValue,
                        floatValue = effect.floatValue
                    }).ToList() ?? new List<RunRewardEffectSaveState>(),
                    localX = state.localX,
                    localY = state.localY,
                    localZ = state.localZ
                }).ToList(),
                looseCoinPickups = looseCoinPickupStates.Select(state => new RunCoinPickupSaveState
                {
                    roomId = state.roomId,
                    pickupId = state.pickupId,
                    denomination = state.denomination,
                    value = state.value,
                    isCollected = state.isCollected,
                    localX = state.localX,
                    localY = state.localY,
                    localZ = state.localZ
                }).ToList()
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
            RecordChallengeCompletionIfNeeded();
            if (canPersist && activeChallenge == null)
            {
                MetaProgressionService.CompleteRun(runSaveStore, activeProfileSlotId, runEconomy);
                RefreshSelectedProfileSummary();
                BankedSouls = ProfileSessionHost.Instance?.SelectedProfileContext?.SelectedProfile?.BankedSouls ?? BankedSouls + runEconomy.RunSouls;
            }
            else if (canPersist && runSaveStore != null)
            {
                runSaveStore.ClearActiveRun(activeProfileSlotId);
                RefreshSelectedProfileSummary();
            }

            SaveStatus = "Completed";
        }

        private void RecordChallengeCompletionIfNeeded()
        {
            if (activeChallenge == null || challengeResultStore == null || challengeCompletionRecorded || gameSessionState == null || gameSessionState.ProfileSlotIndex < 0)
            {
                return;
            }

            challengeCompletionRecorded = true;
            var clearSeconds = challengeStartedRealtime > 0f ? Time.realtimeSinceStartup - challengeStartedRealtime : 0f;
            challengeResultStore.CompleteChallengeAttempt(activeProfileSlotId, activeChallenge.ChallengeId, activeChallenge.FixedRunSeed, clearSeconds);
            RefreshSelectedProfileSummary();
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
                legacySave.baseMeleeRangeBonusMeters = preservedSave.baseMeleeRangeBonusMeters;
                legacySave.baseRangedRangeBonusMeters = preservedSave.baseRangedRangeBonusMeters;
                legacySave.currentStamina = preservedSave.currentStamina;
                legacySave.equipment = preservedSave.equipment;
                legacySave.inventory = preservedSave.inventory;
                foreach (var modifier in preservedSave.modifiers ?? new List<PlayerStatModifierSaveState>())
                {
                    if (!IsRuntimeDerivedModifier(modifier.sourceId))
                    {
                        legacySave.modifiers.Add(modifier);
                    }
                }
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

        private PlayerRunBuild CreateAppliedCurrentRunBuild(bool captureRuntimeStamina = false, bool announceActivation = false)
        {
            var build = CreateCurrentRunBuild(captureRuntimeStamina);
            ApplyEquipmentAndSynergyModifiers(build, announceActivation);
            return build;
        }

        private void ApplyEquipmentAndSynergyModifiers(PlayerRunBuild build, bool announceActivation)
        {
            if (build == null)
            {
                activeSynergyId = string.Empty;
                activeSynergyDisplayName = "None";
                return;
            }

            if (!string.IsNullOrWhiteSpace(build.Equipment.ArmorId) && armorCatalog != null && armorCatalog.TryGetArmor(build.Equipment.ArmorId, out var armor))
            {
                build.AddModifier(PlayerStatModifier.FromCharacterStatModifier($"armor:{armor.ArmorId}", armor.StatModifier));
            }

            var character = characterCatalog != null ? characterCatalog.Resolve(build.SelectedCharacterId) : null;
            var synergy = SynergyResolver.ResolveActiveSynergy(
                build,
                character,
                weaponCatalog,
                armorCatalog,
                ActiveRewardPoolsForSynergies(),
                usableItemCatalog,
                synergyCatalog);

            if (synergy.IsActive)
            {
                build.AddModifier(synergy.ToModifier());
                if (announceActivation && !string.Equals(activeSynergyId, synergy.SynergyId, StringComparison.Ordinal))
                {
                    synergyAcquisitionMessage = $"You acquired a {synergy.DisplayName}!";
                    synergyAcquisitionMessageExpiresAt = Time.time + 5f;
                    LastRewardMessage = synergyAcquisitionMessage;
                    ShowStatusReveal(synergy.DisplayName, synergyAcquisitionMessage, new Color(0.45f, 1f, 0.55f, 1f));
                }

                activeSynergyId = synergy.SynergyId;
                activeSynergyDisplayName = synergy.DisplayName;
                return;
            }

            activeSynergyId = string.Empty;
            activeSynergyDisplayName = "None";
        }

        private IEnumerable<RewardPoolDefinition> ActiveRewardPoolsForSynergies()
        {
            yield return standardRewardPool;
            yield return treasureRewardPool;
            yield return bossRewardPool;
            yield return weaponRewardPool;
        }

        private static bool IsRuntimeDerivedModifier(string sourceId)
        {
            return string.Equals(sourceId, "legacy_player_run_stats", StringComparison.Ordinal) ||
                   (!string.IsNullOrWhiteSpace(sourceId) &&
                    (sourceId.StartsWith("armor:", StringComparison.Ordinal) ||
                     sourceId.StartsWith("synergy:", StringComparison.Ordinal)));
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

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= (uint)character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
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
