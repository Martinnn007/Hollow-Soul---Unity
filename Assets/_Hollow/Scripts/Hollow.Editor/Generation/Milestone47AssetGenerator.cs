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
    public static class Milestone47AssetGenerator
    {
        public const string ChallengeDirectory = "Assets/_Hollow/Data/Challenges/M47";
        public const string ChallengeCatalogPath = ChallengeDirectory + "/ChallengeCatalog_M47.asset";
        public const string ReportPath = "output/reports/m47_challenge_mode_v2_curated_seeds.md";
        public const string CatalogId = "m47_challenge_catalog_v2";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 47 Assets")]
        public static void Generate()
        {
            Milestone46AssetGenerator.Generate();
            Directory.CreateDirectory(ChallengeDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var challenges = GenerateChallenges();
            var catalog = SaveCatalog(challenges);
            AssignToGameScenes(catalog);
            AssignToMainMenu(catalog);
            WriteReport(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 47 challenge catalog, scene wiring, and report.");
        }

        private static IReadOnlyList<ChallengeDefinition> GenerateChallenges()
        {
            return new[]
            {
                SaveChallenge(
                    "Challenge_BladeTrial.asset",
                    "blade_trial",
                    "Blade Trial",
                    47001,
                    "balanced",
                    new CharacterStatModifier(maxHealth: -1, meleeDamage: 1),
                    ChallengeRunLoadout.Create(meleeWeaponId: "iron_cleaver", rangedWeaponId: "starter_pistol", consumableCardId: "ember_card"),
                    8,
                    0,
                    new[] { new ChallengeRuleDefinition(ChallengeRuleKind.BlockShops, displayText: "Shops closed.") },
                    new[] { "Fixed seed 47001.", "Melee-lean starter gear.", "Shops closed.", "-1 max HP, +1 melee damage." }),
                SaveChallenge(
                    "Challenge_GlassRunner.asset",
                    "glass_runner",
                    "Glass Runner",
                    47002,
                    "balanced",
                    new CharacterStatModifier(maxHealth: -2, speed: 0.45f, maxStamina: 10f),
                    ChallengeRunLoadout.Create(meleeWeaponId: "starter_blade", rangedWeaponId: "ember_bolt", consumableCardId: "swift_card"),
                    12,
                    0,
                    new[] { new ChallengeRuleDefinition(ChallengeRuleKind.BlockHealingRewards, displayText: "Healing rewards blocked.") },
                    new[] { "Fixed seed 47002.", "Speed/stamina lean.", "Healing rewards blocked.", "-2 max HP." }),
                SaveChallenge(
                    "Challenge_StoneOath.asset",
                    "stone_oath",
                    "Stone Oath",
                    47003,
                    "heavy",
                    new CharacterStatModifier(speed: -0.25f, defense: 2, staminaRegen: -1f),
                    ChallengeRunLoadout.Create(meleeWeaponId: "iron_cleaver", rangedWeaponId: "starter_pistol", armorId: "dragon_scale_armor", activeItemId: "mending_charm"),
                    6,
                    0,
                    Array.Empty<ChallengeRuleDefinition>(),
                    new[] { "Fixed seed 47003.", "Heavy character.", "Defense/guard lean.", "+2 defense, slower stamina regen." }),
                SaveChallenge(
                    "Challenge_MacroMaze.asset",
                    "macro_maze",
                    "Macro Maze",
                    47004,
                    "balanced",
                    new CharacterStatModifier(maxStamina: 15f),
                    ChallengeRunLoadout.Create(meleeWeaponId: "starter_blade", rangedWeaponId: "bone_pistol", consumableCardId: "swift_card"),
                    10,
                    0,
                    Array.Empty<ChallengeRuleDefinition>(),
                    new[] { "Fixed seed 47004.", "Macro-room traversal showcase.", "+15 stamina.", "Positioning matters." }),
                SaveChallenge(
                    "Challenge_SplitterSwarm.asset",
                    "splitter_swarm",
                    "Splitter Swarm",
                    47005,
                    "balanced",
                    new CharacterStatModifier(rangedDamage: 1),
                    ChallengeRunLoadout.Create(meleeWeaponId: "dragon_fang", rangedWeaponId: "starter_pistol", activeItemId: "echo_burst"),
                    6,
                    0,
                    new[] { new ChallengeRuleDefinition(ChallengeRuleKind.EncounterPressureBonus, 2, "Encounter pressure +2.") },
                    new[] { "Fixed seed 47005.", "Harder M46 encounter bands.", "+1 ranged damage.", "Echo Burst starter." }),
                SaveChallenge(
                    "Challenge_MerchantsDebt.asset",
                    "merchants_debt",
                    "Merchant's Debt",
                    47006,
                    "balanced",
                    new CharacterStatModifier(),
                    ChallengeRunLoadout.Create(meleeWeaponId: "starter_blade", rangedWeaponId: "starter_pistol", activeItemId: "mending_charm", consumableCardId: "mend_card"),
                    2,
                    22,
                    Array.Empty<ChallengeRuleDefinition>(),
                    new[] { "Fixed seed 47006.", "Start with 2 coins.", "Start with 22 souls.", "Shop economy showcase." }),
                SaveChallenge(
                    "Challenge_SmallMonsters.asset",
                    "small_monsters",
                    "Small Monsters",
                    47007,
                    "balanced",
                    new CharacterStatModifier(speed: 0.15f),
                    ChallengeRunLoadout.Create(meleeWeaponId: "starter_blade", rangedWeaponId: "starter_pistol", consumableCardId: "swift_card"),
                    8,
                    0,
                    new[] { new ChallengeRuleDefinition(ChallengeRuleKind.SmallMonstersOnly, displayText: "Non-boss rooms spawn only Rats and Spiders.") },
                    new[] { "Fixed seed 47007.", "Non-boss rooms spawn only Rats and Spiders.", "Boss rooms remain unchanged.", "+0.15 speed for critter footwork." })
            };
        }

        private static ChallengeDefinition SaveChallenge(
            string fileName,
            string challengeId,
            string displayName,
            int fixedRunSeed,
            string selectedCharacterId,
            CharacterStatModifier modifier,
            ChallengeRunLoadout loadout,
            int coins,
            int souls,
            IEnumerable<ChallengeRuleDefinition> ruleDefinitions,
            IEnumerable<string> rules)
        {
            var path = $"{ChallengeDirectory}/{fileName}";
            var challenge = AssetDatabase.LoadAssetAtPath<ChallengeDefinition>(path);
            if (challenge == null)
            {
                challenge = ScriptableObject.CreateInstance<ChallengeDefinition>();
                AssetDatabase.CreateAsset(challenge, path);
            }

            challenge.Configure(challengeId, displayName, fixedRunSeed, selectedCharacterId, modifier, coins, souls, loadout, ruleDefinitions, rules);
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

            catalog.Configure(CatalogId, challenges);
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
            foreach (var scenePath in new[] { "Assets/_Hollow/Scenes/MainMenu.unity", "Assets/_Hollow/Scenes/MainMenu_VisionOS.unity" })
            {
                if (!File.Exists(scenePath))
                {
                    continue;
                }

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
        }

        private static void WriteReport(ChallengeCatalogDefinition catalog)
        {
            File.WriteAllText(
                ReportPath,
                "# M47 Challenge Mode V2 + Curated Seeds\n\n" +
                $"- Catalog: `{catalog.CatalogId}`.\n" +
                "- Challenge runs remain transient and do not mutate active-run saves or bank souls.\n" +
                "- Profile challenge records track attempts, completions, best clear time, last result, and last played seed.\n" +
                "- Curated seeds: `47001` through `47007`.\n" +
                "- `small_monsters` remaps non-boss encounter spawns to Rats and Spiders while preserving boss rooms.\n" +
                "- Runtime path: M46 encounter director branch identity with world lengths `8/10/12` and final extraction completion.\n");
        }
    }
}
