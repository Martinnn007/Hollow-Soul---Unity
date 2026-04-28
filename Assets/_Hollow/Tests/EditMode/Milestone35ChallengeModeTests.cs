using System.IO;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Rewards;
using Hollow.Rooms;
using Hollow.UI.MainMenu;
using Hollow.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone35ChallengeModeTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void ChallengeCatalogContainsFixedCuratedSeeds()
        {
            var catalog = LoadChallengeCatalog();

            Assert.IsTrue(catalog.TryGetChallenge("blade_trial", out var blade));
            Assert.AreEqual(35001, blade.FixedRunSeed);
            Assert.AreEqual("balanced", blade.SelectedCharacterId);
            Assert.Greater(blade.Rules.Count, 0);

            Assert.IsTrue(catalog.TryGetChallenge("glass_runner", out var glass));
            Assert.AreEqual(35002, glass.FixedRunSeed);
            Assert.AreEqual(12, glass.StartingCoins);

            Assert.IsTrue(catalog.TryGetChallenge("stone_oath", out var stone));
            Assert.AreEqual(35003, stone.FixedRunSeed);
            Assert.AreEqual("heavy", stone.SelectedCharacterId);
        }

        [Test]
        public void MainMenuChallengeLaunchIsTransientAndDoesNotOverwriteActiveRun()
        {
            var tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m35_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
            try
            {
                var store = new JsonProfileStore(tempRoot);
                var slotId = new ProfileSlotId(0);
                var profile = store.CreateOrLoadProfile(slotId, "Challenge Runner");
                store.SaveActiveRun(slotId, new RunSaveSnapshot { runId = "existing", branchSeed = 123, currentRoomId = "origin" });
                var selectedContext = new SelectedProfileContext();
                var viewModel = new MainMenuViewModel(store, selectedContext, new AppStateMachine(), LoadChallengeCatalog());
                viewModel.SelectOrCreateSlot(profile.SlotIndex);

                viewModel.OpenChallenges();
                var route = viewModel.LaunchChallenge("stone_oath", HollowPlatformKind.VisionOSImmersive);

                Assert.AreEqual(AppShellRoute.GameVisionOSImmersive, route);
                Assert.AreEqual(MainMenuState.Launching, viewModel.State);
                Assert.AreEqual("stone_oath", selectedContext.SelectedChallengeId);
                Assert.AreEqual("heavy", selectedContext.SelectedCharacterId);
                Assert.IsTrue(store.TryLoadActiveRun(slotId, out var preserved));
                Assert.AreEqual(123, preserved.branchSeed);
                Assert.IsTrue(store.LoadSlotSummaries()[0].HasActiveRun);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Test]
        public void GameSessionStateCarriesTransientChallengeIdentity()
        {
            var profile = new ProfileSlotSummary(0, "profile-0", "Challenge Runner", 0, 0, 0, true, 0, 0);

            var state = GameSessionState.Create(
                RuntimeSessionMode.TransientChallenge,
                HollowPlatformKind.WindowsStandard3D,
                RunLaunchMode.NewRun,
                profile,
                Vector3.zero,
                "balanced",
                "blade_trial");

            Assert.AreEqual(RuntimeSessionMode.TransientChallenge, state.SessionMode);
            Assert.AreEqual("blade_trial", state.SelectedChallengeId);
            Assert.IsFalse(TransientSessionGuard.CanPersist(state.SessionMode, state.HasProfile));
        }

        [Test]
        public void BranchSessionUsesChallengeSeedAndRules()
        {
            var root = CreateBranchHarness(out var branch, out var player);
            try
            {
                var profile = new ProfileSlotSummary(0, "profile-0", "Challenge Runner", 0, 0, 0, false, 0, 0);
                var state = GameSessionState.Create(
                    RuntimeSessionMode.TransientChallenge,
                    HollowPlatformKind.WindowsStandard3D,
                    RunLaunchMode.NewRun,
                    profile,
                    Vector3.zero,
                    "balanced",
                    "glass_runner");

                branch.Initialize(ImportSampleRoom(), state);

                Assert.AreEqual("glass_runner", branch.ActiveChallenge.ChallengeId);
                Assert.AreEqual(35002, branch.RunSeed);
                Assert.AreEqual(12, branch.RunEconomy.RunCoins);
                Assert.AreEqual("Challenge", branch.SaveStatus);
                Assert.AreEqual(4, player.GetComponent<CombatantHealth>().MaxHealth);
                Assert.IsTrue(branch.PlayerBuildHudSummary.Contains("Challenge: Glass Runner"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Milestone35ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Hollow.Editor.Validation.Milestone35Validator.Validate());
        }

        private static GameObject CreateBranchHarness(out BranchSessionController branch, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M35BranchHarness");

            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            roomObject.AddComponent<RoomRuntimeRoot>();

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);

            var enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyPrefab.name = "EnemyBase_TestPrefab";
            enemyPrefab.transform.SetParent(root.transform, false);
            enemyPrefab.SetActive(false);
            enemyPrefab.AddComponent<CombatantHealth>();
            enemyPrefab.AddComponent<EnemyRuntimeController>();

            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = "ProjectileBase_TestPrefab";
            projectilePrefab.transform.SetParent(root.transform, false);
            projectilePrefab.SetActive(false);
            projectilePrefab.AddComponent<ProjectileController>();

            var combat = root.AddComponent<RoomCombatController>();
            combat.Configure(enemyPrefab, projectilePrefab, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());

            branch = root.AddComponent<BranchSessionController>();
            branch.Configure(null, null);
            branch.ConfigureTemplateCatalog(AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath), BranchGenerator.DefaultSeededMacroSeed);
            branch.ConfigureGenerationSettings(AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath));
            branch.ConfigureRewardPools(
                AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.StandardRewardPoolPath),
                AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.TreasureRewardPoolPath),
                AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.BossRewardPoolPath));
            branch.ConfigureWeaponRewardPool(AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath));
            branch.ConfigureWeaponCatalog(AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath));
            branch.ConfigureCharacterCatalog(AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(Milestone29AssetGenerator.CharacterCatalogPath));
            branch.ConfigureChallengeCatalog(LoadChallengeCatalog());
            return root;
        }

        private static ChallengeCatalogDefinition LoadChallengeCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalogDefinition>(Milestone35AssetGenerator.ChallengeCatalogPath);
            Assert.IsNotNull(catalog, "Run M35 generation before validating challenge catalog.");
            return catalog;
        }

        private static ImportedRoomRuntimeAsset ImportSampleRoom()
        {
            return HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
        }
    }
}
