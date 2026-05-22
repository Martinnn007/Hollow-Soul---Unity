using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone30Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ArmorDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ArmorCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/SynergyCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/SynergyResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone30AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone30Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone30SynergyTagsTests.cs",
            "Docs/Milestone30SynergyTagsStarterBuildVariety.md",
            Milestone30AssetGenerator.ArmorCatalogPath,
            Milestone30AssetGenerator.SynergyCatalogPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 30 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M30 file: {file}");
                }
            }

            var armorCatalog = AssetDatabase.LoadAssetAtPath<ArmorCatalogDefinition>(Milestone30AssetGenerator.ArmorCatalogPath);
            var synergyCatalog = AssetDatabase.LoadAssetAtPath<SynergyCatalogDefinition>(Milestone30AssetGenerator.SynergyCatalogPath);
            var weaponCatalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            var usableCatalog = AssetDatabase.LoadAssetAtPath<UsableItemCatalogDefinition>(Milestone28AssetGenerator.UsableItemCatalogPath);
            var standardPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.StandardRewardPoolPath);
            var treasurePool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.TreasureRewardPoolPath);
            var bossPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.BossRewardPoolPath);
            var weaponPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath);

            ValidateArmorCatalog(armorCatalog, failures);
            ValidateSynergyCatalog(synergyCatalog, failures);
            ValidateWeapons(weaponCatalog, failures);
            ValidateRewardsAndUsables(standardPool, treasurePool, bossPool, weaponPool, usableCatalog, failures);
            ValidateScenes(armorCatalog, synergyCatalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 30 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateArmorCatalog(ArmorCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("M30 armor catalog is missing.");
                return;
            }

            ValidateArmor(catalog, "skeletal_armor", BuildTag.Skeletal, failures);
            ValidateArmor(catalog, "dragon_scale_armor", BuildTag.Dragon, failures);
        }

        private static void ValidateArmor(ArmorCatalogDefinition catalog, string armorId, BuildTag setTag, List<string> failures)
        {
            if (!catalog.TryGetArmor(armorId, out var armor))
            {
                failures.Add($"M30 armor catalog missing {armorId}.");
                return;
            }

            if (string.IsNullOrWhiteSpace(armor.DisplayName) || !armor.Tags.Contains(BuildTag.Armor) || !armor.Tags.Contains(setTag) || armor.StatModifier.IsEmpty)
            {
                failures.Add($"M30 armor {armorId} must have display name, Armor tag, set tag, and stat modifier.");
            }
        }

        private static void ValidateSynergyCatalog(SynergyCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("M30 synergy catalog is missing.");
                return;
            }

            ValidateSynergy(catalog, "skeletal_set", BuildTag.Skeletal, failures);
            ValidateSynergy(catalog, "dragon_set", BuildTag.Dragon, failures);
        }

        private static void ValidateSynergy(SynergyCatalogDefinition catalog, string synergyId, BuildTag setTag, List<string> failures)
        {
            if (!catalog.TryGetSynergy(synergyId, out var synergy))
            {
                failures.Add($"M30 synergy catalog missing {synergyId}.");
                return;
            }

            if (synergy.TriggerKind != SynergyTriggerKind.SetCategoryCount || synergy.RequiredSetTag != setTag || synergy.RequiredCategoryCount != 3 || synergy.StatBonus.IsEmpty)
            {
                failures.Add($"M30 synergy {synergyId} must be a 3-category {setTag} set bonus with a stat modifier.");
            }
        }

        private static void ValidateWeapons(WeaponCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("M30 weapon catalog is missing.");
                return;
            }

            ValidateWeapon(catalog, "skeletal_sword", WeaponSlot.Melee, BuildTag.Skeletal, failures);
            ValidateWeapon(catalog, "bone_pistol", WeaponSlot.Ranged, BuildTag.Skeletal, failures);
            ValidateWeapon(catalog, "dragon_fang", WeaponSlot.Melee, BuildTag.Dragon, failures);
            ValidateWeapon(catalog, "dragon_pistol", WeaponSlot.Ranged, BuildTag.Dragon, failures);
        }

        private static void ValidateWeapon(WeaponCatalogDefinition catalog, string weaponId, WeaponSlot slot, BuildTag setTag, List<string> failures)
        {
            if (!catalog.TryGetWeapon(weaponId, out var weapon) || weapon.Slot != slot || !weapon.Tags.Contains(setTag))
            {
                failures.Add($"M30 weapon catalog missing {weaponId} with {setTag} tag and {slot} slot.");
                return;
            }

            if (slot == WeaponSlot.Ranged &&
                (weapon.Category != WeaponCategory.Gun || weapon.RangedFireMode != WeaponRangedFireMode.Instant))
            {
                failures.Add($"M30 ranged weapon {weaponId} must be an instant gun.");
            }
        }

        private static void ValidateRewardsAndUsables(
            RewardPoolDefinition standardPool,
            RewardPoolDefinition treasurePool,
            RewardPoolDefinition bossPool,
            RewardPoolDefinition weaponPool,
            UsableItemCatalogDefinition usableCatalog,
            List<string> failures)
        {
            var rewards = new[] { standardPool, treasurePool, bossPool, weaponPool }
                .Where(pool => pool != null)
                .SelectMany(pool => pool.Rewards)
                .Where(reward => reward != null)
                .GroupBy(reward => reward.RewardId)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var requiredReward in new[] { "skeletal_sword", "bone_pistol", "skeletal_armor", "cursed_skull", "bone_totem", "dragon_fang", "dragon_pistol", "dragon_scale_armor", "dragon_tooth", "dragon_heart" })
            {
                if (!rewards.TryGetValue(requiredReward, out var reward) || string.IsNullOrWhiteSpace(reward.DisplayName) || reward.Tags.Count == 0)
                {
                    failures.Add($"M30 reward pool missing tagged set reward {requiredReward}.");
                }
            }

            if (!rewards.TryGetValue("skeletal_armor", out var skeletalArmorReward) || skeletalArmorReward.RewardKind != RewardKind.Armor)
            {
                failures.Add("M30 skeletal armor reward must use RewardKind.Armor.");
            }

            if (!rewards.TryGetValue("dragon_scale_armor", out var dragonArmorReward) || dragonArmorReward.RewardKind != RewardKind.Armor)
            {
                failures.Add("M30 dragon armor reward must use RewardKind.Armor.");
            }

            foreach (var usableId in new[] { "bone_totem", "dragon_tooth" })
            {
                if (usableCatalog == null || !usableCatalog.TryGet(usableId, out var usable) || usable.Tags.Count == 0 || usable.RewardKind != RewardKind.ActiveItem)
                {
                    failures.Add($"M30 usable catalog missing tagged active item {usableId}.");
                }
            }
        }

        private static void ValidateScenes(ArmorCatalogDefinition armorCatalog, SynergyCatalogDefinition synergyCatalog, List<string> failures)
        {
            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindAnyObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (branch.ArmorCatalog != armorCatalog || branch.SynergyCatalog != synergyCatalog)
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to the M30 armor/synergy catalogs.");
                }
            }
        }
    }
}
