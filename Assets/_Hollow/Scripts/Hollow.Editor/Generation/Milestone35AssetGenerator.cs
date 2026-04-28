using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.UI.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone35AssetGenerator
    {
        public const string ChallengeDirectory = "Assets/_Hollow/Data/Challenges/M35";
        public const string ChallengeCatalogPath = ChallengeDirectory + "/ChallengeCatalog_M35.asset";
        public const string BaselineReportPath = "output/reports/m35_challenge_mode_v1.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 35 Assets")]
        public static void Generate()
        {
            Milestone34AssetGenerator.Generate();
            Directory.CreateDirectory(ChallengeDirectory);

            var blade = SaveChallenge(
                "Challenge_BladeTrial.asset",
                "blade_trial",
                "Blade Trial",
                35001,
                "balanced",
                new CharacterStatModifier(maxHealth: -1, meleeDamage: 1),
                8,
                0,
                new[] { "Fixed seed 35001.", "+1 melee damage.", "-1 max HP.", "Start with 8 coins." });
            var glass = SaveChallenge(
                "Challenge_GlassRunner.asset",
                "glass_runner",
                "Glass Runner",
                35002,
                "balanced",
                new CharacterStatModifier(maxHealth: -2, speed: 0.45f, maxStamina: 10f),
                12,
                0,
                new[] { "Fixed seed 35002.", "+0.45 speed.", "+10 stamina.", "-2 max HP." });
            var stone = SaveChallenge(
                "Challenge_StoneOath.asset",
                "stone_oath",
                "Stone Oath",
                35003,
                "heavy",
                new CharacterStatModifier(speed: -0.25f, defense: 2, staminaRegen: -1f),
                6,
                0,
                new[] { "Fixed seed 35003.", "Heavy character.", "+2 defense.", "-0.25 speed and -1 stamina regen." });

            var catalog = SaveCatalog(new[] { blade, glass, stone });
            AssignToGameScenes(catalog);
            AssignToMainMenu(catalog);
            WriteReport();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static ChallengeDefinition SaveChallenge(
            string fileName,
            string challengeId,
            string displayName,
            int fixedRunSeed,
            string selectedCharacterId,
            CharacterStatModifier modifier,
            int coins,
            int souls,
            IEnumerable<string> rules)
        {
            var path = $"{ChallengeDirectory}/{fileName}";
            var challenge = AssetDatabase.LoadAssetAtPath<ChallengeDefinition>(path);
            if (challenge == null)
            {
                challenge = ScriptableObject.CreateInstance<ChallengeDefinition>();
                AssetDatabase.CreateAsset(challenge, path);
            }

            challenge.Configure(challengeId, displayName, fixedRunSeed, selectedCharacterId, modifier, coins, souls, rules);
            EditorUtility.SetDirty(challenge);
            return challenge;
        }

        private static ChallengeCatalogDefinition SaveCatalog(IEnumerable<ChallengeDefinition> challenges)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalogDefinition>(ChallengeCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ChallengeCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, ChallengeCatalogPath);
            }

            catalog.Configure("m35_challenge_catalog_v1", challenges);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AssignToGameScenes(ChallengeCatalogDefinition catalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureChallengeCatalog(catalog);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void AssignToMainMenu(ChallengeCatalogDefinition catalog)
        {
            const string scenePath = "Assets/_Hollow/Scenes/MainMenu.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
            {
                throw new MissingComponentException($"{scenePath} is missing MainMenuController.");
            }

            controller.ConfigureChallengeCatalog(catalog);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WriteReport()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BaselineReportPath) ?? "output/reports");
            File.WriteAllText(
                BaselineReportPath,
                "# M35 Challenge Mode V1\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                "- Scope: fixed-seed challenge catalog, transient challenge launch flow, curated stat/currency rules, and scene wiring.\n" +
                "- Challenges: Blade Trial, Glass Runner, Stone Oath.\n" +
                "- Verification: run Milestone35Validator and the M32 QA gate.\n");
        }
    }
}
