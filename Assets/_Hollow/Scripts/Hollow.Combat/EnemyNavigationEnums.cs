namespace Hollow.Combat
{
    public enum EnemyNavigationBackend
    {
        LocalSteering = 0,
        FuturePathfinding = 1,
        RoomGridAStar = 2,
        UnityNavMesh = 3
    }

    public enum EnemyPathStatus
    {
        None = 0,
        NotRequested = 1,
        Ready = 2,
        Partial = 3,
        FallbackLocal = 4,
        Unreachable = 5,
        InvalidRequest = 6
    }

    public enum EnemyNavigationMode
    {
        GroundedLocal = 0,
        FlyingLocal = 1,
        IgnoreObstaclesPhase = 2
    }

    public enum EnemyNavigationIntent
    {
        None = 0,
        MoveToPlayer = 1,
        PreferredRange = 2,
        Flee = 3,
        Wander = 4,
        Investigate = 5,
        ReturnHome = 6,
        ActiveCharge = 7,
        ActiveLunge = 8,
        CreatureBurst = 9,
        PhaseMove = 10,
        BumpSeparation = 11
    }
}
