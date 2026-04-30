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
    public static class Milestone51Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone51AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone51Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone51PreBetaRewardHealthRebalanceTests.cs",
            Milestone51AssetGenerator.DocsPath,
            Milestone51AssetGenerator.StandardRewardPoolPath,
            Milestone51AssetGenerator.TreasureRewardPoolPath,
            Milestone51AssetGenerator.BossRewardPoolPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 51 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M51 file: {file}");
                }
            }

            var standard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone51AssetGenerator.StandardRewardPoolPath);
            var treasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone51AssetGenerator.TreasureRewardPoolPath);
            var boss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone51AssetGenerator.BossRewardPoolPath);
            ValidatePools(standard, treasure, boss, failures);
            ValidateResolver(standard, treasure, boss, failures);
            ValidateCharacters(failures);
            ValidateScenes(standard, treasure, boss, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 51 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidatePools(RewardPoolDefinition standard, RewardPoolDefinition treasure, RewardPoolDefinition boss, List<string> failures)
        {
            if (standard == null || standard.PoolId != ProceduralRewardResolver.PreBetaStandardPoolId)
            {
                failures.Add("M51 standard reward pool is missing or has the wrong pool id.");
                return;
            }

            if (standard.Rewards.Any(IsBuildChangingReward))
            {
                failures.Add("M51 standard reward pool must not contain build-changing rewards.");
            }

            foreach (var requiredId in new[] { "small_coin_pouch", "hp_refill", "standard_treasure_chest" })
            {
                if (standard.Rewards.All(reward => reward.RewardId != requiredId))
                {
                    failures.Add($"M51 standard reward pool is missing {requiredId}.");
                }
            }

            if (treasure == null || !treasure.Rewards.Any(IsBuildChangingReward))
            {
                failures.Add("M51 treasure reward pool must contain item/gear rewards.");
            }

            if (boss == null || !boss.Rewards.Any(IsBuildChangingReward))
            {
                failures.Add("M51 boss reward pool must contain item/gear rewards.");
            }
        }

        private static void ValidateResolver(RewardPoolDefinition standard, RewardPoolDefinition treasure, RewardPoolDefinition boss, List<string> failures)
        {
            if (standard == null)
            {
                return;
            }

            var sawCoins = false;
            var sawHeal = false;
            var sawChest = false;
            var sawNothing = false;
            for (var seed = 51001; seed < 51160; seed++)
            {
                var plan = ProceduralRewardResolver.CreateSeededPlan(CreateGraph(seed), standard, treasure, boss);
                if (!plan.TryResolve("combat_01", out var grant))
                {
                    failures.Add("M51 standard reward plan did not include an authoritative combat room entry.");
                    break;
                }

                if (grant.IsEmpty)
                {
                    sawNothing = true;
                    continue;
                }

                if (IsBuildChangingGrant(grant))
                {
                    failures.Add($"M51 standard room resolved forbidden build reward {grant.RewardId}.");
                    break;
                }

                sawCoins |= grant.RewardKind == RewardKind.Currency && grant.RewardId == "small_coin_pouch";
                sawHeal |= grant.RewardKind == RewardKind.Heal;
                sawChest |= grant.RewardId == "standard_treasure_chest";
            }

            if (!sawCoins || !sawHeal || !sawChest || !sawNothing)
            {
                failures.Add("M51 sparse standard rewards must be able to resolve coins, heal, chest, and nothing across seeds.");
            }
        }

        private static void ValidateCharacters(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(Milestone29AssetGenerator.CharacterCatalogPath);
            if (catalog == null)
            {
                failures.Add("Missing M29 character catalog for M51 health rebalance.");
                return;
            }

            if (!catalog.TryGetCharacter("balanced", out var balanced) || balanced.BaseStats.MaxHealth != 3)
            {
                failures.Add("M51 Balanced character must start with 3 max HP.");
            }

            if (!catalog.TryGetCharacter("heavy", out var heavy) || heavy.BaseStats.MaxHealth != 5)
            {
                failures.Add("M51 Heavy character must be rescaled to 5 max HP.");
            }
        }

        private static void ValidateScenes(RewardPoolDefinition standard, RewardPoolDefinition treasure, RewardPoolDefinition boss, List<string> failures)
        {
            if (standard == null || treasure == null || boss == null)
            {
                return;
            }

            var m52SuccessorStandard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone52AssetGenerator.StandardRewardPoolPath);
            var m54SuccessorTreasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.TreasureRewardPoolPath);
            var m54SuccessorBoss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.BossRewardPoolPath);
            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                var standardMatches = branch.StandardRewardPool == standard ||
                                      (m52SuccessorStandard != null && branch.StandardRewardPool == m52SuccessorStandard);
                var treasureMatches = branch.TreasureRewardPool == treasure ||
                                      (m54SuccessorTreasure != null && branch.TreasureRewardPool == m54SuccessorTreasure);
                var bossMatches = branch.BossRewardPool == boss ||
                                  (m54SuccessorBoss != null && branch.BossRewardPool == m54SuccessorBoss);
                if (!standardMatches || !treasureMatches || !bossMatches)
                {
                    failures.Add($"{scenePath} BranchSessionController must reference M51 reward pools or approved successor pools.");
                }
            }
        }

        private static BranchFloorGraph CreateGraph(int seed)
        {
            var graph = new BranchFloorGraph(BranchGenerator.DirectedEncounterBranchId, seed);
            graph.AddRoom(CreateRoom("origin", BranchRoomRole.Origin, Vector2Int.zero));
            graph.AddRoom(CreateRoom("combat_01", BranchRoomRole.Combat, new Vector2Int(1, 0)));
            graph.AddRoom(CreateRoom("treasure_01", BranchRoomRole.Treasure, new Vector2Int(2, 0)));
            graph.AddRoom(CreateRoom("boss_01", BranchRoomRole.Boss, new Vector2Int(3, 0)));
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("combat_01"), "east", "west");
            graph.AddBidirectionalConnection(new BranchRoomId("combat_01"), new BranchRoomId("treasure_01"), "east", "west");
            graph.AddBidirectionalConnection(new BranchRoomId("treasure_01"), new BranchRoomId("boss_01"), "east", "west");
            return graph;
        }

        private static BranchRoomState CreateRoom(string id, BranchRoomRole role, Vector2Int cell)
        {
            return new BranchRoomState(
                new BranchRoomId(id),
                cell,
                new BranchRoomInstanceId(id),
                "test_room",
                new RoomInstanceFootprint(cell, new[] { cell }, new Vector2Int(13, 7)),
                role);
        }

        private static bool IsBuildChangingReward(RewardDefinition reward)
        {
            return reward != null && reward.RewardKind is RewardKind.PassiveItem or RewardKind.Card or RewardKind.PassiveCard or RewardKind.ActiveItem or RewardKind.ConsumableCard or RewardKind.Weapon or RewardKind.Armor;
        }

        private static bool IsBuildChangingGrant(RewardGrant grant)
        {
            return grant.RewardKind is RewardKind.PassiveItem or RewardKind.Card or RewardKind.PassiveCard or RewardKind.ActiveItem or RewardKind.ConsumableCard or RewardKind.Weapon or RewardKind.Armor;
        }
    }
}
