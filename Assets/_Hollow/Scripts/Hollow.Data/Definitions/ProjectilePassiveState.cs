using UnityEngine;

namespace Hollow.Data.Definitions
{
    public readonly struct ProjectilePassiveState
    {
        public ProjectilePassiveState(
            ProjectilePatternKind patternKind,
            float rangedDamageMultiplier,
            float rangedLightFireRateBonusPerSecond,
            ProjectileVisualStyle visualStyle)
        {
            PatternKind = patternKind == 0 ? ProjectilePatternKind.Single : patternKind;
            RangedDamageMultiplier = Mathf.Max(1f, rangedDamageMultiplier);
            RangedLightFireRateBonusPerSecond = Mathf.Max(0f, rangedLightFireRateBonusPerSecond);
            VisualStyle = visualStyle;
        }

        public ProjectilePatternKind PatternKind { get; }

        public float RangedDamageMultiplier { get; }

        public float RangedLightFireRateBonusPerSecond { get; }

        public ProjectileVisualStyle VisualStyle { get; }

        public static ProjectilePassiveState Default { get; } = new(
            ProjectilePatternKind.Single,
            1f,
            0f,
            ProjectileVisualStyle.Default);
    }
}
