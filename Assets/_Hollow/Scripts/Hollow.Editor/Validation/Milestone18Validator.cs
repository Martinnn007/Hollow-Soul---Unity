using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone18Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Rewards/RewardPoolDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/RewardEffect.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone18AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone18Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone18SeededRewardPoolTests.cs",
            "Docs/Milestone18SeededRandomRewards.md",
            Milestone18AssetGenerator.StandardRewardPoolPath,
            Milestone18AssetGenerator.TreasureRewardPoolPath,
            Milestone18AssetGenerator.BossRewardPoolPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 18 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M18 file: {file}");
                }
            }

            var standard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone18AssetGenerator.StandardRewardPoolPath);
            var treasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone18AssetGenerator.TreasureRewardPoolPath);
            var boss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone18AssetGenerator.BossRewardPoolPath);
            ValidatePools(standard, treasure, boss, failures);
            ValidateSeededPlan(standard, treasure, boss, failures);
            ValidateScenes(standard, treasure, boss, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 18 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidatePools(RewardPoolDefinition standard, RewardPoolDefinition treasure, RewardPoolDefinition boss, List<string> failures)
        {
            if (standard == null || standard.Rewards.Count < 6)
            {
                failures.Add("M18 standard reward pool must contain at least six rewards.");
            }

            if (treasure == null || !treasure.Rewards.Any(reward => reward != null && reward.Rarity == RewardRarity.Treasure))
            {
                failures.Add("M18 treasure reward pool must contain a treasure-tier reward.");
            }

            if (boss == null || !boss.Rewards.Any(reward => reward != null && reward.Rarity == RewardRarity.Boss))
            {
                failures.Add("M18 boss reward pool must contain a boss-tier reward.");
            }

            foreach (var reward in new[] { standard, treasure, boss }.Where(pool => pool != null).SelectMany(pool => pool.Rewards).Where(reward => reward != null))
            {
                if (string.IsNullOrWhiteSpace(reward.RewardId) || string.IsNullOrWhiteSpace(reward.DisplayName))
                {
                    failures.Add("M18 reward pools contain a reward with missing identity.");
                }
            }
        }

        private static void ValidateSeededPlan(RewardPoolDefinition standard, RewardPoolDefinition treasure, RewardPoolDefinition boss, List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "missing sample room";
            if (catalog == null || settings == null || sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out importError))
            {
                failures.Add($"M18 could not import branch content: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M18 branch content import error: {contentError}");
                return;
            }

            var graph = BranchGenerator.CreateSeededFeatureBranch(content, settings, settings.DefaultSeed);
            var first = ProceduralRewardResolver.CreateSeededPlan(graph, standard, treasure, boss);
            var second = ProceduralRewardResolver.CreateSeededPlan(graph, standard, treasure, boss);
            if (!Signature(first).Equals(Signature(second), System.StringComparison.Ordinal))
            {
                failures.Add("M18 seeded reward plan is not deterministic for the same branch seed.");
            }

            var treasureRoom = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Treasure);
            var bossRoom = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Boss);
            if (treasureRoom == null || !first.TryResolve(treasureRoom.Id.Value, out var treasureGrant) || treasureGrant.Souls < 10)
            {
                failures.Add("M18 reward plan must resolve a treasure-room reward.");
            }

            if (bossRoom == null || !first.TryResolve(bossRoom.Id.Value, out var bossGrant) || bossGrant.RewardKind != RewardKind.PassiveItem)
            {
                failures.Add("M18 reward plan must resolve a boss reward.");
            }
        }

        private static void ValidateScenes(RewardPoolDefinition standard, RewardPoolDefinition treasure, RewardPoolDefinition boss, List<string> failures)
        {
            var successorStandard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.StandardRewardPoolPath);
            var successorTreasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.TreasureRewardPoolPath);
            var successorBoss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.BossRewardPoolPath);

            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (!IsCompatiblePool(branch.StandardRewardPool, standard, successorStandard, minRewards: 6, requiredRarity: null) ||
                    !IsCompatiblePool(branch.TreasureRewardPool, treasure, successorTreasure, minRewards: 1, requiredRarity: RewardRarity.Treasure) ||
                    !IsCompatiblePool(branch.BossRewardPool, boss, successorBoss, minRewards: 1, requiredRarity: RewardRarity.Boss))
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to M18-compatible reward pools.");
                }
            }
        }

        private static bool IsCompatiblePool(
            RewardPoolDefinition assigned,
            RewardPoolDefinition milestone18Pool,
            RewardPoolDefinition successorPool,
            int minRewards,
            RewardRarity? requiredRarity)
        {
            if (assigned == null)
            {
                return false;
            }

            if (assigned == milestone18Pool || assigned == successorPool)
            {
                return HasRequiredShape(assigned, minRewards, requiredRarity);
            }

            return HasRequiredShape(assigned, minRewards, requiredRarity);
        }

        private static bool HasRequiredShape(RewardPoolDefinition pool, int minRewards, RewardRarity? requiredRarity)
        {
            if (pool == null || pool.Rewards.Count < minRewards)
            {
                return false;
            }

            if (pool.Rewards.Any(reward => reward == null || string.IsNullOrWhiteSpace(reward.RewardId) || string.IsNullOrWhiteSpace(reward.DisplayName)))
            {
                return false;
            }

            return !requiredRarity.HasValue || pool.Rewards.Any(reward => reward != null && reward.Rarity == requiredRarity.Value);
        }

        private static string Signature(ProceduralRewardPlan plan)
        {
            return string.Join("|", plan.Rewards
                .OrderBy(reward => reward.RoomId)
                .Select(reward => $"{reward.RoomId}:{reward.RewardId}:{reward.Souls}:{string.Join(",", reward.Effects.Select(effect => $"{effect.Kind}:{effect.IntValue}:{effect.FloatValue:0.###}"))}"));
        }
    }
}
