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
            if (build == null || playerObject == null)
            {
                return;
            }

            var derived = build.DerivedStats;
            var health = playerObject.GetComponent<CombatantHealth>();
            if (health != null)
            {
                health.SetMaxHealthPreservingCurrent(derived.MaxHealth, healAmount);
            }

            var movement = playerObject.GetComponent<PlayerMovementController>();
            if (movement != null)
            {
                movement.ConfigureDerivedStats(derived.SpeedMetersPerSecond);
            }

            var defense = playerObject.GetComponent<PlayerDefenseController>();
            if (defense != null)
            {
                defense.Configure(derived.Defense);
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
                    derived.RangedRangeBonusMeters);
            }
        }
    }
}
