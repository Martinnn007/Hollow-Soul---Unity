namespace Hollow.Combat
{
    public static class EnemyNavigationDebugOverlay
    {
        private const float RollingWindowSeconds = 1f;

        private static readonly System.Collections.Generic.HashSet<int> ActivePathUserIds = new();

        private static int activePathUserFrame = -1;
        private static int requestsThisWindow;
        private static int freshSolvesThisWindow;
        private static int cacheHitsThisWindow;
        private static int occupancyBuildsThisWindow;
        private static int budgetDeferredThisWindow;
        private static int fallbacksThisWindow;
        private static int budgetUsedThisFrame;
        private static int budgetFrame = -1;
        private static int budgetLimit;
        private static float solveMillisecondsThisWindow;
        private static float maxSolveMillisecondsThisWindow;
        private static float windowStartTime = float.NegativeInfinity;
        private static string lastFallbackReason = string.Empty;

        public static bool PathTracingEnabled { get; private set; }

        public static EnemyNavigationDebugStats Stats
        {
            get
            {
                RefreshRollingWindow(UnityEngine.Time.unscaledTime);
                RefreshActivePathUserFrame();
                return new EnemyNavigationDebugStats(
                    ActivePathUserIds.Count,
                    requestsThisWindow,
                    freshSolvesThisWindow,
                    cacheHitsThisWindow,
                    occupancyBuildsThisWindow,
                    budgetDeferredThisWindow,
                    fallbacksThisWindow,
                    budgetUsedThisFrame,
                    budgetLimit,
                    freshSolvesThisWindow > 0 ? solveMillisecondsThisWindow / freshSolvesThisWindow : 0f,
                    maxSolveMillisecondsThisWindow,
                    lastFallbackReason);
            }
        }

        public static string DiagnosticsSummary
        {
            get
            {
                var stats = Stats;
                return $"Path users {stats.ActivePathUsers} | req/s {stats.RequestsPerSecond} | solves/s {stats.FreshSolvesPerSecond} | cache/s {stats.CacheHitsPerSecond} | builds/s {stats.OccupancyBuildsPerSecond} | deferred/s {stats.BudgetDeferredPerSecond} | fallback/s {stats.FallbacksPerSecond} | budget {stats.BudgetUsedThisFrame}/{stats.BudgetLimitPerFrame} | avg {stats.AverageSolveMilliseconds:0.00}ms max {stats.MaxSolveMilliseconds:0.00}ms | last {stats.LastFallbackReason}";
            }
        }

        public static void SetPathTracingEnabled(bool enabled)
        {
            PathTracingEnabled = enabled;
        }

        public static void ResetDiagnostics()
        {
            ActivePathUserIds.Clear();
            activePathUserFrame = UnityEngine.Time.frameCount;
            requestsThisWindow = 0;
            freshSolvesThisWindow = 0;
            cacheHitsThisWindow = 0;
            occupancyBuildsThisWindow = 0;
            budgetDeferredThisWindow = 0;
            fallbacksThisWindow = 0;
            budgetUsedThisFrame = 0;
            budgetFrame = UnityEngine.Time.frameCount;
            budgetLimit = 0;
            solveMillisecondsThisWindow = 0f;
            maxSolveMillisecondsThisWindow = 0f;
            windowStartTime = UnityEngine.Time.unscaledTime;
            lastFallbackReason = string.Empty;
        }

        public static void ReportActivePathUser(int instanceId)
        {
            RefreshActivePathUserFrame();
            ActivePathUserIds.Add(instanceId);
        }

        public static void RecordPathRequest()
        {
            RefreshRollingWindow(UnityEngine.Time.unscaledTime);
            requestsThisWindow++;
        }

        public static void RecordFreshPathSolve(float milliseconds)
        {
            RefreshRollingWindow(UnityEngine.Time.unscaledTime);
            freshSolvesThisWindow++;
            solveMillisecondsThisWindow += UnityEngine.Mathf.Max(0f, milliseconds);
            maxSolveMillisecondsThisWindow = UnityEngine.Mathf.Max(maxSolveMillisecondsThisWindow, milliseconds);
        }

        public static void RecordCacheHit()
        {
            RefreshRollingWindow(UnityEngine.Time.unscaledTime);
            cacheHitsThisWindow++;
        }

        public static void RecordOccupancyBuild()
        {
            RefreshRollingWindow(UnityEngine.Time.unscaledTime);
            occupancyBuildsThisWindow++;
        }

        public static void RecordBudgetDeferred(string reason)
        {
            RefreshRollingWindow(UnityEngine.Time.unscaledTime);
            budgetDeferredThisWindow++;
            lastFallbackReason = string.IsNullOrWhiteSpace(reason) ? "budget_deferred" : reason;
        }

        public static void RecordFallback(string reason)
        {
            RefreshRollingWindow(UnityEngine.Time.unscaledTime);
            fallbacksThisWindow++;
            lastFallbackReason = string.IsNullOrWhiteSpace(reason) ? "fallback" : reason;
        }

        public static void RecordBudgetUsage(int usedThisFrame, int limitPerFrame)
        {
            var frame = UnityEngine.Time.frameCount;
            if (budgetFrame != frame)
            {
                budgetFrame = frame;
                budgetUsedThisFrame = 0;
            }

            budgetUsedThisFrame = UnityEngine.Mathf.Max(0, usedThisFrame);
            budgetLimit = UnityEngine.Mathf.Max(0, limitPerFrame);
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

            requestsThisWindow = 0;
            freshSolvesThisWindow = 0;
            cacheHitsThisWindow = 0;
            occupancyBuildsThisWindow = 0;
            budgetDeferredThisWindow = 0;
            fallbacksThisWindow = 0;
            solveMillisecondsThisWindow = 0f;
            maxSolveMillisecondsThisWindow = 0f;
            windowStartTime = now;
        }

        private static void RefreshActivePathUserFrame()
        {
            var frame = UnityEngine.Time.frameCount;
            if (activePathUserFrame == frame)
            {
                return;
            }

            ActivePathUserIds.Clear();
            activePathUserFrame = frame;
        }
    }

    public readonly struct EnemyNavigationDebugStats
    {
        public EnemyNavigationDebugStats(
            int activePathUsers,
            int requestsPerSecond,
            int freshSolvesPerSecond,
            int cacheHitsPerSecond,
            int occupancyBuildsPerSecond,
            int budgetDeferredPerSecond,
            int fallbacksPerSecond,
            int budgetUsedThisFrame,
            int budgetLimitPerFrame,
            float averageSolveMilliseconds,
            float maxSolveMilliseconds,
            string lastFallbackReason)
        {
            ActivePathUsers = activePathUsers;
            RequestsPerSecond = requestsPerSecond;
            FreshSolvesPerSecond = freshSolvesPerSecond;
            CacheHitsPerSecond = cacheHitsPerSecond;
            OccupancyBuildsPerSecond = occupancyBuildsPerSecond;
            BudgetDeferredPerSecond = budgetDeferredPerSecond;
            FallbacksPerSecond = fallbacksPerSecond;
            BudgetUsedThisFrame = budgetUsedThisFrame;
            BudgetLimitPerFrame = budgetLimitPerFrame;
            AverageSolveMilliseconds = averageSolveMilliseconds;
            MaxSolveMilliseconds = maxSolveMilliseconds;
            LastFallbackReason = lastFallbackReason ?? string.Empty;
        }

        public int ActivePathUsers { get; }

        public int RequestsPerSecond { get; }

        public int FreshSolvesPerSecond { get; }

        public int CacheHitsPerSecond { get; }

        public int OccupancyBuildsPerSecond { get; }

        public int BudgetDeferredPerSecond { get; }

        public int FallbacksPerSecond { get; }

        public int BudgetUsedThisFrame { get; }

        public int BudgetLimitPerFrame { get; }

        public float AverageSolveMilliseconds { get; }

        public float MaxSolveMilliseconds { get; }

        public string LastFallbackReason { get; }
    }
}
