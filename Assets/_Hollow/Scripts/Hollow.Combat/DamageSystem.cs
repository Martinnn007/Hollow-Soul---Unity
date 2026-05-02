namespace Hollow.Combat
{
    public static class DamageSystem
    {
        public static bool ApplyDamage(CombatantHealth target, DamageRequest request)
        {
            if (target == null)
            {
                return false;
            }

            var damaged = target.ApplyDamage(request);
            if (target.IsAlive && request.Feedback.HasKnockback)
            {
                var guardMultiplier = GuardRecoilMultiplier(target, request);
                if (guardMultiplier <= 0f)
                {
                    return damaged;
                }

                var knockback = target.GetComponent<CombatKnockbackReceiver>();
                knockback?.ApplyKnockback(
                    request.Feedback.Direction,
                    request.Feedback.KnockbackMeters * guardMultiplier,
                    request.Feedback.KnockbackSeconds,
                    request.Classification);
            }

            return damaged;
        }

        private static float GuardRecoilMultiplier(CombatantHealth target, DamageRequest request)
        {
            var defense = target.GetComponent<PlayerDefenseController>();
            if (request.Amount > 0 &&
                defense != null &&
                defense.LastHitWasGuarded)
            {
                if (defense.LastGuardResult == ShieldGuardResult.PerfectParry)
                {
                    return 0f;
                }

                return request.GuardKnockbackMultiplier;
            }

            return 1f;
        }
    }
}
