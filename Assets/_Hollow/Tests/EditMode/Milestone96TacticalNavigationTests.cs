using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Core.Diagnostics;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone96TacticalNavigationTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void TacticalDirectorLimitsActiveThreatsToSoulsLikeSlots()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemies = CreateEnemyRing(root.transform, room, player, 8);
                var director = new RoomTacticalDirector();
                director.Tick(enemies, room, player, 4f);

                Assert.GreaterOrEqual(director.ActiveThreatCount, RoomTacticalDirector.MinActiveThreatSlots);
                Assert.LessOrEqual(director.ActiveThreatCount, RoomTacticalDirector.MaxActiveThreatSlots);
                Assert.AreEqual(director.ActiveThreatCount, enemies.Count(enemy => director.ResolveIntent(enemy).Role == EnemyTacticalRole.ActiveThreat));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ActiveThreatsReceiveValidReservedPositions()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemies = CreateEnemyRing(root.transform, room, player, 7);
                var director = new RoomTacticalDirector();
                director.Tick(enemies, room, player, 4f);

                var activeIntents = enemies
                    .Select(enemy => director.ResolveIntent(enemy))
                    .Where(intent => intent.Role == EnemyTacticalRole.ActiveThreat)
                    .ToArray();

                Assert.Greater(activeIntents.Length, 0);
                foreach (var intent in activeIntents)
                {
                    Assert.IsTrue(intent.HasReservedPosition, intent.Summary);
                    Assert.IsTrue(RoomLocalCollision.CanOccupy(room, intent.ReservedLocalPosition, 0.32f), intent.Summary);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NonActiveEnemiesCannotStartCommittedDogpileActions()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemies = CreateEnemyRing(root.transform, room, player, 9);
                var director = new RoomTacticalDirector();
                director.Tick(enemies, room, player, 4f);
                var nonActive = enemies.First(enemy => director.ResolveIntent(enemy).Role != EnemyTacticalRole.ActiveThreat);
                var requested = new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartMeleeAction, "claw_lunge", 1f, "test_commit");

                var planned = director.PlanCommand(nonActive, requested, 4f, nonActive.DistanceToPlayerMeters, out var intent);

                Assert.AreNotEqual(EnemyTacticalRole.ActiveThreat, intent.Role);
                Assert.IsFalse(planned.StartsCommittedAction);
                Assert.That(planned.Kind, Is.EqualTo(EnemyBehaviorCommandKind.MovePreferredRange)
                    .Or.EqualTo(EnemyBehaviorCommandKind.Hold)
                    .Or.EqualTo(EnemyBehaviorCommandKind.FacePlayer)
                    .Or.EqualTo(EnemyBehaviorCommandKind.Wander)
                    .Or.EqualTo(EnemyBehaviorCommandKind.Flee));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossRoomDowngradesNonActiveAddsWithoutReservationSolves()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var adds = CreateEnemyRing(root.transform, room, player, 12);
                var boss = CreateEnemy(root.transform, room, player, "spawnEnemyBoss", 40);
                boss.transform.localPosition = Vector3.back * 5f;
                var enemies = adds.Concat(new[] { boss }).ToArray();
                var director = new RoomTacticalDirector();

                director.Tick(enemies, room, player, 4f);

                Assert.LessOrEqual(director.ActiveThreatCount, 1);
                foreach (var add in adds)
                {
                    var intent = director.ResolveIntent(add);
                    if (intent.Role == EnemyTacticalRole.ActiveThreat)
                    {
                        continue;
                    }

                    Assert.IsFalse(intent.HasReservedPosition, intent.Summary);
                    Assert.That(intent.Role, Is.EqualTo(EnemyTacticalRole.Hold)
                        .Or.EqualTo(EnemyTacticalRole.Waiting)
                        .Or.EqualTo(EnemyTacticalRole.StationarySentinel)
                        .Or.EqualTo(EnemyTacticalRole.Flee));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossRoomTacticalReservationsUseCheapSingleAddBudget()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                M136PerformanceOperationCounters.Reset();
                var adds = CreateEnemyRing(root.transform, room, player, 12);
                var boss = CreateEnemy(root.transform, room, player, "spawnEnemyBoss", 40);
                boss.transform.localPosition = Vector3.back * 5f;
                var enemies = adds.Concat(new[] { boss }).ToArray();
                var director = new RoomTacticalDirector();

                director.Tick(enemies, room, player, 4f);

                var summary = M136PerformanceOperationCounters.Snapshot(reset: true).TacticalDirectorSummary;
                Assert.LessOrEqual(director.ActiveThreatCount, 1);
                StringAssert.Contains("bossAddSkips=", summary);
                Assert.LessOrEqual(ParseSummaryValue(summary, "reservationAttempts"), 1);
                Assert.LessOrEqual(ParseSummaryValue(summary, "candidates"), 6);
                Assert.GreaterOrEqual(ParseSummaryValue(summary, "bossAddSkips"), adds.Length - 1);
            }
            finally
            {
                M136PerformanceOperationCounters.Reset();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossRoomTacticalDirectorReusesCachedIntentsWhenStateIsUnchanged()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                M136PerformanceOperationCounters.Reset();
                var adds = CreateEnemyRing(root.transform, room, player, 8);
                var boss = CreateEnemy(root.transform, room, player, "spawnEnemyBoss", 40);
                boss.transform.localPosition = Vector3.back * 5f;
                var enemies = adds.Concat(new[] { boss }).ToArray();
                var director = new RoomTacticalDirector();

                director.Tick(enemies, room, player, 4f);
                M136PerformanceOperationCounters.Snapshot(reset: true);
                director.Tick(enemies, room, player, 4.2f);

                var summary = M136PerformanceOperationCounters.Snapshot(reset: true).TacticalDirectorSummary;
                StringAssert.Contains("cachedIntentReuses=", summary);
                Assert.Greater(ParseSummaryValue(summary, "cachedIntentReuses"), 0);
            }
            finally
            {
                M136PerformanceOperationCounters.Reset();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoomCombatControllerConstructionDoesNotCreateNavMeshPath()
        {
            var root = new GameObject("RoomCombatControllerConstructionHarness");
            try
            {
                Assert.DoesNotThrow(() => root.AddComponent<RoomCombatController>());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ToolBakeOffKeepsHollowDataAsSourceOfTruth()
        {
            StringAssert.Contains("Hollow", EnemyAiToolBakeOffEvaluation.HollowSourceOfTruth);
            var options = EnemyAiToolBakeOffEvaluation.Options.Select(option => option.Name).ToArray();
            Assert.Contains("Current Custom RoomGridAStar", options);
            Assert.Contains("Unity AI Navigation", options);
            Assert.Contains("A* Pathfinding Project Pro", options);
            Assert.Contains("Behavior Designer Pro 3", options);
            Assert.IsTrue(EnemyAiToolBakeOffEvaluation.Options.Any(option => option.RequiresPurchase));
        }

        [Test]
        public void TacticalDebugOverlayReportsRoles()
        {
            EnemyTacticalDebugOverlay.ResetDiagnostics();
            EnemyTacticalDebugOverlay.SetEnabled(true);
            try
            {
                EnemyTacticalDebugOverlay.ReportRoomState(3, 5);
                EnemyTacticalDebugOverlay.ReportIntent(
                    12,
                    new EnemyTacticalIntent(
                        EnemyTacticalRole.ActiveThreat,
                        EnemyTacticalCommitPolicy.CommitWhenReady,
                        "rusty_slash",
                        new Vector3(1f, 0f, 2f),
                        true,
                        0,
                        4.2f,
                        ThreatLane.Melee.ToString(),
                        EnemyNavigationBackend.RoomGridAStar.ToString(),
                        "test"));

                StringAssert.Contains("Tactics", EnemyTacticalDebugOverlay.DiagnosticsSummary);
                StringAssert.Contains("ActiveThreat", EnemyTacticalDebugOverlay.DiagnosticsSummary);
            }
            finally
            {
                EnemyTacticalDebugOverlay.SetEnabled(false);
            }
        }

        [Test]
        public void DocsReportAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone96TacticalNavigationAssetGenerator.DocsPath), Milestone96TacticalNavigationAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone96TacticalNavigationAssetGenerator.ReportPath), Milestone96TacticalNavigationAssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone96TacticalNavigationAssetGenerator.DocsPath);
            StringAssert.Contains("Tactical Navigation", markdown);
            StringAssert.Contains("RoomTacticalDirector", markdown);
            StringAssert.Contains("A* Pathfinding Project Pro", markdown);
            StringAssert.Contains("Behavior Designer Pro 3", markdown);
            Assert.IsTrue(Milestone96TacticalNavigationValidator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M96TacticalHarness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            player.transform.localPosition = Vector3.zero;
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            playerObject.AddComponent<CombatKnockbackReceiver>().Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            return root;
        }

        private static EnemyRuntimeController[] CreateEnemyRing(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, int count)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            var result = new EnemyRuntimeController[count];
            for (var index = 0; index < count; index++)
            {
                var enemy = CreateEnemy(parent, room, player, index % 3 == 0 ? "spawnEnemySkeletonSword" : index % 3 == 1 ? "spawnEnemyNormal" : "spawnEnemyFast", index, catalog);
                enemy.transform.localPosition = Quaternion.Euler(0f, index * (360f / count), 0f) * Vector3.forward * 3f;
                result[index] = enemy;
            }

            return result;
        }

        private static EnemyRuntimeController CreateEnemy(
            Transform parent,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            string enemyId,
            int spawnIndex,
            EnemyCatalog catalog = null)
        {
            catalog ??= EnemyCatalog.CreateRuntimeDefault();
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var definition = catalog.Resolve(enemyId);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            var difficulty = DifficultyTierDefinition.CreateRuntimeDeveloperSample();
            enemy.Configure(room, player, definition, difficulty);
            enemy.ConfigureSpawnContext(null, null, catalog, difficulty, new CombatDiagnosticsModel(), spawnIndex);
            return enemy;
        }

        private static int ParseSummaryValue(string summary, string key)
        {
            Assert.IsNotEmpty(summary, $"Missing tactical director summary for {key}");
            var prefix = key + "=";
            var parts = summary.Split(';');
            foreach (var rawPart in parts)
            {
                var part = rawPart.Trim();
                if (!part.StartsWith(prefix))
                {
                    continue;
                }

                Assert.IsTrue(int.TryParse(part.Substring(prefix.Length), out var value), part);
                return value;
            }

            Assert.Fail($"Missing '{key}' in tactical director summary: {summary}");
            return 0;
        }
    }
}
