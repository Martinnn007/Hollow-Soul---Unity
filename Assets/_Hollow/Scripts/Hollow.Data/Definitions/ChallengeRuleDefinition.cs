using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public sealed class ChallengeRuleDefinition
    {
        [SerializeField] private ChallengeRuleKind kind;
        [SerializeField] private int intValue;
        [SerializeField] private string displayText = string.Empty;

        public ChallengeRuleDefinition()
        {
        }

        public ChallengeRuleDefinition(ChallengeRuleKind kind, int intValue = 0, string displayText = "")
        {
            Configure(kind, intValue, displayText);
        }

        public ChallengeRuleKind Kind => kind;

        public int IntValue => intValue;

        public string DisplayText => displayText ?? string.Empty;

        public void Configure(ChallengeRuleKind nextKind, int nextIntValue = 0, string nextDisplayText = "")
        {
            kind = nextKind;
            intValue = nextIntValue;
            displayText = nextDisplayText ?? string.Empty;
        }
    }
}
