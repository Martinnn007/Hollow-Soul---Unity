using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public static class EnemyTacticalDebugOverlay
    {
        private static readonly Dictionary<int, EnemyTacticalIntent> ActiveIntents = new();
        private static int activeFrame = -1;
        private static int activeThreats;
        private static int waitingEnemies;

        public static bool Enabled { get; private set; }

        public static string DiagnosticsSummary
        {
            get
            {
                RefreshFrame();
                if (ActiveIntents.Count == 0)
                {
                    return $"Tactics active/wait {activeThreats}/{waitingEnemies} | no enemy intents";
                }

                var active = ActiveIntents.Values.Count(intent => intent.Role == EnemyTacticalRole.ActiveThreat);
                var support = ActiveIntents.Values.Count(intent => intent.Role is EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition);
                var hold = ActiveIntents.Values.Count(intent => intent.Role is EnemyTacticalRole.Hold or EnemyTacticalRole.Waiting or EnemyTacticalRole.StationarySentinel);
                var sample = ActiveIntents.Values
                    .OrderByDescending(intent => intent.Score)
                    .FirstOrDefault();
                return $"Tactics active/support/hold {active}/{support}/{hold} | room active/wait {activeThreats}/{waitingEnemies} | top {sample.Role}:{sample.ActionId} slot {sample.ActiveSlotIndex}";
            }
        }

        public static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public static void ResetDiagnostics()
        {
            ActiveIntents.Clear();
            activeThreats = 0;
            waitingEnemies = 0;
            activeFrame = UnityEngine.Time.frameCount;
        }

        public static void ReportRoomState(int nextActiveThreats, int nextWaitingEnemies)
        {
            activeThreats = UnityEngine.Mathf.Max(0, nextActiveThreats);
            waitingEnemies = UnityEngine.Mathf.Max(0, nextWaitingEnemies);
        }

        public static void ReportIntent(int instanceId, EnemyTacticalIntent intent)
        {
            RefreshFrame();
            if (intent.Role == EnemyTacticalRole.None)
            {
                return;
            }

            ActiveIntents[instanceId] = intent;
        }

        private static void RefreshFrame()
        {
            var frame = UnityEngine.Time.frameCount;
            if (activeFrame == frame)
            {
                return;
            }

            ActiveIntents.Clear();
            activeFrame = frame;
        }
    }
}
