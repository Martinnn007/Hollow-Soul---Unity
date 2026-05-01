using System;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone69EquipmentLoadShieldStabilityTests
    {
        [Test]
        public void EquipmentLoadResolverComputesTierAndPenalties()
        {
            var weaponCatalog = CreateWeaponCatalog();
            var armorCatalog = CreateArmorCatalog();
            var shieldCatalog = CreateShieldCatalog();
            var build = new PlayerRunBuild();
            try
            {
                var starterLoad = EquipmentLoadResolver.Resolve(build, weaponCatalog, armorCatalog, shieldCatalog);
                Assert.AreEqual(4, starterLoad.TotalLoadScore);
                Assert.AreEqual(EquipmentLoadTier.Light, starterLoad.Tier);
                Assert.AreEqual(1f, starterLoad.SpeedMultiplier);

                build.Equipment.EquipMeleeWeapon("iron_cleaver");
                build.Equipment.EquipRangedWeapon("ember_bolt");
                build.Equipment.EquipArmor("dragon_scale_armor");
                build.Equipment.EquipShield("stone_wall_shield");

                var heavyLoad = EquipmentLoadResolver.Resolve(build, weaponCatalog, armorCatalog, shieldCatalog);
                Assert.AreEqual(11, heavyLoad.TotalLoadScore);
                Assert.AreEqual(EquipmentLoadTier.Heavy, heavyLoad.Tier);
                Assert.AreEqual(0.80f, heavyLoad.SpeedMultiplier, 0.001f);
                Assert.AreEqual(1.25f, heavyLoad.AttackStaminaCostMultiplier, 0.001f);
                Assert.AreEqual(1.25f, heavyLoad.GuardStaminaCostMultiplier, 0.001f);
                Assert.AreEqual(0.90f, heavyLoad.StaminaRegenMultiplier, 0.001f);
                Assert.AreEqual(2, heavyLoad.ArmorStabilityBonus);
                Assert.AreEqual(3, heavyLoad.ActiveGuardShieldStabilityBonus);
            }
            finally
            {
                DestroyCatalogs(weaponCatalog, armorCatalog, shieldCatalog);
            }
        }

        [Test]
        public void ShieldRewardEquipsAndPersistsShieldSlot()
        {
            var shieldCatalog = CreateShieldCatalog();
            var build = new PlayerRunBuild();
            try
            {
                var applied = RewardApplicationService.Apply(
                    new RewardGrant("room", "stone_wall_shield", "Stone Wall Shield", RewardKind.Shield, 0, 0, Array.Empty<RewardEffect>()),
                    new RunEconomy(),
                    new PlayerRunStats(),
                    build,
                    null,
                    shieldCatalog,
                    null);

                Assert.IsTrue(applied.Applied);
                Assert.AreEqual("stone_wall_shield", build.Equipment.ShieldId);
                Assert.AreEqual("stone_wall_shield", RunEquipmentSlots.FromSaveState(build.Equipment.ToSaveState()).ShieldId);
                Assert.AreEqual(ShieldDefinition.StarterShieldId, RunEquipmentSlots.FromSaveState(null).ShieldId);
            }
            finally
            {
                DestroyCatalogs(null, null, shieldCatalog);
            }
        }

        [Test]
        public void StabilityResistsLightForceAndReducesHeavyForce()
        {
            var target = new GameObject("M69KnockbackTarget");
            try
            {
                var receiver = target.AddComponent<CombatKnockbackReceiver>();
                receiver.ConfigureStability(1);
                receiver.ApplyKnockback(Vector3.forward, 1f, 0.25f, DamageClassification.PhysicalMelee(ImpactForceClass.Light));
                Assert.IsFalse(receiver.IsKnockbackActive);

                receiver.ConfigureStability(5);
                receiver.ApplyKnockback(Vector3.forward, 1f, 0.25f, DamageClassification.PhysicalMelee(ImpactForceClass.Heavy));
                Assert.IsTrue(receiver.IsKnockbackActive);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void DamageRequestLegacyAndNewTaxonomyStayCompatible()
        {
            var legacy = new DamageRequest(1, null);
            var strongProjectile = new DamageRequest(1, null, DamageThreatKind.StrongProjectile);
            var explosion = new DamageRequest(2, null, DamageFeedbackContext.None, DamageThreatKind.Environmental, DamageClassification.Explosion(ImpactForceClass.Heavy));

            Assert.AreEqual(DamageThreatKind.Light, legacy.ThreatKind);
            Assert.AreEqual(DamageChannel.Physical, legacy.Classification.Channel);
            Assert.AreEqual(ImpactForceClass.Light, legacy.Classification.ForceClass);
            Assert.AreEqual(DamageDelivery.Projectile, strongProjectile.Classification.Delivery);
            Assert.AreEqual(ImpactForceClass.Heavy, strongProjectile.Classification.ForceClass);
            Assert.AreEqual(DamageChannel.Explosion, explosion.Classification.Channel);
            Assert.AreEqual(DamageDelivery.Area, explosion.Classification.Delivery);
        }

        [Test]
        public void Milestone69ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone69Validator.Validate());
        }

        private static WeaponCatalogDefinition CreateWeaponCatalog()
        {
            var starterBlade = CreateWeapon("starter_blade", "Practice Blade", WeaponSlot.Melee, EquipmentLoadClass.Light);
            var starterBolt = CreateWeapon("starter_bolt", "Practice Bolt", WeaponSlot.Ranged, EquipmentLoadClass.Light);
            var ironCleaver = CreateWeapon("iron_cleaver", "Iron Cleaver", WeaponSlot.Melee, EquipmentLoadClass.Heavy);
            var emberBolt = CreateWeapon("ember_bolt", "Ember Bolt", WeaponSlot.Ranged, EquipmentLoadClass.Medium);
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogDefinition>();
            catalog.Configure("test_weapons", new[] { starterBlade, starterBolt, ironCleaver, emberBolt });
            return catalog;
        }

        private static WeaponDefinition CreateWeapon(string id, string displayName, WeaponSlot slot, EquipmentLoadClass loadClass)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.Configure(
                id,
                displayName,
                slot,
                slot == WeaponSlot.Melee ? WeaponCategory.Blade : WeaponCategory.Wand,
                Array.Empty<BuildTag>(),
                new WeaponAttackDefinition(AttackKind.Light, 1, 1f, 0f, 1f),
                new WeaponAttackDefinition(AttackKind.Heavy, 2, 2f, 10f, 1f),
                loadClass);
            return weapon;
        }

        private static ArmorCatalogDefinition CreateArmorCatalog()
        {
            var dragonArmor = ScriptableObject.CreateInstance<ArmorDefinition>();
            dragonArmor.Configure("dragon_scale_armor", "Dragon Scale Armor", ArmorRarity.Rare, default, Array.Empty<BuildTag>(), EquipmentLoadClass.Heavy);
            var catalog = ScriptableObject.CreateInstance<ArmorCatalogDefinition>();
            catalog.Configure("test_armor", new[] { dragonArmor });
            return catalog;
        }

        private static ShieldCatalogDefinition CreateShieldCatalog()
        {
            var starter = ScriptableObject.CreateInstance<ShieldDefinition>();
            starter.Configure(ShieldDefinition.StarterShieldId, "Starter Buckler", ArmorRarity.Common, EquipmentLoadClass.Light, default, Array.Empty<BuildTag>());
            var stone = ScriptableObject.CreateInstance<ShieldDefinition>();
            stone.Configure("stone_wall_shield", "Stone Wall Shield", ArmorRarity.Rare, EquipmentLoadClass.Heavy, default, Array.Empty<BuildTag>());
            var catalog = ScriptableObject.CreateInstance<ShieldCatalogDefinition>();
            catalog.Configure("test_shields", new[] { starter, stone });
            return catalog;
        }

        private static void DestroyCatalogs(WeaponCatalogDefinition weaponCatalog, ArmorCatalogDefinition armorCatalog, ShieldCatalogDefinition shieldCatalog)
        {
            if (weaponCatalog != null)
            {
                foreach (var weapon in weaponCatalog.Weapons)
                {
                    Object.DestroyImmediate(weapon);
                }

                Object.DestroyImmediate(weaponCatalog);
            }

            if (armorCatalog != null)
            {
                foreach (var armor in armorCatalog.Armors)
                {
                    Object.DestroyImmediate(armor);
                }

                Object.DestroyImmediate(armorCatalog);
            }

            if (shieldCatalog != null)
            {
                foreach (var shield in shieldCatalog.Shields)
                {
                    Object.DestroyImmediate(shield);
                }

                Object.DestroyImmediate(shieldCatalog);
            }
        }
    }
}
