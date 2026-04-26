using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;
using UnityEngine;

namespace Hollow.Rewards
{
    [Serializable]
    public struct RewardEffect
    {
        [SerializeField] private RewardEffectKind kind;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;

        public RewardEffect(RewardEffectKind kind, int intValue = 0, float floatValue = 0f)
        {
            this.kind = kind;
            this.intValue = intValue;
            this.floatValue = floatValue;
        }

        public RewardEffectKind Kind => kind;

        public int IntValue => intValue;

        public float FloatValue => floatValue;

        public bool IsEmpty => kind == RewardEffectKind.None;

        public RunRewardEffectSaveState ToSaveState()
        {
            return new RunRewardEffectSaveState
            {
                kind = Kind.ToString(),
                intValue = IntValue,
                floatValue = FloatValue
            };
        }

        public static RewardEffect FromSaveState(RunRewardEffectSaveState saveState)
        {
            if (saveState == null || !Enum.TryParse(saveState.kind, out RewardEffectKind parsedKind))
            {
                return new RewardEffect(RewardEffectKind.None);
            }

            return new RewardEffect(parsedKind, saveState.intValue, saveState.floatValue);
        }

        public static IReadOnlyList<RewardEffect> DefaultsForRewardId(string rewardId)
        {
            return (rewardId ?? string.Empty) switch
            {
                "stone_heart" => new[]
                {
                    new RewardEffect(RewardEffectKind.MaxHealthBonus, intValue: 1),
                    new RewardEffect(RewardEffectKind.Heal, intValue: 1)
                },
                "quick_draw" => new[] { new RewardEffect(RewardEffectKind.ShotCooldownMultiplier, floatValue: 0.9f) },
                "fleet_step" => new[] { new RewardEffect(RewardEffectKind.MoveSpeedBonus, floatValue: 0.5f) },
                "ember_charm" => new[] { new RewardEffect(RewardEffectKind.ProjectileDamageBonus, intValue: 1) },
                "heavy_spark" => new[]
                {
                    new RewardEffect(RewardEffectKind.ProjectileDamageBonus, intValue: 1),
                    new RewardEffect(RewardEffectKind.ShotCooldownMultiplier, floatValue: 1.05f)
                },
                "swift_charge" => new[]
                {
                    new RewardEffect(RewardEffectKind.MoveSpeedBonus, floatValue: 0.25f),
                    new RewardEffect(RewardEffectKind.ShotCooldownMultiplier, floatValue: 0.95f)
                },
                _ => Array.Empty<RewardEffect>()
            };
        }

        public static RewardEffect[] Clean(IEnumerable<RewardEffect> effects)
        {
            return (effects ?? Enumerable.Empty<RewardEffect>())
                .Where(effect => !effect.IsEmpty)
                .ToArray();
        }
    }
}
