using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Rewards
{
    public static class ChestRewardResolver
    {
        public const string NormalChestRewardId = "standard_treasure_chest";
        public const string GoldenChestRewardId = "golden_treasure_chest";
        public const string CorruptedChestRewardId = "corrupted_treasure_chest";
        public const string SmallCoinPouchRewardId = "small_coin_pouch";
        public const string HpRefillRewardId = "hp_refill";
        public const int CorruptedChestRollPercent = 10;

        private static readonly (string id, string displayName, RewardKind kind, RewardEffect[] effects)[] GoldenCardRewards =
        {
            ("blade_lesson", "Blade Lesson", RewardKind.PassiveCard, new[] { new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1) }),
            ("bolt_lesson", "Bolt Lesson", RewardKind.PassiveCard, new[]
            {
                new RewardEffect(RewardEffectKind.RangedDamageBonus, intValue: 1),
                new RewardEffect(RewardEffectKind.AttackCooldownMultiplier, floatValue: 0.97f)
            }),
            ("ember_card", "Ember Card", RewardKind.ConsumableCard, Array.Empty<RewardEffect>()),
            ("swift_card", "Swift Card", RewardKind.ConsumableCard, Array.Empty<RewardEffect>()),
            ("mend_card", "Mend Card", RewardKind.ConsumableCard, Array.Empty<RewardEffect>())
        };

        private static readonly (string id, string displayName, RewardKind kind, RewardEffect[] effects)[] CorruptedRareRewards =
        {
            ("vital_locket", "Vital Locket", RewardKind.PassiveItem, new[]
            {
                new RewardEffect(RewardEffectKind.MaxHealthBonus, intValue: 1),
                new RewardEffect(RewardEffectKind.Heal, intValue: 1)
            }),
            ("iron_stitch", "Iron Stitch", RewardKind.PassiveItem, new[] { new RewardEffect(RewardEffectKind.DefenseBonus, intValue: 1) }),
            ("fleet_pin", "Fleet Pin", RewardKind.PassiveItem, new[] { new RewardEffect(RewardEffectKind.MoveSpeedBonus, floatValue: 0.35f) }),
            ("stamina_thread", "Stamina Thread", RewardKind.PassiveItem, new[]
            {
                new RewardEffect(RewardEffectKind.MaxStaminaBonus, floatValue: 20f),
                new RewardEffect(RewardEffectKind.StaminaRegenBonus, floatValue: 2f)
            }),
            ("blade_lesson", "Blade Lesson", RewardKind.PassiveCard, new[] { new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1) }),
            ("bolt_lesson", "Bolt Lesson", RewardKind.PassiveCard, new[]
            {
                new RewardEffect(RewardEffectKind.RangedDamageBonus, intValue: 1),
                new RewardEffect(RewardEffectKind.AttackCooldownMultiplier, floatValue: 0.97f)
            }),
            ("mending_charm", "Mending Charm", RewardKind.ActiveItem, Array.Empty<RewardEffect>()),
            ("echo_burst", "Echo Burst", RewardKind.ActiveItem, Array.Empty<RewardEffect>()),
            ("mend_card", "Mend Card", RewardKind.ConsumableCard, Array.Empty<RewardEffect>())
        };

        public static IReadOnlyList<string> CorruptedRareRewardIds => CorruptedRareRewards.Select(reward => reward.id).ToArray();

        public static RewardGrant ResolveCuratedRareReward(string branchId, int seed, string roomId, string contextSuffix)
        {
            var context = $"{branchId}|{seed}|{roomId}|{contextSuffix}";
            var reward = CorruptedRareRewards[StableHash($"{context}|rare_reward") % CorruptedRareRewards.Length];
            return new RewardGrant(roomId, reward.id, reward.displayName, reward.kind, 0, 0, reward.effects);
        }

        public static bool IsChestReward(RewardGrant grant)
        {
            return string.Equals(grant.RewardId, NormalChestRewardId, StringComparison.Ordinal) ||
                   string.Equals(grant.RewardId, GoldenChestRewardId, StringComparison.Ordinal) ||
                   string.Equals(grant.RewardId, CorruptedChestRewardId, StringComparison.Ordinal);
        }

        public static ChestKind KindForGrant(RewardGrant grant)
        {
            if (string.Equals(grant.RewardId, GoldenChestRewardId, StringComparison.Ordinal))
            {
                return ChestKind.Golden;
            }

            return string.Equals(grant.RewardId, CorruptedChestRewardId, StringComparison.Ordinal)
                ? ChestKind.Corrupted
                : ChestKind.Normal;
        }

        public static ChestRewardContents ResolveContents(string branchId, int seed, string roomId, ChestKind kind)
        {
            var context = $"{branchId}|{seed}|{roomId}|{kind}|m52_chest_contents";
            var roll = StableHash($"{context}|roll") % 100;
            if (kind == ChestKind.Normal)
            {
                if (roll < 75)
                {
                    return new ChestRewardContents(8 + StableHash($"{context}|normal_coins") % 7, default);
                }

                return new ChestRewardContents(0, HpRefillGrant($"{roomId}:normal_chest"));
            }

            if (kind == ChestKind.Corrupted)
            {
                var corruptedReward = CorruptedRareRewards[StableHash($"{context}|corrupted_reward") % CorruptedRareRewards.Length];
                return new ChestRewardContents(
                    8 + StableHash($"{context}|corrupted_coins") % 5,
                    new RewardGrant($"{roomId}:corrupted_chest", corruptedReward.id, corruptedReward.displayName, corruptedReward.kind, 0, 0, corruptedReward.effects));
            }

            if (roll < 55)
            {
                return new ChestRewardContents(15 + StableHash($"{context}|golden_coins") % 16, default);
            }

            if (roll < 75)
            {
                return new ChestRewardContents(5 + StableHash($"{context}|golden_heal_coins") % 4, HpRefillGrant($"{roomId}:golden_chest"));
            }

            var reward = GoldenCardRewards[StableHash($"{context}|golden_card") % GoldenCardRewards.Length];
            return new ChestRewardContents(
                4 + StableHash($"{context}|golden_card_coins") % 5,
                new RewardGrant($"{roomId}:golden_chest", reward.id, reward.displayName, reward.kind, 0, 0, reward.effects));
        }

        public static RewardGrant LooseCoinGrant(string roomId, int coinValue)
        {
            return new RewardGrant(roomId, SmallCoinPouchRewardId, "Loose Coins", RewardKind.Currency, 0, Math.Max(0, coinValue), Array.Empty<RewardEffect>());
        }

        public static RewardGrant HpRefillGrant(string roomId)
        {
            return new RewardGrant(roomId, HpRefillRewardId, "HP Refill", RewardKind.Heal, 0, 0, new[] { new RewardEffect(RewardEffectKind.Heal, intValue: 99) });
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= (uint)character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
