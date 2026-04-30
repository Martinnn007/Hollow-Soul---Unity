using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone30AssetGenerator
    {
        public const string EquipmentDirectory = "Assets/_Hollow/Data/Equipment/M30";
        public const string SynergyDirectory = "Assets/_Hollow/Data/Synergies/M30";
        public const string SetRewardDirectory = "Assets/_Hollow/Data/Rewards/M30";
        public const string SetWeaponDirectory = "Assets/_Hollow/Data/Weapons/M30";
        public const string ArmorCatalogPath = EquipmentDirectory + "/ArmorCatalog_M30.asset";
        public const string SynergyCatalogPath = SynergyDirectory + "/SynergyCatalog_M30.asset";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 30 Assets")]
        public static void Generate()
        {
            Milestone29AssetGenerator.Generate();
            Directory.CreateDirectory(EquipmentDirectory);
            Directory.CreateDirectory(SynergyDirectory);
            Directory.CreateDirectory(SetRewardDirectory);
            Directory.CreateDirectory(SetWeaponDirectory);

            RetagExistingContent();

            var skeletalSword = SaveWeapon(
                "Weapon_SkeletalSword.asset",
                "skeletal_sword",
                "Skeletal Sword",
                WeaponSlot.Melee,
                WeaponCategory.Blade,
                new[] { BuildTag.Melee, BuildTag.Heavy, BuildTag.Skeletal },
                new WeaponAttackDefinition(AttackKind.Light, 2, 0.78f, 8f, 1.05f),
                new WeaponAttackDefinition(AttackKind.Heavy, 4, 3f, 42f, 1.25f));
            var boneBow = SaveWeapon(
                "Weapon_BoneBow.asset",
                "bone_bow",
                "Bone Bow",
                WeaponSlot.Ranged,
                WeaponCategory.Bow,
                new[] { BuildTag.Ranged, BuildTag.Fast, BuildTag.Skeletal },
                new WeaponAttackDefinition(AttackKind.Light, 1, 0.95f, 2f, 6.67f),
                new WeaponAttackDefinition(AttackKind.Heavy, 3, 8.5f, 36f, 6.67f));
            var dragonFang = SaveWeapon(
                "Weapon_DragonFang.asset",
                "dragon_fang",
                "Dragon Fang",
                WeaponSlot.Melee,
                WeaponCategory.Blade,
                new[] { BuildTag.Melee, BuildTag.Fire, BuildTag.Dragon },
                new WeaponAttackDefinition(AttackKind.Light, 2, 0.82f, 9f, 1.05f),
                new WeaponAttackDefinition(AttackKind.Heavy, 4, 3f, 42f, 1.3f));
            var dragonBow = SaveWeapon(
                "Weapon_DragonBow.asset",
                "dragon_bow",
                "Dragon Bow",
                WeaponSlot.Ranged,
                WeaponCategory.Bow,
                new[] { BuildTag.Ranged, BuildTag.Fire, BuildTag.Dragon },
                new WeaponAttackDefinition(AttackKind.Light, 2, 0.85f, 4f, 7f),
                new WeaponAttackDefinition(AttackKind.Heavy, 4, 8f, 38f, 6.33f));

            var skeletalArmor = SaveArmor(
                "Armor_SkeletalArmor.asset",
                "skeletal_armor",
                "Skeletal Armor",
                ArmorRarity.Uncommon,
                new CharacterStatModifier(defense: 2, speed: -0.08f),
                new[] { BuildTag.Armor, BuildTag.Defense, BuildTag.Skeletal });
            var dragonScaleArmor = SaveArmor(
                "Armor_DragonScaleArmor.asset",
                "dragon_scale_armor",
                "Dragon Scale Armor",
                ArmorRarity.Rare,
                new CharacterStatModifier(defense: 2, maxStamina: 5f, speed: -0.12f),
                new[] { BuildTag.Armor, BuildTag.Defense, BuildTag.Dragon });

            var cursedSkull = SaveReward(
                "Reward_CursedSkull.asset",
                "cursed_skull",
                "Cursed Skull",
                RewardKind.PassiveItem,
                RewardRarity.Uncommon,
                8,
                0,
                new[] { new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1) },
                new[] { BuildTag.Magic, BuildTag.Skeletal });
            var dragonHeart = SaveReward(
                "Reward_DragonHeart.asset",
                "dragon_heart",
                "Dragon Heart",
                RewardKind.PassiveItem,
                RewardRarity.Rare,
                8,
                0,
                new[] { new RewardEffect(RewardEffectKind.MaxStaminaBonus, floatValue: 10f) },
                new[] { BuildTag.Fire, BuildTag.Dragon });
            var boneTotemReward = SaveReward(
                "Reward_BoneTotem.asset",
                "bone_totem",
                "Bone Totem",
                RewardKind.ActiveItem,
                RewardRarity.Uncommon,
                0,
                0,
                System.Array.Empty<RewardEffect>(),
                new[] { BuildTag.Magic, BuildTag.Skeletal });
            var dragonToothReward = SaveReward(
                "Reward_DragonTooth.asset",
                "dragon_tooth",
                "Dragon Tooth",
                RewardKind.ActiveItem,
                RewardRarity.Rare,
                0,
                0,
                System.Array.Empty<RewardEffect>(),
                new[] { BuildTag.Fire, BuildTag.Dragon });
            var skeletalArmorReward = SaveReward(
                "Reward_SkeletalArmor.asset",
                skeletalArmor.ArmorId,
                skeletalArmor.DisplayName,
                RewardKind.Armor,
                RewardRarity.Uncommon,
                0,
                0,
                System.Array.Empty<RewardEffect>(),
                skeletalArmor.Tags);
            var dragonArmorReward = SaveReward(
                "Reward_DragonScaleArmor.asset",
                dragonScaleArmor.ArmorId,
                dragonScaleArmor.DisplayName,
                RewardKind.Armor,
                RewardRarity.Rare,
                0,
                0,
                System.Array.Empty<RewardEffect>(),
                dragonScaleArmor.Tags);

            var boneTotem = SaveUsable(
                "Usable_BoneTotem.asset",
                "bone_totem",
                "Bone Totem",
                RewardKind.ActiveItem,
                RewardRarity.Uncommon,
                maxCharges: 3,
                consumeOnUse: false,
                new[] { new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1) },
                new[] { BuildTag.Magic, BuildTag.Skeletal });
            var dragonTooth = SaveUsable(
                "Usable_DragonTooth.asset",
                "dragon_tooth",
                "Dragon Tooth",
                RewardKind.ActiveItem,
                RewardRarity.Rare,
                maxCharges: 3,
                consumeOnUse: false,
                new[] { new RewardEffect(RewardEffectKind.RangedDamageBonus, intValue: 1) },
                new[] { BuildTag.Fire, BuildTag.Dragon });

            var armorCatalog = SaveArmorCatalog(new[] { skeletalArmor, dragonScaleArmor });
            var synergyCatalog = SaveSynergyCatalog(new[]
            {
                SaveSynergy(
                    "Synergy_SkeletalSet.asset",
                    "skeletal_set",
                    "Skeletal Set",
                    BuildTag.Skeletal,
                    requiredCategoryCount: 3,
                    priority: 20,
                    new CharacterStatModifier(meleeDamage: 1, attackCooldownMultiplier: 0.98f)),
                SaveSynergy(
                    "Synergy_DragonSet.asset",
                    "dragon_set",
                    "Dragon Set",
                    BuildTag.Dragon,
                    requiredCategoryCount: 3,
                    priority: 10,
                    new CharacterStatModifier(rangedDamage: 1, maxStamina: 10f))
            });

            var weaponCatalog = ExtendWeaponCatalog(new[] { skeletalSword, boneBow, dragonFang, dragonBow });
            var weaponPool = ExtendWeaponRewardPool(new[]
            {
                SaveWeaponReward("Reward_SkeletalSword.asset", skeletalSword, RewardRarity.Rare),
                SaveWeaponReward("Reward_BoneBow.asset", boneBow, RewardRarity.Rare),
                SaveWeaponReward("Reward_DragonFang.asset", dragonFang, RewardRarity.Epic),
                SaveWeaponReward("Reward_DragonBow.asset", dragonBow, RewardRarity.Epic)
            });
            var standardPool = ExtendPool(Milestone28AssetGenerator.StandardRewardPoolPath, "m28_standard_room_rewards", new[]
            {
                cursedSkull,
                boneTotemReward,
                skeletalArmorReward,
                dragonHeart,
                dragonToothReward,
                dragonArmorReward
            });
            var treasurePool = ExtendPool(Milestone28AssetGenerator.TreasureRewardPoolPath, "m28_treasure_room_rewards", new[]
            {
                cursedSkull,
                skeletalArmorReward,
                dragonHeart,
                dragonArmorReward
            });
            var bossPool = ExtendPool(Milestone28AssetGenerator.BossRewardPoolPath, "m28_boss_room_rewards", new[]
            {
                dragonHeart,
                dragonArmorReward,
                dragonToothReward
            });
            var usableCatalog = ExtendUsableCatalog(new[] { boneTotem, dragonTooth });

            AssignToGameScenes(weaponCatalog, weaponPool, standardPool, treasurePool, bossPool, usableCatalog, armorCatalog, synergyCatalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 30 armor, set weapons, set rewards, synergy catalog, and scene wiring.");
        }

        private static void RetagExistingContent()
        {
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_VitalLocket.asset", new[] { BuildTag.Defense });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_IronStitch.asset", new[] { BuildTag.Defense, BuildTag.Armor });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_FleetPin.asset", new[] { BuildTag.Fast });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_StaminaThread.asset", new[] { BuildTag.Stamina });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_BladeLesson.asset", new[] { BuildTag.Melee });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_BoltLesson.asset", new[] { BuildTag.Ranged });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_MendingCharm.asset", new[] { BuildTag.Defense });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_EchoBurst.asset", new[] { BuildTag.Magic });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_EmberCard.asset", new[] { BuildTag.Fire });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_SwiftCard.asset", new[] { BuildTag.Fast });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_MendCard.asset", new[] { BuildTag.Defense });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_CoinCache.asset", new[] { BuildTag.CoinEconomy });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_TreasureCoins.asset", new[] { BuildTag.CoinEconomy });
            RetagReward(Milestone28AssetGenerator.RewardDirectory + "/Reward_BossSigil_M28.asset", new[] { BuildTag.Ranged, BuildTag.SoulEconomy });
            RetagReward(Milestone27AssetGenerator.WeaponDirectory + "/Reward_IronCleaver.asset", new[] { BuildTag.Melee, BuildTag.Heavy });
            RetagReward(Milestone27AssetGenerator.WeaponDirectory + "/Reward_EmberBolt.asset", new[] { BuildTag.Ranged, BuildTag.Fire, BuildTag.Magic });
            RetagUsable(Milestone28AssetGenerator.RewardDirectory + "/Usable_MendingCharm.asset", new[] { BuildTag.Defense });
            RetagUsable(Milestone28AssetGenerator.RewardDirectory + "/Usable_EchoBurst.asset", new[] { BuildTag.Magic });
            RetagUsable(Milestone28AssetGenerator.RewardDirectory + "/Usable_EmberCard.asset", new[] { BuildTag.Fire });
            RetagUsable(Milestone28AssetGenerator.RewardDirectory + "/Usable_SwiftCard.asset", new[] { BuildTag.Fast });
            RetagUsable(Milestone28AssetGenerator.RewardDirectory + "/Usable_MendCard.asset", new[] { BuildTag.Defense });
        }

        private static WeaponDefinition SaveWeapon(string fileName, string weaponId, string displayName, WeaponSlot slot, WeaponCategory category, IEnumerable<BuildTag> tags, WeaponAttackDefinition light, WeaponAttackDefinition heavy)
        {
            var path = $"{SetWeaponDirectory}/{fileName}";
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(weapon, path);
            }

            weapon.Configure(weaponId, displayName, slot, category, tags, light, heavy);
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        private static ArmorDefinition SaveArmor(string fileName, string armorId, string displayName, ArmorRarity rarity, CharacterStatModifier modifier, IEnumerable<BuildTag> tags)
        {
            var path = $"{EquipmentDirectory}/{fileName}";
            var armor = AssetDatabase.LoadAssetAtPath<ArmorDefinition>(path);
            if (armor == null)
            {
                armor = ScriptableObject.CreateInstance<ArmorDefinition>();
                AssetDatabase.CreateAsset(armor, path);
            }

            armor.Configure(armorId, displayName, rarity, modifier, tags);
            EditorUtility.SetDirty(armor);
            return armor;
        }

        private static RewardDefinition SaveReward(string fileName, string rewardId, string displayName, RewardKind kind, RewardRarity rarity, int souls, int coins, IEnumerable<RewardEffect> effects, IEnumerable<BuildTag> tags)
        {
            var path = $"{SetRewardDirectory}/{fileName}";
            var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (reward == null)
            {
                reward = ScriptableObject.CreateInstance<RewardDefinition>();
                AssetDatabase.CreateAsset(reward, path);
            }

            reward.Configure(rewardId, displayName, kind, rarity, souls, coins, effects, tags);
            EditorUtility.SetDirty(reward);
            return reward;
        }

        private static UsableItemDefinition SaveUsable(string fileName, string itemId, string displayName, RewardKind kind, RewardRarity rarity, int maxCharges, bool consumeOnUse, IEnumerable<RewardEffect> effects, IEnumerable<BuildTag> tags)
        {
            var path = $"{SetRewardDirectory}/{fileName}";
            var usable = AssetDatabase.LoadAssetAtPath<UsableItemDefinition>(path);
            if (usable == null)
            {
                usable = ScriptableObject.CreateInstance<UsableItemDefinition>();
                AssetDatabase.CreateAsset(usable, path);
            }

            usable.Configure(itemId, displayName, kind, rarity, maxCharges, consumeOnUse, effects, tags);
            EditorUtility.SetDirty(usable);
            return usable;
        }

        private static RewardDefinition SaveWeaponReward(string fileName, WeaponDefinition weapon, RewardRarity rarity)
        {
            return SaveReward(fileName, weapon.WeaponId, weapon.DisplayName, RewardKind.Weapon, rarity, 0, 0, System.Array.Empty<RewardEffect>(), weapon.Tags);
        }

        private static SynergyDefinition SaveSynergy(string fileName, string synergyId, string displayName, BuildTag requiredSetTag, int requiredCategoryCount, int priority, CharacterStatModifier modifier)
        {
            var path = $"{SynergyDirectory}/{fileName}";
            var synergy = AssetDatabase.LoadAssetAtPath<SynergyDefinition>(path);
            if (synergy == null)
            {
                synergy = ScriptableObject.CreateInstance<SynergyDefinition>();
                AssetDatabase.CreateAsset(synergy, path);
            }

            synergy.Configure(synergyId, displayName, SynergyTriggerKind.SetCategoryCount, requiredSetTag, requiredCategoryCount, priority, System.Array.Empty<string>(), modifier);
            EditorUtility.SetDirty(synergy);
            return synergy;
        }

        private static ArmorCatalogDefinition SaveArmorCatalog(IEnumerable<ArmorDefinition> armors)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ArmorCatalogDefinition>(ArmorCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArmorCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, ArmorCatalogPath);
            }

            catalog.Configure("m30_armor_catalog_v1", armors);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static SynergyCatalogDefinition SaveSynergyCatalog(IEnumerable<SynergyDefinition> synergies)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SynergyCatalogDefinition>(SynergyCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SynergyCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, SynergyCatalogPath);
            }

            catalog.Configure("m30_synergy_catalog_v1", synergies);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static WeaponCatalogDefinition ExtendWeaponCatalog(IEnumerable<WeaponDefinition> additionalWeapons)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            catalog.Configure("m27_weapon_catalog_v1", catalog.Weapons.Concat(additionalWeapons));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static RewardPoolDefinition ExtendWeaponRewardPool(IEnumerable<RewardDefinition> additionalRewards)
        {
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath);
            pool.Configure("m27_rare_weapon_rewards", pool.Rewards.Concat(additionalRewards));
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static RewardPoolDefinition ExtendPool(string path, string poolId, IEnumerable<RewardDefinition> additionalRewards)
        {
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(path);
            pool.Configure(poolId, pool.Rewards.Concat(additionalRewards));
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static UsableItemCatalogDefinition ExtendUsableCatalog(IEnumerable<UsableItemDefinition> additionalUsables)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UsableItemCatalogDefinition>(Milestone28AssetGenerator.UsableItemCatalogPath);
            catalog.Configure("m28_usable_item_catalog_v1", catalog.Items.Concat(additionalUsables));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void RetagReward(string path, IEnumerable<BuildTag> tags)
        {
            var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (reward == null)
            {
                return;
            }

            reward.Configure(reward.RewardId, reward.DisplayName, reward.RewardKind, reward.Rarity, reward.Souls, reward.Coins, reward.Effects, tags);
            EditorUtility.SetDirty(reward);
        }

        private static void RetagUsable(string path, IEnumerable<BuildTag> tags)
        {
            var usable = AssetDatabase.LoadAssetAtPath<UsableItemDefinition>(path);
            if (usable == null)
            {
                return;
            }

            usable.Configure(usable.ItemId, usable.DisplayName, usable.RewardKind, usable.Rarity, usable.MaxCharges, usable.ConsumeOnUse, usable.Effects, tags);
            EditorUtility.SetDirty(usable);
        }

        private static void AssignToGameScenes(
            WeaponCatalogDefinition weaponCatalog,
            RewardPoolDefinition weaponPool,
            RewardPoolDefinition standardPool,
            RewardPoolDefinition treasurePool,
            RewardPoolDefinition bossPool,
            UsableItemCatalogDefinition usableCatalog,
            ArmorCatalogDefinition armorCatalog,
            SynergyCatalogDefinition synergyCatalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureWeaponCatalog(weaponCatalog);
                branch.ConfigureWeaponRewardPool(weaponPool);
                branch.ConfigureRewardPools(standardPool, treasurePool, bossPool);
                branch.ConfigureUsableItemCatalog(usableCatalog);
                branch.ConfigureArmorCatalog(armorCatalog);
                branch.ConfigureSynergyCatalog(synergyCatalog);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
