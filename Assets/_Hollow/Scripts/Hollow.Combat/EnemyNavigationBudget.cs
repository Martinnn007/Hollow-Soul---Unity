using Hollow.Core.Diagnostics;
using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemyNavigationBudget
    {
        private static int frame = -1;
        private static int pathSolvesUsed;
        private static int bossRoomAddPathSolvesUsed;

        public static void ResetForTests()
        {
            frame = -1;
            pathSolvesUsed = 0;
            bossRoomAddPathSolvesUsed = 0;
        }

        public static bool TryAcquirePathSolve(in EnemyNavigationRequest request, bool force)
        {
            var currentFrame = Time.frameCount;
            if (frame != currentFrame)
            {
                frame = currentFrame;
                pathSolvesUsed = 0;
                bossRoomAddPathSolvesUsed = 0;
            }

            var isBossRoomAdd = IsBossRoomAdd(request);
            if (isBossRoomAdd)
            {
                var bossAddBudget = Mathf.Max(1, M137PerformanceComfortPolicy.M3BossRoomAddNavMeshPathSolveBudgetPerFrame);
                if (bossRoomAddPathSolvesUsed >= bossAddBudget)
                {
                    EnemyNavigationDebugOverlay.RecordBudgetUsage(bossRoomAddPathSolvesUsed, bossAddBudget);
                    EnemyNavigationDebugOverlay.RecordBudgetDeferred(
                        $"m3_boss_add_budget:{request.AiLodTier}:{request.TacticalRole}:{(force ? "priority" : "normal")}");
                    return false;
                }
            }

            var budget = Mathf.Max(1, M137PerformanceComfortPolicy.M3NavMeshPathSolveBudgetPerFrame);
            if (pathSolvesUsed >= budget)
            {
                EnemyNavigationDebugOverlay.RecordBudgetUsage(pathSolvesUsed, budget);
                EnemyNavigationDebugOverlay.RecordBudgetDeferred(
                    $"m3_budget:{request.AiLodTier}:{request.TacticalRole}:{(force ? "priority" : "normal")}");
                return false;
            }

            pathSolvesUsed++;
            if (isBossRoomAdd)
            {
                bossRoomAddPathSolvesUsed++;
            }

            EnemyNavigationDebugOverlay.RecordBudgetUsage(pathSolvesUsed, budget);
            return true;
        }

        public static float RepathIntervalFor(in EnemyNavigationRequest request)
        {
            if (IsBossRoomAdd(request))
            {
                var bossAddInterval = request.AiLodTier switch
                {
                    EnemyAiLodTier.Full => request.TacticalRole == EnemyTacticalRole.ActiveThreat ? 0.28f : 0.36f,
                    EnemyAiLodTier.Reduced => request.TacticalRole is EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition ? 0.62f : 0.78f,
                    _ => 1.15f
                };
                return Mathf.Clamp(bossAddInterval, 0.24f, 1.45f);
            }

            var baseInterval = request.AiLodTier switch
            {
                EnemyAiLodTier.Full => request.TacticalRole == EnemyTacticalRole.ActiveThreat ? 0.16f : 0.22f,
                EnemyAiLodTier.Reduced => request.TacticalRole is EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition ? 0.38f : 0.52f,
                _ => 0.85f
            };

            var intelligenceMultiplier = request.Intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => 0.88f,
                EnemyIntelligenceLevel.Tactical => 0.95f,
                EnemyIntelligenceLevel.Trained => 1.05f,
                EnemyIntelligenceLevel.Basic => 1.12f,
                EnemyIntelligenceLevel.Simple => 1.22f,
                _ => 1.3f
            };
            return Mathf.Clamp(baseInterval * intelligenceMultiplier, 0.14f, 1.25f);
        }

        public static float InitialRepathOffsetSeconds(int seed)
        {
            var safeSeed = Mathf.Abs(seed);
            return (safeSeed % 11) * 0.017f;
        }

        public static bool IsHighPriority(in EnemyNavigationRequest request)
        {
            return !IsBossRoomAdd(request) &&
                request.AiLodTier == EnemyAiLodTier.Full &&
                request.TacticalRole == EnemyTacticalRole.ActiveThreat;
        }

        private static bool IsBossRoomAdd(in EnemyNavigationRequest request)
        {
            return request.RoomHasActiveBoss && !request.IsBoss;
        }
    }
}
