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

            PlayerBuildApplier.Apply(PlayerRunBuild.FromLegacy(stats, null), playerObject, healAmount);
        }
    }
}
