using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;

namespace Hollow.Rewards
{
    public enum SynergyPieceCategory
    {
        Character = 0,
        MeleeWeapon = 1,
        RangedWeapon = 2,
        Armor = 3,
        ActiveItem = 4,
        PassiveItem = 5,
        PassiveCard = 6
    }

    public readonly struct SynergyResolution
    {
        public SynergyResolution(SynergyDefinition definition, int matchingPieceCount, int matchingCategoryCount)
        {
            Definition = definition;
            MatchingPieceCount = matchingPieceCount;
            MatchingCategoryCount = matchingCategoryCount;
        }

        public SynergyDefinition Definition { get; }

        public int MatchingPieceCount { get; }

        public int MatchingCategoryCount { get; }

        public bool IsActive => Definition != null;

        public string SynergyId => Definition != null ? Definition.SynergyId : string.Empty;

        public string DisplayName => Definition != null ? Definition.DisplayName : "None";

        public PlayerStatModifier ToModifier()
        {
            return Definition == null
                ? default
                : PlayerStatModifier.FromCharacterStatModifier($"synergy:{Definition.SynergyId}", Definition.StatBonus);
        }

        public static SynergyResolution None => new(null, 0, 0);
    }

    public static class SynergyResolver
    {
        public static SynergyResolution ResolveActiveSynergy(
            PlayerRunBuild build,
            CharacterDefinition character,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            RewardPoolDefinition rewards,
            UsableItemCatalogDefinition usables,
            SynergyCatalogDefinition synergies)
        {
            return ResolveActiveSynergy(build, character, weapons, armors, new[] { rewards }, usables, synergies);
        }

        public static SynergyResolution ResolveActiveSynergy(
            PlayerRunBuild build,
            CharacterDefinition character,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            IEnumerable<RewardPoolDefinition> rewardPools,
            UsableItemCatalogDefinition usables,
            SynergyCatalogDefinition synergies)
        {
            if (build == null || synergies == null || synergies.Synergies.Count == 0)
            {
                return SynergyResolution.None;
            }

            var pieces = CollectPieces(build, character, weapons, armors, rewardPools, usables);
            var active = new List<SynergyResolution>();
            foreach (var synergy in synergies.Synergies.Where(synergy => synergy != null))
            {
                if (synergy.TriggerKind != SynergyTriggerKind.SetCategoryCount || synergy.RequiredSetTag == BuildTag.None)
                {
                    continue;
                }

                var matching = pieces
                    .Where(piece => piece.HasTag(synergy.RequiredSetTag))
                    .ToArray();
                if (!RequiredIdsMatch(synergy, matching))
                {
                    continue;
                }

                var categoryCount = matching.Select(piece => piece.Category).Distinct().Count();
                if (categoryCount >= synergy.RequiredCategoryCount)
                {
                    active.Add(new SynergyResolution(synergy, matching.Length, categoryCount));
                }
            }

            return active
                .OrderByDescending(result => result.MatchingPieceCount)
                .ThenByDescending(result => result.Definition.Priority)
                .ThenBy(result => result.Definition.SynergyId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static IReadOnlyList<SynergyPiece> CollectPieces(
            PlayerRunBuild build,
            CharacterDefinition character,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            IEnumerable<RewardPoolDefinition> rewardPools,
            UsableItemCatalogDefinition usables)
        {
            var pieces = new List<SynergyPiece>();
            if (character != null)
            {
                pieces.Add(new SynergyPiece(character.CharacterId, SynergyPieceCategory.Character, character.Tags));
            }

            if (weapons != null && weapons.TryGetWeapon(build.Equipment.MeleeWeaponId, out var melee))
            {
                pieces.Add(new SynergyPiece(melee.WeaponId, SynergyPieceCategory.MeleeWeapon, melee.Tags));
            }

            if (weapons != null && weapons.TryGetWeapon(build.Equipment.RangedWeaponId, out var ranged))
            {
                pieces.Add(new SynergyPiece(ranged.WeaponId, SynergyPieceCategory.RangedWeapon, ranged.Tags));
            }

            if (armors != null && armors.TryGetArmor(build.Equipment.ArmorId, out var armor))
            {
                pieces.Add(new SynergyPiece(armor.ArmorId, SynergyPieceCategory.Armor, armor.Tags));
            }

            if (usables != null && usables.TryGet(build.Equipment.ActiveItemId, out var activeItem))
            {
                pieces.Add(new SynergyPiece(activeItem.ItemId, SynergyPieceCategory.ActiveItem, activeItem.Tags));
            }

            var rewardLookup = BuildRewardLookup(rewardPools);
            foreach (var passiveId in build.Inventory.PassiveItemIds)
            {
                if (rewardLookup.TryGetValue(passiveId, out var reward))
                {
                    pieces.Add(new SynergyPiece(reward.RewardId, SynergyPieceCategory.PassiveItem, reward.Tags));
                }
            }

            foreach (var passiveCardId in build.Inventory.PassiveCardIds)
            {
                if (rewardLookup.TryGetValue(passiveCardId, out var reward))
                {
                    pieces.Add(new SynergyPiece(reward.RewardId, SynergyPieceCategory.PassiveCard, reward.Tags));
                }
            }

            return pieces;
        }

        private static Dictionary<string, RewardDefinition> BuildRewardLookup(IEnumerable<RewardPoolDefinition> rewardPools)
        {
            return (rewardPools ?? Enumerable.Empty<RewardPoolDefinition>())
                .Where(pool => pool != null)
                .SelectMany(pool => pool.Rewards)
                .Where(reward => reward != null && !string.IsNullOrWhiteSpace(reward.RewardId))
                .GroupBy(reward => reward.RewardId)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private static bool RequiredIdsMatch(SynergyDefinition synergy, IReadOnlyList<SynergyPiece> matchingPieces)
        {
            if (synergy.RequiredIds == null || synergy.RequiredIds.Count == 0)
            {
                return true;
            }

            var availableIds = new HashSet<string>(matchingPieces.Select(piece => piece.Id), StringComparer.Ordinal);
            return synergy.RequiredIds.All(id => availableIds.Contains(id));
        }

        private readonly struct SynergyPiece
        {
            private readonly IReadOnlyList<BuildTag> tags;

            public SynergyPiece(string id, SynergyPieceCategory category, IReadOnlyList<BuildTag> tags)
            {
                Id = id ?? string.Empty;
                Category = category;
                this.tags = tags ?? Array.Empty<BuildTag>();
            }

            public string Id { get; }

            public SynergyPieceCategory Category { get; }

            public bool HasTag(BuildTag tag)
            {
                return tags.Contains(tag);
            }
        }
    }
}
