using Hollow.Persistence;
using UnityEngine;

namespace Hollow.Rewards
{
    public sealed class PlayerRunStats
    {
        public int MaxHealthBonus { get; private set; }

        public float MoveSpeedBonus { get; private set; }

        public float ShotCooldownMultiplier { get; private set; } = 1f;

        public int ProjectileDamageBonus { get; private set; }

        public int ApplyReward(RewardGrant grant)
        {
            return grant.RewardId switch
            {
                "stone_heart" => ApplyStoneHeart(),
                "quick_draw" => ApplyQuickDraw(),
                "fleet_step" => ApplyFleetStep(),
                "ember_charm" => ApplyEmberCharm(),
                _ => 0
            };
        }

        public PlayerRunStatsSaveState ToSaveState()
        {
            return new PlayerRunStatsSaveState
            {
                maxHealthBonus = MaxHealthBonus,
                moveSpeedBonus = MoveSpeedBonus,
                shotCooldownMultiplier = ShotCooldownMultiplier,
                projectileDamageBonus = ProjectileDamageBonus
            };
        }

        public static PlayerRunStats FromSaveState(PlayerRunStatsSaveState saveState)
        {
            var stats = new PlayerRunStats();
            if (saveState == null)
            {
                return stats;
            }

            stats.MaxHealthBonus = Mathf.Max(0, saveState.maxHealthBonus);
            stats.MoveSpeedBonus = Mathf.Max(0f, saveState.moveSpeedBonus);
            stats.ShotCooldownMultiplier = saveState.shotCooldownMultiplier <= 0f ? 1f : saveState.shotCooldownMultiplier;
            stats.ProjectileDamageBonus = Mathf.Max(0, saveState.projectileDamageBonus);
            return stats;
        }

        private int ApplyStoneHeart()
        {
            MaxHealthBonus += 1;
            return 1;
        }

        private int ApplyQuickDraw()
        {
            ShotCooldownMultiplier *= 0.9f;
            return 0;
        }

        private int ApplyFleetStep()
        {
            MoveSpeedBonus += 0.5f;
            return 0;
        }

        private int ApplyEmberCharm()
        {
            ProjectileDamageBonus += 1;
            return 0;
        }
    }
}
