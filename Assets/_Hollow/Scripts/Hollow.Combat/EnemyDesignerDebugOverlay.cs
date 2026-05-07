using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemyDesignerDebugOverlay
    {
        private static readonly HashSet<int> ActiveEnemies = new();
        private static int activeFrame = -1;

        public static bool Enabled { get; private set; }

        public static string DiagnosticsSummary
        {
            get
            {
                RefreshFrame();
                return Enabled
                    ? $"Designer Debug active enemies {ActiveEnemies.Count} | unified path/tactical/behavior/action/awareness/window"
                    : "Designer Debug: OFF";
            }
        }

        public static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public static void ResetDiagnostics()
        {
            ActiveEnemies.Clear();
            activeFrame = Time.frameCount;
        }

        public static void ReportEnemy(int instanceId)
        {
            RefreshFrame();
            ActiveEnemies.Add(instanceId);
        }

        public static string BuildOverlayText(EnemyRuntimeController enemy)
        {
            if (enemy == null)
            {
                return "Designer Debug: missing enemy";
            }

            ReportEnemy(enemy.GetInstanceID());
            var action = !string.IsNullOrWhiteSpace(enemy.CurrentThreatActionDebugId)
                ? enemy.CurrentThreatActionDebugId
                : !string.IsNullOrWhiteSpace(enemy.AiBlackboard.ChosenActionId)
                    ? enemy.AiBlackboard.ChosenActionId
                    : enemy.LastBehaviorReason;
            var tactical = enemy.LastTacticalIntent;
            var nav = enemy.LastNavigationResult;
            var block = ResolveBlockedReason(enemy);
            var behavior = enemy.BehaviorRuntimeMode == EnemyBehaviorRuntimeMode.UnityBehaviorGraph && enemy.UnityBehaviorGraphBridge != null
                ? $"UB {enemy.UnityBehaviorGraphBridge.LastTraceSummary} fail:{Compact(enemy.UnityBehaviorGraphBridge.LastOfficialGraphFailureReason, 32)}"
                : $"BT {Compact(enemy.LastBehaviorTreeNodeId, 42)}";
            return
                $"{enemy.Definition?.DisplayName ?? enemy.name} | Awareness {enemy.AwarenessState} ({Compact(enemy.LastAwarenessReason, 24)})\n" +
                $"State {enemy.ReadabilityState} | Window {enemy.ActiveAttackWindowDebugLine}\n" +
                $"Action {Compact(action, 28)} | Cmd {enemy.LastBehaviorCommand} | LOD {enemy.CurrentAiLodTier}\n" +
                $"Tactical {tactical.Role} slot {tactical.ActiveSlotIndex} {Compact(tactical.ActionId, 22)} {tactical.ReservationPathStatus} {tactical.ReservationPathLengthMeters:0.0}m\n" +
                $"Nav {nav.Backend}/{nav.PathStatus} {nav.Intent} wp {nav.PathWaypointCount} -> {nav.NextWaypointLocalPosition.x:0.0},{nav.NextWaypointLocalPosition.z:0.0}\n" +
                $"Blocked {Compact(block, 46)}\n" +
                $"Behavior {behavior}";
        }

        private static string ResolveBlockedReason(EnemyRuntimeController enemy)
        {
            if (!string.IsNullOrWhiteSpace(enemy.LastDesignerDebugBlockedReason))
            {
                return enemy.LastDesignerDebugBlockedReason;
            }

            if (!string.IsNullOrWhiteSpace(enemy.LastAttackReachabilityReason) &&
                enemy.LastAttackReachability.Status != EnemyAttackReachabilityStatus.Clear)
            {
                return $"{enemy.LastAttackReachability.Status}:{enemy.LastAttackReachabilityReason}";
            }

            if (!string.IsNullOrWhiteSpace(enemy.LastNavigationFallbackReason))
            {
                return enemy.LastNavigationFallbackReason;
            }

            if (!string.IsNullOrWhiteSpace(enemy.LastLocomotionBlockedReason))
            {
                return enemy.LastLocomotionBlockedReason;
            }

            if (!string.IsNullOrWhiteSpace(enemy.AiBlackboard.CooldownReason))
            {
                return enemy.AiBlackboard.CooldownReason;
            }

            return "none";
        }

        private static string Compact(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            var normalized = value.Replace('\n', ' ').Replace('\r', ' ').Trim();
            if (normalized.Length <= maxLength)
            {
                return normalized;
            }

            return normalized.Substring(0, Mathf.Max(0, maxLength - 1)) + "...";
        }

        private static void RefreshFrame()
        {
            var frame = Time.frameCount;
            if (activeFrame == frame)
            {
                return;
            }

            ActiveEnemies.Clear();
            activeFrame = frame;
        }
    }
}
