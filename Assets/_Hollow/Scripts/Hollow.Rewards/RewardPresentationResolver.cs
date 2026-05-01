using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Rewards
{
    public static class RewardPresentationResolver
    {
        public static PickupRevealModel CreateReveal(
            int sequence,
            RewardGrant grant,
            RunEconomy economy,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            UsableItemCatalogDefinition usables,
            IEnumerable<RewardPoolDefinition> rewardPools,
            string replacementText = "")
        {
            return CreateReveal(sequence, grant, economy, weapons, armors, null, usables, rewardPools, replacementText);
        }

        public static PickupRevealModel CreateReveal(
            int sequence,
            RewardGrant grant,
            RunEconomy economy,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            ShieldCatalogDefinition shields,
            UsableItemCatalogDefinition usables,
            IEnumerable<RewardPoolDefinition> rewardPools,
            string replacementText = "")
        {
            var info = ResolveRewardInfo(grant, weapons, armors, shields, usables, rewardPools);
            var currency = CurrencyText(grant, economy);
            var effect = string.IsNullOrWhiteSpace(currency) ? info.EffectText : $"{info.EffectText} {currency}".Trim();
            return new PickupRevealModel(
                sequence,
                info.DisplayName,
                info.Category,
                effect,
                info.Rarity.ToString(),
                info.Glyph,
                RarityColor(info.Rarity),
                replacementText,
                string.IsNullOrWhiteSpace(replacementText) ? $"Picked up {info.DisplayName}" : $"{info.DisplayName} equipped");
        }

        public static RewardPresentationInfo ResolveRewardInfo(
            RewardGrant grant,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            UsableItemCatalogDefinition usables,
            IEnumerable<RewardPoolDefinition> rewardPools)
        {
            return ResolveRewardInfo(grant, weapons, armors, null, usables, rewardPools);
        }

        public static RewardPresentationInfo ResolveRewardInfo(
            RewardGrant grant,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            ShieldCatalogDefinition shields,
            UsableItemCatalogDefinition usables,
            IEnumerable<RewardPoolDefinition> rewardPools)
        {
            var rewardDefinition = FindRewardDefinition(grant.RewardId, rewardPools);
            var displayName = !string.IsNullOrWhiteSpace(rewardDefinition?.DisplayName) ? rewardDefinition.DisplayName : grant.DisplayName;
            var rarity = rewardDefinition != null ? rewardDefinition.Rarity : DefaultRarityFor(grant.RewardKind);
            var effects = rewardDefinition != null ? rewardDefinition.Effects : grant.Effects;

            if (grant.RewardKind == RewardKind.Weapon && weapons != null && weapons.TryGetWeapon(grant.RewardId, out var weapon))
            {
                displayName = weapon.DisplayName;
                return new RewardPresentationInfo(displayName, weapon.Slot == WeaponSlot.Melee ? "Melee Weapon" : "Ranged Weapon", "W", rarity, WeaponEffectText(weapon));
            }

            if (grant.RewardKind == RewardKind.Armor && armors != null && armors.TryGetArmor(grant.RewardId, out var armor))
            {
                displayName = armor.DisplayName;
                rarity = ArmorRarityToRewardRarity(armor.Rarity);
                return new RewardPresentationInfo(displayName, "Armor", "A", rarity, ArmorEffectText(armor));
            }

            if (grant.RewardKind == RewardKind.Shield && shields != null && shields.TryGetShield(grant.RewardId, out var shield))
            {
                displayName = shield.DisplayName;
                rarity = ArmorRarityToRewardRarity(shield.Rarity);
                return new RewardPresentationInfo(displayName, "Shield", "S", rarity, ShieldEffectText(shield));
            }

            if ((grant.RewardKind == RewardKind.ActiveItem || grant.RewardKind == RewardKind.ConsumableCard) &&
                usables != null &&
                usables.TryGet(grant.RewardId, out var usable))
            {
                displayName = usable.DisplayName;
                return new RewardPresentationInfo(displayName, CategoryFor(usable.RewardKind), GlyphFor(usable.RewardKind), usable.Rarity, EffectsText(usable.Effects));
            }

            return new RewardPresentationInfo(
                string.IsNullOrWhiteSpace(displayName) ? grant.RewardId : displayName,
                CategoryFor(grant.RewardKind),
                GlyphFor(grant.RewardKind),
                rarity,
                EffectsText(effects, grant));
        }

        public static string ResolveName(
            RewardKind kind,
            string id,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            UsableItemCatalogDefinition usables,
            IEnumerable<RewardPoolDefinition> rewardPools)
        {
            return ResolveName(kind, id, weapons, armors, null, usables, rewardPools);
        }

        public static string ResolveName(
            RewardKind kind,
            string id,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            ShieldCatalogDefinition shields,
            UsableItemCatalogDefinition usables,
            IEnumerable<RewardPoolDefinition> rewardPools)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "None";
            }

            if (kind == RewardKind.Weapon && weapons != null && weapons.TryGetWeapon(id, out var weapon))
            {
                return weapon.DisplayName;
            }

            if (kind == RewardKind.Armor && armors != null && armors.TryGetArmor(id, out var armor))
            {
                return armor.DisplayName;
            }

            if (kind == RewardKind.Shield && shields != null && shields.TryGetShield(id, out var shield))
            {
                return shield.DisplayName;
            }

            if ((kind == RewardKind.ActiveItem || kind == RewardKind.ConsumableCard) && usables != null && usables.TryGet(id, out var usable))
            {
                return usable.DisplayName;
            }

            var reward = FindRewardDefinition(id, rewardPools);
            return !string.IsNullOrWhiteSpace(reward?.DisplayName) ? reward.DisplayName : id;
        }

        public static Color RarityColor(RewardRarity rarity)
        {
            return rarity switch
            {
                RewardRarity.Uncommon => new Color(0.42f, 0.9f, 0.48f, 1f),
                RewardRarity.Rare => new Color(0.35f, 0.62f, 1f, 1f),
                RewardRarity.Treasure => new Color(1f, 0.82f, 0.25f, 1f),
                RewardRarity.Boss => new Color(1f, 0.32f, 0.26f, 1f),
                RewardRarity.Epic => new Color(0.72f, 0.38f, 1f, 1f),
                RewardRarity.Legendary => new Color(1f, 0.58f, 0.16f, 1f),
                _ => new Color(0.82f, 0.84f, 0.82f, 1f)
            };
        }

        private static RewardDefinition FindRewardDefinition(string rewardId, IEnumerable<RewardPoolDefinition> rewardPools)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return null;
            }

            foreach (var pool in rewardPools ?? Enumerable.Empty<RewardPoolDefinition>())
            {
                var reward = pool?.Rewards?.FirstOrDefault(candidate => candidate != null && candidate.RewardId == rewardId);
                if (reward != null)
                {
                    return reward;
                }
            }

            return null;
        }

        private static string CategoryFor(RewardKind kind)
        {
            return kind switch
            {
                RewardKind.PassiveItem => "Passive Item",
                RewardKind.Card => "Passive Card",
                RewardKind.PassiveCard => "Passive Card",
                RewardKind.ActiveItem => "Active Item",
                RewardKind.ConsumableCard => "Consumable Card",
                RewardKind.Weapon => "Weapon",
                RewardKind.Armor => "Armor",
                RewardKind.Shield => "Shield",
                RewardKind.Currency => "Currency",
                RewardKind.Heal => "Heal",
                _ => "Reward"
            };
        }

        private static string GlyphFor(RewardKind kind)
        {
            return kind switch
            {
                RewardKind.PassiveItem => "P",
                RewardKind.Card => "C",
                RewardKind.PassiveCard => "C",
                RewardKind.ActiveItem => "I",
                RewardKind.ConsumableCard => "K",
                RewardKind.Weapon => "W",
                RewardKind.Armor => "A",
                RewardKind.Shield => "S",
                RewardKind.Currency => "$",
                RewardKind.Heal => "+",
                _ => "*"
            };
        }

        private static RewardRarity DefaultRarityFor(RewardKind kind)
        {
            return kind == RewardKind.Weapon ? RewardRarity.Rare : RewardRarity.Common;
        }

        private static RewardRarity ArmorRarityToRewardRarity(ArmorRarity rarity)
        {
            return rarity switch
            {
                ArmorRarity.Uncommon => RewardRarity.Uncommon,
                ArmorRarity.Rare => RewardRarity.Rare,
                ArmorRarity.Epic => RewardRarity.Epic,
                ArmorRarity.Legendary => RewardRarity.Legendary,
                _ => RewardRarity.Common
            };
        }

        private static string EffectsText(IReadOnlyList<RewardEffect> effects, RewardGrant grant = default)
        {
            var text = (effects ?? System.Array.Empty<RewardEffect>())
                .Where(effect => !effect.IsEmpty)
                .Select(EffectText)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            if (text.Length > 0)
            {
                return string.Join(", ", text);
            }

            return grant.RewardKind switch
            {
                RewardKind.Currency => "Currency",
                RewardKind.Heal => "Restores health",
                RewardKind.Weapon => "Replaces weapon slot",
                RewardKind.Armor => "Replaces armor",
                RewardKind.Shield => "Replaces shield",
                RewardKind.ActiveItem => "Replaces active item",
                RewardKind.ConsumableCard => "Replaces card",
                RewardKind.PassiveItem => "Passive bonus",
                RewardKind.Card or RewardKind.PassiveCard => "Passive card bonus",
                _ => "Reward"
            };
        }

        private static string EffectText(RewardEffect effect)
        {
            return effect.Kind switch
            {
                RewardEffectKind.MaxHealthBonus => $"+{effect.IntValue} max HP",
                RewardEffectKind.Heal => $"Heal +{effect.IntValue}",
                RewardEffectKind.MoveSpeedBonus => $"+{effect.FloatValue:0.##} speed",
                RewardEffectKind.ShotCooldownMultiplier => $"Shots x{effect.FloatValue:0.##}",
                RewardEffectKind.ProjectileDamageBonus => $"+{effect.IntValue} projectile damage",
                RewardEffectKind.ProjectileSpeedBonus => $"+{effect.FloatValue:0.##} projectile speed",
                RewardEffectKind.PlayerContactDamageResist => $"+{effect.IntValue} contact resist",
                RewardEffectKind.StrengthBonus => $"+{effect.IntValue} strength",
                RewardEffectKind.MaxStaminaBonus => $"+{effect.FloatValue:0} stamina",
                RewardEffectKind.StaminaRegenBonus => $"+{effect.FloatValue:0.#} stamina regen",
                RewardEffectKind.DefenseBonus => $"+{effect.IntValue} defense",
                RewardEffectKind.StabilityBonus => $"+{effect.IntValue} stability",
                RewardEffectKind.MeleeDamageBonus => $"+{effect.IntValue} melee damage",
                RewardEffectKind.RangedDamageBonus => $"+{effect.IntValue} ranged damage",
                RewardEffectKind.AttackCooldownMultiplier => $"Cooldown x{effect.FloatValue:0.##}",
                RewardEffectKind.Coins => $"+{effect.IntValue} coins",
                RewardEffectKind.MeleeRangeBonusMeters => $"+{effect.FloatValue:0.##}m melee range",
                RewardEffectKind.RangedRangeBonusMeters => $"+{effect.FloatValue:0.##}m ranged range",
                RewardEffectKind.ProjectilePatternRank => effect.IntValue switch
                {
                    4 => "Quad-shot pattern",
                    3 => "Triple-shot pattern",
                    2 => "Double-barrel pattern",
                    _ => "Projectile pattern"
                },
                RewardEffectKind.RangedDamageMultiplier => $"Ranged damage x{effect.FloatValue:0.##}",
                RewardEffectKind.RangedLightFireRateBonusPerSecond => $"+{effect.FloatValue:0.##}/s ranged light fire rate",
                _ => string.Empty
            };
        }

        private static string WeaponEffectText(WeaponDefinition weapon)
        {
            return weapon == null
                ? "Weapon"
                : $"Light {weapon.LightAttack.Damage} dmg / Heavy {weapon.HeavyAttack.Damage} dmg";
        }

        private static string ArmorEffectText(ArmorDefinition armor)
        {
            if (armor == null || armor.StatModifier.IsEmpty)
            {
                return "Armor";
            }

            var modifier = armor.StatModifier;
            var lines = new List<string>();
            if (modifier.Defense != 0) lines.Add($"+{modifier.Defense} defense");
            if (modifier.Speed != 0f) lines.Add($"{modifier.Speed:+0.#;-0.#} speed");
            if (modifier.MaxHealth != 0) lines.Add($"+{modifier.MaxHealth} max HP");
            if (modifier.MeleeDamage != 0) lines.Add($"+{modifier.MeleeDamage} melee");
            if (modifier.RangedDamage != 0) lines.Add($"+{modifier.RangedDamage} ranged");
            if (modifier.Stability != 0) lines.Add($"+{modifier.Stability} stability");
            return lines.Count == 0 ? "Armor" : string.Join(", ", lines);
        }

        private static string ShieldEffectText(ShieldDefinition shield)
        {
            if (shield == null)
            {
                return "Shield";
            }

            var load = shield.LoadClass == EquipmentLoadClass.Light ? "Small" : shield.LoadClass.ToString();
            return $"{load} shield, +{EquipmentLoadResolver.Score(shield.LoadClass)} load";
        }

        private static string CurrencyText(RewardGrant grant, RunEconomy economy)
        {
            if (grant.Souls <= 0 && grant.Coins <= 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (grant.Souls > 0)
            {
                parts.Add($"+{grant.Souls} souls ({economy?.RunSouls ?? 0})");
            }

            if (grant.Coins > 0)
            {
                parts.Add($"+{grant.Coins} coins ({economy?.RunCoins ?? 0})");
            }

            return string.Join(", ", parts);
        }
    }

    public readonly struct RewardPresentationInfo
    {
        public RewardPresentationInfo(string displayName, string category, string glyph, RewardRarity rarity, string effectText)
        {
            DisplayName = displayName ?? string.Empty;
            Category = category ?? string.Empty;
            Glyph = glyph ?? string.Empty;
            Rarity = rarity;
            EffectText = effectText ?? string.Empty;
        }

        public string DisplayName { get; }
        public string Category { get; }
        public string Glyph { get; }
        public RewardRarity Rarity { get; }
        public string EffectText { get; }
    }
}
