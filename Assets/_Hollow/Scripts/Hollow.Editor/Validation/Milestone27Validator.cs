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
    public static class Milestone27Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/WeaponCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone27AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone27Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone27WeaponModeTests.cs",
            "Docs/Milestone27WeaponModeLightHeavyAttacks.md",
            Milestone27AssetGenerator.WeaponCatalogPath,
            Milestone27AssetGenerator.WeaponRewardPoolPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 27 Validation")]
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
                    failures.Add($"Missing M27 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            var weaponPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath);
            ValidateCatalog(catalog, failures);
            ValidateWeaponRewardPool(weaponPool, failures);
            ValidateScenes(catalog, weaponPool, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 27 validation passed.");
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

        private static void ValidateCatalog(WeaponCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("M27 weapon catalog is missing.");
                return;
            }

            foreach (var weaponId in new[] { "starter_blade", "starter_bolt", "starter_bow", "iron_cleaver", "ember_bolt" })
            {
                if (!catalog.TryGetWeapon(weaponId, out var weapon) || string.IsNullOrWhiteSpace(weapon.DisplayName))
                {
                    failures.Add($"M27 catalog missing configured weapon {weaponId}.");
                    continue;
                }

                if (weapon.LightAttack.Damage <= 0 || weapon.HeavyAttack.Damage <= weapon.LightAttack.Damage || weapon.HeavyAttack.StaminaCost <= weapon.LightAttack.StaminaCost)
                {
                    failures.Add($"M27 weapon {weaponId} must have meaningful light/heavy attack tuning.");
                }

                var minimumLightCooldown = weapon.Slot == WeaponSlot.Melee ? 0.6f : 0.75f;
                var minimumHeavyCooldown = weapon.Slot == WeaponSlot.Melee ? 2.5f : 5f;
                if (weapon.LightAttack.CooldownSeconds < minimumLightCooldown || weapon.HeavyAttack.CooldownSeconds < minimumHeavyCooldown)
                {
                    failures.Add($"M27 weapon {weaponId} is tuned too fast for the readable combat baseline.");
                }
            }

            if (catalog.TryGetWeapon("starter_bow", out var starterBow))
            {
                if (starterBow.RangedFireMode != WeaponRangedFireMode.DrawAndRelease ||
                    starterBow.LightAttack.RequiredDrawSeconds < 0.95f ||
                    starterBow.HeavyAttack.RequiredDrawSeconds <= starterBow.LightAttack.RequiredDrawSeconds)
                {
                    failures.Add("starter_bow must be a draw-and-release bow with configured light/heavy draw timings.");
                }
            }

            if (catalog.WeaponsForSlot(WeaponSlot.Melee).Count < 2 || catalog.WeaponsForSlot(WeaponSlot.Ranged).Count < 2)
            {
                failures.Add("M27 catalog must contain starter and rare replacement weapons for both slots.");
            }
        }

        private static void ValidateWeaponRewardPool(RewardPoolDefinition weaponPool, List<string> failures)
        {
            if (weaponPool == null || weaponPool.Rewards.Count < 2)
            {
                failures.Add("M27 weapon reward pool must contain at least two weapon rewards.");
                return;
            }

            foreach (var reward in weaponPool.Rewards.Where(reward => reward != null))
            {
                if (reward.RewardKind != RewardKind.Weapon || (int)reward.Rarity < (int)RewardRarity.Rare)
                {
                    failures.Add($"M27 weapon pool reward {reward.RewardId} must be a rare Weapon reward.");
                }
            }
        }

        private static void ValidateScenes(WeaponCatalogDefinition catalog, RewardPoolDefinition weaponPool, List<string> failures)
        {
            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (branch.WeaponCatalog != catalog || branch.WeaponRewardPool != weaponPool)
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to M27 weapon catalog/pool.");
                }
            }
        }
    }
}
