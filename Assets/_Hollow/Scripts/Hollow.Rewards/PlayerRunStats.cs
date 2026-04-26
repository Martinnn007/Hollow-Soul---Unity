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
            var effects = grant.Effects != null && grant.Effects.Count > 0
                ? grant.Effects
                : RewardEffect.DefaultsForRewardId(grant.RewardId);
            var healAmount = 0;
            foreach (var effect in effects)
            {
                switch (effect.Kind)
                {
                    case RewardEffectKind.MaxHealthBonus:
                        MaxHealthBonus += Mathf.Max(0, effect.IntValue);
                        break;
                    case RewardEffectKind.Heal:
                        healAmount += Mathf.Max(0, effect.IntValue);
                        break;
                    case RewardEffectKind.MoveSpeedBonus:
                        MoveSpeedBonus += Mathf.Max(0f, effect.FloatValue);
                        break;
                    case RewardEffectKind.ShotCooldownMultiplier:
                        ShotCooldownMultiplier *= effect.FloatValue <= 0f ? 1f : effect.FloatValue;
                        break;
                    case RewardEffectKind.ProjectileDamageBonus:
                        ProjectileDamageBonus += Mathf.Max(0, effect.IntValue);
                        break;
                }
            }

            return effects.Count > 0 ? healAmount : grant.RewardId switch
            {
                "stone_heart" => ApplyStoneHeart(),
                "quick_draw" => ApplyQuickDraw(),
                "fleet_step" => ApplyFleetStep(),
                "ember_charm" => ApplyEmberCharm(),
                "boss_sigil" => 0,
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
