using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public readonly struct EnemyActionProfileSpec
    {
        private static readonly EnemyInstinctDisposition[] DefaultDispositions =
        {
            EnemyInstinctDisposition.Predator,
            EnemyInstinctDisposition.Prey,
            EnemyInstinctDisposition.Sentinel,
            EnemyInstinctDisposition.Mindless,
            EnemyInstinctDisposition.Territorial
        };

        public EnemyActionProfileSpec(
            string ownerId,
            bool isBoss,
            string actionId,
            string displayName,
            EnemyActionCategory category,
            EnemyActionIntent intent,
            EnemyActionShape shape,
            EnemyActionUsageState usageState,
            string linkedAttackId,
            bool explicitlyNonDamaging,
            float minRangeMeters,
            float idealRangeMeters,
            float maxRangeMeters,
            float baseWeight,
            int pressureCost,
            string cooldownGroup,
            EnemyIntelligenceLevel minimumIntelligence,
            IEnumerable<EnemyInstinctDisposition> allowedDispositions,
            EnemyAwarenessState minimumAwareness,
            bool requiresFacing,
            float facingArcDegrees,
            string telegraphNote,
            int punishabilityRating,
            int guardPressureRating,
            string poiseBreakNote,
            bool parryable,
            bool blockable,
            bool dodgeable,
            string recoveryPunishNote,
            IEnumerable<string> bestUserTags,
            string notes)
        {
            OwnerId = ownerId ?? string.Empty;
            IsBoss = isBoss;
            ActionId = actionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Category = category;
            Intent = intent;
            Shape = shape;
            UsageState = usageState;
            LinkedAttackId = linkedAttackId ?? string.Empty;
            ExplicitlyNonDamaging = explicitlyNonDamaging;
            MinRangeMeters = minRangeMeters;
            IdealRangeMeters = idealRangeMeters;
            MaxRangeMeters = maxRangeMeters;
            BaseWeight = baseWeight;
            PressureCost = pressureCost;
            CooldownGroup = cooldownGroup ?? string.Empty;
            MinimumIntelligence = EnemyIntelligenceLevelExtensions.Clamp((int)minimumIntelligence);
            AllowedDispositions = SanitizeDispositions(allowedDispositions);
            MinimumAwareness = minimumAwareness;
            RequiresFacing = requiresFacing;
            FacingArcDegrees = facingArcDegrees;
            TelegraphNote = telegraphNote ?? string.Empty;
            PunishabilityRating = punishabilityRating;
            GuardPressureRating = guardPressureRating;
            PoiseBreakNote = poiseBreakNote ?? string.Empty;
            Parryable = parryable;
            Blockable = blockable;
            Dodgeable = dodgeable;
            RecoveryPunishNote = recoveryPunishNote ?? string.Empty;
            BestUserTags = SanitizeTags(bestUserTags);
            Notes = notes ?? string.Empty;
        }

        public string OwnerId { get; }

        public bool IsBoss { get; }

        public string ActionId { get; }

        public string DisplayName { get; }

        public EnemyActionCategory Category { get; }

        public EnemyActionIntent Intent { get; }

        public EnemyActionShape Shape { get; }

        public EnemyActionUsageState UsageState { get; }

        public string LinkedAttackId { get; }

        public bool ExplicitlyNonDamaging { get; }

        public float MinRangeMeters { get; }

        public float IdealRangeMeters { get; }

        public float MaxRangeMeters { get; }

        public float BaseWeight { get; }

        public int PressureCost { get; }

        public string CooldownGroup { get; }

        public EnemyIntelligenceLevel MinimumIntelligence { get; }

        public IReadOnlyList<EnemyInstinctDisposition> AllowedDispositions { get; }

        public EnemyAwarenessState MinimumAwareness { get; }

        public bool RequiresFacing { get; }

        public float FacingArcDegrees { get; }

        public string TelegraphNote { get; }

        public int PunishabilityRating { get; }

        public int GuardPressureRating { get; }

        public string PoiseBreakNote { get; }

        public bool Parryable { get; }

        public bool Blockable { get; }

        public bool Dodgeable { get; }

        public string RecoveryPunishNote { get; }

        public IReadOnlyList<string> BestUserTags { get; }

        public string Notes { get; }

        public bool HasLinkedAttack => !string.IsNullOrWhiteSpace(LinkedAttackId);

        public string AssetName => $"{AssetPrefix}_{Sanitize(OwnerId)}_{Sanitize(ActionId)}.asset";

        private string AssetPrefix => UsageState == EnemyActionUsageState.LibraryTemplate ? "Template" : IsBoss ? "Boss" : "Enemy";

        private static IReadOnlyList<EnemyInstinctDisposition> SanitizeDispositions(IEnumerable<EnemyInstinctDisposition> dispositions)
        {
            var safe = dispositions?
                .Select(disposition => EnemyInstinctDispositionExtensions.Clamp((int)disposition))
                .Distinct()
                .ToArray();
            return safe != null && safe.Length > 0 ? safe : DefaultDispositions;
        }

        private static IReadOnlyList<string> SanitizeTags(IEnumerable<string> tags)
        {
            var safe = tags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return safe != null && safe.Length > 0 ? safe : new[] { "general" };
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "action";
            }

            var chars = value.Trim().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}
