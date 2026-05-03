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
    public sealed class Milestone87MagicGhostSoulEnemiesTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void MagicRosterDefinitionsProfilesTreesAndSpawnKindsResolve()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone87AssetGenerator.EnemyRows())
            {
                var enemy = catalog.Resolve(spec.SpawnKind);
                Assert.NotNull(enemy, spec.SpawnKind);
                Assert.AreEqual(spec.DisplayName, enemy.DisplayName, spec.SpawnKind);
                Assert.AreEqual(spec.BehaviorId, enemy.BehaviorId, spec.SpawnKind);
                Assert.AreEqual(spec.MovementMode, enemy.MovementMode, spec.SpawnKind);
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

            Assert.NotNull(catalog.Resolve("spawnEnemySoulEater").ResolveAttackProfile("soul_drain"));
            Assert.NotNull(catalog.Resolve("spawnEnemyWraith").ResolveAttackProfile("phase_shift"));
            Assert.NotNull(catalog.Resolve("spawnEnemyCurseBinder").ResolveAttackProfile("curse_field"));
        }

        [Test]
        public void MagicProfilesCarrySoulOrCurseElements()
        {
            foreach (var profile in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => Milestone87AssetGenerator.SpawnKinds.Contains(spec.OwnerId)))
            {
                Assert.AreEqual(DamageChannel.Elemental, profile.DamageChannel, profile.AttackId);
                Assert.IsTrue(profile.DamageElement is DamageElement.Soul or DamageElement.Cursed, profile.AttackId);
                Assert.IsTrue(
                    profile.RuntimeKind is EnemyAttackRuntimeKind.Projectile
                        or EnemyAttackRuntimeKind.FanProjectile
                        or EnemyAttackRuntimeKind.RadialProjectile
                        or EnemyAttackRuntimeKind.Area
                        or EnemyAttackRuntimeKind.Beam
                        or EnemyAttackRuntimeKind.PhaseMove
                        or EnemyAttackRuntimeKind.MeleeLunge,
                    profile.AttackId);
                if (profile.RuntimeKind == EnemyAttackRuntimeKind.PhaseMove)
                {
                    Assert.AreEqual(0, profile.Damage, profile.AttackId);
                }
                else
                {
                    Assert.Greater(profile.Damage, 0, profile.AttackId);
                }
            }
        }

        [Test]
        public void BeamDealsDamageOnlyAtActivePoint()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var health = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySoulEater");
                var profile = definition.ResolveAttackProfile("soul_drain");
                var enemy = CreateEnemy(root.transform, room, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 3.2f);

                Assert.IsTrue(StartRanged(enemy, 2f, "soul_drain"));
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, enemy.ReadabilityState);
                Assert.AreEqual(health.MaxHealth, health.CurrentHealth);

                var activeTime = 2f + profile.WindupSeconds * enemy.AttackWindupScale + 0.02f;
                enemy.Tick(0.05f, activeTime);
                Assert.AreEqual(EnemyReadabilityState.RangedActive, enemy.ReadabilityState);
                Assert.AreEqual(health.MaxHealth - profile.Damage, health.CurrentHealth);
                Assert.AreEqual(0, root.GetComponentsInChildren<EnemyProjectileController>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PhaseMoveIsNonDamagingLocalReposition()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var health = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyWraith");
                var profile = definition.ResolveAttackProfile("phase_shift");
                var enemy = CreateEnemy(root.transform, room, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 2.5f);
                var start = enemy.transform.localPosition;

                Assert.IsTrue(StartCreatureMove(enemy, 2f, "phase_shift"));
                Assert.AreEqual(EnemyReadabilityState.CreatureMoveWindup, enemy.ReadabilityState);
                var activeTime = 2f + profile.WindupSeconds * enemy.AttackWindupScale + 0.02f;
                enemy.Tick(0.05f, activeTime);
                Assert.AreEqual(EnemyReadabilityState.CreatureMoveActive, enemy.ReadabilityState);
                enemy.Tick(0.08f, activeTime + 0.08f);
                Assert.Greater(Vector3.Distance(start, enemy.transform.localPosition), 0.05f);
                Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MagicPatternsUseExpectedProjectileCounts()
        {
            AssertProjectilePattern("spawnEnemyHollowAcolyte", "rune_burst", 6, 3.5f);
            AssertProjectilePattern("spawnEnemyGraveLantern", "lantern_soul_ring", 10, 5f);
            AssertProjectilePattern("spawnEnemyCurseBinder", "sigil_fan", 5, 5f);
        }

        [Test]
        public void MagicRoomsDocsPdfExtractAndValidatorPass()
        {
            foreach (var roomId in Milestone87AssetGenerator.MagicRoomIds)
            {
                var path = $"{Milestone87AssetGenerator.MagicRoomDirectory}/{roomId}.hollowruntime.json";
                Assert.IsTrue(File.Exists(path), path);
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(path));
                Assert.Greater(asset.Layout.WalkableTiles.Count, 0, roomId);
                Assert.Greater(asset.EnemySpawns.Count(spawn => spawn.kind.StartsWith("spawnEnemy")), 0, roomId);
                Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.RoomReward), roomId);
            }

            Assert.IsTrue(File.Exists(Milestone87AssetGenerator.DocsPath), Milestone87AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone87AssetGenerator.ReportPath), Milestone87AssetGenerator.ReportPath);
            Assert.IsTrue(File.Exists(Milestone87AssetGenerator.PdfPath), Milestone87AssetGenerator.PdfPath);
            var markdown = File.ReadAllText(Milestone87AssetGenerator.DocsPath);
            StringAssert.Contains("Magic/Ghost/Soul Enemies", markdown);
            StringAssert.Contains("Hollow Acolyte", markdown);
            StringAssert.Contains("Wraith", markdown);
            StringAssert.Contains("Soul Eater", markdown);
            StringAssert.Contains("PhaseMove", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone87Validator.Validate());
        }

        private static void AssertProjectilePattern(string spawnKind, string actionId, int expectedProjectiles, float playerZ)
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve(spawnKind);
                var profile = definition.ResolveAttackProfile(actionId);
                var enemy = CreateEnemy(root.transform, room, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, playerZ);

                Assert.IsTrue(StartRanged(enemy, 2f, actionId));
                Assert.AreEqual(EnemyReadabilityState.RangedWindup, enemy.ReadabilityState);
                Assert.AreEqual(0, root.GetComponentsInChildren<EnemyProjectileController>().Length);

                var activeTime = 2f + profile.WindupSeconds * enemy.AttackWindupScale + 0.02f;
                enemy.Tick(0.05f, activeTime);
                Assert.AreEqual(EnemyReadabilityState.RangedActive, enemy.ReadabilityState);
                Assert.AreEqual(expectedProjectiles, root.GetComponentsInChildren<EnemyProjectileController>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M87Harness");
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

        private static bool StartCreatureMove(EnemyRuntimeController enemy, float timeSeconds, string actionId)
        {
            var method = typeof(EnemyRuntimeController).GetMethod("TryStartCreatureMoveAction", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(string) }, null);
            Assert.NotNull(method);
            return (bool)method.Invoke(enemy, new object[] { timeSeconds, actionId });
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath(Milestone87AssetGenerator.VerifyScriptPath);
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
                Assert.Fail("Timed out while verifying the M87 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }
    }
}
