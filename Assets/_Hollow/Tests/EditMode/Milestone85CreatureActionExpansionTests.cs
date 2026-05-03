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
    public sealed class Milestone85CreatureActionExpansionTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void CreatureRosterDefinitionsProfilesTreesAndSpawnKindsResolve()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone85AssetGenerator.NewCreatureRows())
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
                Assert.NotNull(enemy.BehaviorTree, spec.SpawnKind);
                Assert.Greater(enemy.AttackProfiles.Count, 0, spec.SpawnKind);
                Assert.Greater(enemy.ActionProfiles.Count, 0, spec.SpawnKind);
            }

            CollectionAssert.IsSubsetOf(
                Milestone85AssetGenerator.NewSpawnKinds.ToArray(),
                Milestone85AssetGenerator.BodyCreatureSpawnKinds.ToArray());
            Assert.NotNull(catalog.Resolve("spawnEnemyHollowBird").ResolveAttackProfile("caw_signal"));
            Assert.NotNull(catalog.Resolve("spawnEnemyHollowBeast").ResolveAttackProfile("body_check"));
        }

        [Test]
        public void BodyOnlyCreatureUpgradesHavePhysicalProfilesActionsAndTrees()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spawnKind in Milestone85AssetGenerator.BodyCreatureSpawnKinds)
            {
                var enemy = catalog.Resolve(spawnKind);
                Assert.NotNull(enemy, spawnKind);
                Assert.NotNull(enemy.BehaviorTree, spawnKind);

                var promoted = EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(spec => spec.OwnerId == spawnKind && Milestone85AssetGenerator.PromotedCreatureActionIds.Contains(spec.AttackId))
                    .ToArray();
                Assert.Greater(promoted.Length, 0, spawnKind);
                foreach (var profile in promoted)
                {
                    Assert.AreEqual(DamageChannel.Physical, profile.DamageChannel, profile.AttackId);
                    Assert.AreEqual(DamageElement.None, profile.DamageElement, profile.AttackId);
                    Assert.NotNull(enemy.ResolveAttackProfile(profile.AttackId), profile.AttackId);
                    Assert.NotNull(enemy.ResolveActionProfile(profile.AttackId), profile.AttackId);
                }
            }
        }

        [Test]
        public void CreatureMovementBurstMovesWithoutDamageAndHasRecovery()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var bird = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyHollowBird"));
                var playerHealth = player.GetComponent<CombatantHealth>();
                bird.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.2f);
                var before = bird.transform.localPosition;

                Assert.IsTrue(StartCreatureMove(bird, 2f, "wing_retreat"));
                Assert.AreEqual(EnemyReadabilityState.CreatureMoveWindup, bird.ReadabilityState);
                bird.Tick(0.05f, 2.1f);
                Assert.AreEqual(EnemyReadabilityState.CreatureMoveActive, bird.ReadabilityState);
                bird.Tick(0.1f, 2.16f);

                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);
                Assert.Greater((bird.transform.localPosition - before).sqrMagnitude, 0.001f);

                bird.Tick(0.05f, 2.45f);
                Assert.AreEqual(EnemyReadabilityState.CreatureMoveRecovery, bird.ReadabilityState);
                Assert.IsFalse(bird.TryApplyContactDamage(2.46f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HollowBirdAndBeastDamageOnlyDuringActiveWindows()
        {
            AssertMeleeCreatureAttack("spawnEnemyHollowBird", "swoop_peck", 1.2f, 4f);
            AssertMeleeCreatureAttack("spawnEnemyHollowBeast", "body_check", 1.45f, 8f);
        }

        [Test]
        public void CreatureSignalsAffectOnlyNearbySameFamilyLivingNonBossEnemies()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var source = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyHollowBird"));
                var sameFamily = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyHollowBird"));
                var unrelated = CreateEnemy(root.transform, room, player, catalog.Resolve("spawnEnemyHollowBeast"));
                source.transform.localPosition = Vector3.zero;
                sameFamily.transform.localPosition = new Vector3(2.5f, 0f, 0f);
                unrelated.transform.localPosition = new Vector3(2.5f, 0f, 0.5f);
                BindEnemies(combat, source, sameFamily, unrelated);

                Assert.IsTrue(StartCreatureSignal(source, 3f, "caw_signal"));
                source.Tick(0.05f, 3.25f);
                Assert.AreEqual(EnemyReadabilityState.CreatureSignalActive, source.ReadabilityState);
                source.Tick(0.05f, 3.3f);

                Assert.AreEqual(EnemyStimulusKind.CreatureSignal, sameFamily.LastStimulusKind);
                Assert.AreEqual(EnemyStimulusTier.Normal, sameFamily.LastStimulusTier);
                Assert.AreEqual(source.transform.localPosition, sameFamily.LastStimulusLocalPosition);
                Assert.AreNotEqual(EnemyStimulusKind.CreatureSignal, unrelated.LastStimulusKind);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CuratedRoomsDocsPdfExtractAndValidatorPass()
        {
            foreach (var roomId in Milestone85AssetGenerator.CreatureRoomIds)
            {
                var path = $"{Milestone85AssetGenerator.CreatureRoomDirectory}/{roomId}.hollowruntime.json";
                Assert.IsTrue(File.Exists(path), path);
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(path));
                Assert.Greater(asset.Layout.WalkableTiles.Count, 0, roomId);
                Assert.Greater(asset.EnemySpawns.Count(spawn => spawn.kind.StartsWith("spawnEnemy")), 0, roomId);
                Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.RoomReward), roomId);
            }

            Assert.IsTrue(File.Exists(Milestone85AssetGenerator.DocsPath), Milestone85AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone85AssetGenerator.ReportPath), Milestone85AssetGenerator.ReportPath);
            Assert.IsTrue(File.Exists(Milestone85AssetGenerator.PdfPath), Milestone85AssetGenerator.PdfPath);
            var markdown = File.ReadAllText(Milestone85AssetGenerator.DocsPath);
            StringAssert.Contains("Creature Action Expansion", markdown);
            StringAssert.Contains("Hollow Bird", markdown);
            StringAssert.Contains("Hollow Beast", markdown);
            StringAssert.Contains("active window", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone85Validator.Validate());
        }

        private static void AssertMeleeCreatureAttack(string spawnKind, string actionId, float playerZ, float startTime)
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve(spawnKind);
                var profile = definition.ResolveAttackProfile(actionId);
                var enemy = CreateEnemy(root.transform, room, player, definition);
                var playerHealth = player.GetComponent<CombatantHealth>();
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, playerZ);

                Assert.IsTrue(StartMelee(enemy, startTime, actionId));
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, enemy.ReadabilityState);
                Assert.IsFalse(enemy.TryApplyContactDamage(startTime + 0.01f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);

                enemy.Tick(0.05f, startTime + profile.WindupSeconds + 0.02f);
                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, enemy.ReadabilityState);
                Assert.IsTrue(enemy.TryApplyContactDamage(startTime + profile.WindupSeconds + 0.04f));
                Assert.IsFalse(enemy.TryApplyContactDamage(startTime + profile.WindupSeconds + 0.05f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth - profile.Damage, playerHealth.CurrentHealth);

                enemy.Tick(0.05f, startTime + profile.WindupSeconds + profile.ActiveSeconds + 0.1f);
                Assert.AreEqual(EnemyReadabilityState.MeleeRecovery, enemy.ReadabilityState);
                playerHealth.Restore(RoomCombatController.PlayerMaxHealth, RoomCombatController.PlayerMaxHealth);
                Assert.IsFalse(enemy.TryApplyContactDamage(startTime + profile.WindupSeconds + profile.ActiveSeconds + 0.12f));
                Assert.AreEqual(RoomCombatController.PlayerMaxHealth, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M85Harness");
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

        private static bool StartMelee(EnemyRuntimeController enemy, float timeSeconds, string actionId)
        {
            var method = typeof(EnemyRuntimeController).GetMethod("TryMeleeLunge", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(string) }, null);
            Assert.NotNull(method);
            return (bool)method.Invoke(enemy, new object[] { timeSeconds, actionId });
        }

        private static bool StartCreatureMove(EnemyRuntimeController enemy, float timeSeconds, string actionId)
        {
            var method = typeof(EnemyRuntimeController).GetMethod("TryStartCreatureMoveAction", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (bool)method.Invoke(enemy, new object[] { timeSeconds, actionId });
        }

        private static bool StartCreatureSignal(EnemyRuntimeController enemy, float timeSeconds, string actionId)
        {
            var method = typeof(EnemyRuntimeController).GetMethod("TryStartCreatureSignalAction", BindingFlags.Instance | BindingFlags.NonPublic);
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
            var scriptPath = Path.GetFullPath(Milestone85AssetGenerator.VerifyScriptPath);
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
                Assert.Fail("Timed out while verifying the M85 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }
    }
}
