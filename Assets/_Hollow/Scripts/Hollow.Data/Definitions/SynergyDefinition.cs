using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    public enum SynergyTriggerKind
    {
        SetCategoryCount = 0,
        ExactIds = 1,
        WeaponAndItemTag = 2,
        CharacterAndItemTag = 3
    }

    [CreateAssetMenu(menuName = "Hollow/Synergies/Synergy Definition")]
    public sealed class SynergyDefinition : ScriptableObject
    {
        [SerializeField] private string synergyId;
        [SerializeField] private string displayName;
        [SerializeField] private SynergyTriggerKind triggerKind = SynergyTriggerKind.SetCategoryCount;
        [SerializeField] private BuildTag requiredSetTag;
        [SerializeField] private int requiredCategoryCount = 3;
        [SerializeField] private int priority;
        [SerializeField] private string[] requiredIds = System.Array.Empty<string>();
        [SerializeField] private CharacterStatModifier statBonus;

        public string SynergyId => synergyId;

        public string DisplayName => displayName;

        public SynergyTriggerKind TriggerKind => triggerKind;

        public BuildTag RequiredSetTag => requiredSetTag;

        public int RequiredCategoryCount => Mathf.Max(1, requiredCategoryCount);

        public int Priority => priority;

        public IReadOnlyList<string> RequiredIds => requiredIds;

        public CharacterStatModifier StatBonus => statBonus;

        public void Configure(
            string nextSynergyId,
            string nextDisplayName,
            BuildTag nextRequiredTag,
            int nextRequiredTagCount,
            IEnumerable<string> nextRequiredIds,
            PlayerBaseStats nextStatBonus)
        {
            Configure(
                nextSynergyId,
                nextDisplayName,
                SynergyTriggerKind.SetCategoryCount,
                nextRequiredTag,
                nextRequiredTagCount,
                0,
                nextRequiredIds,
                new CharacterStatModifier(
                    maxHealth: nextStatBonus.MaxHealth,
                    speed: nextStatBonus.SpeedMetersPerSecond,
                    strength: nextStatBonus.Strength,
                    maxStamina: nextStatBonus.MaxStamina,
                    staminaRegen: nextStatBonus.StaminaRegenPerSecond,
                    defense: nextStatBonus.Defense,
                    meleeDamage: nextStatBonus.MeleeDamageBonus,
                    rangedDamage: nextStatBonus.RangedDamageBonus,
                    attackCooldownMultiplier: nextStatBonus.AttackCooldownMultiplier,
                    stability: nextStatBonus.Stability));
        }

        public void Configure(
            string nextSynergyId,
            string nextDisplayName,
            SynergyTriggerKind nextTriggerKind,
            BuildTag nextRequiredSetTag,
            int nextRequiredCategoryCount,
            int nextPriority,
            IEnumerable<string> nextRequiredIds,
            CharacterStatModifier nextStatBonus)
        {
            synergyId = nextSynergyId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            triggerKind = nextTriggerKind;
            requiredSetTag = nextRequiredSetTag;
            requiredCategoryCount = Mathf.Max(1, nextRequiredCategoryCount);
            priority = nextPriority;
            requiredIds = (nextRequiredIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToArray();
            statBonus = nextStatBonus;
        }
    }
}
