using Hollow.Data.Definitions;

namespace Hollow.Combat
{
    public static class EnemyKnockbackResolver
    {
        public static float ResolveBodyMultiplier(EnemyBodyClass bodyClass, CombatFeelProfileDefinition profile)
        {
            var resolved = CombatFeelProfileDefinition.Resolve(profile);
            return bodyClass switch
            {
                EnemyBodyClass.Light => resolved.LightBodyKnockbackMultiplier,
                EnemyBodyClass.Heavy => resolved.HeavyBodyKnockbackMultiplier,
                EnemyBodyClass.Massive => resolved.MassiveBodyKnockbackMultiplier,
                _ => resolved.MediumBodyKnockbackMultiplier
            };
        }
    }
}
