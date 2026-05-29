using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rewards;
using Hollow.Rooms;
using Hollow.UI.Shell;
using Hollow.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Hollow.Tests.EditMode
{
    public sealed class SpaceshipMetaHubTests
    {
        private const string ArrivalsRoomPath = "Assets/_Hollow/Data/Rooms/Spaceship/ship_arrivals_quarantine.hollowruntime.json";
        private const string MainHallRoomPath = "Assets/_Hollow/Data/Rooms/Spaceship/ship_main_hall.hollowruntime.json";
        private const string DeparturesRoomPath = "Assets/_Hollow/Data/Rooms/Spaceship/ship_departures.hollowruntime.json";
        private const string MissionRoomPath = "Assets/_Hollow/Data/Rooms/Spaceship/ship_mission_center.hollowruntime.json";
        private const string LabRoomPath = "Assets/_Hollow/Data/Rooms/Spaceship/ship_technology_lab.hollowruntime.json";
        private const string SampleRoomPath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";
        private const string PlayerPrefabPath = "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab";
        private const string ProjectilePrefabPath = "Assets/_Hollow/Prefabs/Combat/ProjectileBase.prefab";

        private string tempRoot;
        private GameObject hostObject;

        [SetUp]
        public void SetUp()
        {
            SpaceshipArrivalHandoff.Clear();
            ClearProfileSessionHostInstance();
            tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_spaceship_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            SpaceshipArrivalHandoff.Clear();
            if (hostObject != null)
            {
                Object.DestroyImmediate(hostObject);
            }

            ClearProfileSessionHostInstance();
            foreach (var branch in Object.FindObjectsByType<BranchSessionController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(branch.gameObject);
            }

            if (!string.IsNullOrWhiteSpace(tempRoot) && Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void SpaceshipGraphUsesStandardBranchRoomsAndOpenSpokes()
        {
            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));

            Assert.IsTrue(branch.IsSpaceshipHub);
            Assert.AreEqual(SpaceshipBranchDefinition.DeparturesRoomId, branch.State.CurrentRoomId.Value);
            Assert.AreEqual(5, branch.State.Graph.RoomCount);
            Assert.IsTrue(branch.State.Graph.Rooms.All(room => room.IsCleared));
            Assert.IsTrue(branch.State.Graph.Rooms.All(room => room.RewardState == RoomRewardState.Unavailable));
            Assert.AreEqual(new Vector2(13f, 7f), branch.RuntimeRoomRoot.RoomSizeMeters);

            Assert.IsTrue(branch.TryTraverse("west"));
            Assert.AreEqual(SpaceshipBranchDefinition.MainHallRoomId, branch.State.CurrentRoomId.Value);
            Assert.IsTrue(branch.TryTraverse("east"));
            Assert.AreEqual(SpaceshipBranchDefinition.DeparturesRoomId, branch.State.CurrentRoomId.Value);
        }

        [Test]
        public void ReturnArrivalRequiresSterilizationBeforeMainHallDoorTraverses()
        {
            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.NormalDeath,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));

            Assert.AreEqual(SpaceshipBranchDefinition.ArrivalsRoomId, branch.State.CurrentRoomId.Value);
            Assert.IsTrue(branch.SpaceshipQuarantineRequired);
            Assert.IsFalse(branch.SpaceshipQuarantineUnlocked);
            Assert.IsFalse(branch.TryTraverse("east"));

            branch.PlayerController.transform.localPosition = new Vector3(-2.4f, 0f, -0.9f);
            Assert.IsTrue(branch.TryInteract());
            Assert.IsTrue(branch.SpaceshipQuarantineUnlocked);
            Assert.IsTrue(branch.TryTraverse("east"));
            Assert.AreEqual(SpaceshipBranchDefinition.MainHallRoomId, branch.State.CurrentRoomId.Value);
        }

        [Test]
        public void SpaceshipSceneIgnoresStaleProfileLaunchFlagsOnReturn()
        {
            CreateProfileHostWithSelectedProfile(out var selectedContext);
            var staleChallenge = ChallengeCatalogDefinition.CreateRuntimeDefault().Challenges.First();
            selectedContext.SetLaunchMode(RunLaunchMode.ContinueRun);
            selectedContext.SetSelectedChallengeId(staleChallenge.ChallengeId);
            selectedContext.SetDeveloperLabRequested(true);

            var root = new GameObject("ShipGameSessionUnderTest");
            root.AddComponent<RoomCombatController>().ConfigureAutoInitialize(false);
            var branch = root.AddComponent<BranchSessionController>();
            branch.ConfigureSpaceshipBranch(CreateShipDefinition());
            branch.ConfigureChallengeCatalog(ChallengeCatalogDefinition.CreateRuntimeDefault());
            branch.ConfigureCharacterCatalog(CharacterCatalogDefinition.CreateRuntimeDefault());
            branch.ConfigureWeaponCatalog(AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath));
            branch.ConfigureArmorCatalog(AssetDatabase.LoadAssetAtPath<ArmorCatalogDefinition>(Milestone30AssetGenerator.ArmorCatalogPath));
            branch.ConfigureShieldCatalog(AssetDatabase.LoadAssetAtPath<ShieldCatalogDefinition>(Milestone69AssetGenerator.ShieldCatalogPath));

            var presentationObject = new GameObject("WorldPresentationRoot");
            presentationObject.transform.SetParent(root.transform, false);
            presentationObject.AddComponent<PlatformPresentationRoot>();
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            roomObject.AddComponent<RoomRuntimeRoot>();
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var player = playerPrefab != null
                ? (GameObject)Object.Instantiate(playerPrefab)
                : new GameObject("PlayerCharacter", typeof(CapsuleCollider), typeof(PlaceholderPlayerController), typeof(CombatantHealth), typeof(PlayerMovementController), typeof(PlayerWeaponController), typeof(PlayerDefenseController), typeof(PlayerLocomotionAnimator), typeof(Animator));
            player.name = "PlayerCharacter";
            player.transform.SetParent(root.transform, false);

            SpaceshipArrivalHandoff.Set(
                SpaceshipArrivalReason.NormalSuccess,
                HollowPlatformKind.WindowsStandard3D,
                3,
                staleChallenge.ChallengeId);
            var game = root.AddComponent<GameSessionController>();
            game.Configure(
                HollowPlatformKind.WindowsStandard3D,
                AssetDatabase.LoadAssetAtPath<TextAsset>(ArrivalsRoomPath),
                RuntimeSessionMode.SpaceshipHub);

            game.InitializeSession();

            Assert.AreEqual(RuntimeSessionMode.SpaceshipHub, game.SessionState.SessionMode);
            Assert.AreEqual(RunLaunchMode.NewRun, game.SessionState.LaunchMode);
            Assert.AreEqual(string.Empty, game.SessionState.SelectedChallengeId);
            Assert.IsTrue(branch.IsSpaceshipHub);
            Assert.AreEqual(SpaceshipBranchDefinition.ArrivalsRoomId, branch.State.CurrentRoomId.Value);
            Assert.AreEqual(5, branch.State.Graph.RoomCount);
            CollectionAssert.AreEquivalent(
                new[] { "ARR", "HALL", "DEP", "MIS", "LAB" },
                branch.CreateMiniMapModel().Nodes.Select(node => node.DisplayLabel).Where(label => !string.IsNullOrWhiteSpace(label)).ToArray());
        }

        [Test]
        public void ShipPlayerUsesGameplayStackAndHudShowsBankedSoulsAtFullVitals()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            var slotId = new ProfileSlotId(0);
            store.MarkRunStarted(slotId);
            store.CompleteActiveRun(slotId, new RunCompletionSummary { soulsToBank = 10 });
            selectedContext.UpdateSelectedProfile(store.LoadSlotSummaries()[0]);

            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));

            var player = branch.PlayerController;
            Assert.IsNotNull(player.GetComponent<PlaceholderPlayerController>());
            Assert.IsNotNull(player.GetComponent<PlayerMovementController>());
            Assert.IsNotNull(player.GetComponent<PlayerWeaponController>());
            Assert.IsNotNull(player.GetComponent<PlayerDefenseController>());
            Assert.IsNotNull(player.GetComponent<PlayerLocomotionAnimator>());
            Assert.IsNotNull(player.GetComponentInChildren<Animator>(includeInactive: true));
            Assert.IsNotNull(branch.RoomCombatController);

            var model = branch.CreatePlayerBuildHudModel();
            Assert.AreEqual(10, model.Souls);
            Assert.AreEqual(model.MaxHealth, model.CurrentHealth);
            Assert.AreEqual(model.MaxStamina, model.CurrentStamina, 0.001f);
        }

        [Test]
        public void TechnologyLabPurchaseRefreshesShipHudSoulsAndStats()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            var slotId = new ProfileSlotId(0);
            store.MarkRunStarted(slotId);
            store.CompleteActiveRun(slotId, new RunCompletionSummary { soulsToBank = 10 });
            selectedContext.UpdateSelectedProfile(store.LoadSlotSummaries()[0]);
            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));
            var before = branch.CreatePlayerBuildHudModel();

            EnterTechnologyLab(branch);
            var moduleLabels = Object.FindObjectsByType<SpaceshipTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(terminal => terminal.TerminalKind == SpaceshipTerminalKind.TechnologyUpgrade)
                .Select(terminal => terminal.DisplayName)
                .ToArray();
            CollectionAssert.Contains(moduleLabels, "Vitals Module\n5 Banked Souls");
            CollectionAssert.Contains(moduleLabels, "Stamina Module\n5 Banked Souls");
            CollectionAssert.Contains(moduleLabels, "Reactor Module\n6 Banked Souls");
            CollectionAssert.Contains(moduleLabels, "Combat Module\n8 Banked Souls");

            branch.PlayerController.transform.localPosition = new Vector3(-2.1f, 0f, 0.55f);
            Assert.IsTrue(branch.TryInteract());

            var after = branch.CreatePlayerBuildHudModel();
            Assert.AreEqual(5, after.Souls);
            Assert.AreEqual(before.MaxHealth + 1, after.MaxHealth);
            Assert.AreEqual(after.MaxHealth, after.CurrentHealth);
            Assert.AreEqual("Vitals Module installed.", branch.LastRewardMessage);
        }

        [Test]
        public void BranchMiniMapRevealsShipRoomsWithFacilityLabels()
        {
            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));
            var model = branch.CreateMiniMapModel();

            Assert.IsTrue(model.Nodes.All(node => node.IsRevealed));
            CollectionAssert.Contains(model.Nodes.Select(node => node.DisplayLabel).ToArray(), "ARR");
            CollectionAssert.Contains(model.Nodes.Select(node => node.DisplayLabel).ToArray(), "HALL");
            CollectionAssert.Contains(model.Nodes.Select(node => node.DisplayLabel).ToArray(), "DEP");
            CollectionAssert.Contains(model.Nodes.Select(node => node.DisplayLabel).ToArray(), "MIS");
            CollectionAssert.Contains(model.Nodes.Select(node => node.DisplayLabel).ToArray(), "LAB");

            var canvasObject = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(BranchMiniMapController));
            try
            {
                var minimap = canvasObject.GetComponent<BranchMiniMapController>();
                minimap.Bind(branch);
                Assert.AreEqual(new Vector2(-32f, -32f), minimap.MapPanel.anchoredPosition);
                Assert.AreEqual(new Vector2(420f, 250f), minimap.MapPanel.sizeDelta);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void MissionCenterLaunchesChallengeThroughExistingCatalog()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));

            Assert.IsTrue((bool)InvokePrivate(branch, "TryLaunchShipChallenge", "stone_oath"));

            Assert.AreEqual(AppShellRoute.GameWindows, branch.LastLaunchedRoute);
            Assert.AreEqual("stone_oath", selectedContext.SelectedChallengeId);
            var record = store.GetChallengeRecord(new ProfileSlotId(0), "stone_oath");
            Assert.AreEqual(1, record.Attempts);
            Assert.AreEqual("Started", record.LastResult);
        }

        [Test]
        public void DeparturesStartsNormalRunWithoutChallenge()
        {
            CreateProfileHostWithSelectedProfile(out var selectedContext);
            selectedContext.SetSelectedCharacterId("heavy");
            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));
            var terminal = Object.FindObjectsByType<SpaceshipTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(candidate => candidate.TerminalKind == SpaceshipTerminalKind.Departures);

            Assert.AreEqual(BranchSessionController.PortalEngineDisplayName, terminal.DisplayName);

            Assert.IsTrue((bool)InvokePrivate(branch, "TryLaunchNormalExpeditionFromShip"));

            Assert.AreEqual(AppShellRoute.GameWindows, branch.LastLaunchedRoute);
            Assert.AreEqual(RunLaunchMode.NewRun, selectedContext.LaunchMode);
            Assert.AreEqual(string.Empty, selectedContext.SelectedChallengeId);
            Assert.AreEqual("heavy", selectedContext.SelectedCharacterId);
            Assert.IsTrue(selectedContext.SelectedProfile.HasActiveRun || selectedContext.SelectedProfile.TotalRuns == 1);
        }

        [Test]
        public void MainHallShipLogPanelReportsShipSoulRule()
        {
            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));

            Assert.IsTrue(branch.TryTraverse("west"));
            Assert.AreEqual(SpaceshipBranchDefinition.MainHallRoomId, branch.State.CurrentRoomId.Value);
            var terminal = Object.FindObjectsByType<SpaceshipTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(candidate => candidate.TerminalKind == SpaceshipTerminalKind.ShipLog);

            Assert.AreEqual(BranchSessionController.ShipLogDisplayName, terminal.DisplayName);
            branch.PlayerController.transform.localPosition = terminal.transform.localPosition;
            Assert.IsTrue(branch.TryInteract());
            Assert.AreEqual(BranchSessionController.ShipLogMessage, branch.LastRewardMessage);
        }

        [Test]
        public void TechnologyLabPurchasesPersistentUpgradeWithBankedSoulsOnly()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            var slotId = new ProfileSlotId(0);
            store.MarkRunStarted(slotId);
            store.CompleteActiveRun(slotId, new RunCompletionSummary { soulsToBank = 10 });
            selectedContext.UpdateSelectedProfile(store.LoadSlotSummaries()[0]);
            var branch = CreateShipBranch(new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty));

            Assert.IsTrue((bool)InvokePrivate(branch, "TryPurchaseShipUpgrade", "reinforced_vitals"));
            var summary = store.LoadSlotSummaries()[0];

            Assert.AreEqual(5, summary.BankedSouls);
            CollectionAssert.Contains(summary.PurchasedShipUpgradeIds.ToArray(), "reinforced_vitals");
            Assert.IsFalse((bool)InvokePrivate(branch, "TryPurchaseShipUpgrade", "reinforced_vitals"));
            Assert.IsFalse((bool)InvokePrivate(branch, "TryPurchaseShipUpgrade", "combat_calibration"));
        }

        [Test]
        public void ShipUpgradeModifiersCanBeResolvedForNormalRuns()
        {
            var catalog = ShipUpgradeCatalogDefinition.CreateRuntimeDefault();
            var modifiers = ShipMetaProgressionService
                .CreatePurchasedModifiers(catalog, new[] { "reinforced_vitals", "stamina_cell" })
                .ToArray();

            Assert.AreEqual(2, modifiers.Length);
            Assert.AreEqual(1, modifiers.Sum(modifier => modifier.maxHealth));
            Assert.AreEqual(10f, modifiers.Sum(modifier => modifier.maxStamina), 0.001f);
        }

        [Test]
        public void NormalRunCompletionBanksSoulsAndRoutesToShipArrival()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            var branch = CreateBranchReturnHarness(store, selectedContext, runSouls: 7);

            var soulsBanked = (int)InvokePrivate(branch, "CompleteActiveRunIfPersistent");
            InvokePrivate(
                branch,
                "ReturnToSpaceshipOrProfileMenu",
                SpaceshipArrivalReason.NormalSuccess,
                soulsBanked,
                string.Empty);

            var summary = store.LoadSlotSummaries()[0];
            Assert.AreEqual(7, soulsBanked);
            Assert.AreEqual(7, summary.BankedSouls);
            Assert.IsFalse(summary.HasActiveRun);
            Assert.IsTrue(SpaceshipArrivalHandoff.TryConsume(out var arrival));
            Assert.AreEqual(SpaceshipArrivalReason.NormalSuccess, arrival.Reason);
            Assert.AreEqual(7, arrival.SoulsBanked);
        }

        [Test]
        public void NormalRunDeathClearsActiveRunWithoutBankingSouls()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            store.MarkRunStarted(new ProfileSlotId(0));
            selectedContext.UpdateSelectedProfile(store.LoadSlotSummaries()[0]);
            var branch = CreateBranchReturnHarness(store, selectedContext, runSouls: 7);

            InvokePrivate(branch, "OnPlayerDied", new object[] { null });

            var summary = store.LoadSlotSummaries()[0];
            Assert.AreEqual(0, summary.BankedSouls);
            Assert.IsFalse(summary.HasActiveRun);
            Assert.IsTrue(SpaceshipArrivalHandoff.TryConsume(out var arrival));
            Assert.AreEqual(SpaceshipArrivalReason.NormalDeath, arrival.Reason);
            Assert.AreEqual(0, arrival.SoulsBanked);
        }

        [Test]
        public void ChallengeCompletionRoutesToShipWithoutBankingSouls()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            store.MarkRunStarted(new ProfileSlotId(0));
            selectedContext.UpdateSelectedProfile(store.LoadSlotSummaries()[0]);
            var branch = CreateBranchReturnHarness(store, selectedContext, runSouls: 9, challengeId: "stone_oath");

            var soulsBanked = (int)InvokePrivate(branch, "CompleteActiveRunIfPersistent");
            InvokePrivate(
                branch,
                "ReturnToSpaceshipOrProfileMenu",
                SpaceshipArrivalReason.ChallengeSuccess,
                soulsBanked,
                "stone_oath");

            var summary = store.LoadSlotSummaries()[0];
            var record = store.GetChallengeRecord(new ProfileSlotId(0), "stone_oath");
            Assert.AreEqual(0, soulsBanked);
            Assert.AreEqual(0, summary.BankedSouls);
            Assert.IsFalse(summary.HasActiveRun);
            Assert.AreEqual(1, record.Completions);
            Assert.IsTrue(SpaceshipArrivalHandoff.TryConsume(out var arrival));
            Assert.AreEqual(SpaceshipArrivalReason.ChallengeSuccess, arrival.Reason);
            Assert.AreEqual(0, arrival.SoulsBanked);
            Assert.AreEqual("stone_oath", arrival.ChallengeId);
        }

        [Test]
        public void WorldLoopBranchReturnEntersInterBranchHubWithoutBankingSouls()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            var slotId = new ProfileSlotId(0);
            store.MarkRunStarted(slotId);
            selectedContext.UpdateSelectedProfile(store.LoadSlotSummaries()[0]);
            var branch = CreateBranchReturnHarness(store, selectedContext, runSouls: 7);
            var content = CreateWorldLoopContent(out var settings);
            var hub = InterBranchHubState.CreateWorldHub(12345, 1, 0, null);
            var choice = hub.NextBranchChoices.First(candidate => candidate.Kind == HubPortalKind.Branch);
            var graph = BranchGenerator.CreateSeededBranchFeatures(content, settings, choice.Seed);

            SetPrivate(branch, "branchContent", content);
            SetPrivate(branch, "branchGenerationSettings", settings);
            SetPrivate(branch, "encounterCatalog", AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath));
            SetPrivate(branch, "interBranchHubState", hub);
            SetPrivate(branch, "worldPhase", RunWorldPhase.Branch);
            SetPrivate(branch, "worldIndex", 1);
            SetPrivate(branch, "runSeed", 12345);
            SetPrivate(branch, "currentBranchSeed", choice.Seed);
            SetPrivate(branch, "activeHubPortalId", choice.ChoiceId);
            SetPrivate(branch, "branchFeaturePlan", BranchFeaturePlan.Create(graph));
            SetPrivateProperty(branch, "State", BranchSessionState.Create(graph));
            AttachReturnPortalHarness(branch);

            Assert.IsTrue((bool)InvokePrivate(branch, "TryUseHubReturnPortal"));

            var summary = store.LoadSlotSummaries()[0];
            Assert.AreEqual(0, summary.BankedSouls);
            Assert.IsTrue(branch.IsInInterBranchHub);
            Assert.AreEqual(RunWorldPhase.Hub, branch.WorldPhase);
            Assert.AreEqual(
                HubBranchPortalState.Defeated,
                branch.InterBranchHubState.NextBranchChoices.Single(candidate => candidate.ChoiceId == choice.ChoiceId).State);
            Assert.IsFalse(SpaceshipArrivalHandoff.TryConsume(out _));
        }

        [Test]
        public void WorldLoopFinalReturnToShipBanksSoulsAndRoutesToArrival()
        {
            var store = CreateProfileHostWithSelectedProfile(out var selectedContext);
            var slotId = new ProfileSlotId(0);
            store.MarkRunStarted(slotId);
            selectedContext.UpdateSelectedProfile(store.LoadSlotSummaries()[0]);
            var branch = CreateBranchReturnHarness(store, selectedContext, runSouls: 7);
            var finalReturn = NextBranchChoice.CreateFinalExtraction(12345, 3);

            Assert.AreEqual("Return to Ship", finalReturn.DisplayName);
            branch.StartNextBranch(finalReturn);

            var summary = store.LoadSlotSummaries()[0];
            Assert.AreEqual(7, summary.BankedSouls);
            Assert.IsFalse(summary.HasActiveRun);
            Assert.IsTrue(SpaceshipArrivalHandoff.TryConsume(out var arrival));
            Assert.AreEqual(SpaceshipArrivalReason.NormalSuccess, arrival.Reason);
            Assert.AreEqual(7, arrival.SoulsBanked);
        }

        [Test]
        public void ArrivalHandoffCarriesReasonAndBankedSoulAmount()
        {
            SpaceshipArrivalHandoff.Set(
                SpaceshipArrivalReason.NormalSuccess,
                HollowPlatformKind.VisionOSBoundedTabletop,
                12);

            Assert.IsTrue(SpaceshipArrivalHandoff.TryConsume(out var arrival));
            Assert.AreEqual(SpaceshipArrivalReason.NormalSuccess, arrival.Reason);
            Assert.AreEqual(HollowPlatformKind.VisionOSBoundedTabletop, arrival.PlatformKind);
            Assert.AreEqual(12, arrival.SoulsBanked);
            Assert.IsTrue(arrival.RequiresQuarantine);
        }

        [Test]
        public void GeneratedSpaceshipWindowsSceneContainsNormalBranchPresentationStack()
        {
            var previousLogAssertState = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                SpaceshipHubSceneGenerator.GenerateScenesForTests();
                Assert.IsTrue(File.Exists(SpaceshipHubSceneGenerator.SpaceshipWindowsScenePath));
                EditorSceneManager.OpenScene(SpaceshipHubSceneGenerator.SpaceshipWindowsScenePath, OpenSceneMode.Single);
                var game = Object.FindFirstObjectByType<GameSessionController>();
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                Assert.IsNotNull(game);
                Assert.IsNotNull(branch);
                Assert.AreEqual(RuntimeSessionMode.SpaceshipHub, game.SessionMode);
                Assert.IsNotNull(Object.FindFirstObjectByType<RoomRuntimeRoot>());
                Assert.IsNotNull(Object.FindFirstObjectByType<PlatformPresentationRoot>());
                Assert.IsNotNull(Object.FindFirstObjectByType<CameraRigMetadata>());
                var player = Object.FindFirstObjectByType<PlaceholderPlayerController>();
                Assert.IsNotNull(player);
                Assert.AreEqual("PlayerCharacter", player.gameObject.name);
                Assert.IsNotNull(player.GetComponent<PlayerMovementController>());
                Assert.IsNotNull(player.GetComponent<PlayerWeaponController>());
                Assert.IsNotNull(player.GetComponent<PlayerDefenseController>());
                Assert.IsNotNull(player.GetComponent<PlayerLocomotionAnimator>());

                var shell = GameObject.Find("PlatformShellCanvas");
                Assert.IsNotNull(shell);
                Assert.IsNotNull(shell.GetComponent<PlatformShellController>());
                Assert.IsNotNull(shell.GetComponent<PlayerBuildHudController>());
                Assert.IsNotNull(shell.GetComponent<BranchMiniMapController>());
                Assert.IsNull(shell.GetComponent("ShipMiniMapController"));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousLogAssertState;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private BranchSessionController CreateShipBranch(SpaceshipArrivalSnapshot arrival)
        {
            var root = new GameObject("ShipBranchUnderTest");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            roomObject.AddComponent<RoomRuntimeRoot>();

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var player = playerPrefab != null
                ? (GameObject)Object.Instantiate(playerPrefab)
                : new GameObject("PlayerCharacter", typeof(CapsuleCollider), typeof(PlaceholderPlayerController), typeof(CombatantHealth), typeof(PlayerMovementController), typeof(PlayerWeaponController), typeof(PlayerDefenseController), typeof(PlayerLocomotionAnimator), typeof(Animator));
            player.name = "PlayerCharacter";
            player.transform.SetParent(root.transform, false);

            var combat = root.AddComponent<RoomCombatController>();
            combat.ConfigureAutoInitialize(false);
            combat.Configure(null, AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath));
            var branch = root.AddComponent<BranchSessionController>();
            branch.ConfigureSpaceshipBranch(CreateShipDefinition());
            branch.ConfigureChallengeCatalog(ChallengeCatalogDefinition.CreateRuntimeDefault());
            branch.ConfigureCharacterCatalog(CharacterCatalogDefinition.CreateRuntimeDefault());
            branch.ConfigureWeaponCatalog(AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath));
            branch.ConfigureArmorCatalog(AssetDatabase.LoadAssetAtPath<ArmorCatalogDefinition>(Milestone30AssetGenerator.ArmorCatalogPath));
            branch.ConfigureShieldCatalog(AssetDatabase.LoadAssetAtPath<ShieldCatalogDefinition>(Milestone69AssetGenerator.ShieldCatalogPath));

            SpaceshipArrivalHandoff.Set(arrival.Reason, arrival.PlatformKind, arrival.SoulsBanked, arrival.ChallengeId);
            var profile = ProfileSessionHost.Instance?.SelectedProfileContext?.SelectedProfile;
            var sessionState = GameSessionState.Create(
                RuntimeSessionMode.SpaceshipHub,
                arrival.PlatformKind,
                RunLaunchMode.NewRun,
                profile,
                Vector3.zero);
            var arrivalsText = AssetDatabase.LoadAssetAtPath<TextAsset>(ArrivalsRoomPath);
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(arrivalsText.text, out var arrivalsRoom, out var error), error);
            branch.InitializeFresh(arrivalsRoom, sessionState);
            return branch;
        }

        private static SpaceshipBranchDefinition CreateShipDefinition()
        {
            var definition = ScriptableObject.CreateInstance<SpaceshipBranchDefinition>();
            definition.Configure(new[]
            {
                AssetDatabase.LoadAssetAtPath<TextAsset>(ArrivalsRoomPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(MainHallRoomPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(DeparturesRoomPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(MissionRoomPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(LabRoomPath)
            });
            return definition;
        }

        private static void EnterTechnologyLab(BranchSessionController branch)
        {
            Assert.AreEqual(SpaceshipBranchDefinition.DeparturesRoomId, branch.State.CurrentRoomId.Value);
            Assert.IsTrue(branch.TryTraverse("west"));
            Assert.AreEqual(SpaceshipBranchDefinition.MainHallRoomId, branch.State.CurrentRoomId.Value);
            Assert.IsTrue(branch.TryTraverse("south"));
            Assert.AreEqual(SpaceshipBranchDefinition.TechnologyLabRoomId, branch.State.CurrentRoomId.Value);
        }

        private static BranchSessionController CreateBranchReturnHarness(
            JsonProfileStore store,
            SelectedProfileContext selectedContext,
            int runSouls,
            string challengeId = "")
        {
            var root = new GameObject("BranchReturnUnderTest");
            var branch = root.AddComponent<BranchSessionController>();
            var sessionState = GameSessionState.Create(
                RuntimeSessionMode.ProfileBacked,
                HollowPlatformKind.WindowsStandard3D,
                RunLaunchMode.NewRun,
                selectedContext.SelectedProfile,
                Vector3.zero,
                "balanced",
                challengeId);
            var economy = new RunEconomy();
            economy.AddSouls(runSouls);

            SetPrivate(branch, "gameSessionState", sessionState);
            SetPrivate(branch, "activeProfileSlotId", new ProfileSlotId(selectedContext.SelectedProfile.SlotIndex));
            SetPrivate(branch, "canPersist", true);
            SetPrivate(branch, "runSaveStore", store);
            SetPrivate(branch, "challengeResultStore", store);
            SetPrivate(branch, "runEconomy", economy);
            if (!string.IsNullOrWhiteSpace(challengeId))
            {
                var challenge = ChallengeCatalogDefinition.CreateRuntimeDefault().Resolve(challengeId);
                SetPrivate(branch, "activeChallenge", challenge);
            }

            return branch;
        }

        private static BranchSessionContent CreateWorldLoopContent(out BranchGenerationSettingsDefinition settings)
        {
            settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SampleRoomPath));
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return content;
        }

        private static void AttachReturnPortalHarness(BranchSessionController branch)
        {
            var presentation = new GameObject("WorldLoopReturnHarness");
            presentation.transform.SetParent(branch.transform, false);
            var room = new GameObject("RoomRuntimeRoot", typeof(RoomRuntimeRoot));
            room.transform.SetParent(presentation.transform, false);
            var player = new GameObject("PlayerCharacter", typeof(PlaceholderPlayerController));
            player.transform.SetParent(presentation.transform, false);
            var portal = new GameObject("HubReturnPortal", typeof(HubReturnPortal));
            portal.transform.SetParent(presentation.transform, false);

            player.transform.localPosition = Vector3.zero;
            portal.transform.localPosition = Vector3.zero;
            SetPrivate(branch, "roomRuntimeRoot", room.GetComponent<RoomRuntimeRoot>());
            SetPrivate(branch, "playerController", player.GetComponent<PlaceholderPlayerController>());
            SetPrivate(branch, "currentHubPortal", portal.GetComponent<HubReturnPortal>());
        }

        private JsonProfileStore CreateProfileHostWithSelectedProfile(out SelectedProfileContext selectedContext)
        {
            var store = new JsonProfileStore(tempRoot);
            var slotId = new ProfileSlotId(0);
            var profile = store.CreateOrLoadProfile(slotId, "Ship Runner");
            selectedContext = new SelectedProfileContext();
            selectedContext.Select(profile);
            hostObject = new GameObject("ProfileSessionHostUnderTest");
            hostObject.AddComponent<ProfileSessionHost>().ConfigureForTests(store, selectedContext);
            return store;
        }

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            return method.Invoke(target, arguments);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static void SetPrivateProperty(object target, string propertyName, object value)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, propertyName);
            var setter = property.GetSetMethod(nonPublic: true);
            Assert.IsNotNull(setter, propertyName);
            setter.Invoke(target, new[] { value });
        }

        private static void ClearProfileSessionHostInstance()
        {
            var field = typeof(ProfileSessionHost).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }
    }
}
