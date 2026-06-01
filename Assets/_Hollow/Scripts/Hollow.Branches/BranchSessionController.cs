using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Combat;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rewards;
using Hollow.Rooms;
using Hollow.World;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Branches
{
    public sealed class BranchSessionController : MonoBehaviour, IBranchSessionController, IPlayerBuildHudModelProvider
    {
        private const float RewardInteractionRadiusMeters = 1.25f;
        private const float CoinPickupRadiusMeters = 0.55f;
        private const float PortalInteractionRadiusMeters = 1.5f;
        private const float ShopCardInteractionRadiusMeters = 0.85f;
        private const int FinalWorldIndex = 3;
        private const string HubReplacementContextId = "__hub__";
        private const string RuntimeRewardMarkerKind = "spawn_point_roomReward";
        private const string RuntimeChestMarkerKind = "spawn_point_chest";
        private const string RuntimeGoldenChestMarkerKind = "spawn_point_goldenChest";
        private const string RuntimeCorruptedChestMarkerKind = "spawn_point_corruptedChest";
        private const string TransitionCurtainObjectName = "RoomTransitionCurtain";
        private const float PlayerDeathMainMenuDelaySeconds = 1.1f;
        private const int EnemyKillSoulReward = 1;
        public const string PortalEngineDisplayName = "Portal Engine";
        public const string ShipLogDisplayName = "Ship Log";
        public const string ShipLogMessage = "Portal Engine online. Banked Souls are safe aboard ship. Souls collected during a run bank only after final return.";
        public const string CorruptedChestWarningMessage = "Open Corrupted Chest? Gain a rare reward. Lose 1 max HP for this run. Interact again to confirm.";
        public const string CorruptedChestCurseSourcePrefix = "m130_corrupted_chest:";

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
        [SerializeField] private ShieldCatalogDefinition shieldCatalog;
        [SerializeField] private SynergyCatalogDefinition synergyCatalog;
        [SerializeField] private ChallengeCatalogDefinition challengeCatalog;
        [SerializeField] private ShipUpgradeCatalogDefinition shipUpgradeCatalog;
        [SerializeField] private SpaceshipBranchDefinition spaceshipBranchDefinition;
        [SerializeField] private EncounterCatalogDefinition encounterCatalog;
        [SerializeField] private BossCatalogDefinition bossCatalog;
        [SerializeField] private EncounterDirectorProfileDefinition encounterDirectorProfile;
        [SerializeField] private RunFramingCatalogDefinition runFramingCatalog;
        [SerializeField] private DeveloperLabContentDefinition developerLabContentDefinition;
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
        private readonly List<SpaceshipTerminal> currentShipTerminals = new();
        private readonly List<GameObject> currentShipObjects = new();
        private readonly List<string> availableDoorPortIdsScratch = new();
        private readonly Dictionary<string, ImportedRoomRuntimeAsset> spaceshipRoomAssets = new(StringComparer.Ordinal);
        private readonly List<RunRoomHazardStateSave> roomHazardStates = new();
        private readonly List<RunChestStateSave> roomChestStates = new();
        private readonly List<RunCoinPickupSaveState> looseCoinPickupStates = new();
        private readonly HashSet<Renderer> renderersSuppressedForRoomReveal = new();
        private readonly List<Renderer> rendererSuppressionScratch = new();
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
        private string activeBiomeId = RoomBiomeIds.HollowThreshold;
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
        private string armedCorruptedChestId = string.Empty;
        private GameObject currentSoulEaterEncounter;
        private GameObject currentSoulEaterAltar;
        private EnemyRuntimeController currentEscapistTarget;
        private float currentEscapistEscapeEndsAt;
        private bool currentEscapistActive;
        private Coroutine playerDeathRouteCoroutine;
        private Coroutine roomTransitionCoroutine;
        private Coroutine branchPreloadCoroutine;
        private Coroutine branchLoadingCoroutine;
        private readonly BranchRuntimeCache branchRuntimeCache = new();
        private readonly BranchLiveRoomCache branchLiveRoomCache = new();
        private RoomRuntimeRoot sceneRoomRuntimeRoot;
        private GameObject transitionCurtainRoot;
        private BranchLoadingScreenController branchLoadingScreen;
        private bool roomTransitionActive;
        private bool branchLoadingActive;
        private bool transitionCurtainVisible;
        private float transitionCurtainShownRealtime;
        private int transitionRoomReadyFrame = -1;
        private IDisposable roomTransitionLock;
        private IDisposable branchLoadingLock;
        private float roomTransitionLockStartedRealtime;
        private float branchLoadingStartedRealtime;
        private int branchPreloadVersion;
        private string activeBranchEnemyPoolKey = string.Empty;
        private SpaceshipArrivalSnapshot spaceshipArrival;
        private bool spaceshipQuarantineRequired;
        private bool spaceshipQuarantineUnlocked;

        public BranchSessionState State { get; private set; }

        public RuntimeRewardCounter RewardCounter => rewardCounter;

        public RunEconomy RunEconomy => runEconomy;

        public PlayerRunStats PlayerRunStats => playerRunStats;

        public int BankedSouls { get; private set; }

        public AppShellRoute LastLaunchedRoute { get; private set; } = AppShellRoute.Boot;

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

        public string ActiveBiomeId => RoomBiomeIds.Normalize(activeBiomeId);

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

        public ShieldCatalogDefinition ShieldCatalog => shieldCatalog;

        public SynergyCatalogDefinition SynergyCatalog => synergyCatalog;

        public ChallengeCatalogDefinition ChallengeCatalog => challengeCatalog;

        public RunFramingCatalogDefinition RunFramingCatalog => runFramingCatalog;

        public ChallengeDefinition ActiveChallenge => activeChallenge;

        public string ActiveSynergyDisplayName => string.IsNullOrWhiteSpace(activeSynergyDisplayName) ? "None" : activeSynergyDisplayName;

        public string SynergyAcquisitionMessage => Time.time <= synergyAcquisitionMessageExpiresAt ? synergyAcquisitionMessage : string.Empty;

        public PickupRevealModel LatestPickupReveal => latestPickupReveal;

        public int MacroBranchSeed => macroBranchSeed;

        public bool IsDeveloperLab => gameSessionState?.SessionMode == RuntimeSessionMode.DeveloperLab;

        public bool IsSpaceshipHub => gameSessionState?.SessionMode == RuntimeSessionMode.SpaceshipHub;

        public bool SpaceshipQuarantineRequired => spaceshipQuarantineRequired;

        public bool SpaceshipQuarantineUnlocked => spaceshipQuarantineUnlocked;

        public RoomCombatController RoomCombatController => roomCombatController;

        public PlaceholderPlayerController PlayerController => playerController;

        public RoomRuntimeRoot RuntimeRoomRoot => roomRuntimeRoot;

        public string BranchRuntimeCacheSummary => branchRuntimeCache.CreateDebugSummary();

        public BranchRuntimeCacheSnapshot BranchRuntimeCacheSnapshot => branchRuntimeCache.Snapshot();

        public BranchLiveRoomCacheSnapshot BranchLiveRoomCacheSnapshot => branchLiveRoomCache.Snapshot();

        public string ActiveBranchEnemyPoolKey => activeBranchEnemyPoolKey ?? string.Empty;

        public bool RoomTransitionActive => roomTransitionActive;

        public bool BranchLoadingActive => branchLoadingActive || (branchLoadingScreen != null && branchLoadingScreen.IsVisible);

        public string BranchLoadingStage => branchLoadingScreen != null ? branchLoadingScreen.CurrentStage : string.Empty;

        public float BranchLoadingProgress01 => branchLoadingScreen != null ? branchLoadingScreen.CurrentProgress01 : 0f;

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

        public void ConfigureShieldCatalog(ShieldCatalogDefinition nextShieldCatalog)
        {
            shieldCatalog = nextShieldCatalog;
        }

        public void ConfigureSynergyCatalog(SynergyCatalogDefinition nextSynergyCatalog)
        {
            synergyCatalog = nextSynergyCatalog;
        }

        public void ConfigureChallengeCatalog(ChallengeCatalogDefinition nextChallengeCatalog)
        {
            challengeCatalog = nextChallengeCatalog;
        }

        public void ConfigureSpaceshipBranch(SpaceshipBranchDefinition nextSpaceshipBranchDefinition)
        {
            spaceshipBranchDefinition = nextSpaceshipBranchDefinition;
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

        public void ConfigureDeveloperLabContent(DeveloperLabContentDefinition nextDeveloperLabContentDefinition)
        {
            developerLabContentDefinition = nextDeveloperLabContentDefinition;
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
            roomCombatController.EnemyDefeated -= OnEnemyDefeated;
            roomCombatController.EnemyDefeated += OnEnemyDefeated;
            roomCombatController.InteractiveObjectDestroyed -= OnInteractiveObjectDestroyed;
            roomCombatController.InteractiveObjectDestroyed += OnInteractiveObjectDestroyed;

            if (canPersist && gameSessionState.LaunchMode == Hollow.Core.RunLaunchMode.ContinueRun && runSaveStore.TryLoadActiveRun(activeProfileSlotId, out var snapshot))
            {
                if (ShouldDiscardLegacyWorldLoopSnapshotForBeta(
                    snapshot,
                    gameSessionState.SessionMode,
                    HasBetaBranchRuntime(),
                    IsSpaceshipHub,
                    IsDeveloperLab))
                {
                    Debug.LogWarning(
                        $"Discarding incompatible active run snapshot '{snapshot.branchId}' for the beta world loop. Starting a fresh run instead.");
                    runSaveStore.ClearActiveRun(activeProfileSlotId);
                    InitializeFresh(roomAsset, gameSessionState);
                    return;
                }

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
            armedCorruptedChestId = string.Empty;
            latestPickupReveal = PickupRevealModel.Empty;
            pickupRevealSequence = 0;
            activeChallenge = IsDeveloperLab || IsSpaceshipHub ? null : ResolveActiveChallenge();
            ApplySelectedCharacterForFreshRun();
            ApplyChallengeRulesForFreshRun();
            rewardCounter.SetClaimedRewards(0);
            activeRunCompletedOrFailed = false;
            branchDepth = 0;
            challengeStartedRealtime = activeChallenge != null ? Time.realtimeSinceStartup : 0f;
            challengeCompletionRecorded = false;
            runSeed = IsSpaceshipHub
                ? 0
                : IsDeveloperLab
                ? DeveloperLabDefinition.Seed
                : activeChallenge != null
                ? activeChallenge.FixedRunSeed
                : ShouldUseRandomFreshRunSeed() ? RunSeedProvider.CreateSeed() : macroBranchSeed;
            worldIndex = 1;
            worldPhase = IsDeveloperLab || IsSpaceshipHub ? RunWorldPhase.Legacy : IsWorldLoopRuntime() ? RunWorldPhase.Prologue : RunWorldPhase.Legacy;
            activeBiomeId = ResolveBiomeIdForWorld(worldIndex);
            activeHubPortalId = string.Empty;
            currentBranchSeed = IsSpaceshipHub
                ? 0
                : IsDeveloperLab
                ? DeveloperLabDefinition.Seed
                : worldPhase == RunWorldPhase.Prologue
                ? RunSeedDeriver.PrologueBranchSeed(runSeed, worldIndex)
                : runSeed;
            bossKeyState = BossKeyState.None;
            bossDoorUnlocked = false;
            interBranchHubState = InterBranchHubState.Inactive;
            if (IsSpaceshipHub)
            {
                PrepareSpaceshipSession();
                State = BranchSessionState.Create(
                    CreateSpaceshipGraph(),
                    spaceshipArrival.RequiresQuarantine
                        ? new BranchRoomId(SpaceshipBranchDefinition.ArrivalsRoomId)
                        : new BranchRoomId(SpaceshipBranchDefinition.DeparturesRoomId));
            }
            else
            {
                State = BranchSessionState.Create(CreateFreshGraph());
            }
            branchFeaturePlan = CreateBranchFeaturePlanForGraph(State.Graph);
            proceduralRewardPlan = IsSpaceshipHub
                ? ProceduralRewardPlan.Empty
                : CreateRewardPlanForGraph(State.Graph, legacyFallback: false);
            encounterPlan = IsSpaceshipHub
                ? EncounterPlan.Empty
                : CreateEncounterPlanForGraph(State.Graph);
            LoadCurrentBranchWithLoading("Entering Branch", CheckpointActiveRun);
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
            armedCorruptedChestId = string.Empty;
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
            activeBiomeId = string.IsNullOrWhiteSpace(snapshot?.activeBiomeId)
                ? ResolveBiomeIdForWorld(worldIndex)
                : RoomBiomeIds.Normalize(snapshot.activeBiomeId);
            activeHubPortalId = snapshot?.activeHubPortalId ?? string.Empty;
            currentBranchSeed = snapshot != null && snapshot.currentBranchSeed != 0
                ? snapshot.currentBranchSeed
                : worldPhase == RunWorldPhase.Prologue
                    ? RunSeedDeriver.PrologueBranchSeed(runSeed, worldIndex)
                    : snapshot != null && snapshot.branchSeed != 0 ? snapshot.branchSeed : runSeed;
            bossKeyState = Enum.TryParse(snapshot?.bossKeyState, out BossKeyState parsedBossKeyState) ? parsedBossKeyState : BossKeyState.None;
            bossDoorUnlocked = snapshot?.bossDoorUnlocked ?? false;
            State = BranchSessionState.Create(CreateGraphForSnapshot(snapshot));
            branchFeaturePlan = CreateBranchFeaturePlanForGraph(State.Graph);
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
                RestorePlayerHealth(snapshot);
                ApplyRunStatsToPlayer(healAmount: 0);
                suppressCheckpoint = false;
                CheckpointActiveRun();
            }
            else
            {
                LoadCurrentBranchWithLoading("Restoring Branch", () =>
                {
                    RestorePlayerHealth(snapshot);
                    ApplyRunStatsToPlayer(healAmount: 0);
                    suppressCheckpoint = false;
                    CheckpointActiveRun();
                });
            }
        }

        private void OnDestroy()
        {
            if (roomTransitionCoroutine != null)
            {
                StopCoroutine(roomTransitionCoroutine);
                roomTransitionCoroutine = null;
            }

            if (branchPreloadCoroutine != null)
            {
                StopCoroutine(branchPreloadCoroutine);
                branchPreloadCoroutine = null;
            }

            if (branchLoadingCoroutine != null)
            {
                StopCoroutine(branchLoadingCoroutine);
                branchLoadingCoroutine = null;
            }

            roomCombatController?.SetTransitionSuspended(false);
            roomTransitionLock?.Dispose();
            roomTransitionLock = null;
            branchLoadingLock?.Dispose();
            branchLoadingLock = null;
            roomTransitionActive = false;
            branchLoadingActive = false;
            branchLiveRoomCache.DisposeBranchRooms();
            DestroyTransitionCurtain();
            DestroyBranchLoadingScreen();
            EnemyRuntimePool.ClearBranch(activeBranchEnemyPoolKey);
            if (playerDeathRouteCoroutine != null)
            {
                StopCoroutine(playerDeathRouteCoroutine);
                playerDeathRouteCoroutine = null;
            }

            if (roomCombatController != null)
            {
                roomCombatController.RoomCleared -= OnRoomCleared;
                roomCombatController.EnemyDefeated -= OnEnemyDefeated;
                roomCombatController.InteractiveObjectDestroyed -= OnInteractiveObjectDestroyed;
                if (roomCombatController.PlayerHealth != null)
                {
                    roomCombatController.PlayerHealth.Died -= OnPlayerDied;
                }
            }
        }

        private void Update()
        {
            if (State == null || GameplayPauseState.IsPaused || roomTransitionActive || branchLoadingActive)
            {
                return;
            }

            TryCollectCoinPickupsByProximity();
            TickEscapistEncounter();
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
            if (IsSpaceshipHub)
            {
                return TryUseShipTerminal() ||
                       TryTraverseNearestDoor();
            }

            return TryClaimBossKey() ||
                   TryOpenNearestChest() ||
                   TryClaimReward() ||
                   TryClaimHazardCoinPickup() ||
                   TryClaimReplacementPickup() ||
                   TryUseSoulEaterOffer() ||
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

        public string CreateLocationLabel()
        {
            return RunLocationLabelFormatter.Format(new RunLocationLabelContext(
                IsSpaceshipHub,
                IsDeveloperLab,
                IsInInterBranchHub,
                WorldIndex,
                CurrentWorldBranchNumber(),
                worldPhase,
                CreateRunFramingSnapshot(runFramingCatalog)));
        }

        private int CurrentWorldBranchNumber()
        {
            if (worldPhase == RunWorldPhase.Prologue)
            {
                return 1;
            }

            if (!string.IsNullOrWhiteSpace(activeHubPortalId))
            {
                var choice = interBranchHubState.NextBranchChoices
                    .FirstOrDefault(candidate => candidate.ChoiceId == activeHubPortalId);
                if (choice != null)
                {
                    return choice.SlotIndex + 1;
                }
            }

            return Mathf.Max(1, branchDepth + 1);
        }

        private string ResolveBiomeIdForWorld(int nextWorldIndex)
        {
            var definition = RunWorldItineraryService.Resolve(runFramingCatalog, RunSeed, nextWorldIndex <= 0 ? 1 : nextWorldIndex);
            return definition != null ? definition.BiomeId : RoomBiomeIds.HollowThreshold;
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
            if (roomTransitionActive || branchLoadingActive || State == null || connection == null || !State.CurrentRoom.IsCleared)
            {
                return false;
            }

            if (!TryResolveConnectionLock(connection))
            {
                return false;
            }

            roomTransitionCoroutine = StartCoroutine(TraverseStagedRoutine(connection));
            return true;
        }

        private IEnumerator TraverseStagedRoutine(BranchConnection connection)
        {
            roomTransitionActive = true;
            transitionRoomReadyFrame = -1;
            roomTransitionLock?.Dispose();
            roomTransitionLockStartedRealtime = Time.realtimeSinceStartup;
            roomTransitionLock = GameplayTransitionState.AcquireLock();
            roomCombatController?.SetTransitionSuspended(true);
            CancelBranchPreloadForTransition();
            var showBossLoading = ShouldShowBossLoading(connection);
            var bossLoadingStartedRealtime = Time.realtimeSinceStartup;
            try
            {
                if (showBossLoading)
                {
                    M136PerformanceOperationCounters.ReportBossLoadingStart();
                    ShowBranchLoadingScreen("Boss Room", "Preparing boss chamber", 0f);
                    yield return PreloadBossRoomForLoadingRoutine(connection.ToRoomId);
                }

                var previousRoomId = State.CurrentRoomId;
                var preservedHealth = CaptureCurrentPlayerHealth();
                playerRunBuild = CreateCurrentRunBuild(captureRuntimeStamina: true);
                var succeeded = false;
                M136PerformanceOperationSnapshot cacheBefore;
                State.EnterRoom(connection.ToRoomId);
                cacheBefore = M136PerformanceOperationCounters.Snapshot();

                var roomLoadRoutine = branchLiveRoomCache.Count == 0
                    ? LoadCurrentRoomStaged(entryConnection: connection)
                    : ActivateCurrentRoomFromLiveCacheRoutine(entryConnection: connection);
                yield return RunTransitionStage(roomLoadRoutine);

                var cacheAfter = M136PerformanceOperationCounters.Snapshot();
                if (!showBossLoading &&
                    (cacheAfter.BranchRuntimeCacheMisses > cacheBefore.BranchRuntimeCacheMisses ||
                     cacheAfter.RoomDescriptorBuilds > cacheBefore.RoomDescriptorBuilds ||
                     cacheAfter.PresentationMaterialCacheMisses > cacheBefore.PresentationMaterialCacheMisses ||
                     cacheAfter.PresentationPrefabCacheMisses > cacheBefore.PresentationPrefabCacheMisses ||
                     cacheAfter.PresentationBiomeCacheMisses > cacheBefore.PresentationBiomeCacheMisses))
                {
                    M136PerformanceOperationCounters.ReportTraversalColdCacheMiss(
                        "post-load-delta",
                        connection.ToRoomId.Value,
                        $"branch={cacheAfter.BranchRuntimeCacheMisses - cacheBefore.BranchRuntimeCacheMisses},descriptor={cacheAfter.RoomDescriptorBuilds - cacheBefore.RoomDescriptorBuilds},material={cacheAfter.PresentationMaterialCacheMisses - cacheBefore.PresentationMaterialCacheMisses},prefab={cacheAfter.PresentationPrefabCacheMisses - cacheBefore.PresentationPrefabCacheMisses},biome={cacheAfter.PresentationBiomeCacheMisses - cacheBefore.PresentationBiomeCacheMisses}");
                }

                succeeded = roomRuntimeRoot != null && roomRuntimeRoot.HasNavMeshBake;

                if (!succeeded)
                {
                    Debug.LogError($"Room transition to '{connection.ToRoomId.Value}' failed; restoring '{previousRoomId.Value}'.", this);
                    State.EnterRoom(previousRoomId);
                    LoadCurrentRoom();
                }

                M136PerformanceOperationCounters.ReportRoomTransition();
                RestoreCurrentPlayerHealth(preservedHealth);
                CheckpointActiveRun();
            }
            finally
            {
                if (showBossLoading)
                {
                    M136PerformanceOperationCounters.ReportBossLoadingCompletion((Time.realtimeSinceStartup - bossLoadingStartedRealtime) * 1000f);
                    HideBranchLoadingScreen();
                }

                M136PerformanceOperationCounters.ReportTransitionLock(
                    (Time.realtimeSinceStartup - roomTransitionLockStartedRealtime) * 1000f);
                RevealRoomEntryVisuals();
                roomTransitionLock?.Dispose();
                roomTransitionLock = null;
                roomCombatController?.SetTransitionSuspended(false);
                roomTransitionActive = false;
                roomTransitionCoroutine = null;
            }
        }

        public BranchMiniMapModel CreateMiniMapModel()
        {
            return IsSpaceshipHub
                ? new BranchMiniMapModel(State, revealAll: true, room => SpaceshipBranchDefinition.LabelForRoom(room.Id.Value))
                : new BranchMiniMapModel(State, revealAll: false, room => room.Role == BranchRoomRole.SpecialEncounter
                    ? SpecialEncounterResolver.DisplayNameForAssetId(room.RuntimeRoomAssetId)
                    : string.Empty);
        }

        public PlayerBuildHudModel CreatePlayerBuildHudModel()
        {
            var appliedBuild = CreateAppliedCurrentRunBuild(captureRuntimeStamina: true);
            var derived = appliedBuild.DerivedStats;
            var weapon = playerController != null ? playerController.GetComponent<PlayerWeaponController>() : null;
            var defense = playerController != null ? playerController.GetComponent<PlayerDefenseController>() : null;
            var health = roomCombatController?.PlayerHealth;
            var equipmentLoad = EquipmentLoadResolver.Resolve(appliedBuild, weaponCatalog, armorCatalog, shieldCatalog);
            var hudCombatStats = PlayerBuildHudStatCalculator.Calculate(appliedBuild, weaponCatalog, armorCatalog, shieldCatalog);
            var activeWeaponSlot = weapon != null ? weapon.ActiveWeaponSlot : appliedBuild.Equipment.ActiveWeaponSlot;
            var activeWeaponId = ResolveActiveWeaponId(appliedBuild, weapon, activeWeaponSlot);
            return new PlayerBuildHudModel(
                CharacterDisplayName(appliedBuild.SelectedCharacterId),
                health != null ? health.CurrentHealth : Mathf.RoundToInt(derived.MaxHealth),
                Mathf.RoundToInt(derived.MaxHealth),
                derived.Defense,
                derived.Stability + equipmentLoad.ArmorStabilityBonus,
                defense != null && defense.IsGuarding,
                hudCombatStats.MoveSpeedMetersPerSecond,
                derived.Strength,
                weapon != null ? weapon.CurrentStamina : appliedBuild.CurrentStamina,
                weapon != null ? weapon.MaxStamina : derived.MaxStamina,
                derived.StaminaRegenPerSecond,
                derived.MeleeDamageBonus,
                derived.RangedDamageBonus,
                derived.MeleeRangeBonusMeters,
                derived.RangedRangeBonusMeters,
                derived.AttackCooldownMultiplier,
                IsSpaceshipHub ? 0 : runEconomy.RunCoins,
                IsSpaceshipHub ? BankedSouls : runEconomy.RunSouls,
                0,
                !IsSpaceshipHub && bossKeyState == BossKeyState.Held,
                hudCombatStats.MeleeLightDamage,
                hudCombatStats.MeleeHeavyDamage,
                hudCombatStats.MeleeLightAttacksPerSecond,
                hudCombatStats.RangedLightDamage,
                hudCombatStats.RangedHeavyDamage,
                hudCombatStats.RangedLightAttacksPerSecond,
                hudCombatStats.EffectiveRangeMeters,
                hudCombatStats.MoveSpeedMetersPerSecond,
                hudCombatStats.Karma,
                activeWeaponId,
                activeWeaponSlot,
                appliedBuild.Equipment.ActiveItemId,
                appliedBuild.Equipment.ActiveItemCharges,
                ActiveItemMaxCharges(appliedBuild.Equipment.ActiveItemId),
                appliedBuild.Equipment.ConsumableCardId,
                weapon != null ? $"{weapon.ActiveWeaponSlot} - {weapon.ActiveWeaponDisplayName}" : appliedBuild.Equipment.ActiveWeaponSlot.ToString(),
                ResolveRewardName(RewardKind.Weapon, appliedBuild.Equipment.MeleeWeaponId),
                ResolveRewardName(RewardKind.Weapon, appliedBuild.Equipment.RangedWeaponId),
                ResolveRewardName(RewardKind.Armor, appliedBuild.Equipment.ArmorId),
                ResolveRewardName(RewardKind.Shield, appliedBuild.Equipment.ShieldId),
                equipmentLoad,
                ActiveItemSummary(appliedBuild),
                CardSummary(appliedBuild),
                ActiveSynergyDisplayName);
        }

        private string ResolveActiveWeaponId(PlayerRunBuild appliedBuild, PlayerWeaponController weapon, WeaponSlot activeWeaponSlot)
        {
            var rawWeaponId = activeWeaponSlot == WeaponSlot.Ranged
                ? weapon != null ? weapon.RangedWeaponId : appliedBuild.Equipment.RangedWeaponId
                : weapon != null ? weapon.MeleeWeaponId : appliedBuild.Equipment.MeleeWeaponId;
            if (weaponCatalog != null)
            {
                var resolved = weaponCatalog.Resolve(rawWeaponId, activeWeaponSlot);
                if (resolved != null)
                {
                    return resolved.WeaponId;
                }
            }

            var normalized = WeaponIdAliases.Normalize(rawWeaponId);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            return activeWeaponSlot == WeaponSlot.Ranged ? WeaponIdAliases.StarterPistolId : "starter_blade";
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
            M136PerformanceOperationCounters.ReportNormalTraversalRoomRebuildCall();
            DestroyTransientInteractables();
            currentRoomAsset = ResolveCurrentRoomAsset();
            roomRuntimeRoot.BuildFrom(currentRoomAsset);
            if (!IsSpaceshipHub && State.CurrentRoom.Role == BranchRoomRole.Origin)
            {
                roomRuntimeRoot.ClearHazardsAndInteractiveObjects();
            }

            roomRuntimeRoot.ApplyInteractiveObjectState(DestroyedObjectIdsForCurrentRoom());
            var entryBiasDirection = entryConnection != null
                ? BranchTraversalService.EntryInsetDirectionFor(entryConnection.ToDirection)
                : Vector3.zero;
            var playerLocalPosition = entryConnection != null
                ? BranchTraversalService.EntryPositionFor(roomRuntimeRoot, entryConnection)
                : requestedPlayerLocalPosition ?? currentRoomAsset.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            playerLocalPosition = RoomLocalCollision.ResolveNearestOccupiablePosition(
                roomRuntimeRoot,
                playerLocalPosition,
                Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters,
                entryBiasDirection);
            playerController.transform.localPosition = playerLocalPosition;
            State.CurrentRoom.MarkVisited();
            if (!IsSpaceshipHub && State.CurrentRoom.Role == BranchRoomRole.Origin && !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
            }

            if (!IsSpaceshipHub && State.CurrentRoom.Role is BranchRoomRole.Reward or BranchRoomRole.Treasure or BranchRoomRole.Secret or BranchRoomRole.CorruptedChest && !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardPending();
            }

            var currentSpecialKind = CurrentSpecialEncounterKind();
            if (!IsSpaceshipHub &&
                State.CurrentRoom.Role == BranchRoomRole.SpecialEncounter &&
                currentSpecialKind == SpecialEncounterKind.SoulEater &&
                !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardUnavailable();
            }

            roomCombatController.ConfigureBranchEnemyPool(activeBranchEnemyPoolKey);
            roomCombatController.ConfigureInspectionMode(IsDeveloperLab ? InspectionEntityMode.FrozenRuntime : InspectionEntityMode.LiveRuntime, IsDeveloperLab);
            roomCombatController.BeginRoom(
                roomRuntimeRoot,
                playerController,
                State.CurrentRoom.IsCleared || IsSpaceshipHub,
                State.CurrentRoom.Role == BranchRoomRole.Boss
                    ? RoomCombatEncounterKind.Boss
                    : State.CurrentRoom.Role == BranchRoomRole.Wave
                        ? RoomCombatEncounterKind.Wave
                        : RoomCombatEncounterKind.Standard,
                CreateEncounterContextForCurrentRoom());
            ApplyRunStatsToPlayer(healAmount: 0);
            SubscribePlayerDeath();
            RunMeasuredCpuStage(M136CpuStageKind.DoorVisualState, UpdateDoorVisuals);
            SpawnSpaceshipTerminalsForCurrentRoom();
            SpawnSpecialEncounterIfNeeded();
            playerController.transform.localPosition = RoomLocalCollision.ResolveNearestOccupiablePosition(
                roomRuntimeRoot,
                playerController.transform.localPosition,
                Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters,
                entryBiasDirection,
                2.5f);
            RewardApplicationService.RechargeActiveItem(playerRunBuild, usableItemCatalog);
            ApplyRunStatsToPlayer(healAmount: 0);
            VfxPresenter.Play(VfxCueId.DoorUnlock, roomRuntimeRoot.transform.position, roomRuntimeRoot.transform);
            AudioPresenter.Play(AudioCueId.DoorUnlock, roomRuntimeRoot.transform.position);
            if (!IsSpaceshipHub)
            {
                SpawnRewardIfNeeded();
                SpawnHazardCoinPickupsForCurrentRoom();
                SpawnSavedChestsForCurrentRoom();
                SpawnLooseCoinPickupsForCurrentRoom();
                SpawnReplacementPickupsForCurrentContext();
                PopulateDeveloperLabRoomIfNeeded();
                EnsureDebugSpawnMenu();
                SpawnHubPortalIfReady();
            }

            RunMeasuredCpuStage(M136CpuStageKind.BranchPreloadSchedule, ScheduleBranchPreload);
            CheckpointActiveRun();
        }

        private IEnumerator LoadCurrentRoomStaged(Vector3? requestedPlayerLocalPosition = null, BranchConnection entryConnection = null)
        {
            M136PerformanceOperationCounters.ReportNormalTraversalRoomRebuildCall();
            yield return RunTransitionAction(DestroyTransientInteractables);
            currentRoomAsset = ResolveCurrentRoomAsset();
            yield return RunTransitionStage(roomRuntimeRoot.BuildFromStaged(currentRoomAsset, RoomNavMeshRuntimeFallbackMode.RequireCatalogBake, revealOnCommit: false));
            SuppressRoomEntryRenderers();
            if (!IsSpaceshipHub && State.CurrentRoom.Role == BranchRoomRole.Origin)
            {
                yield return RunTransitionAction(roomRuntimeRoot.ClearHazardsAndInteractiveObjects);
                SuppressRoomEntryRenderers();
            }

            yield return RunTransitionAction(() => roomRuntimeRoot.ApplyInteractiveObjectState(DestroyedObjectIdsForCurrentRoom()));
            SuppressRoomEntryRenderers();
            var entryBiasDirection = entryConnection != null
                ? BranchTraversalService.EntryInsetDirectionFor(entryConnection.ToDirection)
                : Vector3.zero;
            var playerLocalPosition = entryConnection != null
                ? BranchTraversalService.EntryPositionFor(roomRuntimeRoot, entryConnection)
                : requestedPlayerLocalPosition ?? currentRoomAsset.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            playerLocalPosition = RoomLocalCollision.ResolveNearestOccupiablePosition(
                roomRuntimeRoot,
                playerLocalPosition,
                Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters,
                entryBiasDirection);
            var revealPlayerLocalPosition = playerLocalPosition;
            State.CurrentRoom.MarkVisited();
            if (!IsSpaceshipHub && State.CurrentRoom.Role == BranchRoomRole.Origin && !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
            }

            if (!IsSpaceshipHub && State.CurrentRoom.Role is BranchRoomRole.Reward or BranchRoomRole.Treasure or BranchRoomRole.Secret or BranchRoomRole.CorruptedChest && !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardPending();
            }

            var currentSpecialKind = CurrentSpecialEncounterKind();
            if (!IsSpaceshipHub &&
                State.CurrentRoom.Role == BranchRoomRole.SpecialEncounter &&
                currentSpecialKind == SpecialEncounterKind.SoulEater &&
                !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardUnavailable();
            }

            yield return WarmTransitionPools();
            SuppressRoomEntryRenderers();
            roomCombatController.ConfigureBranchEnemyPool(activeBranchEnemyPoolKey);
            roomCombatController.ConfigureInspectionMode(IsDeveloperLab ? InspectionEntityMode.FrozenRuntime : InspectionEntityMode.LiveRuntime, IsDeveloperLab);
            yield return RunTransitionStage(roomCombatController.BeginRoomStaged(
                roomRuntimeRoot,
                playerController,
                State.CurrentRoom.IsCleared || IsSpaceshipHub,
                State.CurrentRoom.Role == BranchRoomRole.Boss
                    ? RoomCombatEncounterKind.Boss
                    : State.CurrentRoom.Role == BranchRoomRole.Wave
                        ? RoomCombatEncounterKind.Wave
                        : RoomCombatEncounterKind.Standard,
                CreateEncounterContextForCurrentRoom()));
            SuppressRoomEntryRenderers();
            yield return RunTransitionAction(() => ApplyRunStatsToPlayer(healAmount: 0));
            yield return RunTransitionAction(SubscribePlayerDeath);
            yield return RunTransitionAction(UpdateDoorVisuals, M136CpuStageKind.DoorVisualState);
            SuppressRoomEntryRenderers();
            yield return RunTransitionAction(SpawnSpaceshipTerminalsForCurrentRoom, M136CpuStageKind.SpaceshipTerminalActivation);
            SuppressRoomEntryRenderers();
            yield return RunTransitionAction(SpawnSpecialEncounterIfNeeded, M136CpuStageKind.EnemyRewardInteractableActivation);
            SuppressRoomEntryRenderers();
            revealPlayerLocalPosition = RoomLocalCollision.ResolveNearestOccupiablePosition(
                roomRuntimeRoot,
                revealPlayerLocalPosition,
                Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters,
                entryBiasDirection,
                2.5f);
            RewardApplicationService.RechargeActiveItem(playerRunBuild, usableItemCatalog);
            ApplyRunStatsToPlayer(healAmount: 0);
            SuppressRoomEntryRenderers();
            yield return null;
            if (!IsSpaceshipHub)
            {
                yield return RunTransitionAction(SpawnRewardIfNeeded, M136CpuStageKind.EnemyRewardInteractableActivation);
                SuppressRoomEntryRenderers();
                yield return RunTransitionAction(SpawnHazardCoinPickupsForCurrentRoom, M136CpuStageKind.EnemyRewardInteractableActivation);
                SuppressRoomEntryRenderers();
                yield return RunTransitionAction(SpawnSavedChestsForCurrentRoom, M136CpuStageKind.EnemyRewardInteractableActivation);
                SuppressRoomEntryRenderers();
                yield return RunTransitionAction(SpawnLooseCoinPickupsForCurrentRoom, M136CpuStageKind.EnemyRewardInteractableActivation);
                SuppressRoomEntryRenderers();
                yield return RunTransitionAction(SpawnReplacementPickupsForCurrentContext, M136CpuStageKind.EnemyRewardInteractableActivation);
                SuppressRoomEntryRenderers();
                yield return RunTransitionAction(PopulateDeveloperLabRoomIfNeeded, M136CpuStageKind.EnemyRewardInteractableActivation);
                SuppressRoomEntryRenderers();
                yield return RunTransitionAction(EnsureDebugSpawnMenu, M136CpuStageKind.EnemyRewardInteractableActivation);
                SuppressRoomEntryRenderers();
                yield return RunTransitionAction(SpawnHubPortalIfReady, M136CpuStageKind.EnemyRewardInteractableActivation);
                SuppressRoomEntryRenderers();
            }

            playerController.transform.localPosition = revealPlayerLocalPosition;
            RevealRoomEntryVisuals();
            RunMeasuredCpuStage(M136CpuStageKind.BranchPreloadSchedule, ScheduleBranchPreload);
        }

        private void PrepareCurrentRoomStateForEntry()
        {
            State.CurrentRoom.MarkVisited();
            if (!IsSpaceshipHub && State.CurrentRoom.Role == BranchRoomRole.Origin && !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
            }

            if (!IsSpaceshipHub && State.CurrentRoom.Role is BranchRoomRole.Reward or BranchRoomRole.Treasure or BranchRoomRole.Secret or BranchRoomRole.CorruptedChest && !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardPending();
            }

            var currentSpecialKind = CurrentSpecialEncounterKind();
            if (!IsSpaceshipHub &&
                State.CurrentRoom.Role == BranchRoomRole.SpecialEncounter &&
                currentSpecialKind == SpecialEncounterKind.SoulEater &&
                !State.CurrentRoom.IsCleared)
            {
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardUnavailable();
            }
        }

        private void ApplyLiveRoomBuiltState(BranchRoomState room, RoomRuntimeRoot runtimeRoot)
        {
            if (room == null || runtimeRoot == null)
            {
                return;
            }

            if (!IsSpaceshipHub && room.Role == BranchRoomRole.Origin)
            {
                runtimeRoot.ClearHazardsAndInteractiveObjects();
            }

            runtimeRoot.ApplyInteractiveObjectState(DestroyedObjectIdsForRoom(room.Id.Value));
            UpdateDoorVisualsForRoom(room, runtimeRoot);
        }

        private void LoadCurrentBranchWithLoading(string title, Action afterLoad)
        {
            var nextPoolKey = CreateActiveBranchEnemyPoolKey();
            if (!string.Equals(activeBranchEnemyPoolKey, nextPoolKey, StringComparison.Ordinal))
            {
                EnemyRuntimePool.ClearBranch(activeBranchEnemyPoolKey);
            }

            activeBranchEnemyPoolKey = nextPoolKey;
            roomCombatController?.ConfigureBranchEnemyPool(activeBranchEnemyPoolKey);
            DisposeLiveRoomCache(resetSceneRoot: true);
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                PreloadFullBranchImmediate();
                LoadCurrentRoom();
                afterLoad?.Invoke();
                return;
            }

            if (branchLoadingCoroutine != null)
            {
                StopCoroutine(branchLoadingCoroutine);
                branchLoadingCoroutine = null;
            }

            branchLoadingCoroutine = StartCoroutine(LoadCurrentBranchRoutine(title, afterLoad));
        }

        private void DisposeLiveRoomCache(bool resetSceneRoot)
        {
            branchLiveRoomCache.DisposeBranchRooms();
            if (!resetSceneRoot || sceneRoomRuntimeRoot == null)
            {
                return;
            }

            roomRuntimeRoot = sceneRoomRuntimeRoot;
            sceneRoomRuntimeRoot.gameObject.SetActive(true);
            sceneRoomRuntimeRoot.ClearRuntime();
        }

        private IEnumerator LoadCurrentBranchRoutine(string title, Action afterLoad)
        {
            branchLoadingActive = true;
            branchLoadingStartedRealtime = Time.realtimeSinceStartup;
            branchLoadingLock?.Dispose();
            branchLoadingLock = GameplayTransitionState.AcquireLock();
            roomCombatController?.SetTransitionSuspended(true);
            M136PerformanceOperationCounters.ReportBranchLoadingStart();
            ShowBranchLoadingScreen(title, "Preparing branch", 0f);
            try
            {
                yield return PreloadFullBranchForLoadingRoutine();
                SetBranchLoadingStage("Building branch rooms", 0.86f);
                yield return BuildLiveRoomsForLoadingRoutine();
                SetBranchLoadingStage("Activating current room", 0.94f);
                yield return RunTransitionStage(ActivateCurrentRoomFromLiveCacheRoutine());
                SetBranchLoadingStage("Finalizing", 0.98f);
                afterLoad?.Invoke();
                M136PerformanceOperationCounters.ReportBranchLoadingCompletion((Time.realtimeSinceStartup - branchLoadingStartedRealtime) * 1000f);
            }
            finally
            {
                HideBranchLoadingScreen();
                branchLoadingLock?.Dispose();
                branchLoadingLock = null;
                roomCombatController?.SetTransitionSuspended(false);
                branchLoadingActive = false;
                branchLoadingCoroutine = null;
            }
        }

        private void PreloadFullBranchImmediate()
        {
            var rooms = BuildFullBranchPreloadRoomList();
            for (var index = 0; index < rooms.Count; index++)
            {
                PreloadRoomCaches(rooms[index]);
            }
        }

        private IEnumerator PreloadFullBranchForLoadingRoutine()
        {
            M136PerformanceOperationCounters.ReportBranchPreloadWarmRequest();
            var rooms = BuildFullBranchPreloadRoomList();
            var totalSteps = Mathf.Max(1, rooms.Count + 2);
            for (var index = 0; index < rooms.Count; index++)
            {
                var room = rooms[index];
                SetBranchLoadingStage($"Preparing {RoomLoadingLabel(room)}", Mathf.Clamp01((index + 1) / (float)totalSteps));
                PreloadRoomCaches(room);
                M136PerformanceOperationCounters.ReportFullBranchPreloadRoom();
                yield return null;
            }

            SetBranchLoadingStage("Warming enemies", Mathf.Clamp01((rooms.Count + 1) / (float)totalSteps));
            yield return WarmBranchEnemyPools(rooms);
            SetBranchLoadingStage("Warming combat effects", 0.84f);
            yield return WarmTransitionPools();
            M136PerformanceOperationCounters.ReportBranchPreloadWarmCompletion();
        }

        private IEnumerator BuildLiveRoomsForLoadingRoutine()
        {
            var rooms = BuildFullBranchPreloadRoomList();
            var parent = sceneRoomRuntimeRoot != null && sceneRoomRuntimeRoot.transform.parent != null
                ? sceneRoomRuntimeRoot.transform.parent
                : transform;
            if (sceneRoomRuntimeRoot != null)
            {
                sceneRoomRuntimeRoot.ClearRuntime();
                sceneRoomRuntimeRoot.gameObject.SetActive(false);
            }

            yield return branchLiveRoomCache.BuildBranchRooms(
                rooms,
                ResolveRoomAssetForState,
                parent,
                ApplyLiveRoomBuiltState,
                (roomId, progress) => SetBranchLoadingStage($"Building {roomId}", Mathf.Lerp(0.86f, 0.94f, progress)));
        }

        private IEnumerator ActivateCurrentRoomFromLiveCacheRoutine(Vector3? requestedPlayerLocalPosition = null, BranchConnection entryConnection = null)
        {
            var roomPrepared = false;
            RunMeasuredCpuStage(
                M136CpuStageKind.LiveRoomActivation,
                () => roomPrepared = State != null && branchLiveRoomCache.PrepareRoomForEntry(State.CurrentRoomId));
            if (!roomPrepared)
            {
                M136PerformanceOperationCounters.ReportTraversalColdCacheMiss(
                    "live-room-cache",
                    State.CurrentRoomId.Value,
                    "prepare-room-failed");
                yield return RunTransitionStage(LoadCurrentRoomStaged(requestedPlayerLocalPosition, entryConnection));
                yield break;
            }

            if (!branchLiveRoomCache.TryGetRoom(State.CurrentRoomId, out var liveRoom) || liveRoom?.RuntimeRoot == null)
            {
                M136PerformanceOperationCounters.ReportTraversalColdCacheMiss(
                    "live-room-cache",
                    State.CurrentRoomId.Value,
                    "runtime-root-missing");
                yield return RunTransitionStage(LoadCurrentRoomStaged(requestedPlayerLocalPosition, entryConnection));
                yield break;
            }

            currentRoomAsset = liveRoom.Asset;
            roomRuntimeRoot = liveRoom.RuntimeRoot;
            roomRuntimeRoot.ApplyInteractiveObjectState(DestroyedObjectIdsForCurrentRoom());
            PrepareCurrentRoomStateForEntry();
            RunMeasuredCpuStage(M136CpuStageKind.DoorVisualState, UpdateDoorVisuals);

            var entryBiasDirection = entryConnection != null
                ? BranchTraversalService.EntryInsetDirectionFor(entryConnection.ToDirection)
                : Vector3.zero;
            var playerLocalPosition = entryConnection != null
                ? BranchTraversalService.EntryPositionFor(roomRuntimeRoot, entryConnection)
                : requestedPlayerLocalPosition ?? currentRoomAsset.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            playerLocalPosition = RoomLocalCollision.ResolveNearestOccupiablePosition(
                roomRuntimeRoot,
                playerLocalPosition,
                Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters,
                entryBiasDirection);

            roomCombatController.ConfigureBranchEnemyPool(activeBranchEnemyPoolKey);
            roomCombatController.ConfigureInspectionMode(IsDeveloperLab ? InspectionEntityMode.FrozenRuntime : InspectionEntityMode.LiveRuntime, IsDeveloperLab);
            DestroyTransientInteractables();
            RunMeasuredCpuStage(M136CpuStageKind.LiveRoomActivation, () => branchLiveRoomCache.ActivateRoom(State.CurrentRoomId));
            playerController.transform.localPosition = playerLocalPosition;
            RunMeasuredCpuStage(
                M136CpuStageKind.RoomCombatBegin,
                () => roomCombatController.BeginRoom(
                    roomRuntimeRoot,
                    playerController,
                    State.CurrentRoom.IsCleared || IsSpaceshipHub,
                    State.CurrentRoom.Role == BranchRoomRole.Boss
                        ? RoomCombatEncounterKind.Boss
                        : State.CurrentRoom.Role == BranchRoomRole.Wave
                            ? RoomCombatEncounterKind.Wave
                            : RoomCombatEncounterKind.Standard,
                    CreateEncounterContextForCurrentRoom()));

            playerLocalPosition = RoomLocalCollision.ResolveNearestOccupiablePosition(
                roomRuntimeRoot,
                playerController.transform.localPosition,
                Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters,
                entryBiasDirection,
                2.5f);
            playerController.transform.localPosition = playerLocalPosition;
            ApplyRunStatsToPlayer(healAmount: 0);
            SubscribePlayerDeath();
            RunMeasuredCpuStage(M136CpuStageKind.SpaceshipTerminalActivation, SpawnSpaceshipTerminalsForCurrentRoom);
            RunMeasuredCpuStage(M136CpuStageKind.EnemyRewardInteractableActivation, SpawnSpecialEncounterIfNeeded);
            RewardApplicationService.RechargeActiveItem(playerRunBuild, usableItemCatalog);
            ApplyRunStatsToPlayer(healAmount: 0);

            if (!IsSpaceshipHub)
            {
                RunMeasuredCpuStage(
                    M136CpuStageKind.EnemyRewardInteractableActivation,
                    () =>
                    {
                        SpawnRewardIfNeeded();
                        SpawnHazardCoinPickupsForCurrentRoom();
                        SpawnSavedChestsForCurrentRoom();
                        SpawnLooseCoinPickupsForCurrentRoom();
                        SpawnReplacementPickupsForCurrentContext();
                        PopulateDeveloperLabRoomIfNeeded();
                        EnsureDebugSpawnMenu();
                        SpawnHubPortalIfReady();
                    });
            }

            RunMeasuredCpuStage(M136CpuStageKind.BranchPreloadSchedule, ScheduleBranchPreload);
            yield break;
        }

        private IEnumerator PreloadBossRoomForLoadingRoutine(BranchRoomId roomId)
        {
            if (State?.Graph == null || !State.Graph.TryGetRoom(roomId, out var room))
            {
                yield break;
            }

            SetBranchLoadingStage("Preparing boss room", 0.15f);
            PreloadRoomCaches(room);
            yield return null;
            SetBranchLoadingStage("Warming boss effects", 0.55f);
            yield return WarmTransitionPools();
            SetBranchLoadingStage("Opening boss chamber", 0.85f);
        }

        private List<BranchRoomState> BuildFullBranchPreloadRoomList()
        {
            var rooms = new List<BranchRoomState>();
            if (State?.Graph == null)
            {
                return rooms;
            }

            var distanceMap = branchRuntimeCache.GetOrCreateRoomDistanceMap(State.Graph);
            var currentRoomId = State.CurrentRoomId.Value;
            rooms.AddRange(State.Graph.Rooms
                .OrderBy(room => FullBranchPreloadPriority(room, currentRoomId))
                .ThenBy(room => distanceMap.TryGetValue(room.Id.Value, out var distance) ? distance : int.MaxValue)
                .ThenBy(room => room.Id.Value, StringComparer.Ordinal));
            return rooms;
        }

        private static int FullBranchPreloadPriority(BranchRoomState room, string currentRoomId)
        {
            if (room == null)
            {
                return 99;
            }

            if (string.Equals(room.Id.Value, currentRoomId, StringComparison.Ordinal))
            {
                return 0;
            }

            return room.Role switch
            {
                BranchRoomRole.Boss => 1,
                BranchRoomRole.Wave => 2,
                BranchRoomRole.Reward or BranchRoomRole.Treasure or BranchRoomRole.Secret or BranchRoomRole.CorruptedChest or BranchRoomRole.SpecialEncounter => 3,
                _ => 4
            };
        }

        private IEnumerator WarmBranchEnemyPools(IReadOnlyList<BranchRoomState> rooms)
        {
            if (roomCombatController == null || roomCombatController.EnemyPrefab == null || rooms == null || string.IsNullOrWhiteSpace(activeBranchEnemyPoolKey))
            {
                yield break;
            }

            var counts = BuildBranchEnemyPoolCounts(rooms);
            foreach (var pair in counts)
            {
                var key = EnemyRuntimePool.KeyFor(activeBranchEnemyPoolKey, roomCombatController.EnemyPrefab, pair.Key, roomCombatController.DifficultyTier);
                yield return EnemyRuntimePool.Warm(key, roomCombatController.EnemyPrefab, pair.Value, perFrame: 3);
            }
        }

        private Dictionary<string, int> BuildBranchEnemyPoolCounts(IReadOnlyList<BranchRoomState> rooms)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var catalog = roomCombatController != null && roomCombatController.EnemyCatalog != null
                ? roomCombatController.EnemyCatalog
                : EnemyCatalog.CreateRuntimeDefault();
            for (var roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                var room = rooms[roomIndex];
                var asset = ResolveRoomAssetForState(room);
                var spawnKinds = PlannedSpawnKindsForRoom(room, asset);
                var multiplier = room != null && room.Role == BranchRoomRole.Wave ? 2 : 1;
                for (var index = 0; index < spawnKinds.Count; index++)
                {
                    var definition = EnemyDefinitionResolver.Resolve(catalog, spawnKinds[index], out _);
                    if (definition == null || definition.ArchetypeId == EnemyArchetypeId.Boss || IsBossSpawnKind(definition.SpawnKind))
                    {
                        continue;
                    }

                    var key = definition.SpawnKind;
                    counts.TryGetValue(key, out var count);
                    counts[key] = count + multiplier;
                }
            }

            var keys = counts.Keys.ToArray();
            for (var index = 0; index < keys.Length; index++)
            {
                counts[keys[index]] += 2;
            }

            return counts;
        }

        private IReadOnlyList<string> PlannedSpawnKindsForRoom(BranchRoomState room, ImportedRoomRuntimeAsset asset)
        {
            if (room != null && encounterPlan.TryResolve(room.Id.Value, out var assignment) && assignment.EnemySpawnKinds.Count > 0)
            {
                return assignment.EnemySpawnKinds;
            }

            if (asset?.EnemySpawns == null || asset.EnemySpawns.Count == 0)
            {
                return Array.Empty<string>();
            }

            var kinds = new List<string>(asset.EnemySpawns.Count);
            for (var index = 0; index < asset.EnemySpawns.Count; index++)
            {
                var kind = asset.EnemySpawns[index]?.kind;
                if (!string.IsNullOrWhiteSpace(kind))
                {
                    kinds.Add(kind);
                }
            }

            return kinds;
        }

        private static bool IsBossSpawnKind(string spawnKind)
        {
            return string.Equals(spawnKind, "spawnEnemyBoss", StringComparison.Ordinal);
        }

        private string CreateActiveBranchEnemyPoolKey()
        {
            var graph = State?.Graph;
            return $"branchEnemyPool|run:{RunSeed}|branch:{currentBranchSeed}|world:{worldIndex}|phase:{worldPhase}|depth:{branchDepth}|biome:{ActiveBiomeId}|graph:{graph?.BranchId ?? string.Empty}:{graph?.Seed ?? 0}";
        }

        private static string RoomLoadingLabel(BranchRoomState room)
        {
            return room == null ? "room" : $"{room.Role} {room.Id.Value}";
        }

        private IEnumerator WarmTransitionPools()
        {
            if (roomTransitionActive && !branchLoadingActive)
            {
                M136PerformanceOperationCounters.ReportNormalTraversalWarmCall();
            }

            if (!Application.isPlaying)
            {
                yield break;
            }

            if (roomCombatController != null)
            {
                if (roomCombatController.ProjectilePrefab != null)
                {
                    yield return HollowRuntimePool.WarmPrefabPool(roomCombatController.ProjectilePrefab, 24, 4);
                    yield return HollowRuntimePool.WarmPrefabPool(roomCombatController.ProjectilePrefab, 48, 4);
                }

                yield return HollowRuntimePool.WarmGeneratedPool("MeleeSwipe", 8, () => new GameObject("MeleeSwipe", typeof(MeshFilter), typeof(MeshRenderer)), 2);
            }

            if (rewardPickupPrefab != null)
            {
                yield return HollowRuntimePool.WarmPrefabPool(rewardPickupPrefab, 16, 4);
            }

            if (bossKeyPickupPrefab != null)
            {
                yield return HollowRuntimePool.WarmPrefabPool(bossKeyPickupPrefab, 4, 2);
            }

            if (nextBranchPortalPrefab != null)
            {
                yield return HollowRuntimePool.WarmPrefabPool(nextBranchPortalPrefab, 2, 1);
            }

            yield return HollowRuntimePool.WarmPrimitivePool("VFX.DoorUnlock.Fallback", PrimitiveType.Sphere, 16, 4);
            yield return HollowRuntimePool.WarmPrimitivePool("EnemyProjectile.Fallback", PrimitiveType.Sphere, 48, 4);
            yield return HollowRuntimePool.WarmPrimitivePool("EnemyProjectile.Boss.Fallback", PrimitiveType.Sphere, 24, 4);
            yield return HollowRuntimePool.WarmPrimitivePool("Pickup.Coin.Fallback", PrimitiveType.Sphere, 16, 4);
            yield return HollowRuntimePool.WarmPrimitivePool($"Pickup.{MaterialRole.RewardPickup}.Fallback", PrimitiveType.Sphere, 16, 4);
            yield return HollowRuntimePool.WarmPrimitivePool($"Pickup.{MaterialRole.BossKeyPickup}.Fallback", PrimitiveType.Cube, 4, 2);
            yield return HollowRuntimePool.WarmPrimitivePool($"Pickup.{MaterialRole.HazardCoinDrop}.Fallback", PrimitiveType.Sphere, 16, 4);
            yield return HollowRuntimePool.WarmPrimitivePool($"Pickup.{MaterialRole.CoinCopper}.Fallback", PrimitiveType.Sphere, 16, 4);
            yield return HollowRuntimePool.WarmPrimitivePool($"Pickup.{MaterialRole.CoinSilver}.Fallback", PrimitiveType.Sphere, 16, 4);
            yield return HollowRuntimePool.WarmPrimitivePool($"Pickup.{MaterialRole.CoinGold}.Fallback", PrimitiveType.Sphere, 16, 4);
            yield return HollowRuntimePool.WarmGeneratedPool("Audio.DoorUnlock", 12, () => new GameObject("Audio.DoorUnlock", typeof(AudioSource)), 4);
        }

        private void ScheduleBranchPreload()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || State == null)
            {
                return;
            }

            if (HasCompleteBranchLiveRoomCache())
            {
                return;
            }

            branchPreloadVersion++;
            if (branchPreloadCoroutine != null)
            {
                StopCoroutine(branchPreloadCoroutine);
                branchPreloadCoroutine = null;
            }

            branchPreloadCoroutine = StartCoroutine(PreloadBranchCandidatesRoutine(branchPreloadVersion));
        }

        private bool HasCompleteBranchLiveRoomCache()
        {
            return State?.Graph != null &&
                   branchLiveRoomCache.Count > 0 &&
                   branchLiveRoomCache.Count >= State.Graph.Rooms.Count;
        }

        private void CancelBranchPreloadForTransition()
        {
            branchPreloadVersion++;
            if (branchPreloadCoroutine == null)
            {
                return;
            }

            StopCoroutine(branchPreloadCoroutine);
            branchPreloadCoroutine = null;
            M136PerformanceOperationCounters.ReportBranchPreloadSkippedStale();
        }

        private void SuppressRoomEntryRenderers()
        {
            SuppressRenderersForRoomReveal(currentRewardPickup != null ? currentRewardPickup.gameObject : null);
            SuppressRenderersForRoomReveal(currentHubPortal != null ? currentHubPortal.gameObject : null);
            SuppressRenderersForRoomReveal(currentBossKeyPickup != null ? currentBossKeyPickup.gameObject : null);
            SuppressRenderersForRoomReveal(currentHubShop != null ? currentHubShop.gameObject : null);
            SuppressRenderersForRoomReveal(currentReplacementPickups);
            SuppressRenderersForRoomReveal(currentHazardCoinPickups);
            SuppressRenderersForRoomReveal(currentRoomChests);
            SuppressRenderersForRoomReveal(currentCoinPickups);
            SuppressRenderersForRoomReveal(currentNextBranchPortals);
            SuppressRenderersForRoomReveal(currentShipTerminals);
            for (var index = 0; index < currentShipObjects.Count; index++)
            {
                SuppressRenderersForRoomReveal(currentShipObjects[index]);
            }
        }

        private void RevealRoomEntryVisuals()
        {
            roomRuntimeRoot?.CommitPendingStagedBuildForReveal();
            roomCombatController?.ActivateStagedEnemiesForReveal();
            foreach (var renderer in renderersSuppressedForRoomReveal)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            renderersSuppressedForRoomReveal.Clear();
        }

        private void SuppressRenderersForRoomReveal<T>(IReadOnlyList<T> components) where T : Component
        {
            if (components == null)
            {
                return;
            }

            for (var index = 0; index < components.Count; index++)
            {
                var component = components[index];
                SuppressRenderersForRoomReveal(component != null ? component.gameObject : null);
            }
        }

        private void SuppressRenderersForRoomReveal(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            rendererSuppressionScratch.Clear();
            root.GetComponentsInChildren<Renderer>(true, rendererSuppressionScratch);
            for (var index = 0; index < rendererSuppressionScratch.Count; index++)
            {
                var renderer = rendererSuppressionScratch[index];
                if (renderer == null ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                renderer.enabled = false;
                renderersSuppressedForRoomReveal.Add(renderer);
            }

            rendererSuppressionScratch.Clear();
        }

        private IEnumerator PreloadBranchCandidatesRoutine(int version)
        {
            M136PerformanceOperationCounters.ReportBranchPreloadWarmRequest();
            var rooms = BuildPreloadRoomList();
            for (var index = 0; index < rooms.Count; index++)
            {
                if (version != branchPreloadVersion)
                {
                    M136PerformanceOperationCounters.ReportBranchPreloadSkippedStale();
                    yield break;
                }

                PreloadRoomCaches(rooms[index]);
                yield return null;
            }

            if (version != branchPreloadVersion)
            {
                M136PerformanceOperationCounters.ReportBranchPreloadSkippedStale();
                yield break;
            }

            yield return WarmTransitionPools();
            if (version == branchPreloadVersion)
            {
                M136PerformanceOperationCounters.ReportBranchPreloadWarmCompletion();
            }
            else
            {
                M136PerformanceOperationCounters.ReportBranchPreloadSkippedStale();
            }

            branchPreloadCoroutine = null;
        }

        private List<BranchRoomState> BuildPreloadRoomList()
        {
            return BranchPreloadPlanner.BuildPreloadRoomList(State, branchRuntimeCache.Policy.MaxPredictivePreloadRooms);
        }

        private void PreloadRoomCaches(BranchRoomState room)
        {
            var asset = ResolveRoomAssetForState(room);
            if (asset == null)
            {
                return;
            }

            RoomRuntimeDescriptorCache.GetOrCreate(asset);
            RoomBiomePresentationResolver.Prewarm(asset.BiomeId);
            var catalog = RoomNavMeshCatalogDefinition.LoadDefault();
            if (catalog != null)
            {
                catalog.TryGetNavMeshData(asset.Id, out _, out _);
            }

            if (room != null && encounterPlan.TryResolve(room.Id.Value, out var assignment))
            {
                PrewarmEncounterPresentation(assignment);
            }
        }

        private static void PrewarmEncounterPresentation(RoomEncounterAssignment assignment)
        {
            if (assignment == null || assignment.EnemySpawnKinds == null)
            {
                return;
            }

            for (var index = 0; index < assignment.EnemySpawnKinds.Count; index++)
            {
                var role = EnemyPresentationRoleForSpawnKind(assignment.EnemySpawnKinds[index]);
                PresentationPrefabResolver.Resolve(role);
                MaterialResolver.Resolve(EnemyMaterialRoleForSpawnKind(assignment.EnemySpawnKinds[index]));
            }
        }

        private static PresentationPrefabRole EnemyPresentationRoleForSpawnKind(string spawnKind)
        {
            return spawnKind switch
            {
                "spawnEnemyFlying" => PresentationPrefabRole.EnemyFlying,
                "spawnEnemyFast" => PresentationPrefabRole.EnemyFast,
                "spawnEnemyHeavy" => PresentationPrefabRole.EnemyHeavy,
                "spawnEnemyCharger" => PresentationPrefabRole.EnemyCharger,
                "spawnEnemyTurret" => PresentationPrefabRole.EnemyTurret,
                "spawnEnemySplitter" => PresentationPrefabRole.EnemySplitter,
                "spawnEnemyRat" => PresentationPrefabRole.EnemyRat,
                "spawnEnemySpider" => PresentationPrefabRole.EnemySpider,
                "spawnEnemyHollowBird" => PresentationPrefabRole.EnemyHollowBird,
                "spawnEnemyHollowBeast" => PresentationPrefabRole.EnemyHollowBeast,
                "spawnEnemySkeletonSword" => PresentationPrefabRole.EnemySkeletonSword,
                "spawnEnemySkeletonSpear" => PresentationPrefabRole.EnemySkeletonSpear,
                "spawnEnemyKnight" => PresentationPrefabRole.EnemyKnight,
                "spawnEnemyGiant" => PresentationPrefabRole.EnemyGiant,
                "spawnEnemyHollowArcher" => PresentationPrefabRole.EnemyHollowArcher,
                "spawnEnemyPowderGunner" => PresentationPrefabRole.EnemyPowderGunner,
                "spawnEnemyKnifeThrower" => PresentationPrefabRole.EnemyKnifeThrower,
                "spawnEnemyRepeaterTurret" => PresentationPrefabRole.EnemyRepeaterTurret,
                "spawnEnemyClockworkSentry" => PresentationPrefabRole.EnemyClockworkSentry,
                "spawnEnemyStarforgedOctantSentry" => PresentationPrefabRole.EnemyStarforgedOctantSentry,
                "spawnEnemyCrimsonRailSpider" => PresentationPrefabRole.EnemyCrimsonRailSpider,
                "spawnEnemyAzureMinigunTurret" => PresentationPrefabRole.EnemyAzureMinigunTurret,
                "spawnEnemyHollowAcolyte" => PresentationPrefabRole.EnemyHollowAcolyte,
                "spawnEnemyWraith" => PresentationPrefabRole.EnemyWraith,
                "spawnEnemySoulEater" => PresentationPrefabRole.EnemySoulEater,
                "spawnEnemyCurseBinder" => PresentationPrefabRole.EnemyCurseBinder,
                "spawnEnemyGraveLantern" => PresentationPrefabRole.EnemyGraveLantern,
                "spawnEnemyBoss" => PresentationPrefabRole.EnemyBoss,
                _ => PresentationPrefabRole.EnemyNormal
            };
        }

        private static MaterialRole EnemyMaterialRoleForSpawnKind(string spawnKind)
        {
            return spawnKind switch
            {
                "spawnEnemyFlying" => MaterialRole.EnemyFlying,
                "spawnEnemyFast" => MaterialRole.EnemyFast,
                "spawnEnemyHeavy" => MaterialRole.EnemyHeavy,
                "spawnEnemyCharger" => MaterialRole.EnemyCharger,
                "spawnEnemyTurret" => MaterialRole.EnemyTurret,
                "spawnEnemySplitter" => MaterialRole.EnemySplitter,
                "spawnEnemyRat" => MaterialRole.EnemyRat,
                "spawnEnemySpider" => MaterialRole.EnemySpider,
                "spawnEnemyHollowBird" => MaterialRole.EnemyHollowBird,
                "spawnEnemyHollowBeast" => MaterialRole.EnemyHollowBeast,
                "spawnEnemySkeletonSword" => MaterialRole.EnemySkeletonSword,
                "spawnEnemySkeletonSpear" => MaterialRole.EnemySkeletonSpear,
                "spawnEnemyKnight" => MaterialRole.EnemyKnight,
                "spawnEnemyGiant" => MaterialRole.EnemyGiant,
                "spawnEnemyHollowArcher" => MaterialRole.EnemyHollowArcher,
                "spawnEnemyPowderGunner" => MaterialRole.EnemyPowderGunner,
                "spawnEnemyKnifeThrower" => MaterialRole.EnemyKnifeThrower,
                "spawnEnemyRepeaterTurret" => MaterialRole.EnemyRepeaterTurret,
                "spawnEnemyClockworkSentry" => MaterialRole.EnemyClockworkSentry,
                "spawnEnemyStarforgedOctantSentry" => MaterialRole.EnemyStarforgedOctantSentry,
                "spawnEnemyCrimsonRailSpider" => MaterialRole.EnemyCrimsonRailSpider,
                "spawnEnemyAzureMinigunTurret" => MaterialRole.EnemyAzureMinigunTurret,
                "spawnEnemyHollowAcolyte" => MaterialRole.EnemyHollowAcolyte,
                "spawnEnemyWraith" => MaterialRole.EnemyWraith,
                "spawnEnemySoulEater" => MaterialRole.EnemySoulEater,
                "spawnEnemyCurseBinder" => MaterialRole.EnemyCurseBinder,
                "spawnEnemyGraveLantern" => MaterialRole.EnemyGraveLantern,
                "spawnEnemyBoss" => MaterialRole.EnemyBoss,
                _ => MaterialRole.EnemyNormal
            };
        }

        private static void RunMeasuredCpuStage(M136CpuStageKind stage, Action action)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var started = Time.realtimeSinceStartup;
            var startingGc = GC.GetTotalMemory(false);
            action?.Invoke();
            ReportMeasuredCpuStage(stage, started, startingGc);
#else
            action?.Invoke();
#endif
        }

        private static IEnumerator RunTransitionAction(Action action, M136CpuStageKind? cpuStage = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var started = Time.realtimeSinceStartup;
            var startingGc = GC.GetTotalMemory(false);
            action?.Invoke();
            if (cpuStage.HasValue)
            {
                ReportMeasuredCpuStage(cpuStage.Value, started, startingGc);
            }

            M136PerformanceOperationCounters.ReportTransitionStage(
                (Time.realtimeSinceStartup - started) * 1000f,
                Math.Max(0L, GC.GetTotalMemory(false) - startingGc));
#else
            action?.Invoke();
#endif
            yield return null;
        }

        private static void ReportMeasuredCpuStage(M136CpuStageKind stage, float startedRealtime, long startingGc)
        {
            M136PerformanceOperationCounters.ReportCpuStage(
                stage,
                (Time.realtimeSinceStartup - startedRealtime) * 1000f,
                Math.Max(0L, GC.GetTotalMemory(false) - startingGc));
        }

        private static IEnumerator RunTransitionStage(IEnumerator stage)
        {
            while (stage != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var started = Time.realtimeSinceStartup;
                var startingGc = GC.GetTotalMemory(false);
#endif
                if (!stage.MoveNext())
                {
                    yield break;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                M136PerformanceOperationCounters.ReportTransitionStage(
                    (Time.realtimeSinceStartup - started) * 1000f,
                    Math.Max(0L, GC.GetTotalMemory(false) - startingGc));
#endif
                yield return stage.Current;
            }
        }

        private void ShowTransitionCurtain()
        {
            RemoveOrphanTransitionCurtains();
            if (transitionCurtainRoot != null)
            {
                ConfigureTransitionCurtain(transitionCurtainRoot);
                transitionCurtainRoot.SetActive(true);
                transitionCurtainVisible = true;
                transitionCurtainShownRealtime = Time.realtimeSinceStartup;
                transitionRoomReadyFrame = -1;
                M136PerformanceOperationCounters.ReportTransitionCurtainShow();
                return;
            }

            transitionCurtainRoot = new GameObject(TransitionCurtainObjectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            ConfigureTransitionCurtain(transitionCurtainRoot);
            transitionCurtainVisible = true;
            transitionCurtainShownRealtime = Time.realtimeSinceStartup;
            transitionRoomReadyFrame = -1;
            M136PerformanceOperationCounters.ReportTransitionCurtainShow();
        }

        private void ConfigureTransitionCurtain(GameObject curtain)
        {
            if (curtain == null)
            {
                return;
            }

            curtain.transform.SetParent(transform, false);
            var canvas = curtain.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = curtain.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var rect = curtain.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = curtain.GetComponent<Image>();
            image.color = Color.black;
        }

        private void HideTransitionCurtain()
        {
            if (transitionCurtainRoot != null)
            {
                transitionCurtainRoot.SetActive(false);
            }

            if (!transitionCurtainVisible)
            {
                return;
            }

            var visibleMilliseconds = Mathf.Max(0f, (Time.realtimeSinceStartup - transitionCurtainShownRealtime) * 1000f);
            var framesAfterReady = transitionRoomReadyFrame >= 0
                ? Mathf.Max(0, Time.frameCount - transitionRoomReadyFrame)
                : 0;
            M136PerformanceOperationCounters.ReportTransitionCurtainHide(visibleMilliseconds, framesAfterReady);
            transitionCurtainVisible = false;
            transitionRoomReadyFrame = -1;
        }

        private void DestroyTransitionCurtain()
        {
            HideTransitionCurtain();
            if (transitionCurtainRoot == null)
            {
                return;
            }

            var curtain = transitionCurtainRoot;
            transitionCurtainRoot = null;
            if (Application.isPlaying)
            {
                Destroy(curtain);
            }
            else
            {
                DestroyImmediate(curtain);
            }
        }

        private void MarkTransitionRoomReadyForReveal()
        {
            transitionRoomReadyFrame = Time.frameCount;
        }

        private void RemoveOrphanTransitionCurtains()
        {
            var removed = 0;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas == null ||
                    canvas.gameObject == transitionCurtainRoot ||
                    canvas.gameObject.name != TransitionCurtainObjectName ||
                    canvas.transform.IsChildOf(transform))
                {
                    continue;
                }

                canvas.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(canvas.gameObject);
                }
                else
                {
                    DestroyImmediate(canvas.gameObject);
                }

                removed++;
            }

            M136PerformanceOperationCounters.ReportTransitionOrphanCurtainsRemoved(removed);
        }

        private void ShowBranchLoadingScreen(string title, string stage, float progress01)
        {
            EnsureBranchLoadingScreen();
            branchLoadingScreen?.Show(title, stage, progress01);
        }

        private void SetBranchLoadingStage(string stage, float progress01)
        {
            branchLoadingScreen?.SetStage(stage, progress01);
        }

        private void HideBranchLoadingScreen()
        {
            branchLoadingScreen?.Hide();
        }

        private void EnsureBranchLoadingScreen()
        {
            if (branchLoadingScreen != null)
            {
                return;
            }

            branchLoadingScreen = BranchLoadingScreenController.Create(transform);
        }

        private void DestroyBranchLoadingScreen()
        {
            if (branchLoadingScreen == null)
            {
                return;
            }

            var target = branchLoadingScreen.gameObject;
            branchLoadingScreen = null;
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private bool ShouldShowBossLoading(BranchConnection connection)
        {
            return connection != null &&
                   State?.Graph != null &&
                   State.Graph.TryGetRoom(connection.ToRoomId, out var targetRoom) &&
                   targetRoom != null &&
                   targetRoom.Role == BranchRoomRole.Boss &&
                   Application.isPlaying;
        }

        private void OnRoomCleared(RoomCombatController _)
        {
            if (State == null)
            {
                return;
            }

            State.CurrentRoom.MarkCleared();
            RewardApplicationService.RechargeActiveItem(playerRunBuild, usableItemCatalog);
            if (State.CurrentRoom.Role == BranchRoomRole.SpecialEncounter)
            {
                var specialKind = CurrentSpecialEncounterKind();
                if (specialKind == SpecialEncounterKind.Escapist)
                {
                    currentEscapistActive = false;
                    currentEscapistTarget = null;
                    roomCombatController?.ClearRuntimeStatusOverride();
                    State.CurrentRoom.MarkRewardPending();
                }
                else
                {
                    State.CurrentRoom.MarkRewardUnavailable();
                }

                UpdateDoorVisuals();
                SpawnRewardIfNeeded();
                SpawnHubPortalIfReady();
                ScheduleBranchPreload();
                CheckpointActiveRun();
                return;
            }

            if (State.CurrentRoom.Id != BranchRoomId.Origin)
            {
                State.CurrentRoom.MarkRewardPending();
            }

            UpdateDoorVisuals();
            SpawnRewardIfNeeded();
            SpawnHubPortalIfReady();
            ScheduleBranchPreload();
        }

        private void OnEnemyDefeated(EnemyRuntimeController enemy)
        {
            if (enemy == null ||
                enemy.BossDefinition != null ||
                enemy.ArchetypeId == EnemyArchetypeId.Boss ||
                SpecialEncounterResolver.IsEscapistSpawnKind(enemy.Definition?.SpawnKind))
            {
                return;
            }

            runEconomy.AddSouls(EnemyKillSoulReward);
            CheckpointActiveRun();
        }

        private SpecialEncounterKind CurrentSpecialEncounterKind()
        {
            return State?.CurrentRoom?.Role == BranchRoomRole.SpecialEncounter
                ? SpecialEncounterResolver.KindForRoomAssetId(State.CurrentRoom.RuntimeRoomAssetId)
                : SpecialEncounterKind.None;
        }

        private void SpawnSpecialEncounterIfNeeded()
        {
            if (IsSpaceshipHub || State?.CurrentRoom == null || State.CurrentRoom.Role != BranchRoomRole.SpecialEncounter)
            {
                return;
            }

            var kind = CurrentSpecialEncounterKind();
            if (kind == SpecialEncounterKind.SoulEater)
            {
                SpawnSoulEaterEncounter();
            }
            else if (kind == SpecialEncounterKind.Escapist && !State.CurrentRoom.IsCleared)
            {
                StartEscapistEncounter();
            }
        }

        private void SpawnSoulEaterEncounter()
        {
            if (playerController == null || currentSoulEaterEncounter != null)
            {
                return;
            }

            var spawn = currentRoomAsset?.EnemySpawns?.FirstOrDefault(candidate => candidate != null && candidate.kind == "spawnEnemySoulEater");
            var localPosition = spawn?.position?.ToUnityVector3() ?? new Vector3(0f, 0f, -1.25f);
            var parent = playerController.transform.parent;
            currentSoulEaterEncounter = new GameObject("SoulEaterSpecialEncounter");
            currentSoulEaterEncounter.transform.SetParent(parent, false);
            currentSoulEaterEncounter.transform.localPosition = new Vector3(localPosition.x, 0f, localPosition.z);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.EnemySoulEater, currentSoulEaterEncounter.transform, Vector3.zero, Vector3.one);

            AddShipLabel(
                currentSoulEaterEncounter.transform,
                $"Soul Eater\n{SpecialEncounterResolver.SoulEaterSoulPrice} Souls",
                new Vector3(0f, 1.15f, 0f),
                0.055f,
                new Color(0.86f, 0.66f, 1f, 0.96f));

            currentSoulEaterAltar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentSoulEaterAltar.name = "SoulEaterOfferingAltar";
            currentSoulEaterAltar.transform.SetParent(parent, false);
            currentSoulEaterAltar.transform.localPosition = new Vector3(localPosition.x, 0.13f, localPosition.z + 0.78f);
            currentSoulEaterAltar.transform.localScale = new Vector3(1.15f, 0.26f, 0.72f);
            MaterialResolver.ApplyTo(currentSoulEaterAltar, MaterialRole.SecretDoorDebug);
            if (currentSoulEaterAltar.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "SoulEaterOfferingGlow";
            glow.transform.SetParent(currentSoulEaterAltar.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.82f, 0f);
            glow.transform.localScale = Vector3.one * 0.28f;
            MaterialResolver.ApplyTo(glow, MaterialRole.RewardPickup);
            if (glow.TryGetComponent<Collider>(out var glowCollider))
            {
                glowCollider.enabled = false;
            }
        }

        private void StartEscapistEncounter()
        {
            if (roomCombatController == null)
            {
                return;
            }

            currentEscapistTarget = roomCombatController.Enemies
                .FirstOrDefault(enemy => enemy != null && enemy.IsAlive && SpecialEncounterResolver.IsEscapistSpawnKind(enemy.Definition?.SpawnKind));
            if (currentEscapistTarget == null)
            {
                currentEscapistActive = false;
                roomCombatController.ClearRuntimeStatusOverride();
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardUnavailable();
                LastRewardMessage = "Escapist escaped";
                UpdateDoorVisuals();
                CheckpointActiveRun();
                return;
            }

            currentEscapistActive = true;
            currentEscapistEscapeEndsAt = Time.time + SpecialEncounterResolver.EscapistTimerSeconds;
            roomCombatController.SetRuntimeStatusOverride($"Escapist {Mathf.CeilToInt(SpecialEncounterResolver.EscapistTimerSeconds)}s");
        }

        private void TickEscapistEncounter()
        {
            if (!currentEscapistActive || State?.CurrentRoom == null)
            {
                return;
            }

            if (State.CurrentRoom.Role != BranchRoomRole.SpecialEncounter ||
                CurrentSpecialEncounterKind() != SpecialEncounterKind.Escapist ||
                State.CurrentRoom.IsCleared)
            {
                currentEscapistActive = false;
                currentEscapistTarget = null;
                roomCombatController?.ClearRuntimeStatusOverride();
                return;
            }

            if (currentEscapistTarget == null || !currentEscapistTarget.IsAlive)
            {
                currentEscapistActive = false;
                currentEscapistTarget = null;
                roomCombatController?.ClearRuntimeStatusOverride();
                return;
            }

            var remaining = Mathf.Max(0f, currentEscapistEscapeEndsAt - Time.time);
            roomCombatController?.SetRuntimeStatusOverride($"Escapist {Mathf.CeilToInt(remaining)}s");
            if (remaining > 0f)
            {
                return;
            }

            ResolveEscapistTimeout();
        }

        private void ResolveEscapistTimeout()
        {
            currentEscapistActive = false;
            currentEscapistTarget = null;
            roomCombatController?.ForceClearRoomWithoutReward();
            if (State?.CurrentRoom != null)
            {
                State.CurrentRoom.MarkCleared();
                State.CurrentRoom.MarkRewardUnavailable();
            }

            LastRewardMessage = "Escapist escaped. No reward.";
            ShowStatusReveal("Escapist", LastRewardMessage, new Color(0.56f, 0.9f, 1f, 1f));
            UpdateDoorVisuals();
            SpawnHubPortalIfReady();
            CheckpointActiveRun();
        }

        private void SpawnSpaceshipTerminalsForCurrentRoom()
        {
            if (!IsSpaceshipHub || currentRoomAsset == null || playerController == null)
            {
                return;
            }

            var roomId = State?.CurrentRoomId.Value ?? string.Empty;
            switch (roomId)
            {
                case SpaceshipBranchDefinition.ArrivalsRoomId:
                    AddShipTerminal(
                        SpaceshipTerminalKind.SterilizationConsole,
                        "sterilize",
                        spaceshipQuarantineUnlocked ? "Quarantine Clear" : "Sterilize",
                        ShipTerminalPosition("ship_terminal_sterilization", new Vector3(-2.6f, 0.45f, -1.85f)),
                        MaterialRole.SecretDoorDebug);
                    break;
                case SpaceshipBranchDefinition.MainHallRoomId:
                    AddShipTerminal(
                        SpaceshipTerminalKind.ShipLog,
                        "ship_log",
                        ShipLogDisplayName,
                        ShipTerminalPosition("ship_terminal_log", new Vector3(-2.4f, 0.45f, -1.2f)),
                        MaterialRole.SecretDoorDebug);
                    break;
                case SpaceshipBranchDefinition.DeparturesRoomId:
                    AddShipTerminal(
                        SpaceshipTerminalKind.Departures,
                        "normal_expedition",
                        PortalEngineDisplayName,
                        ShipTerminalPosition("ship_terminal_departures", new Vector3(0f, 0.45f, 0f)),
                        MaterialRole.HubReturnPortal);
                    AddShipBox("ShipTeleportPlatform", new Vector3(1.2f, 0.08f, 0f), new Vector3(1.6f, 0.16f, 1.6f), MaterialRole.HubReturnPortal);
                    break;
                case SpaceshipBranchDefinition.MissionCenterRoomId:
                    SpawnShipChallengeTerminals();
                    break;
                case SpaceshipBranchDefinition.TechnologyLabRoomId:
                    SpawnShipUpgradeTerminals();
                    break;
            }
        }

        private void SpawnShipChallengeTerminals()
        {
            var catalog = challengeCatalog != null ? challengeCatalog : ChallengeCatalogDefinition.CreateRuntimeDefault();
            var challenges = catalog.Challenges;
            if (challenges == null || challenges.Count == 0)
            {
                return;
            }

            var spacing = 1.15f;
            var startX = -(challenges.Count - 1) * spacing * 0.5f;
            for (var index = 0; index < challenges.Count; index++)
            {
                var challenge = challenges[index];
                AddShipTerminal(
                    SpaceshipTerminalKind.MissionChallenge,
                    challenge.ChallengeId,
                    challenge.DisplayName,
                    ShipTerminalPosition($"ship_terminal_mission_{index}", new Vector3(startX + index * spacing, 0.45f, 0f)),
                    MaterialRole.NextBranchPortal);
            }
        }

        private void SpawnShipUpgradeTerminals()
        {
            var catalog = shipUpgradeCatalog != null ? shipUpgradeCatalog : ShipUpgradeCatalogDefinition.CreateRuntimeDefault();
            var upgrades = catalog.Upgrades;
            if (upgrades == null || upgrades.Count == 0)
            {
                return;
            }

            var spacing = 1.25f;
            var startX = -(upgrades.Count - 1) * spacing * 0.5f;
            for (var index = 0; index < upgrades.Count; index++)
            {
                var upgrade = upgrades[index];
                AddShipTerminal(
                    SpaceshipTerminalKind.TechnologyUpgrade,
                    upgrade.UpgradeId,
                    $"{upgrade.DisplayName}\n{upgrade.SoulCost} Banked Souls",
                    ShipTerminalPosition($"ship_terminal_upgrade_{index}", new Vector3(startX + index * spacing, 0.45f, 0f)),
                    MaterialRole.BossKeyPickup);
            }
        }

        private Vector3 ShipTerminalPosition(string markerKind, Vector3 fallback)
        {
            var marker = currentRoomAsset?.ItemSpawns?.FirstOrDefault(spawn => spawn?.kind == markerKind);
            return marker?.position?.ToUnityVector3() ?? fallback;
        }

        private SpaceshipTerminal AddShipTerminal(
            SpaceshipTerminalKind kind,
            string payloadId,
            string displayName,
            Vector3 localPosition,
            MaterialRole materialRole)
        {
            var terminalObject = AddShipBox($"ShipTerminal_{kind}_{payloadId}", localPosition, new Vector3(0.72f, 0.9f, 0.22f), materialRole);
            var terminal = terminalObject.GetComponent<SpaceshipTerminal>() ?? terminalObject.AddComponent<SpaceshipTerminal>();
            terminal.Configure(kind, payloadId, displayName);
            currentShipTerminals.Add(terminal);
            AddShipLabel(terminalObject.transform, displayName, new Vector3(0f, 0.72f, 0f), 0.055f, new Color(0.78f, 0.95f, 1f, 0.95f));
            return terminal;
        }

        private GameObject AddShipBox(string objectName, Vector3 localPosition, Vector3 localScale, MaterialRole materialRole)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = objectName;
            box.transform.SetParent(playerController.transform.parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            MaterialResolver.ApplyTo(box, materialRole);
            if (box.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            currentShipObjects.Add(box);
            return box;
        }

        private static void AddShipLabel(Transform parent, string label, Vector3 localPosition, float scale, Color color)
        {
            var labelObject = new GameObject("Label", typeof(TextMesh));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * scale;
            var mesh = labelObject.GetComponent<TextMesh>();
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 1f;
            mesh.fontSize = 40;
            mesh.text = label;
            mesh.color = color;
        }

        private bool TryUseShipTerminal()
        {
            if (!IsSpaceshipHub || playerController == null || currentShipTerminals.Count == 0)
            {
                return false;
            }

            var terminal = currentShipTerminals
                .Where(candidate => candidate != null)
                .Select(candidate => new
                {
                    Terminal = candidate,
                    Distance = Vector3.Distance(Flat(playerController.transform.localPosition), Flat(candidate.transform.localPosition))
                })
                .Where(candidate => candidate.Distance <= BranchTraversalService.DoorInteractionRadiusMeters)
                .OrderBy(candidate => candidate.Distance)
                .FirstOrDefault()?.Terminal;
            if (terminal == null)
            {
                return false;
            }

            return terminal.TerminalKind switch
            {
                SpaceshipTerminalKind.SterilizationConsole => TryUnlockSpaceshipQuarantine(),
                SpaceshipTerminalKind.Departures => TryLaunchNormalExpeditionFromShip(),
                SpaceshipTerminalKind.MissionChallenge => TryLaunchShipChallenge(terminal.PayloadId),
                SpaceshipTerminalKind.TechnologyUpgrade => TryPurchaseShipUpgrade(terminal.PayloadId),
                SpaceshipTerminalKind.ShipLog => TryReadShipLog(),
                _ => false
            };
        }

        private bool TryReadShipLog()
        {
            LastRewardMessage = ShipLogMessage;
            SaveStatus = ShipLogDisplayName;
            return true;
        }

        private bool TryUnlockSpaceshipQuarantine()
        {
            if (!spaceshipQuarantineRequired)
            {
                spaceshipQuarantineUnlocked = true;
                LastRewardMessage = "Quarantine already clear.";
                return true;
            }

            spaceshipQuarantineUnlocked = true;
            LastRewardMessage = "Sterilization complete.";
            SaveStatus = "Ship";
            UpdateDoorVisuals();
            VfxPresenter.Play(VfxCueId.DoorUnlock, roomRuntimeRoot.transform.position, roomRuntimeRoot.transform);
            AudioPresenter.Play(AudioCueId.DoorUnlock, roomRuntimeRoot.transform.position);
            return true;
        }

        private bool TryLaunchNormalExpeditionFromShip()
        {
            var host = ProfileSessionHost.Instance;
            var context = host?.SelectedProfileContext;
            var selectedProfile = context?.SelectedProfile;
            if (host?.ProfileStore == null || context == null || selectedProfile == null || selectedProfile.IsEmpty)
            {
                LastRewardMessage = "Select a profile before departure.";
                return false;
            }

            var slotId = new ProfileSlotId(selectedProfile.SlotIndex);
            context.SetLaunchMode(RunLaunchMode.NewRun);
            context.SetSelectedChallengeId(string.Empty);
            context.SetDeveloperLabRequested(false);
            context.SetSelectedCharacterId(string.IsNullOrWhiteSpace(context.SelectedCharacterId) ? "balanced" : context.SelectedCharacterId);
            host.RunSaveStore?.ClearActiveRun(slotId);
            var updated = host.ProfileStore.MarkRunStarted(slotId);
            context.UpdateSelectedProfile(updated);
            LoadAppRoute(PlatformPresentationModeResolver.RouteForPlatform(gameSessionState?.PlatformKind ?? HollowPlatformKind.WindowsStandard3D));
            return true;
        }

        private bool TryLaunchShipChallenge(string challengeId)
        {
            var catalog = challengeCatalog != null ? challengeCatalog : ChallengeCatalogDefinition.CreateRuntimeDefault();
            var challenge = catalog.Resolve(challengeId);
            if (challenge == null)
            {
                LastRewardMessage = "Challenge unavailable.";
                return false;
            }

            var host = ProfileSessionHost.Instance;
            var context = host?.SelectedProfileContext;
            var selectedProfile = context?.SelectedProfile;
            if (host?.ProfileStore == null || context == null || selectedProfile == null || selectedProfile.IsEmpty)
            {
                LastRewardMessage = "Select a profile before mission launch.";
                return false;
            }

            var slotId = new ProfileSlotId(selectedProfile.SlotIndex);
            context.SetLaunchMode(RunLaunchMode.NewRun);
            context.SetSelectedChallengeId(challenge.ChallengeId);
            context.SetDeveloperLabRequested(false);
            context.SetSelectedCharacterId(challenge.SelectedCharacterId);
            host.RunSaveStore?.ClearActiveRun(slotId);
            host.ChallengeResultStore?.MarkChallengeAttemptStarted(slotId, challenge.ChallengeId, challenge.FixedRunSeed);
            var updated = host.ProfileStore.MarkLastPlayed(slotId);
            context.UpdateSelectedProfile(updated);
            LoadAppRoute(PlatformPresentationModeResolver.RouteForPlatform(gameSessionState?.PlatformKind ?? HollowPlatformKind.WindowsStandard3D));
            return true;
        }

        private bool TryPurchaseShipUpgrade(string upgradeId)
        {
            var catalog = shipUpgradeCatalog != null ? shipUpgradeCatalog : ShipUpgradeCatalogDefinition.CreateRuntimeDefault();
            if (!catalog.TryGetUpgrade(upgradeId, out var upgrade))
            {
                LastRewardMessage = "Upgrade unavailable.";
                return false;
            }

            var host = ProfileSessionHost.Instance;
            var context = host?.SelectedProfileContext;
            var selectedProfile = context?.SelectedProfile;
            if (host?.ProfileStore is not IShipUpgradeStore store || context == null || selectedProfile == null || selectedProfile.IsEmpty)
            {
                LastRewardMessage = "Profile upgrade store unavailable.";
                return false;
            }

            if (!ShipMetaProgressionService.TryPurchase(store, new ProfileSlotId(selectedProfile.SlotIndex), upgrade, out var updated, out var error))
            {
                LastRewardMessage = error;
                return false;
            }

            context.UpdateSelectedProfile(updated);
            BankedSouls = updated.BankedSouls;
            LastRewardMessage = $"{upgrade.DisplayName} installed.";
            playerRunBuild = new PlayerRunBuild();
            ApplySelectedCharacterForFreshRun();
            ApplyRunStatsToPlayer(Mathf.RoundToInt(playerRunBuild.DerivedStats.MaxHealth));
            DestroyShipTerminals();
            SpawnSpaceshipTerminalsForCurrentRoom();
            return true;
        }

        private void PopulateDeveloperLabRoomIfNeeded()
        {
            if (!IsDeveloperLab || State?.CurrentRoom == null)
            {
                return;
            }

            DeveloperLabRoomPopulator.Populate(
                State.CurrentRoomId,
                roomRuntimeRoot,
                playerController,
                roomCombatController,
                enemyCatalog: roomCombatController != null ? roomCombatController.EnemyCatalog : null,
                bossCatalog: bossCatalog,
                difficultyTier: roomCombatController != null ? roomCombatController.DifficultyTier : null,
                contentDefinition: developerLabContentDefinition);
        }

        private void EnsureDebugSpawnMenu()
        {
            if (gameObject == null || roomRuntimeRoot == null || playerController == null || roomCombatController == null)
            {
                return;
            }

            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                return;
            }

            var menu = GetComponent<DebugSpawnMenuController>() ?? gameObject.AddComponent<DebugSpawnMenuController>();
            menu.Bind(this);
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

        private bool TryUseSoulEaterOffer()
        {
            if (State?.CurrentRoom == null ||
                State.CurrentRoom.Role != BranchRoomRole.SpecialEncounter ||
                CurrentSpecialEncounterKind() != SpecialEncounterKind.SoulEater ||
                playerController == null ||
                currentSoulEaterEncounter == null)
            {
                return false;
            }

            if (Vector3.Distance(Flat(playerController.transform.localPosition), Flat(currentSoulEaterEncounter.transform.localPosition)) > RewardInteractionRadiusMeters)
            {
                return false;
            }

            var offerRoomId = SpecialEncounterResolver.SoulEaterRewardContextId(State.CurrentRoomId.Value);
            if (runEconomy.HasCollectedRoomReward(offerRoomId))
            {
                LastRewardMessage = "Soul Eater offer claimed";
                ShowStatusReveal("Soul Eater", "Offer claimed.", new Color(0.72f, 0.38f, 1f, 1f));
                return true;
            }

            if (runEconomy.RunSouls < SpecialEncounterResolver.SoulEaterSoulPrice)
            {
                LastRewardMessage = $"Need {SpecialEncounterResolver.SoulEaterSoulPrice} Souls";
                ShowStatusReveal("Soul Eater", LastRewardMessage, new Color(0.72f, 0.38f, 1f, 1f));
                return true;
            }

            var grant = SpecialEncounterResolver.ResolveSoulEaterOffer(State.Graph.BranchId, State.Graph.Seed, State.CurrentRoomId.Value);
            if (!runEconomy.SpendSouls(SpecialEncounterResolver.SoulEaterSoulPrice))
            {
                LastRewardMessage = $"Need {SpecialEncounterResolver.SoulEaterSoulPrice} Souls";
                ShowStatusReveal("Soul Eater", LastRewardMessage, new Color(0.72f, 0.38f, 1f, 1f));
                return true;
            }

            var result = ApplyRewardGrant(grant);
            LastRewardMessage = result.Applied
                ? $"Soul Eater: {grant.DisplayName} gained. -{SpecialEncounterResolver.SoulEaterSoulPrice} Souls."
                : "Soul Eater offer unavailable";
            ShowStatusReveal("Soul Eater", LastRewardMessage, new Color(0.72f, 0.38f, 1f, 1f));
            State.CurrentRoom.MarkRewardUnavailable();
            CheckpointActiveRun();
            return true;
        }

        private bool TryOpenNearestChest()
        {
            if (playerController == null || State == null || currentRoomChests.Count == 0)
            {
                return false;
            }

            var playerFlatPosition = Flat(playerController.transform.localPosition);
            RoomChestController nearest = null;
            var nearestDistance = float.PositiveInfinity;
            for (var index = 0; index < currentRoomChests.Count; index++)
            {
                var chest = currentRoomChests[index];
                if (chest == null || chest.IsOpened)
                {
                    continue;
                }

                var distance = Vector3.Distance(playerFlatPosition, Flat(chest.transform.localPosition));
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = chest;
                nearestDistance = distance;
            }

            if (nearest == null ||
                nearestDistance > RewardInteractionRadiusMeters)
            {
                return false;
            }

            var state = FindChestState(nearest.RoomId, nearest.ChestId);
            if (state == null)
            {
                return false;
            }

            if (nearest.Kind == ChestKind.Corrupted &&
                !string.Equals(armedCorruptedChestId, nearest.ChestId, StringComparison.Ordinal))
            {
                armedCorruptedChestId = nearest.ChestId;
                LastRewardMessage = CorruptedChestWarningMessage;
                ShowStatusReveal("Corrupted Chest", CorruptedChestWarningMessage, new Color(0.8f, 0.22f, 0.92f, 1f));
                return true;
            }

            armedCorruptedChestId = string.Empty;
            if (!nearest.Open())
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
            var playerFlatPosition = Flat(playerController.transform.localPosition);
            for (var index = currentCoinPickups.Count - 1; index >= 0; index--)
            {
                var pickup = currentCoinPickups[index];
                if (pickup == null || pickup.IsCollected ||
                    Vector3.Distance(playerFlatPosition, Flat(pickup.transform.localPosition)) > CoinPickupRadiusMeters ||
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

                currentCoinPickups.RemoveAt(index);
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

            var playerFlatPosition = Flat(playerController.transform.localPosition);
            ReplacementPickup nearest = null;
            var nearestDistance = float.PositiveInfinity;
            for (var index = 0; index < currentReplacementPickups.Count; index++)
            {
                var pickup = currentReplacementPickups[index];
                if (pickup == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(playerFlatPosition, Flat(pickup.transform.localPosition));
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = pickup;
                nearestDistance = distance;
            }

            if (nearest == null ||
                nearestDistance > RewardInteractionRadiusMeters ||
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

            var playerFlatPosition = Flat(playerController.transform.localPosition);
            HazardCoinPickup nearest = null;
            var nearestDistance = float.PositiveInfinity;
            for (var index = 0; index < currentHazardCoinPickups.Count; index++)
            {
                var pickup = currentHazardCoinPickups[index];
                if (pickup == null || pickup.IsClaimed)
                {
                    continue;
                }

                var distance = Vector3.Distance(playerFlatPosition, Flat(pickup.transform.localPosition));
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = pickup;
                nearestDistance = distance;
            }

            if (nearest == null ||
                nearestDistance > RewardInteractionRadiusMeters ||
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

            var soulsBanked = CompleteActiveRunIfPersistent();
            ReturnToSpaceshipOrProfileMenu(
                activeChallenge != null ? SpaceshipArrivalReason.ChallengeSuccess : SpaceshipArrivalReason.NormalSuccess,
                soulsBanked,
                activeChallenge?.ChallengeId ?? string.Empty);

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
            DisposeLiveRoomCache(resetSceneRoot: true);
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
                var soulsBanked = CompleteActiveRunIfPersistent();
                ReturnToSpaceshipOrProfileMenu(SpaceshipArrivalReason.NormalSuccess, soulsBanked);

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
            activeBiomeId = ResolveBiomeIdForWorld(worldIndex);
            bossKeyState = BossKeyState.None;
            bossDoorUnlocked = false;
            if (choice.Kind == HubPortalKind.NextWorld)
            {
                interBranchHubState = InterBranchHubState.Inactive;
            }
            HubReturnRequested = false;
            State = BranchSessionState.Create(CreateWorldLoopGraph(currentBranchSeed));
            branchFeaturePlan = CreateBranchFeaturePlanForGraph(State.Graph);
            proceduralRewardPlan = CreateRewardPlanForGraph(State.Graph, legacyFallback: false);
            encounterPlan = CreateEncounterPlanForGraph(State.Graph);
            LoadCurrentBranchWithLoading("Entering Branch", () =>
            {
                if (roomCombatController?.PlayerHealth != null)
                {
                    roomCombatController.PlayerHealth.Restore(CreateAppliedCurrentRunBuild().DerivedStats.MaxHealth, preservedHealth);
                }

                ApplyRunStatsToPlayer(healAmount: 0);
                CheckpointActiveRun();
            });
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
            LastRewardMessage = state.kind == ChestKind.Corrupted.ToString()
                ? "Corrupted chest appeared"
                : state.kind == ChestKind.Golden.ToString() ? "Golden chest appeared" : "Chest appeared";
            CheckpointActiveRun();
        }

        private RunChestStateSave CreateChestState(RewardGrant grant, string chestId)
        {
            var roomId = State.CurrentRoomId.Value;
            var kind = ChestRewardResolver.KindForGrant(grant);
            var contents = ChestRewardResolver.ResolveContents(State.Graph.BranchId, State.Graph.Seed, roomId, kind);
            var contentGrant = contents.RewardGrant;
            var position = CurrentRewardSpawnPosition(preferChestMarker: true, kind);
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
            var role = PresentationRoleForChest(kind);
            var chestObject = new GameObject($"Chest_{kind}_{state.chestId}");
            chestObject.transform.SetParent(playerController.transform.parent, false);
            chestObject.transform.localPosition = new Vector3(state.localX, 0f, state.localZ);
            PresentationPrefabResolver.InstantiateVisual(role, chestObject.transform, Vector3.zero, Vector3.one);
            var chest = chestObject.GetComponent<RoomChestController>() ?? chestObject.AddComponent<RoomChestController>();
            chest.Configure(state.roomId, state.chestId, kind, chestState);
            currentRoomChests.Add(chest);
        }

        private static PresentationPrefabRole PresentationRoleForChest(ChestKind kind)
        {
            return kind switch
            {
                ChestKind.Golden => PresentationPrefabRole.ChestGolden,
                ChestKind.Corrupted => PresentationPrefabRole.ChestCorrupted,
                _ => PresentationPrefabRole.ChestNormal
            };
        }

        private void ApplyChestContents(RunChestStateSave state, Vector3 chestPosition)
        {
            if (state == null)
            {
                return;
            }

            var isCorrupted = string.Equals(state.kind, ChestKind.Corrupted.ToString(), StringComparison.Ordinal);
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

            if (isCorrupted)
            {
                ApplyCorruptedChestCurse(state);
            }
        }

        private void ApplyCorruptedChestCurse(RunChestStateSave state)
        {
            playerRunBuild = CreateCurrentRunBuild(captureRuntimeStamina: true);
            var sourceId = $"{CorruptedChestCurseSourcePrefix}{State?.Graph?.BranchId}:{State?.Graph?.Seed ?? 0}:{state.chestId}";
            if (!playerRunBuild.Modifiers.Any(modifier => string.Equals(modifier.sourceId, sourceId, StringComparison.Ordinal)))
            {
                playerRunBuild.AddModifier(new PlayerStatModifier
                {
                    sourceId = sourceId,
                    maxHealth = -1
                });
            }

            ApplyRunStatsToPlayer(0);
            var rewardName = string.IsNullOrWhiteSpace(state.contentDisplayName) ? "Rare reward" : state.contentDisplayName;
            LastRewardMessage = $"Corrupted Chest: {rewardName} gained. -1 max HP for this run.";
            ShowStatusReveal("Corrupted Chest", $"{rewardName} gained. -1 max HP for this run.", new Color(0.8f, 0.22f, 0.92f, 1f));
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

        private Vector3 CurrentRewardSpawnPosition(bool preferChestMarker, ChestKind chestKind = ChestKind.Normal)
        {
            if (currentRoomAsset?.ItemSpawns != null)
            {
                var marker = preferChestMarker
                    ? PreferredChestMarker(chestKind) ??
                      currentRoomAsset.ItemSpawns.FirstOrDefault(spawn => spawn?.kind == RuntimeChestMarkerKind)
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

        private ImportedSpawnPoint PreferredChestMarker(ChestKind chestKind)
        {
            if (currentRoomAsset?.ItemSpawns == null)
            {
                return null;
            }

            return chestKind switch
            {
                ChestKind.Golden => currentRoomAsset.ItemSpawns.FirstOrDefault(spawn => spawn?.kind == RuntimeGoldenChestMarkerKind),
                ChestKind.Corrupted => currentRoomAsset.ItemSpawns.FirstOrDefault(spawn => spawn?.kind == RuntimeCorruptedChestMarkerKind),
                _ => null
            };
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
            return DestroyedObjectIdsForRoom(State?.CurrentRoomId.Value ?? string.Empty);
        }

        private IEnumerable<string> DestroyedObjectIdsForRoom(string roomId)
        {
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
            UpdateDoorVisualsForRoom(State?.CurrentRoom, roomRuntimeRoot);
        }

        private void UpdateDoorVisualsForRoom(BranchRoomState room, RoomRuntimeRoot runtimeRoot)
        {
            if (State == null || room == null || runtimeRoot == null)
            {
                return;
            }

            availableDoorPortIdsScratch.Clear();
            var anyExplicitPorts = false;
            foreach (var connection in State.Graph.ConnectionsFrom(room.Id))
            {
                if (connection.HasExplicitPorts)
                {
                    anyExplicitPorts = true;
                    break;
                }
            }

            foreach (var port in runtimeRoot.DoorPorts)
            {
                var hasConnection = State.Graph.TryGetConnectionByPort(room.Id, port.Id, out var connectedConnection);
                if (!hasConnection)
                {
                    hasConnection = !anyExplicitPorts &&
                                    State.Graph.TryGetConnection(room.Id, port.Direction, out connectedConnection);
                }

                if (hasConnection)
                {
                    availableDoorPortIdsScratch.Add(port.Id);
                }
            }

            runtimeRoot.ApplyAvailableDoorPorts(availableDoorPortIdsScratch);

            foreach (var port in runtimeRoot.DoorPorts)
            {
                var hasConnection = State.Graph.TryGetConnectionByPort(room.Id, port.Id, out var connectedConnection);
                if (!hasConnection)
                {
                    hasConnection = !anyExplicitPorts &&
                                    State.Graph.TryGetConnection(room.Id, port.Direction, out connectedConnection);
                }

                if (!hasConnection)
                {
                    runtimeRoot.SetDoorVisualStateById(port.Id, RoomDoorVisualState.Unavailable);
                    continue;
                }

                var connectedRoom = State.Graph.TryGetRoom(connectedConnection.ToRoomId, out var connectedRoomState) ? connectedRoomState : null;
                var visualState = RoomDoorVisualState.Locked;
                if (connectedConnection.LockKind == BranchConnectionLockKind.Quarantine && IsSpaceshipHub && !spaceshipQuarantineUnlocked)
                {
                    visualState = RoomDoorVisualState.Locked;
                }
                else if (connectedConnection.LockKind == BranchConnectionLockKind.BossKey && !bossDoorUnlocked)
                {
                    visualState = bossKeyState == BossKeyState.Held && room.IsCleared
                        ? RoomDoorVisualState.Active
                        : RoomDoorVisualState.Locked;
                }
                else if (connectedRoom?.Role == BranchRoomRole.Secret)
                {
                    visualState = room.IsCleared ? RoomDoorVisualState.Active : RoomDoorVisualState.Locked;
                }
                else
                {
                    visualState = room.IsCleared ? RoomDoorVisualState.Cleared : RoomDoorVisualState.Locked;
                }

                runtimeRoot.SetDoorVisualStateById(port.Id, visualState);
            }
        }

        private bool TryResolveConnectionLock(BranchConnection connection)
        {
            if (connection.LockKind == BranchConnectionLockKind.Quarantine)
            {
                if (!IsSpaceshipHub || spaceshipQuarantineUnlocked)
                {
                    return true;
                }

                LastRewardMessage = "Sterilization required.";
                return false;
            }

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

        private void DestroyShipTerminals()
        {
            foreach (var shipObject in currentShipObjects.ToArray())
            {
                if (shipObject != null)
                {
                    DestroyRuntimeObject(shipObject);
                }
            }

            currentShipObjects.Clear();
            currentShipTerminals.Clear();
        }

        private void DestroyTransientInteractables()
        {
            DestroyShipTerminals();
            armedCorruptedChestId = string.Empty;
            currentEscapistActive = false;
            currentEscapistTarget = null;
            roomCombatController?.ClearRuntimeStatusOverride();

            if (currentSoulEaterEncounter != null)
            {
                DestroyRuntimeObject(currentSoulEaterEncounter);
                currentSoulEaterEncounter = null;
            }

            if (currentSoulEaterAltar != null)
            {
                DestroyRuntimeObject(currentSoulEaterAltar);
                currentSoulEaterAltar = null;
            }

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
                var portal = portalObject.GetComponent<NextBranchPortal>() ?? portalObject.AddComponent<NextBranchPortal>();
                portal.Configure(choice, DisplayNameForHubChoice(choice), BiomeIdForHubChoice(choice));
                currentNextBranchPortals.Add(portal);
            }
        }

        private string BiomeIdForHubChoice(NextBranchChoice choice)
        {
            if (choice == null)
            {
                return ActiveBiomeId;
            }

            if (choice.Kind == HubPortalKind.Branch || choice.Kind == HubPortalKind.NextWorld)
            {
                var nextWorld = RunWorldItineraryService.Resolve(runFramingCatalog, RunSeed, choice.WorldIndex);
                return nextWorld != null ? nextWorld.BiomeId : ActiveBiomeId;
            }

            return ActiveBiomeId;
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
                return "Return to Ship";
            }

            return choice.DisplayName;
        }

        private void ResolveReferences()
        {
            roomRuntimeRoot = roomRuntimeRoot != null ? roomRuntimeRoot : GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true) ?? FindAnyObjectByType<RoomRuntimeRoot>();
            sceneRoomRuntimeRoot ??= roomRuntimeRoot;
            playerController = playerController != null ? playerController : GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true) ?? FindAnyObjectByType<PlaceholderPlayerController>();
            roomCombatController = roomCombatController != null ? roomCombatController : GetComponent<RoomCombatController>() ?? FindAnyObjectByType<RoomCombatController>();
            if (roomCombatController != null)
            {
                roomCombatController.ConfigureAutoInitialize(false);
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
            branchContent = branchRuntimeCache.GetOrCreateContent(roomAsset, branchRoomTemplateCatalog, macroBranchSeed, out var error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning($"Branch template catalog import warning: {error}");
            }
        }

        private void PrepareSpaceshipSession()
        {
            spaceshipArrival = SpaceshipArrivalHandoff.TryConsume(out var snapshot)
                ? snapshot
                : new SpaceshipArrivalSnapshot(
                    SpaceshipArrivalReason.DirectProfile,
                    gameSessionState?.PlatformKind ?? HollowPlatformKind.WindowsStandard3D,
                    0,
                    string.Empty);
            spaceshipQuarantineRequired = spaceshipArrival.RequiresQuarantine;
            spaceshipQuarantineUnlocked = !spaceshipQuarantineRequired;
            LastLaunchedRoute = AppShellRoute.Boot;
            SaveStatus = spaceshipQuarantineRequired ? "Ship: Quarantine" : "Ship";
            LastRewardMessage = SpaceshipStatusForArrival(spaceshipArrival);
        }

        private BranchFloorGraph CreateSpaceshipGraph()
        {
            spaceshipRoomAssets.Clear();
            var importError = string.Empty;
            if (spaceshipBranchDefinition == null ||
                !TryImportSpaceshipRooms(spaceshipBranchDefinition, out var importedRooms, out importError))
            {
                if (!string.IsNullOrWhiteSpace(importError))
                {
                    Debug.LogWarning($"Spaceship branch definition import warning: {importError}");
                }

                foreach (var roomId in SpaceshipBranchDefinition.RequiredRoomIds)
                {
                    spaceshipRoomAssets[roomId] = roomAsset;
                }
            }
            else
            {
                foreach (var pair in importedRooms)
                {
                    spaceshipRoomAssets[pair.Key] = pair.Value;
                }
            }

            var graph = new BranchFloorGraph(SpaceshipBranchDefinition.BranchId, 0);
            AddSpaceshipRoom(graph, SpaceshipBranchDefinition.ArrivalsRoomId, new Vector2Int(-1, 0), BranchRoomRole.Origin);
            AddSpaceshipRoom(graph, SpaceshipBranchDefinition.MainHallRoomId, Vector2Int.zero, BranchRoomRole.Combat);
            AddSpaceshipRoom(graph, SpaceshipBranchDefinition.DeparturesRoomId, new Vector2Int(1, 0), BranchRoomRole.Combat);
            AddSpaceshipRoom(graph, SpaceshipBranchDefinition.MissionCenterRoomId, new Vector2Int(0, -1), BranchRoomRole.Combat);
            AddSpaceshipRoom(graph, SpaceshipBranchDefinition.TechnologyLabRoomId, new Vector2Int(0, 1), BranchRoomRole.Combat);

            var arrivals = new BranchRoomId(SpaceshipBranchDefinition.ArrivalsRoomId);
            var main = new BranchRoomId(SpaceshipBranchDefinition.MainHallRoomId);
            graph.AddBidirectionalConnection(
                arrivals,
                main,
                "east",
                "west",
                "east_0",
                "west_0",
                BranchConnectionLockKind.Quarantine);
            graph.AddBidirectionalConnection(main, new BranchRoomId(SpaceshipBranchDefinition.DeparturesRoomId), "east", "west", "east_0", "west_0");
            graph.AddBidirectionalConnection(main, new BranchRoomId(SpaceshipBranchDefinition.MissionCenterRoomId), "north", "south", "north_0", "south_0");
            graph.AddBidirectionalConnection(main, new BranchRoomId(SpaceshipBranchDefinition.TechnologyLabRoomId), "south", "north", "south_0", "north_0");
            return graph;
        }

        private static bool TryImportSpaceshipRooms(
            SpaceshipBranchDefinition definition,
            out IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> rooms,
            out string error)
        {
            var imported = new Dictionary<string, ImportedRoomRuntimeAsset>(StringComparer.Ordinal);
            error = string.Empty;
            foreach (var template in definition.RoomTemplates)
            {
                if (!HollowRuntimeV2Importer.TryImport(template.text, out var room, out var importError))
                {
                    AppendSpaceshipImportError(ref error, $"{template.name}: {importError}");
                    continue;
                }

                if (room == null || string.IsNullOrWhiteSpace(room.Id))
                {
                    AppendSpaceshipImportError(ref error, $"{template.name}: missing canonicalRoomId");
                    continue;
                }

                imported[room.Id] = room;
            }

            foreach (var required in SpaceshipBranchDefinition.RequiredRoomIds)
            {
                if (!imported.ContainsKey(required))
                {
                    AppendSpaceshipImportError(ref error, $"missing ship room '{required}'");
                }
            }

            rooms = imported;
            return string.IsNullOrWhiteSpace(error);
        }

        private static void AppendSpaceshipImportError(ref string error, string next)
        {
            if (string.IsNullOrWhiteSpace(next))
            {
                return;
            }

            error = string.IsNullOrWhiteSpace(error) ? next : $"{error}; {next}";
        }

        private static string SpaceshipStatusForArrival(SpaceshipArrivalSnapshot arrival)
        {
            return arrival.Reason switch
            {
                SpaceshipArrivalReason.NormalSuccess => arrival.SoulsBanked > 0
                    ? $"Arrival confirmed. {arrival.SoulsBanked} souls transferred."
                    : "Arrival confirmed.",
                SpaceshipArrivalReason.NormalDeath => "Emergency retrieval complete. No souls recovered.",
                SpaceshipArrivalReason.ChallengeSuccess => "Challenge complete. Quarantine required.",
                SpaceshipArrivalReason.ChallengeDeath => "Challenge failed. Quarantine required.",
                _ => "Spaceship online."
            };
        }

        private void AddSpaceshipRoom(BranchFloorGraph graph, string roomId, Vector2Int coordinate, BranchRoomRole role)
        {
            var footprint = new RoomInstanceFootprint(coordinate, new[] { coordinate }, new Vector2Int(13, 7));
            var room = new BranchRoomState(
                new BranchRoomId(roomId),
                coordinate,
                new BranchRoomInstanceId(roomId),
                roomId,
                footprint,
                role);
            room.MarkVisited();
            room.MarkCleared();
            room.MarkRewardUnavailable();
            graph.AddRoom(room);
        }

        private BranchFloorGraph CreateFreshGraph()
        {
            var seed = currentBranchSeed == 0 ? macroBranchSeed : currentBranchSeed;
            if (IsDeveloperLab && branchContent != null)
            {
                return branchRuntimeCache.GetOrCreateGraph(
                    BranchGraphCacheKey("developer_lab", DeveloperLabDefinition.Seed, worldIndex),
                    () => DeveloperInspectionBranchBuilder.CreateGraph(branchContent, DeveloperLabDefinition.Seed));
            }

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
                            ? branchRuntimeCache.GetOrCreateGraph(
                                BranchGraphCacheKey("m17_feature", seed, worldIndex),
                                () => BranchGenerator.CreateSeededFeatureBranch(branchContent, branchGenerationSettings, seed, ActiveBiomeId))
                            : branchRuntimeCache.GetOrCreateGraph(
                                BranchGraphCacheKey("m15_seeded_macro", seed, worldIndex),
                                () => BranchGenerator.CreateSeededMacroBranch(branchContent, branchGenerationSettings, seed, ActiveBiomeId));
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning($"Seeded macro branch generation failed. {error.Message}");
                        if (IsWorldLoopRuntime())
                        {
                            try
                            {
                                Debug.LogWarning("World-loop directed branch generation failed; falling back to M20 feature branch instead of legacy reward-only branch.");
                                return CreateM20Graph(seed);
                            }
                            catch (Exception fallbackError)
                            {
                                Debug.LogWarning($"World-loop M20 fallback branch generation also failed; falling back to M14 fixed macro branch. {fallbackError.Message}");
                            }
                        }
                    }
                }

                return branchRuntimeCache.GetOrCreateGraph(
                    BranchGraphCacheKey("m14_macro_fixture", seed == 0 ? branchContent.BranchSeed : seed, worldIndex),
                    () => BranchGenerator.CreateMacroFixtureBranch(
                        branchContent.ResolveRoomPoolForBiome(ActiveBiomeId, out _),
                        seed == 0 ? branchContent.BranchSeed : seed));
            }

            return branchRuntimeCache.GetOrCreateGraph(
                BranchGraphCacheKey("legacy_five_room", 0, worldIndex),
                () => BranchGenerator.CreateFiveRoomCross(roomAsset));
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
            if (IsDeveloperLab)
            {
                return $"Developer Lab: M55 inspection branch | Debug Spawn F10 | Seed {DeveloperLabDefinition.Seed}\n";
            }

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
            return HasBetaBranchRuntime() &&
                   encounterCatalog != null;
        }

        private bool HasBetaBranchRuntime()
        {
            return !IsDeveloperLab &&
                   !IsSpaceshipHub &&
                   branchContent != null &&
                   branchContent.HasMacroFixturePool &&
                   branchGenerationSettings != null;
        }

        public static bool ShouldDiscardLegacyWorldLoopSnapshotForBeta(
            RunSaveSnapshot snapshot,
            RuntimeSessionMode sessionMode,
            bool hasBetaBranchRuntime,
            bool isSpaceshipHub,
            bool isDeveloperLab)
        {
            if (snapshot == null ||
                !hasBetaBranchRuntime ||
                isSpaceshipHub ||
                isDeveloperLab ||
                sessionMode != RuntimeSessionMode.ProfileBacked ||
                !string.IsNullOrWhiteSpace(snapshot.challengeId))
            {
                return false;
            }

            return !string.Equals(snapshot.branchId, BranchGenerator.DirectedEncounterBranchId, StringComparison.Ordinal);
        }

        private bool ShouldEnableCorruptedChestLeaf()
        {
            return !IsDeveloperLab &&
                   !IsSpaceshipHub &&
                   activeChallenge == null &&
                   gameSessionState != null &&
                   gameSessionState.SessionMode == RuntimeSessionMode.ProfileBacked &&
                   branchContent != null &&
                   branchContent.HasMacroFixturePool &&
                   branchGenerationSettings != null;
        }

        private bool ShouldEnableWaveRoomLeaf()
        {
            return ShouldEnableCorruptedChestLeaf();
        }

        private bool ShouldEnableSpecialEncounterLeaf()
        {
            return ShouldEnableCorruptedChestLeaf();
        }

        private string BranchGraphCacheKey(string mode, int seed, int nextWorldIndex)
        {
            return string.Join(
                "|",
                "graph",
                mode ?? string.Empty,
                $"seed:{seed}",
                $"run:{RunSeed}",
                $"world:{(nextWorldIndex <= 0 ? worldIndex : nextWorldIndex)}",
                $"phase:{worldPhase}",
                $"biome:{ActiveBiomeId}",
                $"content:{branchRuntimeCache.ActiveContentKey}",
                $"settings:{BranchRuntimeCache.DefinitionKey(branchGenerationSettings)}",
                $"encounters:{BranchRuntimeCache.DefinitionKey(encounterCatalog)}",
                $"director:{BranchRuntimeCache.DefinitionKey(encounterDirectorProfile)}",
                $"boss:{BranchRuntimeCache.DefinitionKey(bossCatalog)}",
                $"challenge:{activeChallenge?.ChallengeId ?? string.Empty}",
                $"corrupt:{ShouldEnableCorruptedChestLeaf()}",
                $"wave:{ShouldEnableWaveRoomLeaf()}",
                $"special:{ShouldEnableSpecialEncounterLeaf()}",
                $"room:{roomAsset?.Id ?? string.Empty}");
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
                var seed = snapshot.branchSeed == 0 ? branchContent.BranchSeed : snapshot.branchSeed;
                return branchRuntimeCache.GetOrCreateGraph(
                    BranchGraphCacheKey("m19_enemy_encounter_snapshot", seed, worldIndex),
                    () => BranchGenerator.CreateSeededEncounterBranch(
                        branchContent,
                        branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                        seed,
                        ActiveBiomeId));
            }

            if (snapshot != null &&
                snapshot.branchId == BranchGenerator.FeatureBranchId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                var seed = snapshot.branchSeed == 0 ? branchContent.BranchSeed : snapshot.branchSeed;
                return branchRuntimeCache.GetOrCreateGraph(
                    BranchGraphCacheKey("m17_feature_snapshot", seed, worldIndex),
                    () => BranchGenerator.CreateSeededFeatureBranch(
                        branchContent,
                        branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                        seed,
                        ActiveBiomeId));
            }

            if (snapshot != null &&
                snapshot.branchId == BranchGenerator.SeededMacroBranchId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                var seed = snapshot.branchSeed == 0 ? branchContent.BranchSeed : snapshot.branchSeed;
                return branchRuntimeCache.GetOrCreateGraph(
                    BranchGraphCacheKey("m15_seeded_macro_snapshot", seed, worldIndex),
                    () => BranchGenerator.CreateSeededMacroBranch(
                        branchContent,
                        branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                        seed,
                        ActiveBiomeId));
            }

            if (snapshot != null &&
                snapshot.branchId == BranchGenerator.MacroFixtureBranchId &&
                branchContent != null &&
                branchContent.HasMacroFixturePool)
            {
                var seed = snapshot.branchSeed == 0 ? branchContent.BranchSeed : snapshot.branchSeed;
                return branchRuntimeCache.GetOrCreateGraph(
                    BranchGraphCacheKey("m14_macro_fixture_snapshot", seed, worldIndex),
                    () => BranchGenerator.CreateMacroFixtureBranch(
                        branchContent.ResolveRoomPoolForBiome(ActiveBiomeId, out _),
                        seed));
            }

            return branchRuntimeCache.GetOrCreateGraph(
                BranchGraphCacheKey("legacy_five_room_snapshot", snapshot?.branchSeed ?? 0, worldIndex),
                () => BranchGenerator.CreateFiveRoomCross(roomAsset));
        }

        private BranchFloorGraph CreateM20Graph(int seed)
        {
            var resolvedSeed = seed == 0 ? macroBranchSeed : seed;
            return branchRuntimeCache.GetOrCreateGraph(
                BranchGraphCacheKey("m20_features", resolvedSeed, worldIndex),
                () => BranchGenerator.CreateSeededBranchFeatures(
                    branchContent,
                    branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                    resolvedSeed,
                    ActiveBiomeId,
                    ShouldEnableCorruptedChestLeaf(),
                    ShouldEnableWaveRoomLeaf(),
                    ShouldEnableSpecialEncounterLeaf()));
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
            var bossArenaId = selectedBoss != null ? selectedBoss.Arena.arenaId : string.Empty;
            return branchRuntimeCache.GetOrCreateGraph(
                BranchGraphCacheKey($"m46_directed:{bossArenaId}", resolvedSeed, resolvedWorldIndex),
                () => BranchGenerator.CreateDirectedEncounterBranch(
                    branchContent,
                    branchGenerationSettings != null ? branchGenerationSettings : BranchGenerationSettingsDefinition.CreateRuntimeDefault(),
                    encounterDirectorProfile,
                    resolvedWorldIndex,
                    resolvedSeed,
                    bossArenaId,
                    ActiveBiomeId,
                    ShouldEnableCorruptedChestLeaf(),
                    ShouldEnableWaveRoomLeaf(),
                    ShouldEnableSpecialEncounterLeaf()));
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

        private BranchFeaturePlan CreateBranchFeaturePlanForGraph(BranchFloorGraph graph)
        {
            return branchRuntimeCache.GetOrCreateFeaturePlan(
                graph,
                () => BranchFeaturePlan.Create(graph, branchRuntimeCache.GetOrCreateRoomDistanceMap(graph)));
        }

        private ProceduralRewardPlan CreateRewardPlanForGraph(BranchFloorGraph graph, bool legacyFallback)
        {
            if (graph == null || !IsProceduralRewardBranch(graph.BranchId))
            {
                return ProceduralRewardPlan.Empty;
            }

            var cacheKey = $"reward|legacy:{legacyFallback}|{BranchRuntimeCache.GraphSignature(graph)}|standard:{BranchRuntimeCache.DefinitionKey(standardRewardPool)}|treasure:{BranchRuntimeCache.DefinitionKey(treasureRewardPool)}|boss:{BranchRuntimeCache.DefinitionKey(bossRewardPool)}|weapon:{BranchRuntimeCache.DefinitionKey(weaponRewardPool)}";
            return branchRuntimeCache.GetOrCreateRewardPlan(
                cacheKey,
                () => legacyFallback
                    ? ProceduralRewardResolver.CreatePlan(graph)
                    : ProceduralRewardResolver.CreateSeededPlan(graph, standardRewardPool, treasureRewardPool, bossRewardPool, weaponRewardPool));
        }

        private EncounterPlan CreateEncounterPlanForGraph(BranchFloorGraph graph)
        {
            if (graph == null)
            {
                return EncounterPlan.Empty;
            }

            if (graph.BranchId == BranchGenerator.DirectedEncounterBranchId)
            {
                var cacheKey = $"encounter|directed|{BranchRuntimeCache.GraphSignature(graph)}|world:{worldIndex}|encounter:{BranchRuntimeCache.DefinitionKey(encounterCatalog)}|profile:{BranchRuntimeCache.DefinitionKey(encounterDirectorProfile)}|boss:{BranchRuntimeCache.DefinitionKey(bossCatalog)}|pressure:{ChallengeRuleIntValue(ChallengeRuleKind.EncounterPressureBonus)}|spawns:{string.Join(",", ChallengeAllowedNonBossSpawnKinds())}|challenge:{activeChallenge?.ChallengeId ?? string.Empty}";
                return branchRuntimeCache.GetOrCreateEncounterPlan(
                    cacheKey,
                    () =>
                    {
                        var distanceMap = branchRuntimeCache.GetOrCreateRoomDistanceMap(graph);
                        return EncounterResolver.CreateDirectedSeededPlan(
                            graph,
                            encounterCatalog,
                            graph.Seed,
                            worldIndex,
                            encounterDirectorProfile,
                            ChallengeRuleIntValue(ChallengeRuleKind.EncounterPressureBonus),
                            bossCatalog,
                            ChallengeAllowedNonBossSpawnKinds(),
                            distanceMap);
                    });
            }

            return graph.BranchId == BranchGenerator.EnemyEncounterBranchId || graph.BranchId == BranchGenerator.BranchFeaturesId
                ? branchRuntimeCache.GetOrCreateEncounterPlan(
                    $"encounter|seeded|{BranchRuntimeCache.GraphSignature(graph)}|encounter:{BranchRuntimeCache.DefinitionKey(encounterCatalog)}|spawns:{string.Join(",", ChallengeAllowedNonBossSpawnKinds())}|challenge:{activeChallenge?.ChallengeId ?? string.Empty}",
                    () =>
                    {
                        var distanceMap = branchRuntimeCache.GetOrCreateRoomDistanceMap(graph);
                        return EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed, ChallengeAllowedNonBossSpawnKinds(), distanceMap);
                    })
                : EncounterPlan.Empty;
        }

        private IReadOnlyList<string> ChallengeAllowedNonBossSpawnKinds()
        {
            return ChallengeHasRule(ChallengeRuleKind.SmallMonstersOnly)
                ? new[] { "spawnEnemyRat", "spawnEnemySpider" }
                : System.Array.Empty<string>();
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
                assignment.BossPhaseState,
                assignment.EnemyIntelligenceLevels,
                assignment.EnemyDispositions);
        }

        private ImportedRoomRuntimeAsset ResolveCurrentRoomAsset()
        {
            return ResolveRoomAssetForState(State?.CurrentRoom);
        }

        private ImportedRoomRuntimeAsset ResolveRoomAssetForState(BranchRoomState room)
        {
            var roomAssetId = room?.RuntimeRoomAssetId ?? string.Empty;
            var roomId = room?.Id.Value ?? string.Empty;
            var cacheKey = $"roomAsset|ship:{IsSpaceshipHub}|room:{roomId}|asset:{roomAssetId}|biome:{ActiveBiomeId}|content:{branchRuntimeCache.ActiveContentKey}";
            if (IsSpaceshipHub &&
                room != null)
            {
                return branchRuntimeCache.GetOrCreateRoomAsset(
                    cacheKey,
                    () => spaceshipRoomAssets.TryGetValue(room.RuntimeRoomAssetId, out var spaceshipRoom) && spaceshipRoom != null
                        ? spaceshipRoom
                        : roomAsset);
            }

            if (room != null &&
                branchContent != null)
            {
                return branchRuntimeCache.GetOrCreateRoomAsset(
                    cacheKey,
                    () => branchContent.TryGetRoomAsset(room.RuntimeRoomAssetId, ActiveBiomeId, out var asset)
                        ? asset
                        : roomAsset);
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

        private int CaptureCurrentPlayerHealth()
        {
            return roomCombatController?.PlayerHealth != null
                ? roomCombatController.PlayerHealth.CurrentHealth
                : CreateAppliedCurrentRunBuild().DerivedStats.MaxHealth;
        }

        private void RestoreCurrentPlayerHealth(int currentHealth)
        {
            if (roomCombatController?.PlayerHealth == null)
            {
                return;
            }

            roomCombatController.PlayerHealth.Restore(CreateAppliedCurrentRunBuild().DerivedStats.MaxHealth, currentHealth);
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
            RecordChallengeFailureIfNeeded();
            if (canPersist)
            {
                runSaveStore.ClearActiveRun(activeProfileSlotId);
                RefreshSelectedProfileSummary();
            }

            SaveStatus = "Run Lost";
            if (HollowBootstrap.Instance != null && playerDeathRouteCoroutine == null)
            {
                playerDeathRouteCoroutine = StartCoroutine(LoadMainMenuAfterPlayerDeathDelay());
            }
            else if (HollowBootstrap.Instance == null)
            {
                ReturnToSpaceshipOrProfileMenu(
                    activeChallenge != null ? SpaceshipArrivalReason.ChallengeDeath : SpaceshipArrivalReason.NormalDeath,
                    0,
                    activeChallenge?.ChallengeId ?? string.Empty);
            }
        }

        private IEnumerator LoadMainMenuAfterPlayerDeathDelay()
        {
            yield return new WaitForSeconds(PlayerDeathMainMenuDelaySeconds);
            playerDeathRouteCoroutine = null;
            if (HollowBootstrap.Instance == null)
            {
                yield break;
            }

            ReturnToSpaceshipOrProfileMenu(
                activeChallenge != null ? SpaceshipArrivalReason.ChallengeDeath : SpaceshipArrivalReason.NormalDeath,
                0,
                activeChallenge?.ChallengeId ?? string.Empty);
        }

        private void ReturnToProfileMenu()
        {
            if (HollowBootstrap.Instance == null)
            {
                return;
            }

            var route = gameSessionState?.PlatformKind == HollowPlatformKind.WindowsStandard3D
                ? AppShellRoute.MainMenu
                : AppShellRoute.MainMenuVisionOS;
            HollowBootstrap.Instance.AppStateMachine.TransitionTo(route);
            SceneLoaderService.LoadRouteAsync(route);
        }

        private void ReturnToSpaceshipOrProfileMenu(
            SpaceshipArrivalReason reason,
            int soulsBanked = 0,
            string challengeId = "")
        {
            if (gameSessionState == null ||
                !gameSessionState.HasProfile ||
                gameSessionState.SessionMode == RuntimeSessionMode.DeveloperLab ||
                gameSessionState.SessionMode == RuntimeSessionMode.TransientArena ||
                gameSessionState.SessionMode == RuntimeSessionMode.TransientRoomDesignerPlaytest)
            {
                ReturnToProfileMenu();
                return;
            }

            SpaceshipArrivalHandoff.Set(reason, gameSessionState.PlatformKind, soulsBanked, challengeId);
            var context = ProfileSessionHost.Instance?.SelectedProfileContext;
            context?.SetLaunchMode(RunLaunchMode.NewRun);
            context?.SetSelectedChallengeId(string.Empty);
            context?.SetDeveloperLabRequested(false);
            if (HollowBootstrap.Instance == null)
            {
                return;
            }

            var route = PlatformPresentationModeResolver.SpaceshipRouteForPlatform(gameSessionState.PlatformKind);
            HollowBootstrap.Instance.AppStateMachine.TransitionTo(route);
            SceneLoaderService.LoadRouteAsync(route);
        }

        private void LoadAppRoute(AppShellRoute route)
        {
            LastLaunchedRoute = route;
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(route);
                SceneLoaderService.LoadRouteAsync(route);
            }
        }

        private void ApplyRunStatsToPlayer(int healAmount)
        {
            playerRunBuild = CreateAppliedCurrentRunBuild(announceActivation: true);
            PlayerBuildApplier.Apply(playerRunBuild, playerController != null ? playerController.gameObject : null, weaponCatalog, armorCatalog, shieldCatalog, healAmount);
        }

        private void ApplySelectedCharacterForFreshRun()
        {
            var selectedCharacterId = activeChallenge?.SelectedCharacterId ?? gameSessionState?.SelectedCharacterId ?? "balanced";
            var character = characterCatalog != null ? characterCatalog.Resolve(selectedCharacterId) : null;
            playerRunBuild.ConfigureCharacter(character);
            ApplyShipUpgradesForFreshRun();
            ApplyEquipmentAndSynergyModifiers(playerRunBuild, announceActivation: false);
        }

        private void ApplyShipUpgradesForFreshRun()
        {
            if (activeChallenge != null ||
                gameSessionState == null ||
                (gameSessionState.SessionMode != RuntimeSessionMode.ProfileBacked &&
                 gameSessionState.SessionMode != RuntimeSessionMode.SpaceshipHub) ||
                !gameSessionState.HasProfile)
            {
                return;
            }

            var selectedProfile = ProfileSessionHost.Instance?.SelectedProfileContext?.SelectedProfile;
            if (selectedProfile == null || selectedProfile.PurchasedShipUpgradeIds.Count == 0)
            {
                return;
            }

            foreach (var modifier in ShipMetaProgressionService.CreatePurchasedModifiers(shipUpgradeCatalog, selectedProfile.PurchasedShipUpgradeIds))
            {
                playerRunBuild.AddModifier(modifier);
            }
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
                shieldCatalog,
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
                case RewardKind.Shield:
                    playerRunBuild.Equipment.EquipShield(state.RewardId);
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
                shieldCatalog,
                usableItemCatalog,
                dropPosition);
            var result = RewardApplicationService.Apply(grant, runEconomy, playerRunStats, playerRunBuild, weaponCatalog, shieldCatalog, usableItemCatalog);
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
            var maxCharges = ActiveItemMaxCharges(itemId);
            return maxCharges > 0
                ? $"{displayName} ({build.Equipment.ActiveItemCharges}/{maxCharges})"
                : $"{displayName} ({build.Equipment.ActiveItemCharges})";
        }

        private int ActiveItemMaxCharges(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && usableItemCatalog != null && usableItemCatalog.TryGet(itemId, out var item)
                ? item.MaxCharges
                : 0;
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
            return RewardPresentationResolver.ResolveName(kind, id, weaponCatalog, armorCatalog, shieldCatalog, usableItemCatalog, ActiveRewardPoolsForSynergies());
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
                shieldCatalog,
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
                RewardKind.Shield => PresentationPrefabRole.RewardPickup,
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
            if (IsDeveloperLab || IsSpaceshipHub || suppressCheckpoint || !canPersist || activeRunCompletedOrFailed || runSaveStore == null)
            {
                return;
            }

            runSaveStore.SaveActiveRun(activeProfileSlotId, CreateSnapshot());
            SaveStatus = "Saved";
            RefreshSelectedProfileSummary();
        }

        public RunSaveSnapshot CreateSnapshot()
        {
            var liveRoomId = State?.CurrentRoomId.Value ?? string.Empty;
            IReadOnlyList<int> liveEnemyIntelligenceLevels = null;
            IReadOnlyList<string> liveEnemyDispositions = null;
            if (!string.IsNullOrWhiteSpace(liveRoomId) &&
                roomCombatController != null &&
                encounterPlan.TryResolve(liveRoomId, out var liveAssignment) &&
                roomCombatController.TryGetEnemyIntelligenceSnapshot(
                    liveAssignment.EnemySpawnKinds.Count,
                    out var runtimeIntelligence,
                    out var runtimeDispositions))
            {
                liveEnemyIntelligenceLevels = runtimeIntelligence;
                liveEnemyDispositions = runtimeDispositions;
            }

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
                activeBiomeId = ActiveBiomeId,
                activeHubPortalId = activeHubPortalId ?? string.Empty,
                hubShopRefreshIndex = interBranchHubState.ShopRefreshIndex,
                bossKeyState = bossKeyState.ToString(),
                bossKeyRoomId = branchFeaturePlan.BossKeyRoomId,
                secretRoomId = branchFeaturePlan.SecretRoomId,
                bossDoorUnlocked = bossDoorUnlocked,
                proceduralRewardPlan = proceduralRewardPlan.ToSaveState(),
                encounterPlan = encounterPlan.ToSaveState(
                    roomCombatController != null ? roomCombatController.EnemyCatalog : null,
                    liveRoomId,
                    liveEnemyIntelligenceLevels,
                    liveEnemyDispositions),
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

        private int CompleteActiveRunIfPersistent()
        {
            if (activeRunCompletedOrFailed)
            {
                return 0;
            }

            activeRunCompletedOrFailed = true;
            RecordChallengeCompletionIfNeeded();
            var soulsToBank = activeChallenge == null ? runEconomy.RunSouls : 0;
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
            return soulsToBank;
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

        private void RecordChallengeFailureIfNeeded()
        {
            if (activeChallenge == null || challengeResultStore == null || gameSessionState == null || gameSessionState.ProfileSlotIndex < 0)
            {
                return;
            }

            challengeResultStore.FailChallengeAttempt(activeProfileSlotId, activeChallenge.ChallengeId, activeChallenge.FixedRunSeed);
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
            var instance = ShouldPoolRuntimePickup(role)
                ? prefab != null
                    ? HollowRuntimePool.Rent(prefab, null)
                    : HollowRuntimePool.RentPrimitive($"Pickup.{role}.Fallback", primitiveType, null)
                : prefab != null
                    ? Instantiate(prefab)
                    : GameObject.CreatePrimitive(primitiveType);
            instance.name = objectName;
            MaterialResolver.ApplyTo(instance, role);
            var collider = instance.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return instance;
        }

        private static bool ShouldPoolRuntimePickup(MaterialRole role)
        {
            return role is MaterialRole.RewardPickup or
                MaterialRole.BossKeyPickup or
                MaterialRole.HazardCoinDrop or
                MaterialRole.CoinCopper or
                MaterialRole.CoinSilver or
                MaterialRole.CoinGold;
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
                if (target.GetComponent<RoomRewardPickup>() != null ||
                    target.GetComponent<ReplacementPickup>() != null ||
                    target.GetComponent<CoinPickupController>() != null ||
                    target.GetComponent<HazardCoinPickup>() != null ||
                    target.GetComponent<BossKeyPickup>() != null)
                {
                    HollowRuntimePool.Return(target);
                    return;
                }

                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
