namespace Hollow.Combat
{
    public static class DamageSystem
    {
        public static bool ApplyDamage(CombatantHealth target, DamageRequest request)
        {
            return target != null && target.ApplyDamage(request);
        }
    }
}
