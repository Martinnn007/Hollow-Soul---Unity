using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Rewards
{
    public readonly struct PlayerBuildHudCombatStats
    {
        public PlayerBuildHudCombatStats(
            int meleeLightDamage,
            int meleeHeavyDamage,
            float meleeLightAttacksPerSecond,
            int rangedLightDamage,
            int rangedHeavyDamage,
            float rangedLightAttacksPerSecond,
            float effectiveRangeMeters,
            float moveSpeedMetersPerSecond,
            int karma)
        {
            MeleeLightDamage = Mathf.Max(0, meleeLightDamage);
            MeleeHeavyDamage = Mathf.Max(0, meleeHeavyDamage);
            MeleeLightAttacksPerSecond = Mathf.Max(0f, meleeLightAttacksPerSecond);
            RangedLightDamage = Mathf.Max(0, rangedLightDamage);
            RangedHeavyDamage = Mathf.Max(0, rangedHeavyDamage);
            RangedLightAttacksPerSecond = Mathf.Max(0f, rangedLightAttacksPerSecond);
            EffectiveRangeMeters = Mathf.Max(0f, effectiveRangeMeters);
            MoveSpeedMetersPerSecond = Mathf.Max(0f, moveSpeedMetersPerSecond);
            Karma = Mathf.Clamp(karma, -3, 3);
        }

        public int MeleeLightDamage { get; }
        public int MeleeHeavyDamage { get; }
        public float MeleeLightAttacksPerSecond { get; }
        public int RangedLightDamage { get; }
        public int RangedHeavyDamage { get; }
        public float RangedLightAttacksPerSecond { get; }
        public float EffectiveRangeMeters { get; }
        public float MoveSpeedMetersPerSecond { get; }
        public int Karma { get; }

        public static PlayerBuildHudCombatStats Empty { get; } = new(0, 0, 0f, 0, 0, 0f, 0f, 0f, 0);
    }

    public static class PlayerBuildHudStatCalculator
    {
        private const float MinimumCooldownSeconds = 0.05f;

        public static PlayerBuildHudCombatStats Calculate(
            PlayerRunBuild build,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            ShieldCatalogDefinition shields,
            int karma = 0)
        {
            if (build == null)
            {
                return PlayerBuildHudCombatStats.Empty;
            }

            var derived = build.DerivedStats;
            var equipmentLoad = EquipmentLoadResolver.Resolve(build, weapons, armors, shields);
            var projectilePassives = ProjectilePassiveResolver.Resolve(build);
            var meleeWeapon = weapons != null ? weapons.Resolve(build.Equipment.MeleeWeaponId, WeaponSlot.Melee) : null;
            var rangedWeapon = weapons != null ? weapons.Resolve(build.Equipment.RangedWeaponId, WeaponSlot.Ranged) : null;
            var meleeLight = meleeWeapon != null ? meleeWeapon.LightAttack : WeaponAttackDefinition.DefaultLight(WeaponSlot.Melee);
            var meleeHeavy = meleeWeapon != null ? meleeWeapon.HeavyAttack : WeaponAttackDefinition.DefaultHeavy(WeaponSlot.Melee);
            var rangedLight = rangedWeapon != null ? rangedWeapon.LightAttack : WeaponAttackDefinition.DefaultLight(WeaponSlot.Ranged);
            var rangedHeavy = rangedWeapon != null ? rangedWeapon.HeavyAttack : WeaponAttackDefinition.DefaultHeavy(WeaponSlot.Ranged);
            var cooldownMultiplier = derived.AttackCooldownMultiplier <= 0f ? 1f : derived.AttackCooldownMultiplier;

            return new PlayerBuildHudCombatStats(
                FinalMeleeDamage(meleeLight, derived),
                FinalMeleeDamage(meleeHeavy, derived),
                AttacksPerSecond(EffectiveCooldown(meleeLight.CooldownSeconds, cooldownMultiplier)),
                FinalRangedDamage(rangedLight, derived, projectilePassives),
                FinalRangedDamage(rangedHeavy, derived, projectilePassives),
                AttacksPerSecond(EffectiveRangedLightCooldown(rangedLight.CooldownSeconds, cooldownMultiplier, projectilePassives)),
                Mathf.Max(0.1f, rangedLight.RangeMeters + derived.RangedRangeBonusMeters),
                derived.SpeedMetersPerSecond * equipmentLoad.SpeedMultiplier,
                karma);
        }

        private static int FinalMeleeDamage(WeaponAttackDefinition attack, PlayerDerivedStats derived)
        {
            return Mathf.Max(1, attack.Damage + derived.MeleeDamageBonus + derived.Strength);
        }

        private static int FinalRangedDamage(
            WeaponAttackDefinition attack,
            PlayerDerivedStats derived,
            ProjectilePassiveState projectilePassives)
        {
            return Mathf.Max(1, Mathf.RoundToInt((attack.Damage + derived.RangedDamageBonus) * projectilePassives.RangedDamageMultiplier));
        }

        private static float EffectiveRangedLightCooldown(
            float baseCooldownSeconds,
            float cooldownMultiplier,
            ProjectilePassiveState projectilePassives)
        {
            if (projectilePassives.RangedLightFireRateBonusPerSecond <= 0f)
            {
                return EffectiveCooldown(baseCooldownSeconds, cooldownMultiplier);
            }

            var baseShotsPerSecond = 1f / Mathf.Max(MinimumCooldownSeconds, baseCooldownSeconds);
            var effectiveShotsPerSecond = baseShotsPerSecond + projectilePassives.RangedLightFireRateBonusPerSecond;
            return Mathf.Max(MinimumCooldownSeconds, (1f / effectiveShotsPerSecond) * cooldownMultiplier);
        }

        private static float EffectiveCooldown(float baseCooldownSeconds, float cooldownMultiplier)
        {
            return Mathf.Max(MinimumCooldownSeconds, baseCooldownSeconds * cooldownMultiplier);
        }

        private static float AttacksPerSecond(float cooldownSeconds)
        {
            return cooldownSeconds > 0f ? 1f / cooldownSeconds : 0f;
        }
    }
}
