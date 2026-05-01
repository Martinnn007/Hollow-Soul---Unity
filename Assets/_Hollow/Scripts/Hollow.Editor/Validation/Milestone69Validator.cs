using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone69Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/EquipmentLoadClass.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ShieldDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ShieldCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/EquipmentLoadResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/DamageClassification.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone69AssetGenerator.cs",
            Milestone69AssetGenerator.DocsPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 69 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M69 file: {file}");
                }
            }

            ValidateShieldCatalog(failures);
            ValidateRewardPools(failures);
            ValidateLoadRetuning(failures);
            ValidateRuntimeRules(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 69 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateShieldCatalog(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShieldCatalogDefinition>(Milestone69AssetGenerator.ShieldCatalogPath);
            if (catalog == null)
            {
                failures.Add($"Missing M69 shield catalog: {Milestone69AssetGenerator.ShieldCatalogPath}");
                return;
            }

            if (catalog.Shields.Count < 3)
            {
                failures.Add("M69 shield catalog should contain starter, medium, and heavy shields.");
            }

            if (!catalog.TryGetShield(ShieldDefinition.StarterShieldId, out var starter) || starter.LoadClass != EquipmentLoadClass.Light)
            {
                failures.Add("M69 starter shield fallback must be `starter_buckler` with Light load.");
            }

            if (!catalog.TryGetShield("iron_kite_shield", out var medium) || medium.LoadClass != EquipmentLoadClass.Medium)
            {
                failures.Add("M69 medium shield `iron_kite_shield` is missing or has the wrong load class.");
            }

            if (!catalog.TryGetShield("stone_wall_shield", out var heavy) || heavy.LoadClass != EquipmentLoadClass.Heavy)
            {
                failures.Add("M69 heavy shield `stone_wall_shield` is missing or has the wrong load class.");
            }
        }

        private static void ValidateRewardPools(List<string> failures)
        {
            var standard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone52AssetGenerator.StandardRewardPoolPath);
            var treasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone69AssetGenerator.TreasureRewardPoolPath);
            var boss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone69AssetGenerator.BossRewardPoolPath);
            if (treasure == null || boss == null)
            {
                failures.Add("M69 treasure/boss reward pools are missing.");
                return;
            }

            foreach (var id in new[] { "iron_kite_shield", "stone_wall_shield" })
            {
                if (!treasure.Rewards.Any(reward => reward != null && reward.RewardId == id && reward.RewardKind == RewardKind.Shield))
                {
                    failures.Add($"M69 treasure pool is missing shield reward `{id}`.");
                }

                if (!boss.Rewards.Any(reward => reward != null && reward.RewardId == id && reward.RewardKind == RewardKind.Shield))
                {
                    failures.Add($"M69 boss pool is missing shield reward `{id}`.");
                }

                if (standard != null && standard.Rewards.Any(reward => reward != null && reward.RewardId == id))
                {
                    failures.Add($"M69 shield reward `{id}` must not appear in standard room rewards.");
                }
            }
        }

        private static void ValidateLoadRetuning(List<string> failures)
        {
            ExpectWeaponLoad($"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_StarterBlade.asset", EquipmentLoadClass.Light, failures);
            ExpectWeaponLoad($"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_StarterBolt.asset", EquipmentLoadClass.Light, failures);
            ExpectWeaponLoad($"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_IronCleaver.asset", EquipmentLoadClass.Heavy, failures);
            ExpectWeaponLoad($"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_EmberBolt.asset", EquipmentLoadClass.Medium, failures);
            ExpectArmorLoad($"{Milestone30AssetGenerator.EquipmentDirectory}/Armor_SkeletalArmor.asset", EquipmentLoadClass.Medium, failures);
            ExpectArmorLoad($"{Milestone30AssetGenerator.EquipmentDirectory}/Armor_DragonScaleArmor.asset", EquipmentLoadClass.Heavy, failures);
        }

        private static void ExpectWeaponLoad(string path, EquipmentLoadClass expected, List<string> failures)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon != null && weapon.LoadClass != expected)
            {
                failures.Add($"{weapon.WeaponId} should be {expected} load.");
            }
        }

        private static void ExpectArmorLoad(string path, EquipmentLoadClass expected, List<string> failures)
        {
            var armor = AssetDatabase.LoadAssetAtPath<ArmorDefinition>(path);
            if (armor != null && armor.LoadClass != expected)
            {
                failures.Add($"{armor.ArmorId} should be {expected} load.");
            }
        }

        private static void ValidateRuntimeRules(List<string> failures)
        {
            if (EquipmentLoadResolver.ResolveTier(4) != EquipmentLoadTier.Light ||
                EquipmentLoadResolver.ResolveTier(7) != EquipmentLoadTier.Medium ||
                EquipmentLoadResolver.ResolveTier(10) != EquipmentLoadTier.Heavy)
            {
                failures.Add("M69 equipment load thresholds are not locked to 4-6 / 7-9 / 10-12.");
            }

            var legacy = new DamageRequest(1, null);
            if (legacy.ThreatKind != DamageThreatKind.Light || legacy.Classification.ForceClass != ImpactForceClass.Light)
            {
                failures.Add("M69 DamageRequest legacy constructor must remain Light and classify as light force.");
            }

            var strong = new DamageRequest(1, null, DamageThreatKind.StrongProjectile);
            if (strong.Classification.Delivery != DamageDelivery.Projectile || strong.Classification.ForceClass != ImpactForceClass.Heavy)
            {
                failures.Add("M69 strong projectile threat should classify as heavy projectile force.");
            }
        }
    }
}
