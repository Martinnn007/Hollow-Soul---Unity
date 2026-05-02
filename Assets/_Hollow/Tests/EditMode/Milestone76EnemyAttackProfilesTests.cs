using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone76EnemyAttackProfilesTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void CurrentEnemyAndBossRostersResolveValidAttackProfiles()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var group in EnemyAttackProfileDefaults.AllEnemySpecs.GroupBy(spec => spec.OwnerId))
            {
                var definition = catalog.Resolve(group.Key);
                Assert.NotNull(definition, group.Key);
                var attackIds = definition.AttackProfiles.Select(profile => profile.AttackId).ToArray();
                foreach (var spec in group)
                {
                    Assert.Contains(spec.AttackId, attackIds, group.Key);
                }

                foreach (var profile in definition.AttackProfiles)
                {
                    AssertProfileValid(profile);
                }
            }

            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            foreach (var group in EnemyAttackProfileDefaults.AllBossSpecs.GroupBy(spec => spec.OwnerId))
            {
                var boss = bosses.FirstOrDefault(candidate => candidate.BossId == group.Key);
                Assert.NotNull(boss, group.Key);
                var attackIds = boss.AttackProfiles.Select(profile => profile.AttackId).ToArray();
                foreach (var spec in group)
                {
                    Assert.Contains(spec.AttackId, attackIds, group.Key);
                }

                foreach (var profile in boss.AttackProfiles)
                {
                    AssertProfileValid(profile);
                }
            }
        }

        [Test]
        public void NormalChaserLungeUsesProfileDamageAndKnockback()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var health = player.GetComponent<CombatantHealth>();
                var receiver = player.GetComponent<CombatKnockbackReceiver>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
                var profile = definition.ResolveAttackProfile("claw_lunge");
                player.transform.localPosition = new Vector3(0f, 0f, 1.35f);
                var enemy = CreateEnemy(root.transform, room, player, definition);
                enemy.transform.localPosition = Vector3.zero;

                enemy.Tick(0.05f, 5f);
                Assert.AreEqual(EnemyReadabilityState.MeleeWindup, enemy.ReadabilityState);

                enemy.Tick(0.05f, 5f + profile.WindupSeconds + 0.01f);
                Assert.AreEqual(EnemyReadabilityState.MeleeLunge, enemy.ReadabilityState);

                enemy.transform.localPosition = new Vector3(0f, 0f, 0.82f);
                var damaged = enemy.TryApplyContactDamage(5f + profile.WindupSeconds + 0.08f);

                Assert.IsTrue(damaged);
                Assert.AreEqual(health.MaxHealth - profile.Damage, health.CurrentHealth);
                Assert.IsTrue(receiver.IsKnockbackActive);
                receiver.Tick(0.12f);
                Assert.Greater(player.transform.localPosition.z, 1.35f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProjectileControllerCarriesProfileClassificationAndKnockback()
        {
            var projectileObject = new GameObject("M76Projectile");
            try
            {
                var projectile = projectileObject.AddComponent<EnemyProjectileController>();
                var profile = EnemyAttackProfileDefaults.ResolveEnemyProfile("spawnEnemyTurret", "braced_spike");

                projectile.Configure(null, null, Vector3.forward, 1, 1f);
                projectile.ConfigureAttackProfile(profile);

                Assert.AreEqual(profile.Damage, projectile.Damage);
                Assert.AreEqual(DamageChannel.Physical, projectile.DamageClassification.Channel);
                Assert.AreEqual(DamageDelivery.Projectile, projectile.DamageClassification.Delivery);
                Assert.AreEqual(ImpactForceClass.Medium, projectile.DamageClassification.ForceClass);
                Assert.AreEqual(0.48f, projectile.KnockbackMeters, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(projectileObject);
            }
        }

        [Test]
        public void GuardedProfileHitsApplyReducedRecoilButPerfectParryDoesNot()
        {
            var blockedPlayer = CreateGuardPlayer("M76BlockedPlayer", out var blockedHealth, out var blockedDefense, out var blockedReceiver);
            var blockedSource = new GameObject("M76BlockedSource");
            try
            {
                blockedSource.transform.localPosition = new Vector3(0f, 0f, 1f);
                var heavyProfile = EnemyAttackProfileDefaults.ResolveEnemyProfile("spawnEnemyHeavy", "maul_lunge");
                blockedDefense.Tick(Snapshot(Vector2.up, true), 0f, 1f);

                var applied = DamageSystem.ApplyDamage(
                    blockedHealth,
                    heavyProfile.CreateDamageRequest(blockedSource, Vector3.forward, 0.1f));

                Assert.IsTrue(applied);
                Assert.AreEqual(5, blockedHealth.CurrentHealth);
                Assert.AreEqual(ShieldGuardResult.RejectedThreat, blockedDefense.LastGuardResult);
                blockedReceiver.Tick(0.1f);
                Assert.AreEqual(
                    heavyProfile.KnockbackMeters * heavyProfile.GuardKnockbackMultiplier,
                    blockedPlayer.transform.localPosition.z,
                    0.02f);
            }
            finally
            {
                Object.DestroyImmediate(blockedSource);
                Object.DestroyImmediate(blockedPlayer);
            }

            var parryPlayer = CreateGuardPlayer("M76ParryPlayer", out var parryHealth, out var parryDefense, out var parryReceiver);
            var parrySource = new GameObject("M76ParrySource");
            try
            {
                parrySource.transform.localPosition = new Vector3(0f, 0f, 1f);
                var lightProfile = EnemyAttackProfileDefaults.ResolveEnemyProfile("spawnEnemyNormal", "claw_lunge");
                parryDefense.Tick(Snapshot(Vector2.up, true), 0f, 2f);

                var applied = DamageSystem.ApplyDamage(
                    parryHealth,
                    lightProfile.CreateDamageRequest(parrySource, Vector3.forward, 0.1f));

                Assert.IsFalse(applied);
                Assert.AreEqual(parryHealth.MaxHealth, parryHealth.CurrentHealth);
                Assert.AreEqual(ShieldGuardResult.PerfectParry, parryDefense.LastGuardResult);
                parryReceiver.Tick(0.1f);
                Assert.AreEqual(0f, parryPlayer.transform.localPosition.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(parrySource);
                Object.DestroyImmediate(parryPlayer);
            }
        }

        [Test]
        public void BossProfilesCarryAuthoredElementalIdentity()
        {
            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            var ashComet = bosses.First(boss => boss.BossId == "ash_comet");
            var hollowStar = bosses.First(boss => boss.BossId == "hollow_star_larva");

            var ashFire = ashComet.ResolveAttackProfile("ash_fire_radial");
            Assert.AreEqual(DamageChannel.Elemental, ashFire.DamageChannel);
            Assert.AreEqual(DamageDelivery.Projectile, ashFire.DamageDelivery);
            Assert.AreEqual(DamageElement.Fire, ashFire.DamageElement);
            Assert.AreEqual(ImpactForceClass.Heavy, ashFire.ForceClass);

            var starfall = hollowStar.ResolveAttackProfile("larva_starfall");
            Assert.AreEqual(DamageChannel.Elemental, starfall.DamageChannel);
            Assert.AreEqual(DamageElement.Cosmic, starfall.DamageElement);
        }

        [Test]
        public void CatalogueFilesExistPdfExtractsAndValidatorPasses()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M76_Enemy_Attack_Profiles.md"));
            Assert.IsTrue(File.Exists("output/pdf/Hollow_M76_Enemy_Attack_Profiles.pdf"));
            var markdown = File.ReadAllText("Docs/Hollow_M76_Enemy_Attack_Profiles.md");
            StringAssert.Contains("Attack Profiles", markdown);
            StringAssert.Contains("Physical", markdown);
            StringAssert.Contains("Projectile", markdown);
            StringAssert.Contains("knockback", markdown);
            StringAssert.Contains("stability", markdown);
            StringAssert.Contains("Normal Chaser", markdown);
            StringAssert.Contains("Bone Turret", markdown);
            StringAssert.Contains("Hollow Star Larva", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone76Validator.Validate());
        }

        private static void AssertProfileValid(EnemyAttackProfileDefinition profile)
        {
            Assert.NotNull(profile);
            Assert.IsNotEmpty(profile.AttackId);
            Assert.IsNotEmpty(profile.DisplayName);
            Assert.GreaterOrEqual(profile.Damage, 0, profile.AttackId);
            Assert.Greater(profile.CooldownSeconds, 0f, profile.AttackId);
            Assert.Greater(profile.ActiveSeconds, 0f, profile.AttackId);
            Assert.GreaterOrEqual(profile.RangeMeters, 0f, profile.AttackId);
            Assert.GreaterOrEqual(profile.KnockbackMeters, 0f, profile.AttackId);
            Assert.GreaterOrEqual(profile.GuardKnockbackMultiplier, 0f, profile.AttackId);
            Assert.LessOrEqual(profile.GuardKnockbackMultiplier, 1f, profile.AttackId);
            Assert.AreEqual(profile.DamageChannel, profile.Classification.Channel, profile.AttackId);
            Assert.AreEqual(profile.DamageDelivery, profile.Classification.Delivery, profile.AttackId);
            Assert.AreEqual(profile.DamageElement, profile.Classification.Element, profile.AttackId);
            Assert.AreEqual(profile.ForceClass, profile.Classification.ForceClass, profile.AttackId);
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath("tools/verify_m76_enemy_attack_profiles_pdf.py");
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
                Assert.Fail("Timed out while verifying the M76 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M76Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            var receiver = playerObject.AddComponent<CombatKnockbackReceiver>();
            receiver.Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            receiver.ConfigureStability(0);

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

        private static GameObject CreateGuardPlayer(
            string name,
            out CombatantHealth health,
            out PlayerDefenseController defense,
            out CombatKnockbackReceiver receiver)
        {
            var player = new GameObject(name);
            player.AddComponent<PlaceholderPlayerController>();
            health = player.AddComponent<CombatantHealth>();
            health.Configure(6);
            receiver = player.AddComponent<CombatKnockbackReceiver>();
            defense = player.AddComponent<PlayerDefenseController>();
            defense.Configure(0);
            defense.ConfigureStability(0, 0, 1f);
            defense.ConfigureShieldProfile(null);
            receiver.Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            receiver.ConfigureStability(0);
            return player;
        }

        private static GameplayInputSnapshot Snapshot(Vector2 aim, bool guardHeld)
        {
            return new GameplayInputSnapshot(
                Vector2.zero,
                aim,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: guardHeld);
        }
    }
}
