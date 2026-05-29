using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone46EncounterDirectorDifficultyCurveTests
    {
        [Test]
        public void DirectorProfileDefaultsLockWorldLengthsAndSpawnCap()
        {
            var profile = EncounterDirectorProfileDefinition.CreateRuntimeDefault();
            try
            {
                Assert.AreEqual(8, profile.WorldConfigFor(1).TargetRoomCount);
                Assert.AreEqual(10, profile.WorldConfigFor(2).TargetRoomCount);
                Assert.AreEqual(12, profile.WorldConfigFor(3).TargetRoomCount);
                Assert.AreEqual(0, profile.WorldConfigFor(1).DifficultyOffset);
                Assert.AreEqual(1, profile.WorldConfigFor(2).DifficultyOffset);
                Assert.AreEqual(2, profile.WorldConfigFor(3).DifficultyOffset);
                Assert.AreEqual(6, profile.MaxNonBossEnemySpawns);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void DirectedEncounterPlanSkipsSafeRoomsAndCapsNonBossSpawns()
        {
            var profile = EncounterDirectorProfileDefinition.CreateRuntimeDefault();
            var catalog = CreateCatalog();
            try
            {
                var graph = CreateGraph();
                var plan = EncounterResolver.CreateDirectedSeededPlan(graph, catalog, 46001, 3, profile);

                Assert.IsFalse(plan.TryResolve("origin", out _), "Starter/origin rooms must stay no-combat.");
                Assert.IsFalse(plan.TryResolve("reward_01", out _), "M134 Reward rooms stay no-combat.");
                Assert.IsFalse(plan.TryResolve("treasure_01", out _), "Treasure rooms stay no-combat.");
                Assert.IsFalse(plan.TryResolve("secret_01", out _), "Secret rooms stay no-combat.");
                Assert.IsTrue(plan.TryResolve("combat_01", out var combat));
                Assert.IsTrue(plan.TryResolve("boss_01", out var boss));

                Assert.AreEqual(3, combat.WorldIndex);
                Assert.AreEqual(3, combat.DifficultyBand);
                Assert.LessOrEqual(combat.EnemySpawnKinds.Count, 6);
                Assert.AreEqual("stone_warden_boss", boss.EncounterId);
                Assert.AreEqual(1, boss.EnemySpawnKinds.Count);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                foreach (var encounter in catalog.Encounters.Distinct())
                {
                    Object.DestroyImmediate(encounter);
                }

                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void DirectedEncounterPlanIsDeterministicAndPersistsMetadata()
        {
            var profile = EncounterDirectorProfileDefinition.CreateRuntimeDefault();
            var catalog = CreateCatalog();
            try
            {
                var graph = CreateGraph();
                var first = EncounterResolver.CreateDirectedSeededPlan(graph, catalog, 46002, 2, profile);
                var second = EncounterResolver.CreateDirectedSeededPlan(graph, catalog, 46002, 2, profile);

                Assert.AreEqual(Signature(first), Signature(second));

                var restored = EncounterPlan.FromSaveState(first.ToSaveState());
                Assert.AreEqual(Signature(first), Signature(restored));
                Assert.IsTrue(restored.Assignments.All(assignment => assignment.WorldIndex == 2));
                Assert.IsTrue(restored.Assignments.Any(assignment => assignment.DifficultyBand > 0));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                foreach (var encounter in catalog.Encounters.Distinct())
                {
                    Object.DestroyImmediate(encounter);
                }

                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CombatEncounterContextFormatsDirectorDebugLine()
        {
            var context = new RoomCombatEncounterContext(
                "turret_crossfire",
                new[] { "spawnEnemyTurret", "spawnEnemyFast" },
                worldIndex: 2,
                difficultyBand: 4,
                directorPressure: 3);

            Assert.AreEqual("Director: W2 B4 | turret_crossfire", context.DirectorDebugLine);
        }

        private static BranchFloorGraph CreateGraph()
        {
            var graph = new BranchFloorGraph(BranchGenerator.DirectedEncounterBranchId, 46001);
            graph.AddRoom(CreateRoom("origin", BranchRoomRole.Origin, Vector2Int.zero));
            graph.AddRoom(CreateRoom("combat_01", BranchRoomRole.Combat, new Vector2Int(1, 0)));
            graph.AddRoom(CreateRoom("reward_01", BranchRoomRole.Reward, new Vector2Int(2, 0)));
            graph.AddRoom(CreateRoom("boss_01", BranchRoomRole.Boss, new Vector2Int(3, 0)));
            graph.AddRoom(CreateRoom("treasure_01", BranchRoomRole.Treasure, new Vector2Int(0, 1)));
            graph.AddRoom(CreateRoom("secret_01", BranchRoomRole.Secret, new Vector2Int(0, -1)));
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("combat_01"), "east", "west");
            graph.AddBidirectionalConnection(new BranchRoomId("combat_01"), new BranchRoomId("reward_01"), "east", "west");
            graph.AddBidirectionalConnection(new BranchRoomId("reward_01"), new BranchRoomId("boss_01"), "east", "west");
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("treasure_01"), "south", "north");
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("secret_01"), "north", "south");
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

        private static EncounterCatalogDefinition CreateCatalog()
        {
            var combat = ScriptableObject.CreateInstance<EncounterDefinition>();
            combat.Configure(
                "crowded_combat",
                "Crowded Combat",
                BranchRoomRole.Combat,
                0,
                99,
                1,
                99,
                1,
                new[] { new EncounterSpawnEntry("spawnEnemyNormal", 8) });
            var reward = ScriptableObject.CreateInstance<EncounterDefinition>();
            reward.Configure(
                "crowded_reward",
                "Crowded Reward",
                BranchRoomRole.Reward,
                0,
                99,
                1,
                99,
                1,
                new[] { new EncounterSpawnEntry("spawnEnemyFast", 7) });
            var boss = ScriptableObject.CreateInstance<EncounterDefinition>();
            boss.Configure(
                "stone_warden_boss",
                "Stone Warden",
                BranchRoomRole.Boss,
                0,
                99,
                1,
                99,
                1,
                new[] { new EncounterSpawnEntry("spawnEnemyBoss", 1) });
            var catalog = ScriptableObject.CreateInstance<EncounterCatalogDefinition>();
            catalog.Configure("m46_test_catalog", new[] { combat, reward, boss }, boss);
            return catalog;
        }

        private static string Signature(EncounterPlan plan)
        {
            return string.Join("|", plan.Assignments
                .OrderBy(assignment => assignment.RoomId)
                .Select(assignment => $"{assignment.RoomId}:{assignment.WorldIndex}:{assignment.DifficultyBand}:{assignment.DirectorPressure}:{assignment.EncounterId}:{string.Join(",", assignment.EnemySpawnKinds)}"));
        }
    }
}
