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
                "m35_challenge_catalog_v1",
                new[]
                {
                    CreateRuntimeChallenge(
                        "blade_trial",
                        "Blade Trial",
                        35001,
                        "balanced",
                        new CharacterStatModifier(maxHealth: -1, meleeDamage: 1),
                        8,
                        0,
                        new[] { "Fixed seed 35001.", "+1 melee damage.", "-1 max HP.", "Start with 8 coins." }),
                    CreateRuntimeChallenge(
                        "glass_runner",
                        "Glass Runner",
                        35002,
                        "balanced",
                        new CharacterStatModifier(maxHealth: -2, speed: 0.45f, maxStamina: 10f),
                        12,
                        0,
                        new[] { "Fixed seed 35002.", "+0.45 speed.", "+10 stamina.", "-2 max HP." }),
                    CreateRuntimeChallenge(
                        "stone_oath",
                        "Stone Oath",
                        35003,
                        "heavy",
                        new CharacterStatModifier(speed: -0.25f, defense: 2, staminaRegen: -1f),
                        6,
                        0,
                        new[] { "Fixed seed 35003.", "Heavy character.", "+2 defense.", "-0.25 speed and -1 stamina regen." })
                });
            return catalog;
        }

        private static ChallengeDefinition CreateRuntimeChallenge(
            string challengeId,
            string displayName,
            int fixedSeed,
            string characterId,
            CharacterStatModifier modifier,
            int coins,
            int souls,
            IEnumerable<string> rules)
        {
            var challenge = CreateInstance<ChallengeDefinition>();
            challenge.Configure(challengeId, displayName, fixedSeed, characterId, modifier, coins, souls, rules);
            return challenge;
        }
    }
}
