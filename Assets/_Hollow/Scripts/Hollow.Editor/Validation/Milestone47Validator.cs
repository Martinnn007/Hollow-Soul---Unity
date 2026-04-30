using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Persistence;
using Hollow.UI.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone47Validator
    {
        private static readonly Dictionary<string, int> ExpectedSeeds = new()
        {
            ["blade_trial"] = 47001,
            ["glass_runner"] = 47002,
            ["stone_oath"] = 47003,
            ["macro_maze"] = 47004,
            ["splitter_swarm"] = 47005,
            ["merchants_debt"] = 47006
        };

        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ChallengeRuleKind.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ChallengeRuleDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ChallengeRunLoadout.cs",
            "Assets/_Hollow/Scripts/Hollow.Persistence/IChallengeResultStore.cs",
            "Assets/_Hollow/Scripts/Hollow.Persistence/ChallengeResultRecord.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone47AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone47Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone47ChallengeModeV2CuratedSeedsTests.cs",
            "Docs/Milestone47ChallengeModeV2CuratedSeeds.md",
            Milestone47AssetGenerator.ChallengeCatalogPath,
            Milestone47AssetGenerator.ReportPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 47 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M47 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalogDefinition>(Milestone47AssetGenerator.ChallengeCatalogPath);
            ValidateCatalog(catalog, failures);
            ValidateSceneWiring(catalog, failures);
            ValidateProfileChallengeRecords(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 47 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateCatalog(ChallengeCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("Missing M47 challenge catalog asset.");
                return;
            }

            if (catalog.CatalogId != Milestone47AssetGenerator.CatalogId)
            {
                failures.Add("M47 challenge catalog must use the V2 catalog id.");
            }

            if (catalog.Challenges.Count != ExpectedSeeds.Count)
            {
                failures.Add($"M47 challenge catalog must contain exactly {ExpectedSeeds.Count} curated challenges.");
            }

            foreach (var pair in ExpectedSeeds)
            {
                if (!catalog.TryGetChallenge(pair.Key, out var challenge))
                {
                    failures.Add($"M47 challenge catalog is missing '{pair.Key}'.");
                    continue;
                }

                if (challenge.FixedRunSeed != pair.Value)
                {
                    failures.Add($"M47 challenge '{pair.Key}' must use seed {pair.Value}.");
                }

                if (string.IsNullOrWhiteSpace(challenge.SelectedCharacterId) ||
                    string.IsNullOrWhiteSpace(challenge.DisplayName) ||
                    challenge.Rules.Count == 0 ||
                    string.IsNullOrWhiteSpace(challenge.Loadout.MeleeWeaponId) ||
                    string.IsNullOrWhiteSpace(challenge.Loadout.RangedWeaponId))
                {
                    failures.Add($"M47 challenge '{pair.Key}' must have display data, rules, character, and starter weapons.");
                }
            }

            ValidateRule(catalog, "blade_trial", ChallengeRuleKind.BlockShops, failures);
            ValidateRule(catalog, "glass_runner", ChallengeRuleKind.BlockHealingRewards, failures);
            ValidateRule(catalog, "splitter_swarm", ChallengeRuleKind.EncounterPressureBonus, failures);
        }

        private static void ValidateRule(ChallengeCatalogDefinition catalog, string challengeId, ChallengeRuleKind expectedRule, List<string> failures)
        {
            if (catalog != null && catalog.TryGetChallenge(challengeId, out var challenge) && !challenge.HasRule(expectedRule))
            {
                failures.Add($"M47 challenge '{challengeId}' must include rule {expectedRule}.");
            }
        }

        private static void ValidateSceneWiring(ChallengeCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                return;
            }

            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindAnyObjectByType<BranchSessionController>();
                if (branch == null || branch.ChallengeCatalog != catalog)
                {
                    failures.Add($"{scenePath} BranchSessionController must reference the M47 challenge catalog.");
                }
            }

            var menuScene = EditorSceneManager.OpenScene("Assets/_Hollow/Scenes/MainMenu.unity");
            var mainMenu = Object.FindAnyObjectByType<MainMenuController>();
            if (mainMenu == null || mainMenu.ChallengeCatalog != catalog)
            {
                failures.Add("MainMenuController must reference the M47 challenge catalog.");
            }
        }

        private static void ValidateProfileChallengeRecords(List<string> failures)
        {
            var root = Path.Combine(Path.GetTempPath(), "hollow_m47_validation", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var store = new JsonProfileStore(root);
                var slotId = new ProfileSlotId(0);
                store.CreateOrLoadProfile(slotId, "M47 Validator");
                store.SaveActiveRun(slotId, new RunSaveSnapshot { runId = "preserved", branchSeed = 123, currentRoomId = "origin" });
                store.MarkChallengeAttemptStarted(slotId, "blade_trial", 47001);
                store.CompleteChallengeAttempt(slotId, "blade_trial", 47001, 380f);
                store.CompleteChallengeAttempt(slotId, "blade_trial", 47001, 310f);
                store.CompleteChallengeAttempt(slotId, "blade_trial", 47001, 400f);

                var record = store.GetChallengeRecord(slotId, "blade_trial");
                if (record.Attempts != 3 || record.Completions != 3 || Mathf.RoundToInt(record.BestClearTimeSeconds) != 310)
                {
                    failures.Add("M47 challenge records must persist attempts, completions, and improved best clear time.");
                }

                if (!store.TryLoadActiveRun(slotId, out var snapshot) || snapshot.branchSeed != 123)
                {
                    failures.Add("M47 challenge record writes must not clear or mutate active-run snapshots.");
                }
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }
    }
}
