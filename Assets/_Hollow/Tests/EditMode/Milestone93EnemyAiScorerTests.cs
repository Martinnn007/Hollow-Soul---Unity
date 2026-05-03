using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone93EnemyAiScorerTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void ScorerChoosesValidMeleeActionInsideRange()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.25f);
                var command = new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartMeleeAction, string.Empty, 1f, "test");

                var chosen = EnemyActionScorer.TryChooseAction(
                    enemy,
                    command,
                    3f,
                    enemy.DistanceToPlayerMeters,
                    new RoomThreatDirector(),
                    out var best,
                    out var topScores);

                Assert.IsTrue(chosen);
                Assert.IsTrue(best.IsValid);
                Assert.AreEqual(EnemyBehaviorCommandKind.StartMeleeAction, best.CommandKind);
                Assert.IsNotEmpty(best.ActionId);
                Assert.IsNotEmpty(topScores);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LodPromotesCloseEnemiesAndReducesFarEngagedEnemies()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemy = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 12f);

                Assert.AreEqual(EnemyAiLodTier.Reduced, EnemyAiBrain.ResolveLodTier(enemy, enemy.DistanceToPlayerMeters));

                player.transform.localPosition = new Vector3(0f, 0f, 4f);
                Assert.AreEqual(EnemyAiLodTier.Full, EnemyAiBrain.ResolveLodTier(enemy, enemy.DistanceToPlayerMeters));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ThreatDirectorAppliesSoftPenaltyWhenMeleePressureIsHigh()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var enemies = new List<EnemyRuntimeController>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
                var attack = definition.ResolveAttackProfile("claw_lunge") ?? definition.AttackProfiles.First(profile => profile != null);
                var action = definition.ActionProfiles.FirstOrDefault(profile => profile != null && profile.LinkedAttackId == attack.AttackId)
                    ?? definition.ActionProfiles.First(profile => profile != null);
                for (var index = 0; index < 5; index++)
                {
                    var enemy = CreateEnemy(root.transform, room, player, definition);
                    enemy.transform.localPosition = new Vector3(index * 0.35f, 0f, 0f);
                    SetPrivateField(enemy, "activeMeleeProfile", attack);
                    SetPrivateField(enemy, "readabilityState", EnemyReadabilityState.MeleeWindup);
                    enemies.Add(enemy);
                }

                var director = new RoomThreatDirector();
                director.Tick(enemies);

                Assert.Greater(director.MeleePressure, 0f);
                Assert.Greater(director.PressurePenaltyFor(enemies[0], action, attack), 0f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AiBlackboardDebugOverlayTracksReportedEnemies()
        {
            EnemyAiDebugOverlay.ResetDiagnostics();
            EnemyAiDebugOverlay.SetBlackboardEnabled(true);
            try
            {
                EnemyAiDebugOverlay.ReportBlackboard(
                    12,
                    new EnemyAiBlackboard(
                        EnemyAiLodTier.Full,
                        EnemyBehaviorCommandKind.StartMeleeAction,
                        EnemyBehaviorCommandKind.StartMeleeAction,
                        "claw_lunge",
                        3.5f,
                        0.25f,
                        1.2f,
                        EnemyPathStatus.Ready,
                        "test",
                        "claw_lunge:3.50"));

                StringAssert.Contains("AI LOD", EnemyAiDebugOverlay.DiagnosticsSummary);
                StringAssert.Contains("claw_lunge", EnemyAiDebugOverlay.DiagnosticsSummary);
            }
            finally
            {
                EnemyAiDebugOverlay.SetBlackboardEnabled(false);
            }
        }

        [Test]
        public void M93DocsAndReportExist()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M93_Enemy_AI_Action_Scorer.md"));
            Assert.IsTrue(File.Exists("output/reports/m93_enemy_ai_action_scorer.md"));
            var docs = File.ReadAllText("Docs/Hollow_M93_Enemy_AI_Action_Scorer.md");
            StringAssert.Contains("Enemy AI Action Scorer", docs);
            StringAssert.Contains("adaptive AI LOD", docs);
            StringAssert.Contains("soft pressure caps", docs);
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M93Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            playerObject.AddComponent<CombatKnockbackReceiver>().Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            return root;
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            return enemy;
        }

        private static void SetPrivateField<T>(EnemyRuntimeController enemy, string fieldName, T value)
        {
            var field = typeof(EnemyRuntimeController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(enemy, value);
        }
    }
}
