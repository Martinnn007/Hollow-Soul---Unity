using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone86RangedFirearmEnemiesTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void RangedRosterDefinitionsProfilesTreesAndSpawnKindsResolve()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone86AssetGenerator.EnemyRows())
            {
                var enemy = catalog.Resolve(spec.SpawnKind);
                Assert.NotNull(enemy, spec.SpawnKind);
                Assert.AreEqual(spec.DisplayName, enemy.DisplayName, spec.SpawnKind);
                Assert.AreEqual(spec.BehaviorId, enemy.BehaviorId, spec.SpawnKind);
                Assert.AreEqual(spec.MaxHealth, enemy.MaxHealth, spec.SpawnKind);
                Assert.AreEqual(spec.SpeedMetersPerSecond, enemy.SpeedMetersPerSecond, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.RadiusMeters, enemy.RadiusMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.BodyClass, enemy.BodyClass, spec.SpawnKind);
                Assert.AreEqual(spec.Intelligence, enemy.Intelligence, spec.SpawnKind);
                Assert.AreEqual(spec.Disposition, enemy.Disposition, spec.SpawnKind);
                Assert.AreEqual(EnemyContactDamagePolicy.ActiveOnly, enemy.ContactDamagePolicy, spec.SpawnKind);
                Assert.AreEqual(EnemyPassiveContactHazardType.None, enemy.PassiveContactHazardType, spec.SpawnKind);
                Assert.AreEqual(spec.PreferredRangeMinMeters, enemy.PreferredRangeMinMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.PreferredRangeMaxMeters, enemy.PreferredRangeMaxMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.SightRadiusMeters, enemy.SightRadiusMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.SightAngleDegrees, enemy.SightAngleDegrees, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.HearingRadiusMeters, enemy.HearingRadiusMeters, 0.001f, spec.SpawnKind);
                Assert.IsFalse(enemy.LungeAttackEnabled, spec.SpawnKind);
                Assert.NotNull(enemy.BehaviorTree, spec.SpawnKind);
                Assert.Greater(enemy.AttackProfiles.Count, 0, spec.SpawnKind);
                Assert.Greater(enemy.ActionProfiles.Count, 0, spec.SpawnKind);
            }

            Assert.NotNull(catalog.Resolve("spawnEnemyHollowArcher").ResolveAttackProfile("arrow_volley"));
            Assert.NotNull(catalog.Resolve("spawnEnemyPowderGunner").ResolveAttackProfile("aimed_musket_shot"));
            Assert.NotNull(catalog.Resolve("spawnEnemyClockworkSentry").ResolveAttackProfile("clockwork_radial"));
        }

        [Test]
        public void RangedProfilesArePhysicalProjectilePatternProfiles()
        {
            foreach (var profile in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => Milestone86AssetGenerator.SpawnKinds.Contains(spec.OwnerId)))
            {
                Assert.AreEqual(DamageChannel.Physical, profile.DamageChannel, profile.AttackId);
                Assert.AreEqual(DamageElement.None, profile.DamageElement, profile.AttackId);
                Assert.IsTrue(
                    profile.RuntimeKind is EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile or EnemyAttackRuntimeKind.CreatureMove,
                    profile.AttackId);
                if (profile.RuntimeKind != EnemyAttackRuntimeKind.CreatureMove)
                {
                    Assert.AreEqual(DamageDelivery.Projectile, profile.DamageDelivery, profile.AttackId);
                    Assert.Greater(profile.Damage, 0, profile.AttackId);
                    Assert.Greater(profile.ProjectileSpeedMetersPerSecond, 0f, profile.AttackId);
                }
            }
        }

        [Test]
        public void FanAndRadialProfilesFireOnlyAtActivePoint()
        {
            AssertProjectilePattern("spawnEnemyHollowArcher", "arrow_volley", 3, 4f);
            AssertProjectilePattern("spawnEnemyClockworkSentry", "clockwork_radial", 8, 5f);
        }

        [Test]
        public void RangedAttackBudgetStillLimitsStarts()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var first = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyHollowArcher"));
                var second = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyHollowArcher"));
                first.transform.localPosition = new Vector3(-1f, 0f, 0f);
                second.transform.localPosition = new Vector3(1f, 0f, 0f);
                player.transform.localPosition = new Vector3(0f, 0f, 3.8f);
                BindEnemies(combat, first, second);

                Assert.IsTrue(StartRanged(first, 3f, "arrow_shot"));
                Assert.IsFalse(StartRanged(second, 3.05f, "arrow_shot"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CuratedRoomsDocsPdfExtractAndValidatorPass()
        {
            foreach (var roomId in Milestone86AssetGenerator.RangedRoomIds)
            {
                var path = $"{Milestone86AssetGenerator.RangedRoomDirectory}/{roomId}.hollowruntime.json";
                Assert.IsTrue(File.Exists(path), path);
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(path));
                Assert.Greater(asset.Layout.WalkableTiles.Count, 0, roomId);
                Assert.Greater(asset.EnemySpawns.Count(spawn => spawn.kind.StartsWith("spawnEnemy")), 0, roomId);
                Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.RoomReward), roomId);
            }

            Assert.IsTrue(File.Exists(Milestone86AssetGenerator.DocsPath), Milestone86AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone86AssetGenerator.ReportPath), Milestone86AssetGenerator.ReportPath);
            Assert.IsTrue(File.Exists(Milestone86AssetGenerator.PdfPath), Milestone86AssetGenerator.PdfPath);
            var markdown = File.ReadAllText(Milestone86AssetGenerator.DocsPath);
            StringAssert.Contains("Ranged + Firearm Enemies", markdown);
            StringAssert.Contains("Hollow Archer", markdown);
            StringAssert.Contains("Powder Gunner", markdown);
            StringAssert.Contains("Clockwork Sentry", markdown);
            StringAssert.Contains("active window", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone86Validator.Validate());
        }

        private static void AssertProjectilePattern(string spawnKind, string actionId, int expectedProjectiles, float playerZ)
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve(spawnKind);
                var profile = definition.ResolveAttackProfile(actionId);
                var enemy = CreateEnemy(root.transform, null, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, playerZ);

                Assert.IsTrue(StartRanged(enemy, 2f, actionId));
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, enemy.ReadabilityState);
                Assert.AreEqual(0, root.GetComponentsInChildren<EnemyProjectileController>().Length);

                var activeTime = 2f + profile.WindupSeconds + 0.5f;
                enemy.Tick(0.05f, activeTime);
                Assert.AreEqual(EnemyReadabilityState.RangedActive, enemy.ReadabilityState);
                Assert.AreEqual(expectedProjectiles, root.GetComponentsInChildren<EnemyProjectileController>().Length);

                enemy.Tick(0.05f, activeTime + profile.ActiveSeconds + 0.5f);
                Assert.AreEqual(EnemyReadabilityState.RangedRecovery, enemy.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M86Harness");
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

        private static bool StartRanged(EnemyRuntimeController enemy, float timeSeconds, string actionId)
        {
            var method = typeof(EnemyRuntimeController).GetMethod("TryRangedAttack", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(string) }, null);
            Assert.NotNull(method);
            return (bool)method.Invoke(enemy, new object[] { timeSeconds, actionId });
        }

        private static void BindEnemies(RoomCombatController combat, params EnemyRuntimeController[] enemies)
        {
            var field = typeof(RoomCombatController).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var list = (List<EnemyRuntimeController>)field.GetValue(combat);
            foreach (var enemy in enemies)
            {
                enemy.BindRoomCombatController(combat);
                list.Add(enemy);
            }
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath(Milestone86AssetGenerator.VerifyScriptPath);
            Assert.IsTrue(File.Exists(scriptPath), scriptPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);
            if (!process.WaitForExit(15000))
            {
                process.Kill();
                Assert.Fail("Timed out while verifying the M86 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }
    }
}
