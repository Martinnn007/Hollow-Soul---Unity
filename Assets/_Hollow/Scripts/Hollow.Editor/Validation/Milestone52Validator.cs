using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone52Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Rewards/ChestKind.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/ChestState.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/CoinDenomination.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/ChestRewardResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/CoinDenominationResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/RoomChestController.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/CoinPickupController.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone52AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone52Validator.cs",
            Milestone52AssetGenerator.DocsPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 52 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M52 file: {file}");
                }
            }

            ValidateRewardRolls(failures);
            ValidateChestContents(failures);
            ValidateCoins(failures);
            ValidateRoomDesigner(failures);
            ValidatePresentationRoles(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 52 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateRewardRolls(List<string> failures)
        {
            var standard = ScriptableObject.CreateInstance<RewardPoolDefinition>();
            standard.Configure(ProceduralRewardResolver.PreBetaStandardPoolId, System.Array.Empty<RewardDefinition>());
            try
            {
                var sawGolden = false;
                var sawNormal = false;
                var sawCoins = false;
                var sawHeal = false;
                var sawNothing = false;
                for (var seed = 52001; seed < 53000; seed++)
                {
                    var plan = ProceduralRewardResolver.CreateSeededPlan(CreateGraph(seed), standard, null, null);
                    if (!plan.TryResolve("combat_01", out var grant))
                    {
                        failures.Add("M52 standard reward plan did not include an authoritative combat room entry.");
                        break;
                    }

                    if (grant.IsEmpty)
                    {
                        sawNothing = true;
                        continue;
                    }

                    sawGolden |= grant.RewardId == ChestRewardResolver.GoldenChestRewardId;
                    sawNormal |= grant.RewardId == ChestRewardResolver.NormalChestRewardId;
                    sawCoins |= grant.RewardId == ChestRewardResolver.SmallCoinPouchRewardId && grant.Coins > 0;
                    sawHeal |= grant.RewardKind == RewardKind.Heal;
                    if (grant.RewardKind is RewardKind.PassiveItem or RewardKind.Card or RewardKind.PassiveCard or RewardKind.ActiveItem or RewardKind.ConsumableCard or RewardKind.Weapon or RewardKind.Armor or RewardKind.Shield)
                    {
                        failures.Add($"Standard room rolled forbidden build-changing reward {grant.RewardId}.");
                        break;
                    }
                }

                if (!sawGolden || !sawNormal || !sawCoins || !sawHeal || !sawNothing)
                {
                    failures.Add("M52 standard reward roll must include golden chest, normal chest, loose coins, HP refill, and no reward across deterministic seeds.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(standard);
            }
        }

        private static void ValidateChestContents(List<string> failures)
        {
            var normalSawCoins = false;
            var normalSawHeal = false;
            var goldenSawCoins = false;
            var goldenSawHeal = false;
            var goldenSawCard = false;
            for (var seed = 52001; seed < 53000; seed++)
            {
                var normal = ChestRewardResolver.ResolveContents("m52_test", seed, "combat_01", ChestKind.Normal);
                normalSawCoins |= normal.CoinValue >= 8 && normal.CoinValue <= 14 && normal.RewardGrant.IsEmpty;
                normalSawHeal |= normal.RewardGrant.RewardKind == RewardKind.Heal;

                var golden = ChestRewardResolver.ResolveContents("m52_test", seed, "combat_01", ChestKind.Golden);
                goldenSawCoins |= golden.CoinValue >= 15 && golden.CoinValue <= 30 && golden.RewardGrant.IsEmpty;
                goldenSawHeal |= golden.CoinValue > 0 && golden.RewardGrant.RewardKind == RewardKind.Heal;
                goldenSawCard |= golden.CoinValue > 0 && golden.RewardGrant.RewardKind is RewardKind.PassiveCard or RewardKind.ConsumableCard;
            }

            if (!normalSawCoins || !normalSawHeal || !goldenSawCoins || !goldenSawHeal || !goldenSawCard)
            {
                failures.Add("M52 chest contents must cover normal coin/heal and golden coin/heal/card outcomes.");
            }
        }

        private static void ValidateCoins(List<string> failures)
        {
            if (CoinDenominationResolver.ValueFor(CoinDenomination.Copper) != 1 ||
                CoinDenominationResolver.ValueFor(CoinDenomination.Silver) != 5 ||
                CoinDenominationResolver.ValueFor(CoinDenomination.Gold) != 10)
            {
                failures.Add("M52 coin denomination values must be Copper=1, Silver=5, Gold=10.");
            }

            for (var value = 1; value <= 30; value++)
            {
                var coins = CoinDenominationResolver.ResolveExactValue(value, 52000 + value);
                if (coins.Sum(denomination => CoinDenominationResolver.ValueFor(denomination)) != value || coins.Count > CoinDenominationResolver.DefaultMaxPhysicalCoins)
                {
                    failures.Add($"M52 coin mix failed exact/max-count validation for value {value}.");
                    break;
                }
            }
        }

        private static void ValidateRoomDesigner(List<string> failures)
        {
            if (!System.Enum.IsDefined(typeof(RoomDesignerTool), RoomDesignerTool.ChestSpawn))
            {
                failures.Add("Room Designer must expose a chest spawn marker tool.");
                return;
            }

            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "M52 Chest Marker Validation");
            project.markers.Add(new RoomDesignerMarker("spawn_chest_validation", RoomDesignerMarkerKinds.ChestSpawn, 1, 0f, 0));
            var json = RoomDesignerCompiler.ExportRuntimeJson(project);
            if (!Hollow.Rooms.HollowRuntimeV2Importer.TryImport(json, out var asset, out var error))
            {
                failures.Add($"M52 Room Designer chest marker roundtrip failed: {error}");
            }
            else if (asset.ItemSpawns.All(spawn => spawn.kind != RoomDesignerMarkerKinds.ChestSpawn))
            {
                failures.Add("M52 Room Designer export did not preserve the chest spawn marker.");
            }
        }

        private static void ValidatePresentationRoles(List<string> failures)
        {
            foreach (var role in new[]
                     {
                         PresentationPrefabRole.ChestNormal,
                         PresentationPrefabRole.ChestGolden,
                         PresentationPrefabRole.CoinCopper,
                         PresentationPrefabRole.CoinSilver,
                         PresentationPrefabRole.CoinGold
                     })
            {
                if (!System.Enum.IsDefined(typeof(PresentationPrefabRole), role))
                {
                    failures.Add($"Missing M52 presentation role {role}.");
                }
            }
        }

        private static BranchFloorGraph CreateGraph(int seed)
        {
            var graph = new BranchFloorGraph(BranchGenerator.DirectedEncounterBranchId, seed);
            graph.AddRoom(CreateRoom("origin", BranchRoomRole.Origin, Vector2Int.zero));
            graph.AddRoom(CreateRoom("combat_01", BranchRoomRole.Combat, new Vector2Int(1, 0)));
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("combat_01"), "east", "west");
            return graph;
        }

        private static BranchRoomState CreateRoom(string id, BranchRoomRole role, Vector2Int cell)
        {
            return new BranchRoomState(
                new BranchRoomId(id),
                cell,
                new BranchRoomInstanceId(id),
                "test_room",
                new Hollow.Rooms.RoomInstanceFootprint(cell, new[] { cell }, new Vector2Int(13, 7)),
                role);
        }
    }
}
