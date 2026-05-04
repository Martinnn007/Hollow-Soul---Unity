namespace Hollow.Combat
{
    public enum ArenaRoomSize
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Grand = 3
    }

    public enum ArenaLayoutStyle
    {
        Open = 0,
        Cover = 1,
        Pillars = 2,
        Lanes = 3,
        Scramble = 4
    }

    public enum ArenaObstaclePreset
    {
        None = 0,
        LightCover = 1,
        Pillars = 2,
        RockField = 3,
        HazardLanes = 4
    }

    public enum ArenaSpawnPattern
    {
        CenterRing = 0,
        OuterRing = 1,
        Corners = 2,
        EdgeLanes = 3,
        Cluster = 4,
        Scattered = 5,
        RangedBackline = 6,
        PatrolLine = 7
    }

    public enum ArenaGroupingMode
    {
        Solo = 0,
        Pairs = 1,
        TightPack = 2,
        LoosePack = 3,
        MixedSpread = 4
    }

    public enum ArenaPatrolIntent
    {
        None = 0,
        Hold = 1,
        Wander = 2,
        PatrolLine = 3,
        GuardPoint = 4
    }
}
