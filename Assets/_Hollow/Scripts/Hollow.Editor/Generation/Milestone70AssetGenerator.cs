using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone70AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone70PlayerEnemyKnockbackBodyWeight.md";
        public const string ReportPath = "output/reports/m70_player_enemy_knockback_body_weight.md";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        [MenuItem("Hollow/Generation/Generate Milestone 70 Assets")]
        public static void Generate()
        {
            Milestone69AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");

            RetuneWeaponAttackForces();
            RetuneEnemyBodyClasses();
            RetuneBossBodyClasses();
            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 70 player-to-enemy knockback and body weight assets.");
        }

        private static void RetuneWeaponAttackForces()
        {
            RetuneWeapon(
                $"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_StarterBlade.asset",
                ImpactForceClass.Medium,
                0.55f,
                ImpactForceClass.Heavy,
                0.85f);
            RetuneWeapon(
                $"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_StarterBolt.asset",
                ImpactForceClass.Light,
                0.3f,
                ImpactForceClass.Medium,
                0.55f);
            RetuneWeapon(
                $"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_IronCleaver.asset",
                ImpactForceClass.Heavy,
                0.75f,
                ImpactForceClass.Heavy,
                1f);
            RetuneWeapon(
                $"{Milestone27AssetGenerator.WeaponDirectory}/Weapon_EmberBolt.asset",
                ImpactForceClass.Medium,
                0.45f,
                ImpactForceClass.Heavy,
                0.75f);
            RetuneWeapon(
                $"{Milestone30AssetGenerator.SetWeaponDirectory}/Weapon_SkeletalSword.asset",
                ImpactForceClass.Medium,
                0.6f,
                ImpactForceClass.Heavy,
                0.9f);
            RetuneWeapon(
                $"{Milestone30AssetGenerator.SetWeaponDirectory}/Weapon_BonePistol.asset",
                ImpactForceClass.Light,
                0.35f,
                ImpactForceClass.Medium,
                0.6f);
            RetuneWeapon(
                $"{Milestone30AssetGenerator.SetWeaponDirectory}/Weapon_DragonFang.asset",
                ImpactForceClass.Heavy,
                0.75f,
                ImpactForceClass.Heavy,
                1f);
            RetuneWeapon(
                $"{Milestone30AssetGenerator.SetWeaponDirectory}/Weapon_DragonPistol.asset",
                ImpactForceClass.Medium,
                0.45f,
                ImpactForceClass.Heavy,
                0.75f);
        }

        private static void RetuneWeapon(
            string path,
            ImpactForceClass lightForce,
            float lightKnockbackMeters,
            ImpactForceClass heavyForce,
            float heavyKnockbackMeters)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon == null)
            {
                return;
            }

            weapon.Configure(
                weapon.WeaponId,
                weapon.DisplayName,
                weapon.Slot,
                weapon.Category,
                weapon.Tags,
                WithForce(weapon.LightAttack, lightForce, lightKnockbackMeters),
                WithForce(weapon.HeavyAttack, heavyForce, heavyKnockbackMeters),
                weapon.LoadClass,
                nextIsDoubleHandedForPresentation: weapon.IsDoubleHandedForPresentation);
            EditorUtility.SetDirty(weapon);
        }

        private static WeaponAttackDefinition WithForce(
            WeaponAttackDefinition attack,
            ImpactForceClass forceClass,
            float knockbackMeters)
        {
            return new WeaponAttackDefinition(
                attack.AttackKind,
                attack.Damage,
                attack.CooldownSeconds,
                attack.StaminaCost,
                attack.RangeMeters,
                forceClass,
                knockbackMeters);
        }

        private static void RetuneEnemyBodyClasses()
        {
            RetuneEnemy("Enemy_Normal.asset", EnemyBodyClass.Medium);
            RetuneEnemy("Enemy_Flying.asset", EnemyBodyClass.Light);
            RetuneEnemy("Enemy_Fast.asset", EnemyBodyClass.Light);
            RetuneEnemy("Enemy_Heavy.asset", EnemyBodyClass.Heavy);
            RetuneEnemy("Enemy_Charger.asset", EnemyBodyClass.Medium);
            RetuneEnemy("Enemy_Turret.asset", EnemyBodyClass.Heavy);
            RetuneEnemy("Enemy_Splitter.asset", EnemyBodyClass.Medium);
            RetuneEnemy("Enemy_Boss.asset", EnemyBodyClass.Massive);
        }

        private static void RetuneEnemy(string fileName, EnemyBodyClass bodyClass)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Hollow/Data/Enemies/{fileName}");
            if (enemy == null)
            {
                return;
            }

            enemy.Configure(
                enemy.SpawnKind,
                enemy.DisplayName,
                enemy.ArchetypeId,
                enemy.BehaviorId,
                enemy.MovementMode,
                enemy.MaxHealth,
                enemy.SpeedMetersPerSecond,
                enemy.ContactDamage,
                enemy.ContactCooldownSeconds,
                enemy.RadiusMeters,
                enemy.AttackRangeMeters,
                enemy.AttackCooldownSeconds,
                enemy.ProjectileDamage,
                enemy.ProjectileSpeedMetersPerSecond,
                enemy.ChargeSpeedMetersPerSecond,
                enemy.ChargeCooldownSeconds,
                enemy.SplitSpawnKind,
                enemy.SplitCount,
                bodyClass,
                enemy.Color);
            EditorUtility.SetDirty(enemy);
        }

        private static void RetuneBossBodyClasses()
        {
            var bossCatalog = AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(Milestone53AssetGenerator.BossCatalogPath);
            foreach (var boss in bossCatalog != null ? bossCatalog.Bosses : Enumerable.Empty<BossDefinition>())
            {
                if (boss == null)
                {
                    continue;
                }

                boss.Configure(
                    boss.BossId,
                    boss.DisplayName,
                    boss.WorldBand,
                    boss.BehaviorId,
                    boss.MaxHealth,
                    boss.SpeedMetersPerSecond,
                    boss.ContactDamage,
                    boss.ContactCooldownSeconds,
                    boss.RadiusMeters,
                    boss.ProjectileSpeedMetersPerSecond,
                    boss.VisualScale,
                    boss.DebugColor,
                    boss.Arena,
                    boss.Phases,
                    boss.Attacks,
                    EnemyBodyClass.Massive);
                EditorUtility.SetDirty(boss);
            }
        }

        private static void WriteDocs()
        {
            File.WriteAllText(DocsPath, @"# M70: Player-To-Enemy Knockback + Enemy Body Weight V1

M70 makes player attacks physically move enemies using authored attack force and enemy body weight.

- `WeaponAttackDefinition` now exposes impact force and knockback distance.
- `EnemyDefinition` and `BossDefinition` now expose body class.
- Light enemies move more, medium enemies move normally, heavy enemies resist, and bosses use massive nudge-only resistance.
- Knockback is move-only: no stun, no stagger, no attack cancellation, and no save-state mutation.
- Frozen Developer Lab enemies ignore knockback.
");
        }

        private static void WriteReport()
        {
            var enemyCatalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);
            var bossCatalog = AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(Milestone53AssetGenerator.BossCatalogPath);
            File.WriteAllText(ReportPath, $@"# M70 Player-To-Enemy Knockback + Body Weight Report

- Enemy catalog: `{EnemyCatalogPath}` with {enemyCatalog?.Definitions.Count ?? 0} definitions.
- Boss catalog: `{Milestone53AssetGenerator.BossCatalogPath}` with {bossCatalog?.Bosses.Count ?? 0} bosses.
- Body multipliers: Light x1.20, Medium x1.00, Heavy x0.55, Massive x0.18.
- Starter ranged light: Light force, 0.30m.
- Starter ranged heavy: Medium force, 0.55m.
- Starter melee light: Medium force, 0.55m.
- Starter melee heavy: Heavy force, 0.85m.
- Safety: no stun, no attack interruption, no save mutation, frozen Developer Lab entities ignore knockback.
");
        }
    }
}
