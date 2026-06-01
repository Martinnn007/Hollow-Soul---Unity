using Unity.Profiling;
using UnityEngine;

namespace Hollow.Core.Diagnostics
{
    public static class M137PerformanceComfortPolicy
    {
        public const string LockId = "M137";
        public const int WindowsComfortTargetFrameRate = 60;
        public const int WallVisibilityMaxRefreshHz = 10;
        public const int CombatHudMaxRefreshHz = 10;
        public const int BossHudMaxRefreshHz = 15;
        public const int MiniMapModelMaxRefreshHz = 4;
        public const int PlayerBuildHudMaxRefreshHz = 10;
        public const int PickupRevealMaxRefreshHz = 10;
        public const int TacticalDirectorMaxTickHz = 8;
        public const int M3BossRoomTacticalDirectorMaxTickHz = 5;
        public const int M3FullThreatThinkMaxHz = 15;
        public const int M3FullThreatThinkMinHz = 8;
        public const int M3ReducedThreatThinkMaxHz = 6;
        public const int M3ReducedThreatThinkMinHz = 3;
        public const int M3BackgroundThinkMaxHz = 2;
        public const int M3BackgroundThinkMinHz = 1;
        public const int M3AiThinkBudgetPerFrame = 6;
        public const int M3BossRoomAddThinkBudgetPerFrame = 2;
        public const int M3BossRoomAddScorerBudgetPerFrame = 1;
        public const int M3NavMeshPathSolveBudgetPerFrame = 4;
        public const int M3BossRoomAddNavMeshPathSolveBudgetPerFrame = 1;
        public const int M3CrowdedRoomEnemyThreshold = 12;
        public const int M3CrowdedRoomActiveThreatSlots = 2;
        public const int M3CrowdedRoomSupportReservationBudgetPerTick = 0;
        public const float M3CrowdedRoomProtectResponsivenessDistanceMeters = 4.75f;
        public const float M3CrowdedRoomNonActiveCloseProtectionDistanceMeters = 0.75f;
        public const float M3CrowdedRoomCheapCommandDistanceMeters = 9f;
        public const float M3CrowdedRoomBackgroundDistanceMeters = 8f;
        public const int CaptureDefaultMaxSampleHz = 240;
        public const float WallVisibilityCameraAngleThresholdDegrees = 2f;

        public static readonly float WallVisibilityMinRefreshIntervalSeconds = 1f / WallVisibilityMaxRefreshHz;
        public static readonly float CombatHudMinRefreshIntervalSeconds = 1f / CombatHudMaxRefreshHz;
        public static readonly float BossHudMinRefreshIntervalSeconds = 1f / BossHudMaxRefreshHz;
        public static readonly float MiniMapModelMinRefreshIntervalSeconds = 1f / MiniMapModelMaxRefreshHz;
        public static readonly float PlayerBuildHudMinRefreshIntervalSeconds = 1f / PlayerBuildHudMaxRefreshHz;
        public static readonly float PickupRevealMinRefreshIntervalSeconds = 1f / PickupRevealMaxRefreshHz;
        public static readonly float TacticalDirectorMinTickIntervalSeconds = 1f / TacticalDirectorMaxTickHz;
        public static readonly float M3BossRoomTacticalDirectorMinTickIntervalSeconds = 1f / M3BossRoomTacticalDirectorMaxTickHz;
        public static readonly float M3FullThreatMinThinkIntervalSeconds = 1f / M3FullThreatThinkMaxHz;
        public static readonly float M3FullThreatMaxThinkIntervalSeconds = 1f / M3FullThreatThinkMinHz;
        public static readonly float M3ReducedThreatMinThinkIntervalSeconds = 1f / M3ReducedThreatThinkMaxHz;
        public static readonly float M3ReducedThreatMaxThinkIntervalSeconds = 1f / M3ReducedThreatThinkMinHz;
        public static readonly float M3BackgroundMinThinkIntervalSeconds = 1f / M3BackgroundThinkMaxHz;
        public static readonly float M3BackgroundMaxThinkIntervalSeconds = 1f / M3BackgroundThinkMinHz;
        public static readonly float WallVisibilityCameraForwardDotThreshold =
            Mathf.Cos(WallVisibilityCameraAngleThresholdDegrees * Mathf.Deg2Rad);

        public static int ExpectedCaptureSampleCapacity(float sampleSeconds)
        {
            return Mathf.CeilToInt(Mathf.Max(1f, sampleSeconds) * CaptureDefaultMaxSampleHz);
        }
    }

    public static class M137PerformanceProfilerMarkers
    {
        public static readonly ProfilerMarker WallVisibilityRefresh = new("Hollow.M137.WallVisibility.Refresh");
        public static readonly ProfilerMarker CombatHudRefresh = new("Hollow.M137.CombatHud.Refresh");
        public static readonly ProfilerMarker BossHudRefresh = new("Hollow.M137.BossHud.Refresh");
        public static readonly ProfilerMarker RoomTransitionLoad = new("Hollow.M137.RoomTransition.Load");
        public static readonly ProfilerMarker BossSpawnActivate = new("Hollow.M137.Boss.SpawnActivate");
        public static readonly ProfilerMarker MiniMapRebuild = new("Hollow.M137.MiniMap.Rebuild");
    }
}
