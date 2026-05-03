using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public static class EnemyAiDebugOverlay
    {
        private static readonly Dictionary<int, EnemyAiBlackboard> ActiveBlackboards = new();

        private static int activeFrame = -1;

        public static bool BlackboardEnabled { get; private set; }

        public static string DiagnosticsSummary
        {
            get
            {
                RefreshFrame();
                if (ActiveBlackboards.Count == 0)
                {
                    return "AI blackboard: no active enemies";
                }

                var full = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Full);
                var reduced = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Reduced);
                var background = ActiveBlackboards.Values.Count(value => value.LodTier == EnemyAiLodTier.Background);
                var sample = ActiveBlackboards.Values
                    .OrderByDescending(value => value.ChosenScore)
                    .FirstOrDefault();
                return $"AI LOD full/reduced/bg {full}/{reduced}/{background} | top {sample.ChosenCommand}:{sample.ChosenActionId} {sample.ChosenScore:0.00} | pressure -{sample.PressurePenalty:0.00}";
            }
        }

        public static void SetBlackboardEnabled(bool enabled)
        {
            BlackboardEnabled = enabled;
        }

        public static void ResetDiagnostics()
        {
            ActiveBlackboards.Clear();
            activeFrame = UnityEngine.Time.frameCount;
        }

        public static void ReportBlackboard(int instanceId, EnemyAiBlackboard blackboard)
        {
            RefreshFrame();
            ActiveBlackboards[instanceId] = blackboard;
        }

        private static void RefreshFrame()
        {
            var frame = UnityEngine.Time.frameCount;
            if (activeFrame == frame)
            {
                return;
            }

            ActiveBlackboards.Clear();
            activeFrame = frame;
        }
    }
}
