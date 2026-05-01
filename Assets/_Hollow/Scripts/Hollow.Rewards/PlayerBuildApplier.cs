using Hollow.Combat;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Rewards
{
    public static class PlayerBuildApplier
    {
        public static void Apply(PlayerRunBuild build, GameObject playerObject, int healAmount = 0)
        {
            Apply(build, playerObject, null, healAmount);
        }

        public static void Apply(PlayerRunBuild build, GameObject playerObject, WeaponCatalogDefinition weaponCatalog, int healAmount = 0)
        {
            Apply(build, playerObject, weaponCatalog, null, null, healAmount);
        }

        public static void Apply(
            PlayerRunBuild build,
            GameObject playerObject,
            WeaponCatalogDefinition weaponCatalog,
            ArmorCatalogDefinition armorCatalog,
            ShieldCatalogDefinition shieldCatalog,
            int healAmount = 0)
        {
            if (build == null || playerObject == null)
            {
                return;
            }

            var derived = build.DerivedStats;
            var load = EquipmentLoadResolver.Resolve(build, weaponCatalog, armorCatalog, shieldCatalog);
            var stability = derived.Stability + load.ArmorStabilityBonus;
            var health = playerObject.GetComponent<CombatantHealth>();
            if (health != null)
            {
                health.SetMaxHealthPreservingCurrent(derived.MaxHealth, healAmount);
            }

            var movement = playerObject.GetComponent<PlayerMovementController>();
            if (movement != null)
            {
                movement.ConfigureDerivedStats(derived.SpeedMetersPerSecond * load.SpeedMultiplier);
            }

            var defense = playerObject.GetComponent<PlayerDefenseController>();
            if (defense != null)
            {
                defense.Configure(derived.Defense);
                defense.ConfigureStability(stability, load.ActiveGuardShieldStabilityBonus, load.GuardStaminaCostMultiplier);
            }

            var knockback = playerObject.GetComponent<CombatKnockbackReceiver>();
            if (knockback != null)
            {
                knockback.ConfigureStability(stability);
            }

            var weapon = playerObject.GetComponent<PlayerWeaponController>();
            if (weapon != null)
            {
                weapon.ConfigureBuildStats(
                    derived.AttackCooldownMultiplier,
                    derived.RangedDamageBonus,
                    derived.MeleeDamageBonus + derived.Strength,
                    derived.MaxStamina,
                    derived.StaminaRegenPerSecond,
                    build.Equipment.MeleeWeaponId,
                    build.Equipment.RangedWeaponId,
                    build.Equipment.ActiveWeaponSlot,
                    build.CurrentStamina,
                    weaponCatalog,
                    derived.MeleeRangeBonusMeters,
                    derived.RangedRangeBonusMeters,
                    load.AttackStaminaCostMultiplier);
                weapon.ConfigureProjectilePassives(ProjectilePassiveResolver.Resolve(build));
            }
        }
    }
}
