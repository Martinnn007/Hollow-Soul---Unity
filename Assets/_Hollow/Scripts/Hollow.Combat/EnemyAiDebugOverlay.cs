using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemyAiDebugOverlay
    {
        private const float RollingWindowSeconds = 1f;

        private static readonly Dictionary<int, EnemyAiBlackboard> ActiveBlackboards = new();
        private static readonly Dictionary<int, EnemyAiLodTier> ActiveBrainAgents = new();
        private static readonly HashSet<int> StuckAgentIds = new();

        private static int activeFrame = -1;
        private static int brainAgentFrame = -1;
        private static int stuckAgentFrame = -1;
        private static int roomEnemyCountHint;
        private static int brainThinksThisWindow;
        private static int commandReusesThisWindow;
        private static int scorerCallsThisWindow;
        private static int scorerCandidatesThisWindow;
        private static int behaviorGraphTicksThisWindow;
        private static int behaviorGraphFallbacksThisWindow;
        private static int pressureSamplesThisWindow;
        private static float pressurePenaltyTotalThisWindow;
        private static float pressurePenaltyMaxThisWindow;
        private static float thinkIntervalTotalThisWindow;
        private static float maxThinkIntervalThisWindow;
        private static float meleePressure;
        private static float rangedPressure;
        private static float areaPressure;
        private static float chargePressure;
        private static float roomEnemyCountReportTime = float.NegativeInfinity;
        private static float windowStartTime = float.NegativeInfinity;
        private static string lastStuckReason = string.Empty;

        public static bool BlackboardEnabled { get; private set; }

        public static int EstimatedActiveAiAgents
        {
            get
            {
                RefreshFrame();
                RefreshBrainFrame();
                RefreshRoomEnemyCountFrame();
                return Mathf.Max(Mathf.Max(ActiveBrainAgents.Count, ActiveBlackboards.Count), roomEnemyCountHint);
            }
        }

        public static EnemyAiPerformanceStats PerformanceStats
        {
            get
            {
                RefreshRollingWindow(Time.unscaledTime);
                RefreshFrame();
                RefreshBrainFrame();
                RefreshStuckFrame();
                RefreshRoomEnemyCountFrame();
                var full = ActiveBrainAgents.Values.Count(value => value == EnemyAiLodTier.Full);
                var reduced = ActiveBrainAgents.Values.Count(value => value == EnemyAiLodTier.Reduced);
                var background = ActiveBrainAgents.Values.Count(value => value == EnemyAiLodTier.Background);
                if (ActiveBrainAgents.Count == 0 && ActiveBlackboards.Count > 0)
                {
                    full = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Full);
                    reduced = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Reduced);
                    background = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Background);
                }

                return new EnemyAiPerformanceStats(
                    Mathf.Max(Mathf.Max(ActiveBrainAgents.Count, ActiveBlackboards.Count), roomEnemyCountHint),
                    full,
                    reduced,
                    background,
                    brainThinksThisWindow,
                    commandReusesThisWindow,
                    scorerCallsThisWindow,
                    scorerCandidatesThisWindow,
                    behaviorGraphTicksThisWindow,
                    behaviorGraphFallbacksThisWindow,
                    StuckAgentIds.Count,
                    pressureSamplesThisWindow > 0 ? pressurePenaltyTotalThisWindow / pressureSamplesThisWindow : 0f,
                    pressurePenaltyMaxThisWindow,
                    brainThinksThisWindow > 0 ? thinkIntervalTotalThisWindow / brainThinksThisWindow : 0f,
                    maxThinkIntervalThisWindow,
                    meleePressure,
                    rangedPressure,
                    areaPressure,
                    chargePressure,
                    lastStuckReason);
            }
        }

        public static string DiagnosticsSummary
        {
            get
            {
                RefreshFrame();
                var stats = PerformanceStats;
                var perf = $"perf brain/s {stats.BrainThinksPerSecond} reuse/s {stats.CommandReusesPerSecond} scorer/s {stats.ScorerCallsPerSecond} UB/s {stats.BehaviorGraphTicksPerSecond} stuck {stats.StuckAgents} pressure avg/max {stats.AveragePressurePenalty:0.00}/{stats.MaxPressurePenalty:0.00}";
                if (ActiveBlackboards.Count == 0)
                {
                    return $"AI blackboard: no active enemies | active {stats.ActiveAiAgents} | {perf}";
                }

                var full = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Full);
                var reduced = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Reduced);
                var background = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Background);
                var sample = ActiveBlackboards.Values
                    .OrderByDescending(value => value.ChosenScore)
                    .FirstOrDefault();
                return $"AI LOD full/reduced/bg {full}/{reduced}/{background} | top {sample.ChosenCommand}:{sample.ChosenActionId} {sample.ChosenScore:0.00} | pressure -{sample.PressurePenalty:0.00} | {perf}";
            }
        }

        public static void SetBlackboardEnabled(bool enabled)
        {
            BlackboardEnabled = enabled;
        }

        public static void ResetDiagnostics()
        {
            ActiveBlackboards.Clear();
            ActiveBrainAgents.Clear();
            StuckAgentIds.Clear();
            activeFrame = Time.frameCount;
            brainAgentFrame = Time.frameCount;
            stuckAgentFrame = Time.frameCount;
            roomEnemyCountHint = 0;
            roomEnemyCountReportTime = float.NegativeInfinity;
            brainThinksThisWindow = 0;
            commandReusesThisWindow = 0;
            scorerCallsThisWindow = 0;
            scorerCandidatesThisWindow = 0;
            behaviorGraphTicksThisWindow = 0;
            behaviorGraphFallbacksThisWindow = 0;
            pressureSamplesThisWindow = 0;
            pressurePenaltyTotalThisWindow = 0f;
            pressurePenaltyMaxThisWindow = 0f;
            thinkIntervalTotalThisWindow = 0f;
            maxThinkIntervalThisWindow = 0f;
            meleePressure = 0f;
            rangedPressure = 0f;
            areaPressure = 0f;
            chargePressure = 0f;
            windowStartTime = Time.unscaledTime;
            lastStuckReason = string.Empty;
        }

        public static void ReportBlackboard(int instanceId, EnemyAiBlackboard blackboard)
        {
            RefreshFrame();
            ActiveBlackboards[instanceId] = blackboard;
            ReportBrainAgent(instanceId, blackboard.LodTier);
        }

        public static void ReportRoomEnemyCount(int enemyCount)
        {
            roomEnemyCountHint = Mathf.Max(0, enemyCount);
            roomEnemyCountReportTime = Time.unscaledTime;
        }

        public static void ReportRoomPressure(float melee, float ranged, float area, float charge)
        {
            meleePressure = Mathf.Max(0f, melee);
            rangedPressure = Mathf.Max(0f, ranged);
            areaPressure = Mathf.Max(0f, area);
            chargePressure = Mathf.Max(0f, charge);
        }

        public static void ReportBrainAgent(int instanceId, EnemyAiLodTier tier)
        {
            RefreshBrainFrame();
            ActiveBrainAgents[instanceId] = tier;
        }

        public static void RecordBrainThink(int instanceId, EnemyAiLodTier tier, float intervalSeconds)
        {
            RefreshRollingWindow(Time.unscaledTime);
            ReportBrainAgent(instanceId, tier);
            brainThinksThisWindow++;
            var safeInterval = Mathf.Max(0f, intervalSeconds);
            thinkIntervalTotalThisWindow += safeInterval;
            maxThinkIntervalThisWindow = Mathf.Max(maxThinkIntervalThisWindow, safeInterval);
        }

        public static void RecordCommandReuse(int instanceId, EnemyAiLodTier tier)
        {
            RefreshRollingWindow(Time.unscaledTime);
            ReportBrainAgent(instanceId, tier);
            commandReusesThisWindow++;
        }

        public static void RecordScorerCall(int candidateCount)
        {
            RefreshRollingWindow(Time.unscaledTime);
            scorerCallsThisWindow++;
            scorerCandidatesThisWindow += Mathf.Max(0, candidateCount);
        }

        public static void RecordBehaviorGraphTick(bool usedEmergencyFallback)
        {
            RefreshRollingWindow(Time.unscaledTime);
            behaviorGraphTicksThisWindow++;
            if (usedEmergencyFallback)
            {
                behaviorGraphFallbacksThisWindow++;
            }
        }

        public static void RecordPressurePenalty(float pressurePenalty)
        {
            RefreshRollingWindow(Time.unscaledTime);
            var penalty = Mathf.Max(0f, pressurePenalty);
            pressureSamplesThisWindow++;
            pressurePenaltyTotalThisWindow += penalty;
            pressurePenaltyMaxThisWindow = Mathf.Max(pressurePenaltyMaxThisWindow, penalty);
        }

        public static void RecordStuckAgent(int instanceId, string reason)
        {
            RefreshStuckFrame();
            StuckAgentIds.Add(instanceId);
            lastStuckReason = string.IsNullOrWhiteSpace(reason) ? "stuck" : reason;
        }

        private static void RefreshFrame()
        {
            var frame = Time.frameCount;
            if (activeFrame == frame)
            {
                return;
            }

            ActiveBlackboards.Clear();
            activeFrame = frame;
        }

        private static void RefreshBrainFrame()
        {
            var frame = Time.frameCount;
            if (brainAgentFrame == frame)
            {
                return;
            }

            ActiveBrainAgents.Clear();
            brainAgentFrame = frame;
        }

        private static void RefreshStuckFrame()
        {
            var frame = Time.frameCount;
            if (stuckAgentFrame == frame)
            {
                return;
            }

            StuckAgentIds.Clear();
            stuckAgentFrame = frame;
        }

        private static void RefreshRoomEnemyCountFrame()
        {
            if (Time.unscaledTime - roomEnemyCountReportTime <= 1.5f)
            {
                return;
            }

            roomEnemyCountHint = 0;
        }

        private static void RefreshRollingWindow(float now)
        {
            if (windowStartTime <= float.NegativeInfinity)
            {
                windowStartTime = now;
                return;
            }

            if (now - windowStartTime < RollingWindowSeconds)
            {
                return;
            }

            brainThinksThisWindow = 0;
            commandReusesThisWindow = 0;
            scorerCallsThisWindow = 0;
            scorerCandidatesThisWindow = 0;
            behaviorGraphTicksThisWindow = 0;
            behaviorGraphFallbacksThisWindow = 0;
            pressureSamplesThisWindow = 0;
            pressurePenaltyTotalThisWindow = 0f;
            pressurePenaltyMaxThisWindow = 0f;
            thinkIntervalTotalThisWindow = 0f;
            maxThinkIntervalThisWindow = 0f;
            windowStartTime = now;
        }
    }

    public readonly struct EnemyAiPerformanceStats
    {
        public EnemyAiPerformanceStats(
            int activeAiAgents,
            int fullLodAgents,
            int reducedLodAgents,
            int backgroundLodAgents,
            int brainThinksPerSecond,
            int commandReusesPerSecond,
            int scorerCallsPerSecond,
            int scorerCandidatesPerSecond,
            int behaviorGraphTicksPerSecond,
            int behaviorGraphFallbacksPerSecond,
            int stuckAgents,
            float averagePressurePenalty,
            float maxPressurePenalty,
            float averageThinkIntervalSeconds,
            float maxThinkIntervalSeconds,
            float meleePressure,
            float rangedPressure,
            float areaPressure,
            float chargePressure,
            string lastStuckReason)
        {
            ActiveAiAgents = activeAiAgents;
            FullLodAgents = fullLodAgents;
            ReducedLodAgents = reducedLodAgents;
            BackgroundLodAgents = backgroundLodAgents;
            BrainThinksPerSecond = brainThinksPerSecond;
            CommandReusesPerSecond = commandReusesPerSecond;
            ScorerCallsPerSecond = scorerCallsPerSecond;
            ScorerCandidatesPerSecond = scorerCandidatesPerSecond;
            BehaviorGraphTicksPerSecond = behaviorGraphTicksPerSecond;
            BehaviorGraphFallbacksPerSecond = behaviorGraphFallbacksPerSecond;
            StuckAgents = stuckAgents;
            AveragePressurePenalty = averagePressurePenalty;
            MaxPressurePenalty = maxPressurePenalty;
            AverageThinkIntervalSeconds = averageThinkIntervalSeconds;
            MaxThinkIntervalSeconds = maxThinkIntervalSeconds;
            MeleePressure = meleePressure;
            RangedPressure = rangedPressure;
            AreaPressure = areaPressure;
            ChargePressure = chargePressure;
            LastStuckReason = lastStuckReason ?? string.Empty;
        }

        public int ActiveAiAgents { get; }

        public int FullLodAgents { get; }

        public int ReducedLodAgents { get; }

        public int BackgroundLodAgents { get; }

        public int BrainThinksPerSecond { get; }

        public int CommandReusesPerSecond { get; }

        public int ScorerCallsPerSecond { get; }

        public int ScorerCandidatesPerSecond { get; }

        public int BehaviorGraphTicksPerSecond { get; }

        public int BehaviorGraphFallbacksPerSecond { get; }

        public int StuckAgents { get; }

        public float AveragePressurePenalty { get; }

        public float MaxPressurePenalty { get; }

        public float AverageThinkIntervalSeconds { get; }

        public float MaxThinkIntervalSeconds { get; }

        public float MeleePressure { get; }

        public float RangedPressure { get; }

        public float AreaPressure { get; }

        public float ChargePressure { get; }

        public string LastStuckReason { get; }
    }
}
