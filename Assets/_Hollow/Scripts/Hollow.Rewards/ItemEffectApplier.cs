using Hollow.Combat;
using UnityEngine;

namespace Hollow.Rewards
{
    public static class ItemEffectApplier
    {
        public static void ApplyToPlayer(GameObject playerObject, PlayerRunStats stats, int healAmount)
        {
            if (playerObject == null || stats == null)
            {
                return;
            }

            var health = playerObject.GetComponent<CombatantHealth>();
            if (health != null)
            {
                health.SetMaxHealthPreservingCurrent(RoomCombatController.PlayerMaxHealth + stats.MaxHealthBonus, healAmount);
            }

            var movement = playerObject.GetComponent<PlayerMovementController>();
            if (movement != null)
            {
                movement.ConfigureStats(stats.MoveSpeedBonus);
            }

            var weapon = playerObject.GetComponent<PlayerWeaponController>();
            if (weapon != null)
            {
                weapon.ConfigureStats(stats.ShotCooldownMultiplier, stats.ProjectileDamageBonus);
            }
        }
    }
}
