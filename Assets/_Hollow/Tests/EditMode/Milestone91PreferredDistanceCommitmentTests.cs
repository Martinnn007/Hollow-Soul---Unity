using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone91PreferredDistanceCommitmentTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void CurrentRosterResolvesSpacingProfilesForCurrentRuntimeActions()
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            Assert.GreaterOrEqual(enemies.Length, 20);

            foreach (var enemy in enemies)
            {
                var profile = enemy.SpacingProfile;
                Assert.NotNull(profile, enemy.SpawnKind);
                Assert.Greater(profile.DefaultIdealDistanceMeters, 0f, enemy.SpawnKind);
                Assert.Greater(enemy.PreferredRangeMaxMeters, enemy.PreferredRangeMinMeters, enemy.SpawnKind);

                var runtimeActions = enemy.ActionProfiles
                    .Where(action => action != null && action.UsageState == EnemyActionUsageState.CurrentRuntime)
                    .ToArray();
                Assert.Greater(runtimeActions.Length, 0, enemy.SpawnKind);

                foreach (var action in runtimeActions)
                {
                    var attack = action.HasLinkedAttack ? enemy.ResolveAttackProfile(action.LinkedAttackId) : null;
                    var spacing = profile.ResolveActionSpacing(action, attack);
                    Assert.Greater(spacing.CommitRangeMaxMeters, spacing.CommitRangeMinMeters, $"{enemy.SpawnKind}/{action.ActionId}");
                    Assert.GreaterOrEqual(spacing.MaxResetCountBeforeCommit, 0, $"{enemy.SpawnKind}/{action.ActionId}");
                }
            }
        }

        [Test]
        public void CloseBodyEnemyCanCommitInsideDeprecatedPreferredBand()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var normal = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"));
                normal.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 0.92f);

                Assert.Less(Vector3.Distance(normal.transform.localPosition, player.transform.localPosition), normal.PreferredRangeMinMeters);
                Assert.IsTrue(normal.CanStartBehaviorMeleeAction("claw_lunge", 1f));
                Assert.IsFalse(normal.IsTooFarForCurrentSpacing(Vector3.Distance(normal.transform.localPosition, player.transform.localPosition)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RecoveryModesMatchEnemyIdentity()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            var knight = catalog.Resolve("spawnEnemyKnight");
            var giant = catalog.Resolve("spawnEnemyGiant");
            var archer = catalog.Resolve("spawnEnemyHollowArcher");
            var wraith = catalog.Resolve("spawnEnemyWraith");

            Assert.IsTrue(knight.SpacingProfile.ActionOverrides.Any(row => row.RecoveryMovementMode is EnemySpacingRecoveryMode.Planted or EnemySpacingRecoveryMode.MinimalDrift));
            Assert.IsTrue(giant.SpacingProfile.ActionOverrides.All(row => row.RecoveryMovementMode == EnemySpacingRecoveryMode.Planted));
            Assert.IsTrue(archer.SpacingProfile.ActionOverrides.Any(row => row.RecoveryMovementMode == EnemySpacingRecoveryMode.RangedReset));
            Assert.IsTrue(wraith.SpacingProfile.ActionOverrides.Any(row => row.RecoveryMovementMode == EnemySpacingRecoveryMode.PhaseDrift));
        }

        [Test]
        public void RangedAndCasterResetsAreCapped()
        {
            var rangedEnemies = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .Where(enemy => enemy.BehaviorId is EnemyBehaviorId.HollowArcher
                    or EnemyBehaviorId.PowderGunner
                    or EnemyBehaviorId.KnifeThrower
                    or EnemyBehaviorId.HollowAcolyte
                    or EnemyBehaviorId.Wraith
                    or EnemyBehaviorId.CurseBinder)
                .ToArray();

            Assert.Greater(rangedEnemies.Length, 0);
            foreach (var enemy in rangedEnemies)
            {
                foreach (var row in enemy.SpacingProfile.ActionOverrides.Where(row => row.RecoveryMovementMode is EnemySpacingRecoveryMode.RangedReset or EnemySpacingRecoveryMode.PhaseDrift))
                {
                    Assert.LessOrEqual(row.MaxResetCountBeforeCommit, 1, $"{enemy.SpawnKind}/{row.ActionId}");
                }
            }
        }

        [Test]
        public void DocsReportAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone91AssetGenerator.DocsPath), Milestone91AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone91AssetGenerator.ReportPath), Milestone91AssetGenerator.ReportPath);

            var markdown = File.ReadAllText(Milestone91AssetGenerator.DocsPath);
            StringAssert.Contains("Preferred Distance", markdown);
            StringAssert.Contains("action-specific range", markdown);
            StringAssert.Contains("recovery spacing", markdown);
            StringAssert.Contains("retreat caps", markdown);
            StringAssert.Contains("Current Roster Spacing Table", markdown);
            Assert.IsTrue(Milestone91Validator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M91Harness");
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
    }
}
