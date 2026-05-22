using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Challenges/Challenge Catalog")]
    public sealed class ChallengeCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId = "m35_challenge_catalog_v1";
        [SerializeField] private List<ChallengeDefinition> challenges = new();

        public string CatalogId => catalogId;

        public IReadOnlyList<ChallengeDefinition> Challenges => challenges;

        public void Configure(string nextCatalogId, IEnumerable<ChallengeDefinition> nextChallenges)
        {
            catalogId = string.IsNullOrWhiteSpace(nextCatalogId) ? "m35_challenge_catalog_v1" : nextCatalogId;
            challenges = (nextChallenges ?? Enumerable.Empty<ChallengeDefinition>())
                .Where(challenge => challenge != null && !string.IsNullOrWhiteSpace(challenge.ChallengeId))
                .GroupBy(challenge => challenge.ChallengeId)
                .Select(group => group.First())
                .OrderBy(challenge => challenge.ChallengeId)
                .ToList();
        }

        public bool TryGetChallenge(string challengeId, out ChallengeDefinition challenge)
        {
            challenge = challenges.FirstOrDefault(candidate => candidate != null && candidate.ChallengeId == challengeId);
            return challenge != null;
        }

        public ChallengeDefinition Resolve(string challengeId)
        {
            if (TryGetChallenge(challengeId, out var challenge))
            {
                return challenge;
            }

            return challenges.FirstOrDefault();
        }

        public static ChallengeCatalogDefinition CreateRuntimeDefault()
        {
            var catalog = CreateInstance<ChallengeCatalogDefinition>();
            catalog.Configure(
                "m47_challenge_catalog_v2",
                new[]
                {
                    CreateRuntimeChallenge(
                        "blade_trial",
                        "Blade Trial",
                        47001,
                        "balanced",
                        new CharacterStatModifier(maxHealth: -1, meleeDamage: 1),
                        ChallengeRunLoadout.Create(meleeWeaponId: "iron_cleaver", rangedWeaponId: "starter_pistol", consumableCardId: "ember_card"),
                        8,
                        0,
                        new[] { new ChallengeRuleDefinition(ChallengeRuleKind.BlockShops, displayText: "Shops closed.") },
                        new[] { "Fixed seed 47001.", "Melee-lean gear.", "Shops closed.", "-1 max HP, +1 melee." }),
                    CreateRuntimeChallenge(
                        "glass_runner",
                        "Glass Runner",
                        47002,
                        "balanced",
                        new CharacterStatModifier(maxHealth: -2, speed: 0.45f, maxStamina: 10f),
                        ChallengeRunLoadout.Create(meleeWeaponId: "starter_blade", rangedWeaponId: "ember_bolt", consumableCardId: "swift_card"),
                        12,
                        0,
                        new[] { new ChallengeRuleDefinition(ChallengeRuleKind.BlockHealingRewards, displayText: "Healing rewards blocked.") },
                        new[] { "Fixed seed 47002.", "+0.45 speed.", "+10 stamina.", "Healing rewards blocked." }),
                    CreateRuntimeChallenge(
                        "stone_oath",
                        "Stone Oath",
                        47003,
                        "heavy",
                        new CharacterStatModifier(speed: -0.25f, defense: 2, staminaRegen: -1f),
                        ChallengeRunLoadout.Create(meleeWeaponId: "iron_cleaver", rangedWeaponId: "starter_pistol", armorId: "dragon_scale_armor", activeItemId: "mending_charm"),
                        6,
                        0,
                        null,
                        new[] { "Fixed seed 47003.", "Heavy character.", "Defense/guard lean.", "+2 defense, slower stamina regen." }),
                    CreateRuntimeChallenge(
                        "macro_maze",
                        "Macro Maze",
                        47004,
                        "balanced",
                        new CharacterStatModifier(maxStamina: 15f),
                        ChallengeRunLoadout.Create(meleeWeaponId: "starter_blade", rangedWeaponId: "bone_pistol", consumableCardId: "swift_card"),
                        10,
                        0,
                        null,
                        new[] { "Fixed seed 47004.", "Macro traversal showcase.", "+15 stamina.", "Positioning matters." }),
                    CreateRuntimeChallenge(
                        "splitter_swarm",
                        "Splitter Swarm",
                        47005,
                        "balanced",
                        new CharacterStatModifier(rangedDamage: 1),
                        ChallengeRunLoadout.Create(meleeWeaponId: "dragon_fang", rangedWeaponId: "starter_pistol", activeItemId: "echo_burst"),
                        6,
                        0,
                        new[] { new ChallengeRuleDefinition(ChallengeRuleKind.EncounterPressureBonus, 2, "Encounter pressure +2.") },
                        new[] { "Fixed seed 47005.", "Harder encounter bands.", "+1 ranged damage.", "Echo Burst starter." }),
                    CreateRuntimeChallenge(
                        "merchants_debt",
                        "Merchant's Debt",
                        47006,
                        "balanced",
                        new CharacterStatModifier(),
                        ChallengeRunLoadout.Create(meleeWeaponId: "starter_blade", rangedWeaponId: "starter_pistol", activeItemId: "mending_charm", consumableCardId: "mend_card"),
                        2,
                        22,
                        null,
                        new[] { "Fixed seed 47006.", "Start poor in coins.", "Start with 22 souls.", "Shop economy showcase." }),
                    CreateRuntimeChallenge(
                        "small_monsters",
                        "Small Monsters",
                        47007,
                        "balanced",
                        new CharacterStatModifier(speed: 0.15f),
                        ChallengeRunLoadout.Create(meleeWeaponId: "starter_blade", rangedWeaponId: "starter_pistol", consumableCardId: "swift_card"),
                        8,
                        0,
                        new[] { new ChallengeRuleDefinition(ChallengeRuleKind.SmallMonstersOnly, displayText: "Non-boss rooms spawn only Rats and Spiders.") },
                        new[] { "Fixed seed 47007.", "Non-boss rooms spawn only Rats and Spiders.", "Boss rooms remain unchanged.", "+0.15 speed." })
                });
            return catalog;
        }

        private static ChallengeDefinition CreateRuntimeChallenge(
            string challengeId,
            string displayName,
            int fixedSeed,
            string characterId,
            CharacterStatModifier modifier,
            ChallengeRunLoadout loadout,
            int coins,
            int souls,
            IEnumerable<ChallengeRuleDefinition> ruleDefinitions,
            IEnumerable<string> rules)
        {
            var challenge = CreateInstance<ChallengeDefinition>();
            challenge.Configure(challengeId, displayName, fixedSeed, characterId, modifier, coins, souls, loadout, ruleDefinitions, rules);
            return challenge;
        }
    }
}
