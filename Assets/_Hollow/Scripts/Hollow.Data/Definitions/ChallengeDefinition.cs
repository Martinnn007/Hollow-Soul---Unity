using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Challenges/Challenge Definition")]
    public sealed class ChallengeDefinition : ScriptableObject
    {
        [SerializeField] private string challengeId = "challenge";
        [SerializeField] private string displayName = "Challenge";
        [SerializeField] private int fixedRunSeed = 35001;
        [SerializeField] private string selectedCharacterId = "balanced";
        [SerializeField] private CharacterStatModifier statModifier;
        [SerializeField] private int startingCoins;
        [SerializeField] private int startingSouls;
        [SerializeField] private List<string> rules = new();

        public string ChallengeId => challengeId;

        public string DisplayName => displayName;

        public int FixedRunSeed => fixedRunSeed;

        public string SelectedCharacterId => string.IsNullOrWhiteSpace(selectedCharacterId) ? "balanced" : selectedCharacterId;

        public CharacterStatModifier StatModifier => statModifier;

        public int StartingCoins => Mathf.Max(0, startingCoins);

        public int StartingSouls => Mathf.Max(0, startingSouls);

        public IReadOnlyList<string> Rules => rules;

        public void Configure(
            string nextChallengeId,
            string nextDisplayName,
            int nextFixedRunSeed,
            string nextSelectedCharacterId,
            CharacterStatModifier nextStatModifier,
            int nextStartingCoins,
            int nextStartingSouls,
            IEnumerable<string> nextRules)
        {
            challengeId = nextChallengeId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            fixedRunSeed = nextFixedRunSeed == 0 ? 35001 : Mathf.Abs(nextFixedRunSeed);
            selectedCharacterId = string.IsNullOrWhiteSpace(nextSelectedCharacterId) ? "balanced" : nextSelectedCharacterId;
            statModifier = nextStatModifier;
            startingCoins = Mathf.Max(0, nextStartingCoins);
            startingSouls = Mathf.Max(0, nextStartingSouls);
            rules = (nextRules ?? Enumerable.Empty<string>())
                .Where(rule => !string.IsNullOrWhiteSpace(rule))
                .Distinct()
                .ToList();
        }

        public string RulesSummary => rules == null || rules.Count == 0 ? "Fixed seed challenge." : string.Join("\n", rules);
    }
}
