using System;
using System.Linq;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Rewards
{
    public static class ProjectilePassiveResolver
    {
        public const string DoubleBarrelId = "double_barrel";
        public const string TripleShotId = "triple_shot";
        public const string QuadShotId = "quad_shot";
        public const string PowerUpId = "power_up";
        public const string FireRateUpId = "fire_rate_up";
        public const int FireRateUpMaxStacks = 3;

        public static ProjectilePassiveState Resolve(PlayerRunBuild build)
        {
            if (build?.Inventory == null)
            {
                return ProjectilePassiveState.Default;
            }

            var pattern = ResolvePattern(build);
            var hasPowerUp = build.Inventory.PassiveItemCount(PowerUpId) > 0;
            var fireRateStacks = Mathf.Min(FireRateUpMaxStacks, build.Inventory.PassiveItemCount(FireRateUpId));
            return new ProjectilePassiveState(
                pattern,
                hasPowerUp ? 2f : 1f,
                fireRateStacks,
                hasPowerUp ? ProjectileVisualStyle.RedPower : ProjectileVisualStyle.Default);
        }

        public static int MaxStacksForReward(string rewardId)
        {
            return string.Equals(rewardId, FireRateUpId, StringComparison.Ordinal)
                ? FireRateUpMaxStacks
                : 1;
        }

        public static bool IsM54ProjectilePassive(string rewardId)
        {
            return AllProjectilePassiveIds.Contains(rewardId ?? string.Empty);
        }

        public static readonly string[] AllProjectilePassiveIds =
        {
            DoubleBarrelId,
            TripleShotId,
            QuadShotId,
            PowerUpId,
            FireRateUpId
        };

        private static ProjectilePatternKind ResolvePattern(PlayerRunBuild build)
        {
            if (build.Inventory.PassiveItemCount(QuadShotId) > 0)
            {
                return ProjectilePatternKind.QuadShot;
            }

            if (build.Inventory.PassiveItemCount(TripleShotId) > 0)
            {
                return ProjectilePatternKind.TripleShot;
            }

            return build.Inventory.PassiveItemCount(DoubleBarrelId) > 0
                ? ProjectilePatternKind.DoubleBarrel
                : ProjectilePatternKind.Single;
        }
    }
}
