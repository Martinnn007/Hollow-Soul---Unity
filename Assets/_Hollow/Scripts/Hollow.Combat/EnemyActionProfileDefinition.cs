using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Enemy Action Profile", fileName = "EnemyActionProfile")]
    public sealed class EnemyActionProfileDefinition : ScriptableObject
    {
        [SerializeField] private string actionId = "enemy_action";
        [SerializeField] private string displayName = "Enemy Action";
        [SerializeField] private EnemyActionCategory category = EnemyActionCategory.Body;
        [SerializeField] private EnemyActionIntent intent = EnemyActionIntent.Damage;
        [SerializeField] private EnemyActionShape shape = EnemyActionShape.ForwardArc;
        [SerializeField] private EnemyActionUsageState usageState = EnemyActionUsageState.CurrentRuntime;
        [SerializeField] private EnemyAttackProfileDefinition linkedAttackProfile;
        [SerializeField] private string linkedAttackId = string.Empty;
        [SerializeField] private bool explicitlyNonDamaging;
        [SerializeField] private float minRangeMeters;
        [SerializeField] private float idealRangeMeters = 1f;
        [SerializeField] private float maxRangeMeters = 1.5f;
        [SerializeField] private float baseWeight = 1f;
        [SerializeField] private int pressureCost = 1;
        [SerializeField] private string cooldownGroup = "default";
        [SerializeField] private EnemyIntelligenceLevel minimumIntelligence = EnemyIntelligenceLevel.Instinctive;
        [SerializeField] private List<EnemyInstinctDisposition> allowedDispositions = new()
        {
            EnemyInstinctDisposition.Predator,
            EnemyInstinctDisposition.Prey,
            EnemyInstinctDisposition.Sentinel,
            EnemyInstinctDisposition.Mindless,
            EnemyInstinctDisposition.Territorial
        };
        [SerializeField] private EnemyAwarenessState minimumAwareness = EnemyAwarenessState.Alerted;
        [SerializeField] private bool requiresFacing = true;
        [SerializeField] private float facingArcDegrees = 120f;
        [TextArea(1, 4)]
        [SerializeField] private string telegraphNote = string.Empty;
        [Range(0, 5)]
        [SerializeField] private int punishabilityRating = 2;
        [Range(0, 5)]
        [SerializeField] private int guardPressureRating = 1;
        [TextArea(1, 4)]
        [SerializeField] private string poiseBreakNote = string.Empty;
        [SerializeField] private bool parryable = true;
        [SerializeField] private bool blockable = true;
        [SerializeField] private bool dodgeable = true;
        [TextArea(1, 4)]
        [SerializeField] private string recoveryPunishNote = string.Empty;
        [SerializeField] private List<string> bestUserTags = new() { "body-only" };
        [TextArea(1, 5)]
        [SerializeField] private string notes = string.Empty;

        public string ActionId => string.IsNullOrWhiteSpace(actionId) ? "enemy_action" : actionId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ActionId : displayName;

        public EnemyActionCategory Category => category;

        public EnemyActionIntent Intent => intent;

        public EnemyActionShape Shape => shape;

        public EnemyActionUsageState UsageState => usageState;

        public EnemyAttackProfileDefinition LinkedAttackProfile => linkedAttackProfile;

        public string LinkedAttackId => linkedAttackProfile != null
            ? linkedAttackProfile.AttackId
            : linkedAttackId ?? string.Empty;

        public bool ExplicitlyNonDamaging => explicitlyNonDamaging;

        public float MinRangeMeters => Mathf.Max(0f, minRangeMeters);

        public float IdealRangeMeters => Mathf.Clamp(idealRangeMeters, MinRangeMeters, MaxRangeMeters);

        public float MaxRangeMeters => Mathf.Max(MinRangeMeters + 0.01f, maxRangeMeters);

        public float BaseWeight => Mathf.Max(0.01f, baseWeight);

        public int PressureCost => Mathf.Max(0, pressureCost);

        public string CooldownGroup => string.IsNullOrWhiteSpace(cooldownGroup) ? "default" : cooldownGroup;

        public EnemyIntelligenceLevel MinimumIntelligence => EnemyIntelligenceLevelExtensions.Clamp((int)minimumIntelligence);

        public IReadOnlyList<EnemyInstinctDisposition> AllowedDispositions
        {
            get
            {
                var safe = allowedDispositions?
                    .Select(disposition => EnemyInstinctDispositionExtensions.Clamp((int)disposition))
                    .Distinct()
                    .ToArray() ?? Array.Empty<EnemyInstinctDisposition>();
                return safe.Length > 0
                    ? safe
                    : new[]
                    {
                        EnemyInstinctDisposition.Predator,
                        EnemyInstinctDisposition.Prey,
                        EnemyInstinctDisposition.Sentinel,
                        EnemyInstinctDisposition.Mindless,
                        EnemyInstinctDisposition.Territorial
                    };
            }
        }

        public EnemyAwarenessState MinimumAwareness => minimumAwareness;

        public bool RequiresFacing => requiresFacing;

        public float FacingArcDegrees => Mathf.Clamp(facingArcDegrees, 0f, 360f);

        public string TelegraphNote => telegraphNote ?? string.Empty;

        public int PunishabilityRating => Mathf.Clamp(punishabilityRating, 0, 5);

        public int GuardPressureRating => Mathf.Clamp(guardPressureRating, 0, 5);

        public string PoiseBreakNote => poiseBreakNote ?? string.Empty;

        public bool Parryable => parryable;

        public bool Blockable => blockable;

        public bool Dodgeable => dodgeable;

        public string RecoveryPunishNote => recoveryPunishNote ?? string.Empty;

        public IReadOnlyList<string> BestUserTags
        {
            get
            {
                var safe = bestUserTags?
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Select(tag => tag.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToArray() ?? Array.Empty<string>();
                return safe.Length > 0 ? safe : new[] { "general" };
            }
        }

        public string Notes => notes ?? string.Empty;

        public bool HasLinkedAttack => linkedAttackProfile != null || !string.IsNullOrWhiteSpace(linkedAttackId);

        public void Configure(EnemyActionProfileSpec spec, EnemyAttackProfileDefinition nextLinkedAttackProfile)
        {
            actionId = string.IsNullOrWhiteSpace(spec.ActionId) ? "enemy_action" : spec.ActionId;
            displayName = string.IsNullOrWhiteSpace(spec.DisplayName) ? actionId : spec.DisplayName;
            category = spec.Category;
            intent = spec.Intent;
            shape = spec.Shape;
            usageState = spec.UsageState;
            linkedAttackProfile = nextLinkedAttackProfile;
            linkedAttackId = string.IsNullOrWhiteSpace(spec.LinkedAttackId) ? string.Empty : spec.LinkedAttackId;
            explicitlyNonDamaging = spec.ExplicitlyNonDamaging;
            minRangeMeters = Mathf.Max(0f, spec.MinRangeMeters);
            maxRangeMeters = Mathf.Max(minRangeMeters + 0.01f, spec.MaxRangeMeters);
            idealRangeMeters = Mathf.Clamp(spec.IdealRangeMeters, minRangeMeters, maxRangeMeters);
            baseWeight = Mathf.Max(0.01f, spec.BaseWeight);
            pressureCost = Mathf.Max(0, spec.PressureCost);
            cooldownGroup = string.IsNullOrWhiteSpace(spec.CooldownGroup) ? "default" : spec.CooldownGroup;
            minimumIntelligence = EnemyIntelligenceLevelExtensions.Clamp((int)spec.MinimumIntelligence);
            allowedDispositions = spec.AllowedDispositions
                .Select(disposition => EnemyInstinctDispositionExtensions.Clamp((int)disposition))
                .Distinct()
                .ToList();
            minimumAwareness = spec.MinimumAwareness;
            requiresFacing = spec.RequiresFacing;
            facingArcDegrees = Mathf.Clamp(spec.FacingArcDegrees, 0f, 360f);
            telegraphNote = spec.TelegraphNote;
            punishabilityRating = Mathf.Clamp(spec.PunishabilityRating, 0, 5);
            guardPressureRating = Mathf.Clamp(spec.GuardPressureRating, 0, 5);
            poiseBreakNote = spec.PoiseBreakNote;
            parryable = spec.Parryable;
            blockable = spec.Blockable;
            dodgeable = spec.Dodgeable;
            recoveryPunishNote = spec.RecoveryPunishNote;
            bestUserTags = spec.BestUserTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct().ToList();
            notes = spec.Notes;
        }

        public static EnemyActionProfileDefinition CreateRuntime(EnemyActionProfileSpec spec, EnemyAttackProfileDefinition linkedAttackProfile)
        {
            var profile = CreateInstance<EnemyActionProfileDefinition>();
            profile.Configure(spec, linkedAttackProfile);
            return profile;
        }
    }
}
