namespace Hollow.Combat
{
    public enum EnemyActionCategory
    {
        Body = 0,
        Weapon = 1,
        Ranged = 2,
        Projectile = 3,
        Magic = 4,
        Movement = 5,
        Defense = 6,
        Summon = 7,
        Hazard = 8,
        GhostSoul = 9,
        BossScale = 10
    }

    public enum EnemyActionIntent
    {
        Damage = 0,
        Pressure = 1,
        Reposition = 2,
        Defend = 3,
        Summon = 4,
        HazardSetup = 5,
        Escape = 6,
        Feint = 7,
        Interrupt = 8
    }

    public enum EnemyActionShape
    {
        None = 0,
        Self = 1,
        ForwardArc = 2,
        CircleArea = 3,
        Lane = 4,
        Cone = 5,
        Projectile = 6,
        Fan = 7,
        Radial = 8,
        TargetPoint = 9,
        HazardZone = 10
    }

    public enum EnemyActionUsageState
    {
        CurrentRuntime = 0,
        FutureCandidate = 1,
        LibraryTemplate = 2
    }
}
