namespace Hollow.Combat
{
    public static class DamageSystem
    {
        public static bool ApplyDamage(CombatantHealth target, DamageRequest request)
        {
            if (target == null || !target.ApplyDamage(request))
            {
                return false;
            }

            if (target.IsAlive && request.Feedback.HasKnockback)
            {
                var knockback = target.GetComponent<CombatKnockbackReceiver>();
                knockback?.ApplyKnockback(
                    request.Feedback.Direction,
                    request.Feedback.KnockbackMeters,
                    request.Feedback.KnockbackSeconds,
                    request.Classification);
            }

            return true;
        }
    }
}
