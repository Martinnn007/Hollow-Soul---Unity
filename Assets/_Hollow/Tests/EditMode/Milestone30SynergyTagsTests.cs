using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone30SynergyTagsTests
    {
        [Test]
        public void ArmorRewardEquipsAndPersistsArmorSlot()
        {
            var economy = new RunEconomy();
            var stats = new PlayerRunStats();
            var build = new PlayerRunBuild();

            var result = RewardApplicationService.Apply(
                new RewardGrant("armor", "skeletal_armor", "Skeletal Armor", RewardKind.Armor, 0, System.Array.Empty<RewardEffect>()),
                economy,
                stats,
                build,
                null,
                null);

            Assert.IsTrue(result.Applied);
            Assert.AreEqual("skeletal_armor", build.Equipment.ArmorId);

            var restored = PlayerRunBuild.FromSaveState(build.ToSaveState());
            Assert.AreEqual("skeletal_armor", restored.Equipment.ArmorId);
        }

        [Test]
        public void SynergyResolverActivatesSkeletalSetFromThreeCategories()
        {
            var build = new PlayerRunBuild();
            build.Equipment.EquipMeleeWeapon("skeletal_sword");
            build.Equipment.EquipArmor("skeletal_armor");
            build.Inventory.AddPassiveItem("cursed_skull");

            var resolution = SynergyResolver.ResolveActiveSynergy(
                build,
                null,
                LoadWeapons(),
                LoadArmors(),
                RewardPools(),
                LoadUsables(),
                LoadSynergies());

            Assert.IsTrue(resolution.IsActive);
            Assert.AreEqual("skeletal_set", resolution.SynergyId);
            Assert.AreEqual(3, resolution.MatchingCategoryCount);
            Assert.AreEqual(1, resolution.ToModifier().meleeDamage);
        }

        [Test]
        public void SynergyResolverIgnoresConsumableCards()
        {
            var build = new PlayerRunBuild();
            build.Equipment.EquipMeleeWeapon("skeletal_sword");
            build.Equipment.EquipArmor("skeletal_armor");
            build.Equipment.EquipConsumableCard("skeletal_debug_card");

            var resolution = SynergyResolver.ResolveActiveSynergy(
                build,
                null,
                LoadWeapons(),
                LoadArmors(),
                RewardPools(),
                LoadUsables(),
                LoadSynergies());

            Assert.IsFalse(resolution.IsActive);
        }

        [Test]
        public void SynergyResolverChoosesHighestMatchingPieceCountThenPriority()
        {
            var build = new PlayerRunBuild();
            build.Equipment.EquipMeleeWeapon("skeletal_sword");
            build.Equipment.EquipRangedWeapon("bone_pistol");
            build.Equipment.EquipArmor("skeletal_armor");
            build.Inventory.AddPassiveItem("cursed_skull");
            build.Equipment.EquipActiveItem("bone_totem");

            var resolution = SynergyResolver.ResolveActiveSynergy(
                build,
                null,
                LoadWeapons(),
                LoadArmors(),
                RewardPools(),
                LoadUsables(),
                LoadSynergies());

            Assert.AreEqual("skeletal_set", resolution.SynergyId);
            Assert.GreaterOrEqual(resolution.MatchingPieceCount, 5);
        }

        [Test]
        public void ArmorAndSynergyModifiersDoNotMutateBaseStats()
        {
            var build = new PlayerRunBuild();
            build.Equipment.EquipMeleeWeapon("dragon_fang");
            build.Equipment.EquipRangedWeapon("dragon_pistol");
            build.Equipment.EquipArmor("dragon_scale_armor");

            var baseMaxStamina = build.BaseStats.MaxStamina;
            var armor = LoadArmors();
            Assert.IsTrue(armor.TryGetArmor("dragon_scale_armor", out var dragonArmor));
            build.AddModifier(PlayerStatModifier.FromCharacterStatModifier($"armor:{dragonArmor.ArmorId}", dragonArmor.StatModifier));
            var resolution = SynergyResolver.ResolveActiveSynergy(build, null, LoadWeapons(), armor, RewardPools(), LoadUsables(), LoadSynergies());
            build.AddModifier(resolution.ToModifier());

            Assert.AreEqual(baseMaxStamina, build.BaseStats.MaxStamina);
            Assert.Greater(build.DerivedStats.MaxStamina, baseMaxStamina);
        }

        [Test]
        public void GeneratedM30ContentHasRequiredTagsAndCatalogs()
        {
            var weapons = LoadWeapons();
            Assert.IsTrue(weapons.TryGetWeapon("skeletal_sword", out var skeletalSword));
            Assert.Contains(BuildTag.Skeletal, (System.Collections.ICollection)skeletalSword.Tags);
            Assert.IsTrue(weapons.TryGetWeapon("dragon_pistol", out var dragonPistol));
            Assert.AreEqual(WeaponCategory.Gun, dragonPistol.Category);
            Assert.AreEqual(WeaponRangedFireMode.Instant, dragonPistol.RangedFireMode);
            Assert.Contains(BuildTag.Dragon, (System.Collections.ICollection)dragonPistol.Tags);

            var armors = LoadArmors();
            Assert.IsTrue(armors.TryGetArmor("dragon_scale_armor", out var dragonArmor));
            Assert.Contains(BuildTag.Armor, (System.Collections.ICollection)dragonArmor.Tags);
            Assert.Contains(BuildTag.Dragon, (System.Collections.ICollection)dragonArmor.Tags);

            Assert.IsTrue(LoadUsables().TryGet("dragon_tooth", out var dragonTooth));
            Assert.Contains(BuildTag.Dragon, (System.Collections.ICollection)dragonTooth.Tags);
        }

        [Test]
        public void Milestone30ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone30Validator.Validate());
        }

        private static WeaponCatalogDefinition LoadWeapons()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            Assert.IsNotNull(catalog, "Run M30 generation before validating weapon catalog.");
            return catalog;
        }

        private static ArmorCatalogDefinition LoadArmors()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ArmorCatalogDefinition>(Milestone30AssetGenerator.ArmorCatalogPath);
            Assert.IsNotNull(catalog, "Run M30 generation before validating armor catalog.");
            return catalog;
        }

        private static UsableItemCatalogDefinition LoadUsables()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UsableItemCatalogDefinition>(Milestone28AssetGenerator.UsableItemCatalogPath);
            Assert.IsNotNull(catalog, "Run M30 generation before validating usable catalog.");
            return catalog;
        }

        private static SynergyCatalogDefinition LoadSynergies()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SynergyCatalogDefinition>(Milestone30AssetGenerator.SynergyCatalogPath);
            Assert.IsNotNull(catalog, "Run M30 generation before validating synergy catalog.");
            return catalog;
        }

        private static RewardPoolDefinition[] RewardPools()
        {
            return new[]
            {
                AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.StandardRewardPoolPath),
                AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.TreasureRewardPoolPath),
                AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.BossRewardPoolPath),
                AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath)
            }.Where(pool => pool != null).ToArray();
        }
    }
}
