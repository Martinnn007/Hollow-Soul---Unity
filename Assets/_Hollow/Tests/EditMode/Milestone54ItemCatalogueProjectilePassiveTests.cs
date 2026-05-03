using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone54ItemCatalogueProjectilePassiveTests
    {
        [Test]
        public void PassiveInventorySavesStacksAndRestoresLegacyIds()
        {
            var inventory = new RunInventoryState();
            for (var index = 0; index < 4; index++)
            {
                inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, ProjectilePassiveResolver.FireRateUpMaxStacks);
            }

            Assert.AreEqual(3, inventory.PassiveItemCount(ProjectilePassiveResolver.FireRateUpId));
            var restored = RunInventoryState.FromSaveState(inventory.ToSaveState());
            Assert.AreEqual(3, restored.PassiveItemCount(ProjectilePassiveResolver.FireRateUpId));

            var legacy = RunInventoryState.FromSaveState(new Hollow.Persistence.RunInventoryStateSaveState
            {
                passiveItemIds = new List<string> { ProjectilePassiveResolver.PowerUpId }
            });
            Assert.AreEqual(1, legacy.PassiveItemCount(ProjectilePassiveResolver.PowerUpId));

            var overStacked = RunInventoryState.FromSaveState(new Hollow.Persistence.RunInventoryStateSaveState
            {
                passiveItemStacks = new List<Hollow.Persistence.PassiveItemStackSaveState>
                {
                    new() { itemId = ProjectilePassiveResolver.FireRateUpId, count = 99 }
                }
            });
            Assert.AreEqual(3, overStacked.PassiveItemCount(ProjectilePassiveResolver.FireRateUpId));
        }

        [Test]
        public void ProjectilePassiveResolverChoosesStrongestPatternAndCapsFireRate()
        {
            var build = new PlayerRunBuild();
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.DoubleBarrelId, 1);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.TripleShotId, 1);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.QuadShotId, 1);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.PowerUpId, 1);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, 3);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, 3);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, 3);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, 3);

            var state = ProjectilePassiveResolver.Resolve(build);
            Assert.AreEqual(ProjectilePatternKind.QuadShot, state.PatternKind);
            Assert.AreEqual(ProjectileVisualStyle.RedPower, state.VisualStyle);
            Assert.AreEqual(2f, state.RangedDamageMultiplier);
            Assert.AreEqual(3f, state.RangedLightFireRateBonusPerSecond);
        }

        [Test]
        public void PlayerWeaponControllerSpawnsMultiShotProjectiles()
        {
            var parent = new GameObject("M54ProjectileParent");
            var player = new GameObject("Player");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            player.transform.SetParent(parent.transform, false);
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.ConfigureProjectilePassives(new ProjectilePassiveState(ProjectilePatternKind.QuadShot, 1f, 0f, ProjectileVisualStyle.Default));

                Assert.IsTrue(weapon.TryFire(Vector2.up, 0f));
                weapon.TickAction(0f, WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged).WindupSeconds + 0.01f);
                Assert.AreEqual(4, parent.transform.Cast<Transform>().Count(child => child.name == "PlayerProjectile"));
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(combat.gameObject);
                Object.DestroyImmediate(projectilePrefab);
            }
        }

        [Test]
        public void GeneratedPoolsKeepM54PassivesOutOfStandardRooms()
        {
            var standard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone52AssetGenerator.StandardRewardPoolPath);
            var treasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.TreasureRewardPoolPath);
            var boss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.BossRewardPoolPath);

            Assert.IsNotNull(standard);
            Assert.IsNotNull(treasure);
            Assert.IsNotNull(boss);
            Assert.IsFalse(standard.Rewards.Any(reward => reward != null && ProjectilePassiveResolver.IsM54ProjectilePassive(reward.RewardId)));
            foreach (var id in ProjectilePassiveResolver.AllProjectilePassiveIds)
            {
                Assert.IsTrue(treasure.Rewards.Any(reward => reward != null && reward.RewardId == id), id);
                Assert.IsTrue(boss.Rewards.Any(reward => reward != null && reward.RewardId == id), id);
            }
        }

        [Test]
        public void ItemCataloguePdfExistsAndMentionsM54Items()
        {
            Assert.IsTrue(File.Exists(Milestone54AssetGenerator.PdfPath));
            var bytes = File.ReadAllBytes(Milestone54AssetGenerator.PdfPath);
            Assert.Greater(bytes.Length, 1000);

            var text = File.ReadAllText(Milestone54AssetGenerator.ReportPath);
            Assert.That(text, Does.Contain("Double-Barrel"));
            Assert.That(text, Does.Contain("Triple-Shot"));
            Assert.That(text, Does.Contain("Quad-Shot"));
            Assert.That(text, Does.Contain("Power-up"));
            Assert.That(text, Does.Contain("Fire-rate Up"));
        }

        [Test]
        public void Milestone54ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone54Validator.Validate());
        }
    }
}
