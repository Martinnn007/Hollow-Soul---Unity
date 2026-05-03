using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Combat;
using Hollow.Data.Definitions;
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
    public sealed class Milestone84WeaponUserEnemiesTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void WeaponUserRosterResolvesDefinitionsProfilesTreesAndGuardData()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone84AssetGenerator.EnemyRows())
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
                Assert.NotNull(enemy.BehaviorTree, spec.SpawnKind);
                Assert.Greater(enemy.AttackProfiles.Count, 0, spec.SpawnKind);
                Assert.Greater(enemy.ActionProfiles.Count, 0, spec.SpawnKind);

                if (spec.SpawnKind == "spawnEnemyKnight")
                {
                    Assert.NotNull(enemy.GuardProfile, spec.SpawnKind);
                    Assert.AreEqual(EnemyShieldTier.Medium, enemy.GuardProfile.ShieldTier);
                }
                else
                {
                    Assert.IsNull(enemy.GuardProfile, spec.SpawnKind);
                }
            }

            Assert.AreEqual(EnemyAttackRuntimeKind.WeaponMelee, catalog.Resolve("spawnEnemySkeletonSword").ResolveAttackProfile("rusty_slash").RuntimeKind);
            Assert.AreEqual("backhand_slash", catalog.Resolve("spawnEnemySkeletonSword").ResolveAttackProfile("rusty_slash").ComboFollowUpAttackId);
            Assert.AreEqual(EnemyAttackRuntimeKind.Defense, catalog.Resolve("spawnEnemyKnight").ResolveAttackProfile("shield_guard").RuntimeKind);
        }

        [Test]
        public void WeaponMeleeDamagesOnlyDuringActiveArcAndRecoveryIsHarmless()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySkeletonSpear");
                var profile = definition.ResolveAttackProfile("spear_thrust");
                var spear = CreateEnemy(root.transform, room, player, definition);
                spear.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 2.15f);

                spear.Tick(0.05f, 4f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, spear.ReadabilityState);
                Assert.IsFalse(spear.TryApplyContactDamage(4.05f));
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);

                var activeTime = 4f + profile.WindupSeconds + 0.5f;
                spear.Tick(0.05f, activeTime);
                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, spear.ReadabilityState);
                Assert.IsTrue(spear.TryApplyContactDamage(activeTime + 0.04f));
                Assert.IsFalse(spear.TryApplyContactDamage(activeTime + 0.05f));
                Assert.AreEqual(playerHealth.MaxHealth - profile.Damage, playerHealth.CurrentHealth);

                spear.Tick(0.05f, activeTime + profile.ActiveSeconds + 0.5f);
                Assert.AreEqual(EnemyReadabilityState.MeleeRecovery, spear.ReadabilityState);
                playerHealth.Restore(playerHealth.MaxHealth, playerHealth.MaxHealth);
                Assert.IsFalse(spear.TryApplyContactDamage(activeTime + profile.ActiveSeconds + 0.51f));
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);

                var behind = CreateEnemy(root.transform, room, player, definition);
                behind.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 2.1f);
                behind.Tick(0.05f, 8f);
                var behindActiveTime = 8f + profile.WindupSeconds + 0.5f;
                behind.Tick(0.05f, behindActiveTime);
                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, behind.ReadabilityState);
                playerHealth.Restore(playerHealth.MaxHealth, playerHealth.MaxHealth);
                behind.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(2.1f, 0f, 0f);
                Assert.IsFalse(behind.TryApplyContactDamage(behindActiveTime + 0.04f));
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SwordSkeletonCanStartOneFollowUpButNotAThreeHitChain()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var sword = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemySkeletonSword"));
                sword.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.25f);
                var opener = sword.Definition.ResolveAttackProfile("rusty_slash");
                var followUp = sword.Definition.ResolveAttackProfile("backhand_slash");

                sword.Tick(0.05f, 5f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, sword.ReadabilityState);
                Assert.AreEqual("rusty_slash", sword.LastBehaviorReason);

                sword.Tick(0.05f, 5f + opener.WindupSeconds + 0.01f);
                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, sword.ReadabilityState);
                sword.Tick(0.05f, 5f + opener.WindupSeconds + opener.ActiveSeconds + 0.04f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, sword.ReadabilityState);

                sword.Tick(0.05f, 5f + opener.WindupSeconds + opener.ActiveSeconds + followUp.WindupSeconds + 0.08f);
                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, sword.ReadabilityState);
                sword.Tick(0.05f, 5f + opener.WindupSeconds + opener.ActiveSeconds + followUp.WindupSeconds + followUp.ActiveSeconds + 0.12f);
                Assert.AreEqual(EnemyReadabilityState.MeleeRecovery, sword.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void KnightMediumGuardReducesFrontalHitsBreaksOnHeavyAndFlanksBypass()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var knight = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyKnight"));
                knight.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.5f);
                var startingHealth = knight.Health.CurrentHealth;
                Assert.IsTrue(StartGuard(knight, 3f));
                knight.Tick(0.05f, 3.13f);
                Assert.AreEqual(EnemyReadabilityState.GuardActive, knight.ReadabilityState);

                var lightApplied = DamageSystem.ApplyDamage(
                    knight.Health,
                    new DamageRequest(1, player.gameObject, DamageFeedbackContext.None, DamageThreatKind.Light, DamageClassification.PhysicalMelee(ImpactForceClass.Light)));
                Assert.IsFalse(lightApplied);
                Assert.AreEqual(startingHealth, knight.Health.CurrentHealth);

                var heavyApplied = DamageSystem.ApplyDamage(
                    knight.Health,
                    new DamageRequest(2, player.gameObject, DamageFeedbackContext.None, DamageThreatKind.Heavy, DamageClassification.PhysicalMelee(ImpactForceClass.Heavy)));
                Assert.IsTrue(heavyApplied);
                Assert.AreEqual(startingHealth - 1, knight.Health.CurrentHealth);
                Assert.AreEqual(EnemyReadabilityState.GuardRecovery, knight.ReadabilityState);

                var flankKnight = CreateEnemy(root.transform, room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyKnight"));
                flankKnight.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.5f);
                Assert.IsTrue(StartGuard(flankKnight, 6f));
                flankKnight.Tick(0.05f, 6.13f);
                Assert.AreEqual(EnemyReadabilityState.GuardActive, flankKnight.ReadabilityState);
                player.transform.localPosition = new Vector3(1.5f, 0f, 0f);
                var flankStart = flankKnight.Health.CurrentHealth;
                var flankApplied = DamageSystem.ApplyDamage(
                    flankKnight.Health,
                    new DamageRequest(1, player.gameObject, DamageFeedbackContext.None, DamageThreatKind.Light, DamageClassification.PhysicalMelee(ImpactForceClass.Light)));
                Assert.IsTrue(flankApplied);
                Assert.AreEqual(flankStart - 1, flankKnight.Health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GiantAreaAttackDamagesOnlyDuringActiveWindow()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var playerHealth = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyGiant");
                var profile = definition.ResolveAttackProfile("overhead_slam");
                var giant = CreateEnemy(root.transform, room, player, definition);
                giant.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 1.35f);

                giant.Tick(0.05f, 10f);
                Assert.AreEqual(EnemyReadabilityState.AreaWindup, giant.ReadabilityState);
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);

                var activeTime = 10f + profile.WindupSeconds + 0.5f;
                giant.Tick(0.05f, activeTime);
                Assert.AreEqual(EnemyReadabilityState.AreaActive, giant.ReadabilityState);
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);

                giant.Tick(0.05f, activeTime + 0.06f);
                Assert.AreEqual(playerHealth.MaxHealth - profile.Damage, playerHealth.CurrentHealth);

                giant.Tick(0.05f, activeTime + profile.ActiveSeconds + 0.5f);
                Assert.AreEqual(EnemyReadabilityState.AreaRecovery, giant.ReadabilityState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BattlefieldRoomsAndCatalogueFilesExistPdfExtractsAndValidatorPasses()
        {
            foreach (var roomId in Milestone84AssetGenerator.BattlefieldRoomIds)
            {
                var path = $"{Milestone84AssetGenerator.BattlefieldRoomDirectory}/{roomId}.hollowruntime.json";
                Assert.IsTrue(File.Exists(path), path);
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(path));
                Assert.Greater(asset.EnemySpawns.Count(spawn => Milestone84AssetGenerator.SpawnKinds.Contains(spawn.kind)), 0, roomId);
                Assert.Greater(asset.Layout.WalkableTiles.Count, 0, roomId);
                Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.RoomReward), roomId);
            }

            Assert.IsTrue(File.Exists(Milestone84AssetGenerator.DocsPath), Milestone84AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone84AssetGenerator.PdfPath), Milestone84AssetGenerator.PdfPath);
            var markdown = File.ReadAllText(Milestone84AssetGenerator.DocsPath);
            StringAssert.Contains("Weapon-User Enemies", markdown);
            StringAssert.Contains("Skeleton Sword", markdown);
            StringAssert.Contains("Knight", markdown);
            StringAssert.Contains("Giant", markdown);
            StringAssert.Contains("shield", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone84Validator.Validate());
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M84Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<PlayerWeaponController>();
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

        private static bool StartGuard(EnemyRuntimeController enemy, float timeSeconds)
        {
            var method = typeof(EnemyRuntimeController).GetMethod("TryStartGuardAction", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (bool)method.Invoke(enemy, new object[] { timeSeconds, "shield_guard" });
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath(Milestone84AssetGenerator.VerifyScriptPath);
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
                Assert.Fail("Timed out while verifying the M84 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }
    }
}
