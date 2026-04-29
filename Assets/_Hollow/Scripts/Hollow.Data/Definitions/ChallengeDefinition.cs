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
        [SerializeField] private ChallengeRunLoadout loadout = new();
        [SerializeField] private List<ChallengeRuleDefinition> ruleDefinitions = new();
        [SerializeField] private List<string> rules = new();

        public string ChallengeId => challengeId;

        public string DisplayName => displayName;

        public int FixedRunSeed => fixedRunSeed;

        public string SelectedCharacterId => string.IsNullOrWhiteSpace(selectedCharacterId) ? "balanced" : selectedCharacterId;

        public CharacterStatModifier StatModifier => statModifier;

        public int StartingCoins => Mathf.Max(0, startingCoins);

        public int StartingSouls => Mathf.Max(0, startingSouls);

        public ChallengeRunLoadout Loadout => loadout ??= new ChallengeRunLoadout();

        public IReadOnlyList<ChallengeRuleDefinition> RuleDefinitions => ruleDefinitions ??= new List<ChallengeRuleDefinition>();

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
            Configure(
                nextChallengeId,
                nextDisplayName,
                nextFixedRunSeed,
                nextSelectedCharacterId,
                nextStatModifier,
                nextStartingCoins,
                nextStartingSouls,
                null,
                null,
                nextRules);
        }

        public void Configure(
            string nextChallengeId,
            string nextDisplayName,
            int nextFixedRunSeed,
            string nextSelectedCharacterId,
            CharacterStatModifier nextStatModifier,
            int nextStartingCoins,
            int nextStartingSouls,
            ChallengeRunLoadout nextLoadout,
            IEnumerable<ChallengeRuleDefinition> nextRuleDefinitions,
            IEnumerable<string> nextRules)
        {
            challengeId = nextChallengeId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            fixedRunSeed = nextFixedRunSeed == 0 ? 35001 : Mathf.Abs(nextFixedRunSeed);
            selectedCharacterId = string.IsNullOrWhiteSpace(nextSelectedCharacterId) ? "balanced" : nextSelectedCharacterId;
            statModifier = nextStatModifier;
            startingCoins = Mathf.Max(0, nextStartingCoins);
            startingSouls = Mathf.Max(0, nextStartingSouls);
            loadout = nextLoadout ?? new ChallengeRunLoadout();
            ruleDefinitions = (nextRuleDefinitions ?? Enumerable.Empty<ChallengeRuleDefinition>())
                .Where(rule => rule != null && rule.Kind != ChallengeRuleKind.None)
                .Select(rule => new ChallengeRuleDefinition(rule.Kind, rule.IntValue, rule.DisplayText))
                .ToList();
            rules = (nextRules ?? Enumerable.Empty<string>())
                .Where(rule => !string.IsNullOrWhiteSpace(rule))
                .Distinct()
                .ToList();
        }

        public bool HasRule(ChallengeRuleKind kind)
        {
            return RuleDefinitions.Any(rule => rule != null && rule.Kind == kind);
        }

        public int RuleIntValue(ChallengeRuleKind kind, int fallback = 0)
        {
            var rule = RuleDefinitions.FirstOrDefault(candidate => candidate != null && candidate.Kind == kind);
            return rule != null ? rule.IntValue : fallback;
        }

        public string RulesSummary => rules == null || rules.Count == 0 ? "Fixed seed challenge." : string.Join("\n", rules);
    }
}
