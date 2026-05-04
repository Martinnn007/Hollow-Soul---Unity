using System.Collections.Generic;
using System.Linq;
using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class RoomTacticalDirector
    {
        public const int MinActiveThreatSlots = 2;
        public const int MaxActiveThreatSlots = 4;

        private readonly Dictionary<int, EnemyTacticalIntent> intents = new();
        private readonly List<Vector3> reservedPositions = new();
        private int activeThreatCount;
        private int waitingCount;
        private float lastTickTime = float.NegativeInfinity;

        public int ActiveThreatCount => activeThreatCount;

        public int WaitingCount => waitingCount;

        public float LastTickTime => lastTickTime;

        public void Reset()
        {
            intents.Clear();
            reservedPositions.Clear();
            activeThreatCount = 0;
            waitingCount = 0;
            lastTickTime = float.NegativeInfinity;
        }

        public void Tick(
            IReadOnlyList<EnemyRuntimeController> enemies,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            float timeSeconds)
        {
            intents.Clear();
            reservedPositions.Clear();
            activeThreatCount = 0;
            waitingCount = 0;
            lastTickTime = timeSeconds;

            if (enemies == null || room == null || player == null)
            {
                EnemyTacticalDebugOverlay.ReportRoomState(0, 0);
                return;
            }

            var living = enemies
                .Where(enemy => enemy != null && enemy.IsAlive && enemy.BossDefinition == null && enemy.ArchetypeId != EnemyArchetypeId.Boss)
                .ToArray();
            var activeCandidates = living
                .Where(CanBeTacticalThreat)
                .OrderByDescending(enemy => ThreatScore(enemy))
                .ThenBy(enemy => enemy.SpawnIndex)
                .ToArray();
            var activeLimit = ResolveActiveThreatLimit(activeCandidates.Length, living.Length);
            var activeSet = new HashSet<int>();
            for (var index = 0; index < activeCandidates.Length && index < activeLimit; index++)
            {
                activeSet.Add(activeCandidates[index].GetInstanceID());
            }

            var activeSlot = 0;
            foreach (var enemy in living.OrderBy(enemy => enemy.SpawnIndex < 0 ? enemy.GetInstanceID() : enemy.SpawnIndex))
            {
                var isActive = activeSet.Contains(enemy.GetInstanceID());
                var role = ResolveRole(enemy, isActive);
                var commitPolicy = ResolveCommitPolicy(role);
                var slotIndex = isActive ? activeSlot++ : -1;
                var actionId = enemy.AiBlackboard.ChosenActionId;
                var hasReservation = TryResolveReservedPosition(
                    enemy,
                    room,
                    player,
                    actionId,
                    role,
                    slotIndex,
                    Mathf.Max(1, activeLimit),
                    out var reserved);
                var intent = new EnemyTacticalIntent(
                    role,
                    commitPolicy,
                    actionId,
                    reserved,
                    hasReservation,
                    slotIndex,
                    ThreatScore(enemy),
                    enemy.CurrentThreatLane.ToString(),
                    EnemyNavigationBackend.RoomGridAStar.ToString(),
                    isActive ? "active_slot" : "support_or_wait");

                intents[enemy.GetInstanceID()] = intent;
                if (role == EnemyTacticalRole.ActiveThreat)
                {
                    activeThreatCount++;
                }
                else if (role is EnemyTacticalRole.Waiting or EnemyTacticalRole.Hold or EnemyTacticalRole.StationarySentinel)
                {
                    waitingCount++;
                }
            }

            EnemyTacticalDebugOverlay.ReportRoomState(activeThreatCount, waitingCount);
        }

        public EnemyBehaviorCommand PlanCommand(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand requested,
            float timeSeconds,
            float distanceToPlayer,
            out EnemyTacticalIntent intent)
        {
            intent = ResolveIntent(enemy, requested.ActionId);
            if (enemy == null || enemy.BossDefinition != null || enemy.ArchetypeId == EnemyArchetypeId.Boss)
            {
                return requested;
            }

            if (requested.StartsCommittedAction && intent.Role != EnemyTacticalRole.ActiveThreat)
            {
                return intent.Role switch
                {
                    EnemyTacticalRole.Flee => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Flee, string.Empty, Mathf.Max(0.65f, requested.SpeedMultiplier), "tactical_flee_no_slot"),
                    EnemyTacticalRole.Reposition or EnemyTacticalRole.SupportPressure => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, requested.ActionId, Mathf.Max(0.55f, requested.SpeedMultiplier), "tactical_reposition_no_slot"),
                    EnemyTacticalRole.Investigate => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Wander, string.Empty, Mathf.Max(0.45f, requested.SpeedMultiplier), "tactical_investigate_no_slot"),
                    _ => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "tactical_wait_no_slot")
                };
            }

            if (requested.Kind is EnemyBehaviorCommandKind.MoveToPlayer or EnemyBehaviorCommandKind.MovePreferredRange &&
                intent.Role is EnemyTacticalRole.Waiting or EnemyTacticalRole.Hold or EnemyTacticalRole.StationarySentinel &&
                distanceToPlayer <= 10f)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "tactical_hold_readable");
            }

            return requested;
        }

        public EnemyTacticalIntent ResolveIntent(EnemyRuntimeController enemy, string actionId = "")
        {
            if (enemy == null)
            {
                return EnemyTacticalIntent.Empty;
            }

            if (!intents.TryGetValue(enemy.GetInstanceID(), out var intent))
            {
                intent = BuildFallbackIntent(enemy);
            }

            return string.IsNullOrWhiteSpace(actionId) ? intent : intent.WithAction(actionId);
        }

        public static int ResolveActiveThreatLimit(int candidateCount, int livingCount)
        {
            if (candidateCount <= 0 || livingCount <= 0)
            {
                return 0;
            }

            if (candidateCount <= MinActiveThreatSlots)
            {
                return candidateCount;
            }

            var target = livingCount <= 5 ? MinActiveThreatSlots : livingCount <= 10 ? 3 : MaxActiveThreatSlots;
            return Mathf.Clamp(target, MinActiveThreatSlots, MaxActiveThreatSlots);
        }

        private static bool CanBeTacticalThreat(EnemyRuntimeController enemy)
        {
            if (enemy == null || !enemy.IsAlive || enemy.BossDefinition != null || enemy.ArchetypeId == EnemyArchetypeId.Boss)
            {
                return false;
            }

            if (enemy.AwarenessState < EnemyAwarenessState.Alerted && !enemy.IsEndangeredNow)
            {
                return false;
            }

            return enemy.DistanceToPlayerMeters <= 13f || enemy.IsEndangeredNow;
        }

        private static float ThreatScore(EnemyRuntimeController enemy)
        {
            if (enemy == null)
            {
                return 0f;
            }

            var distanceScore = Mathf.Clamp(13f - enemy.DistanceToPlayerMeters, 0f, 13f);
            var awarenessScore = enemy.AwarenessState switch
            {
                EnemyAwarenessState.Engaged => 3.5f,
                EnemyAwarenessState.Alerted => 2f,
                EnemyAwarenessState.Suspicious => 0.75f,
                _ => 0f
            };
            var intelligenceScore = Mathf.Max(0, (int)enemy.Intelligence) * 0.18f;
            var dispositionScore = enemy.Disposition switch
            {
                EnemyInstinctDisposition.Predator => 0.7f,
                EnemyInstinctDisposition.Territorial => 0.45f,
                EnemyInstinctDisposition.Sentinel => 0.35f,
                EnemyInstinctDisposition.Mindless => 0.25f,
                EnemyInstinctDisposition.Prey => enemy.IsEndangeredNow ? 0.4f : -0.85f,
                _ => 0f
            };
            var mobilityScore = enemy.SpeedMetersPerSecond <= 0f ? -0.15f : 0.25f;
            return distanceScore + awarenessScore + intelligenceScore + dispositionScore + mobilityScore;
        }

        private static EnemyTacticalRole ResolveRole(EnemyRuntimeController enemy, bool isActive)
        {
            if (enemy == null)
            {
                return EnemyTacticalRole.None;
            }

            if (enemy.Disposition == EnemyInstinctDisposition.Prey && !enemy.IsEndangeredNow && enemy.AwarenessState < EnemyAwarenessState.Engaged)
            {
                return EnemyTacticalRole.Flee;
            }

            if (isActive)
            {
                return EnemyTacticalRole.ActiveThreat;
            }

            if (enemy.SpeedMetersPerSecond <= 0f)
            {
                return EnemyTacticalRole.StationarySentinel;
            }

            if ((enemy.AwarenessState is EnemyAwarenessState.Suspicious or EnemyAwarenessState.Alerted) && enemy.DistanceToPlayerMeters > 6f)
            {
                return EnemyTacticalRole.Investigate;
            }

            if (enemy.DistanceToPlayerMeters > 9f)
            {
                return EnemyTacticalRole.Waiting;
            }

            var seed = Mathf.Abs((enemy.SpawnIndex + 3) * 37 + enemy.GetInstanceID());
            return seed % 3 == 0 ? EnemyTacticalRole.Reposition : seed % 3 == 1 ? EnemyTacticalRole.SupportPressure : EnemyTacticalRole.Hold;
        }

        private static EnemyTacticalCommitPolicy ResolveCommitPolicy(EnemyTacticalRole role)
        {
            return role switch
            {
                EnemyTacticalRole.ActiveThreat => EnemyTacticalCommitPolicy.CommitWhenReady,
                EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition or EnemyTacticalRole.Investigate => EnemyTacticalCommitPolicy.PreparePosition,
                EnemyTacticalRole.Flee => EnemyTacticalCommitPolicy.FleeOnly,
                EnemyTacticalRole.Hold or EnemyTacticalRole.Waiting or EnemyTacticalRole.StationarySentinel => EnemyTacticalCommitPolicy.HoldReadable,
                _ => EnemyTacticalCommitPolicy.None
            };
        }

        private bool TryResolveReservedPosition(
            EnemyRuntimeController enemy,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            string actionId,
            EnemyTacticalRole role,
            int slotIndex,
            int slotCount,
            out Vector3 reserved)
        {
            reserved = enemy != null ? enemy.transform.localPosition : Vector3.zero;
            if (enemy == null || room == null || player == null || role is EnemyTacticalRole.Hold or EnemyTacticalRole.Waiting or EnemyTacticalRole.StationarySentinel)
            {
                return false;
            }

            var anchor = player.transform.localPosition;
            var spacing = enemy.ResolveActionSpacingForTacticalIntent(actionId);
            var desiredDistance = Mathf.Max(
                enemy.RadiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.18f,
                spacing.DesiredStartDistanceMeters);
            if (role is EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition or EnemyTacticalRole.Investigate)
            {
                desiredDistance += role == EnemyTacticalRole.Investigate ? 1.35f : 0.65f;
            }

            var currentDelta = enemy.transform.localPosition - anchor;
            currentDelta.y = 0f;
            var currentAngle = currentDelta.sqrMagnitude > 0.01f
                ? Mathf.Atan2(currentDelta.x, currentDelta.z) * Mathf.Rad2Deg
                : 0f;
            var slotAngle = slotIndex >= 0
                ? slotIndex * (360f / Mathf.Max(1, slotCount)) + 18f
                : currentAngle + ((Mathf.Abs(enemy.SpawnIndex) % 5) - 2) * 32f;
            var primaryAngle = role == EnemyTacticalRole.ActiveThreat
                ? Mathf.LerpAngle(currentAngle, slotAngle, 0.55f)
                : slotAngle;
            var angleOffsets = new[] { 0f, -28f, 28f, -56f, 56f, 92f, -92f, 180f };
            var distances = new[] { desiredDistance, desiredDistance + 0.45f, Mathf.Max(0.35f, desiredDistance - 0.35f), desiredDistance + 0.9f };

            var bestScore = float.NegativeInfinity;
            var best = reserved;
            for (var distanceIndex = 0; distanceIndex < distances.Length; distanceIndex++)
            {
                for (var angleIndex = 0; angleIndex < angleOffsets.Length; angleIndex++)
                {
                    var angle = primaryAngle + angleOffsets[angleIndex];
                    var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    var candidate = anchor + direction.normalized * distances[distanceIndex];
                    candidate.y = enemy.transform.localPosition.y;
                    if (!RoomLocalCollision.CanOccupy(room, candidate, enemy.RadiusMeters))
                    {
                        continue;
                    }

                    if (IsTooCloseToReserved(candidate, enemy.RadiusMeters + 0.42f))
                    {
                        continue;
                    }

                    var score = ClearanceScore(room, candidate, enemy.RadiusMeters) * 0.35f
                        - Mathf.Abs(distances[distanceIndex] - desiredDistance) * 0.75f
                        - Vector3.Distance(Flat(candidate), Flat(enemy.transform.localPosition)) * 0.04f
                        - angleIndex * 0.02f;
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    best = candidate;
                }
            }

            if (bestScore <= float.NegativeInfinity)
            {
                return false;
            }

            reserved = best;
            reservedPositions.Add(best);
            return true;
        }

        private bool IsTooCloseToReserved(Vector3 candidate, float minimumDistance)
        {
            var candidateFlat = Flat(candidate);
            for (var index = 0; index < reservedPositions.Count; index++)
            {
                if (Vector3.Distance(candidateFlat, Flat(reservedPositions[index])) < minimumDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static float ClearanceScore(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            var score = 0f;
            var samples = new[]
            {
                Vector3.forward,
                Vector3.back,
                Vector3.left,
                Vector3.right,
                (Vector3.forward + Vector3.left).normalized,
                (Vector3.forward + Vector3.right).normalized,
                (Vector3.back + Vector3.left).normalized,
                (Vector3.back + Vector3.right).normalized
            };

            for (var index = 0; index < samples.Length; index++)
            {
                var sample = localPosition + samples[index] * Mathf.Max(0.5f, radius + 0.25f);
                if (RoomLocalCollision.CanOccupy(room, sample, radius))
                {
                    score += 1f;
                }
            }

            return score;
        }

        private static EnemyTacticalIntent BuildFallbackIntent(EnemyRuntimeController enemy)
        {
            if (enemy == null)
            {
                return EnemyTacticalIntent.Empty;
            }

            var role = enemy.SpeedMetersPerSecond <= 0f ? EnemyTacticalRole.StationarySentinel : EnemyTacticalRole.SupportPressure;
            return new EnemyTacticalIntent(
                role,
                ResolveCommitPolicy(role),
                enemy.AiBlackboard.ChosenActionId,
                enemy.transform.localPosition,
                false,
                -1,
                ThreatScore(enemy),
                enemy.CurrentThreatLane.ToString(),
                EnemyNavigationAdapter.CurrentBackend.ToString(),
                "fallback_tactical_intent");
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
