namespace Hollow.Combat
{
    public enum ShieldGuardResult
    {
        None,
        PassiveReduced,
        GuardBlocked,
        PerfectParry,
        FailedOutOfCone,
        FailedNoStamina,
        RejectedThreat
    }

    public enum ShieldGuardAnimationCue
    {
        Blocked,
        Breakthrough
    }
}
