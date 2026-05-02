using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone70Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Combat/EnemyBodyClass.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/EnemyKnockbackResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ImpactForceClass.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone70AssetGenerator.cs",
            DocsPath
        };

        private const string DocsPath = Milestone70AssetGenerator.DocsPath;

        [MenuItem("Hollow/Validation/Run Milestone 70 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M70 file: {file}");
                }
            }

            ValidateWeaponForces(failures);
            ValidateEnemyBodies(failures);
            ValidateBossBodies(failures);
            ValidateRuntimeRules(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 70 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateWeaponForces(List<string> failures)
        {
            ExpectWeaponAttack(
                $"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_StarterBlade.asset",
                ImpactForceClass.Medium,
                0.55f,
                ImpactForceClass.Heavy,
                0.85f,
                failures);
            ExpectWeaponAttack(
                $"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_StarterBolt.asset",
                ImpactForceClass.Light,
                0.3f,
                ImpactForceClass.Medium,
                0.55f,
                failures);
        }

        private static void ExpectWeaponAttack(
            string path,
            ImpactForceClass expectedLightForce,
            float expectedLightKnockback,
            ImpactForceClass expectedHeavyForce,
            float expectedHeavyKnockback,
            List<string> failures)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon == null)
            {
                failures.Add($"Missing M70 weapon: {path}");
                return;
            }

            if (weapon.LightAttack.ImpactForceClass != expectedLightForce ||
                !Mathf.Approximately(weapon.LightAttack.KnockbackMeters, expectedLightKnockback))
            {
                failures.Add($"{weapon.WeaponId} light attack has wrong M70 force/knockback.");
            }

            if (weapon.HeavyAttack.ImpactForceClass != expectedHeavyForce ||
                !Mathf.Approximately(weapon.HeavyAttack.KnockbackMeters, expectedHeavyKnockback))
            {
                failures.Add($"{weapon.WeaponId} heavy attack has wrong M70 force/knockback.");
            }
        }

        private static void ValidateEnemyBodies(List<string> failures)
        {
            ExpectEnemyBody("Enemy_Normal.asset", EnemyBodyClass.Medium, failures);
            ExpectEnemyBody("Enemy_Flying.asset", EnemyBodyClass.Light, failures);
            ExpectEnemyBody("Enemy_Fast.asset", EnemyBodyClass.Light, failures);
            ExpectEnemyBody("Enemy_Heavy.asset", EnemyBodyClass.Heavy, failures);
            ExpectEnemyBody("Enemy_Charger.asset", EnemyBodyClass.Medium, failures);
            ExpectEnemyBody("Enemy_Turret.asset", EnemyBodyClass.Heavy, failures);
            ExpectEnemyBody("Enemy_Splitter.asset", EnemyBodyClass.Medium, failures);
            ExpectEnemyBody("Enemy_Boss.asset", EnemyBodyClass.Massive, failures);
        }

        private static void ExpectEnemyBody(string fileName, EnemyBodyClass expected, List<string> failures)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Hollow/Data/Enemies/{fileName}");
            if (enemy != null && enemy.BodyClass != expected)
            {
                failures.Add($"{enemy.SpawnKind} should be {expected} body class.");
            }
        }

        private static void ValidateBossBodies(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(Milestone53AssetGenerator.BossCatalogPath);
            if (catalog == null)
            {
                failures.Add("M70 requires the M53 boss catalog.");
                return;
            }

            foreach (var boss in catalog.Bosses.Where(boss => boss != null))
            {
                if (boss.BodyClass != EnemyBodyClass.Massive)
                {
                    failures.Add($"{boss.BossId} should use Massive body class in M70.");
                }
            }
        }

        private static void ValidateRuntimeRules(List<string> failures)
        {
            var profile = CombatFeelProfileDefinition.CreateRuntimeDefault();
            if (!Mathf.Approximately(EnemyKnockbackResolver.ResolveBodyMultiplier(EnemyBodyClass.Light, profile), 1.2f) ||
                !Mathf.Approximately(EnemyKnockbackResolver.ResolveBodyMultiplier(EnemyBodyClass.Medium, profile), 1f) ||
                !Mathf.Approximately(EnemyKnockbackResolver.ResolveBodyMultiplier(EnemyBodyClass.Heavy, profile), 0.55f) ||
                !Mathf.Approximately(EnemyKnockbackResolver.ResolveBodyMultiplier(EnemyBodyClass.Massive, profile), 0.18f))
            {
                failures.Add("M70 body-class knockback multipliers are not locked to 1.20/1.00/0.55/0.18.");
            }

            var oldConstructor = new WeaponAttackDefinition(AttackKind.Light, 1, 1f, 0f, 1f);
            if (oldConstructor.KnockbackMeters <= 0f)
            {
                failures.Add("M70 old WeaponAttackDefinition constructor should remain knockback-safe.");
            }
        }
    }
}
