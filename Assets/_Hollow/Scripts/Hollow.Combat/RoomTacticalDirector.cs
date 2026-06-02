using System.Collections.Generic;
using Hollow.Core.Diagnostics;
using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;
using UnityEngine.AI;

namespace Hollow.Combat
{
    public sealed class RoomTacticalDirector
    {
        public const int MinActiveThreatSlots = 2;
        public const int MaxActiveThreatSlots = 4;
        public const float ReservationNavMeshSampleRadiusMeters = 0.85f;

        private readonly Dictionary<int, EnemyTacticalIntent> intents = new();
        private readonly List<Vector3> reservedPositions = new();
        private readonly List<EnemyRuntimeController> livingEnemies = new();
        private readonly List<EnemyRuntimeController> activeCandidates = new();
        private readonly HashSet<int> activeThreatIds = new();
        private NavMeshPath reservationPath;
        private int activeThreatCount;
        private int waitingCount;
        private float lastTickTime = float.NegativeInfinity;
        private int lastBossRoomSignature;
        private bool hasLastBossRoomSignature;
        private int lastCrowdedRoomSignature;
        private bool hasLastCrowdedRoomSignature;
        private static readonly float[] ReservationAngleOffsets = { 0f, -28f, 28f, -56f, 56f, 92f, -92f, 180f };
        private static readonly float[] BossReservationAngleOffsets = { 0f, -28f, 28f };
        private static readonly Vector3[] ClearanceSampleDirections =
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
            hasLastBossRoomSignature = false;
            lastBossRoomSignature = 0;
            hasLastCrowdedRoomSignature = false;
            lastCrowdedRoomSignature = 0;
        }

        public void Tick(
            IReadOnlyList<EnemyRuntimeController> enemies,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            float timeSeconds)
        {
            var hasBossRoomSignature = TryBuildBossRoomSignature(enemies, player, out var bossRoomSignature, out var livingBossAdds);
            if (hasBossRoomSignature &&
                hasLastBossRoomSignature &&
                bossRoomSignature == lastBossRoomSignature &&
                intents.Count > 0)
            {
                lastTickTime = timeSeconds;
                M136PerformanceOperationCounters.ReportTacticalCachedIntentReuse(livingBossAdds);
                EnemyTacticalDebugOverlay.ReportRoomState(activeThreatCount, waitingCount);
                return;
            }

            var hasCrowdedRoomSignature = TryBuildCrowdedRoomSignature(enemies, player, out var crowdedRoomSignature, out var livingCrowdEnemies);
            if (hasCrowdedRoomSignature &&
                hasLastCrowdedRoomSignature &&
                crowdedRoomSignature == lastCrowdedRoomSignature &&
                intents.Count > 0)
            {
                lastTickTime = timeSeconds;
                M136PerformanceOperationCounters.ReportTacticalCrowdCachedIntentReuse(livingCrowdEnemies);
                EnemyTacticalDebugOverlay.ReportRoomState(activeThreatCount, waitingCount);
                return;
            }

            intents.Clear();
            reservedPositions.Clear();
            livingEnemies.Clear();
            activeCandidates.Clear();
            activeThreatIds.Clear();
            activeThreatCount = 0;
            waitingCount = 0;
            lastTickTime = timeSeconds;

            if (enemies == null || room == null || player == null)
            {
                EnemyTacticalDebugOverlay.ReportRoomState(0, 0);
                return;
            }

            var bossPresent = false;
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (enemy.BossDefinition != null || enemy.ArchetypeId == EnemyArchetypeId.Boss)
                {
                    bossPresent = true;
                    continue;
                }

                livingEnemies.Add(enemy);
                if (CanBeTacticalThreat(enemy))
                {
                    activeCandidates.Add(enemy);
                }
            }

            activeCandidates.Sort(CompareThreatCandidates);
            livingEnemies.Sort(CompareSpawnOrder);
            var crowdPresent = IsCrowdedNonBossRoom(bossPresent, livingEnemies.Count);
            var activeLimit = ResolveActiveThreatLimit(activeCandidates.Count, livingEnemies.Count, bossPresent);
            if (crowdPresent)
            {
                M136PerformanceOperationCounters.ReportTacticalCrowdActiveThreatLimit(activeLimit);
            }

            for (var index = 0; index < activeCandidates.Count && index < activeLimit; index++)
            {
                activeThreatIds.Add(activeCandidates[index].GetInstanceID());
            }

            var activeSlot = 0;
            var crowdSupportReservationBudgetUsed = 0;
            for (var index = 0; index < livingEnemies.Count; index++)
            {
                var enemy = livingEnemies[index];
                var isActive = activeThreatIds.Contains(enemy.GetInstanceID());
                var role = ResolveRole(enemy, isActive);
                if (bossPresent && !isActive)
                {
                    role = DowngradeBossAddRole(enemy, role);
                }
                else if (crowdPresent && !isActive)
                {
                    role = DowngradeCrowdNonActiveRole(enemy, role);
                }

                var commitPolicy = ResolveCommitPolicy(role);
                var slotIndex = isActive ? activeSlot : -1;
                var actionId = enemy.AiBlackboard.ChosenActionId;
                var reserved = enemy.transform.localPosition;
                var reservationPathStatus = EnemyPathStatus.NotRequested;
                var reservationPathCornerCount = 0;
                var reservationPathLengthMeters = 0f;
                var reservationReason = bossPresent && role != EnemyTacticalRole.ActiveThreat
                    ? "boss_add_cached_hold_no_reservation"
                    : crowdPresent && role != EnemyTacticalRole.ActiveThreat
                        ? "crowd_cached_intent_no_reservation"
                        : "reservation_not_requested";
                if (bossPresent && role != EnemyTacticalRole.ActiveThreat)
                {
                    M136PerformanceOperationCounters.ReportTacticalBossAddReservationSkip();
                }
                else if (crowdPresent && role != EnemyTacticalRole.ActiveThreat)
                {
                    M136PerformanceOperationCounters.ReportTacticalCrowdReservationSkip();
                }

                var compactReservationMode = (bossPresent || crowdPresent) && role == EnemyTacticalRole.ActiveThreat;
                var crowdSupportReservationAllowed = !bossPresent &&
                    crowdPresent &&
                    role != EnemyTacticalRole.ActiveThreat &&
                    ShouldUseCrowdSupportReservation(enemy, role, crowdSupportReservationBudgetUsed);
                if (crowdSupportReservationAllowed)
                {
                    crowdSupportReservationBudgetUsed++;
                    M136PerformanceOperationCounters.ReportTacticalCrowdSupportReservationBudgetUse();
                }

                var canRequestReservation = role == EnemyTacticalRole.ActiveThreat ||
                    (!bossPresent && !crowdPresent) ||
                    crowdSupportReservationAllowed;
                var hasReservation = canRequestReservation
                    ? TryResolveReservedPosition(
                        enemy,
                        room,
                        player,
                        actionId,
                        role,
                        slotIndex,
                        Mathf.Max(1, activeLimit),
                        out reserved,
                        out reservationPathStatus,
                        out reservationPathCornerCount,
                        out reservationPathLengthMeters,
                        out reservationReason,
                        compactReservationMode: compactReservationMode)
                    : false;
                if (role == EnemyTacticalRole.ActiveThreat && !hasReservation)
                {
                    var activeReservationReason = reservationReason;
                    isActive = false;
                    role = bossPresent ? EnemyTacticalRole.Hold : EnemyTacticalRole.SupportPressure;
                    commitPolicy = ResolveCommitPolicy(role);
                    slotIndex = -1;
                    if (bossPresent || crowdPresent)
                    {
                        reserved = enemy.transform.localPosition;
                        reservationPathStatus = EnemyPathStatus.NotRequested;
                        reservationPathCornerCount = 0;
                        reservationPathLengthMeters = 0f;
                        reservationReason = bossPresent
                            ? "boss_add_active_slot_missing_cached_hold"
                            : "crowd_active_slot_missing_cached_support";
                        hasReservation = false;
                        if (crowdPresent)
                        {
                            M136PerformanceOperationCounters.ReportTacticalCrowdReservationSkip();
                        }
                    }
                    else
                    {
                        hasReservation = TryResolveReservedPosition(
                            enemy,
                            room,
                            player,
                            actionId,
                            role,
                            slotIndex,
                            Mathf.Max(1, activeLimit),
                            out reserved,
                            out reservationPathStatus,
                            out reservationPathCornerCount,
                            out reservationPathLengthMeters,
                            out reservationReason);
                    }

                    var activeMissingPrefix = string.IsNullOrWhiteSpace(activeReservationReason)
                        ? "active_slot_missing_reachable_reservation"
                        : $"active_slot_missing_reachable_reservation:{activeReservationReason}";
                    reservationReason = hasReservation
                        ? $"{activeMissingPrefix}:support_reachable:{reservationReason}"
                        : activeMissingPrefix;
                }
                else if (role == EnemyTacticalRole.ActiveThreat)
                {
                    activeSlot++;
                }

                var intent = new EnemyTacticalIntent(
                    role,
                    commitPolicy,
                    actionId,
                    reserved,
                    hasReservation,
                    slotIndex,
                    ThreatScore(enemy),
                    enemy.CurrentThreatLane.ToString(),
                    EnemyNavigationBackend.UnityNavMesh.ToString(),
                    hasReservation
                        ? $"{(isActive ? "active_slot" : "support_or_wait")}:{reservationReason}"
                        : $"{(isActive ? "active_slot" : "support_or_wait")}:no_reachable_reservation",
                    reservationPathStatus,
                    reservationPathCornerCount,
                    reservationPathLengthMeters);

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

            if (hasBossRoomSignature)
            {
                lastBossRoomSignature = bossRoomSignature;
                hasLastBossRoomSignature = true;
                hasLastCrowdedRoomSignature = false;
            }
            else if (hasCrowdedRoomSignature)
            {
                lastCrowdedRoomSignature = crowdedRoomSignature;
                hasLastCrowdedRoomSignature = true;
                hasLastBossRoomSignature = false;
            }
            else
            {
                hasLastBossRoomSignature = false;
                hasLastCrowdedRoomSignature = false;
            }

            EnemyTacticalDebugOverlay.ReportRoomState(activeThreatCount, waitingCount);
        }

        private static int CompareThreatCandidates(EnemyRuntimeController left, EnemyRuntimeController right)
        {
            var scoreCompare = ThreatScore(right).CompareTo(ThreatScore(left));
            return scoreCompare != 0 ? scoreCompare : left.SpawnIndex.CompareTo(right.SpawnIndex);
        }

        private static int CompareSpawnOrder(EnemyRuntimeController left, EnemyRuntimeController right)
        {
            var leftKey = left.SpawnIndex < 0 ? left.GetInstanceID() : left.SpawnIndex;
            var rightKey = right.SpawnIndex < 0 ? right.GetInstanceID() : right.SpawnIndex;
            return leftKey.CompareTo(rightKey);
        }

        public EnemyBehaviorCommand PlanCommand(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand requested,
            float timeSeconds,
            float distanceToPlayer,
            out EnemyTacticalIntent intent)
        {
            if (enemy == null || enemy.BossDefinition != null || enemy.ArchetypeId == EnemyArchetypeId.Boss)
            {
                intent = EnemyTacticalIntent.Empty;
                return requested;
            }

            intent = ResolveIntent(enemy, requested.ActionId);
            if (enemy.IsRootedStaticEnemy && requested.StartsCommittedAction)
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

        public bool TryResolveClearAttackReposition(
            EnemyRuntimeController enemy,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            string actionId,
            out Vector3 reserved,
            out string reason)
        {
            reserved = enemy != null ? enemy.transform.localPosition : Vector3.zero;
            reason = string.Empty;
            if (enemy == null || room == null || player == null || string.IsNullOrWhiteSpace(actionId))
            {
                reason = "missing_attack_reposition_context";
                return false;
            }

            if (!enemy.RoomHasActiveBoss &&
                enemy.LastTacticalIntent.Role != EnemyTacticalRole.ActiveThreat &&
                enemy.CurrentAiLodTier != EnemyAiLodTier.Full)
            {
                reason = "crowd_non_active_lod_skips_clear_attack_reposition";
                M136PerformanceOperationCounters.ReportTacticalCrowdReservationSkip();
                return false;
            }

            if (!TryResolveReservedPosition(
                    enemy,
                    room,
                    player,
                    actionId,
                    EnemyTacticalRole.ActiveThreat,
                    Mathf.Abs(enemy.SpawnIndex) % MaxActiveThreatSlots,
                    MaxActiveThreatSlots,
                    out reserved,
                    out var pathStatus,
                    out _,
                    out _,
                    out reason,
                    respectExistingReservations: false,
                    recordReservation: false))
            {
                return false;
            }

            if (pathStatus != EnemyPathStatus.Ready)
            {
                reason = $"clear_attack_reposition_path_{pathStatus.ToString().ToLowerInvariant()}";
                return false;
            }

            reason = string.IsNullOrWhiteSpace(reason) ? "clear_attack_reposition" : $"clear_attack_reposition:{reason}";
            return true;
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
            return ResolveActiveThreatLimit(candidateCount, livingCount, bossPresent: false);
        }

        public static int ResolveActiveThreatLimit(int candidateCount, int livingCount, bool bossPresent)
        {
            if (candidateCount <= 0 || livingCount <= 0)
            {
                return 0;
            }

            if (bossPresent)
            {
                return Mathf.Clamp(candidateCount, 1, 1);
            }

            if (candidateCount <= MinActiveThreatSlots)
            {
                return candidateCount;
            }

            if (livingCount >= M137PerformanceComfortPolicy.M3CrowdedRoomEnemyThreshold)
            {
                return Mathf.Clamp(
                    M137PerformanceComfortPolicy.M3CrowdedRoomActiveThreatSlots,
                    1,
                    Mathf.Min(candidateCount, MaxActiveThreatSlots));
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

        private static EnemyTacticalRole DowngradeBossAddRole(EnemyRuntimeController enemy, EnemyTacticalRole role)
        {
            if (enemy == null)
            {
                return EnemyTacticalRole.Hold;
            }

            return role switch
            {
                EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition or EnemyTacticalRole.Investigate =>
                    enemy.DistanceToPlayerMeters > 5.5f ? EnemyTacticalRole.Waiting : EnemyTacticalRole.Hold,
                _ => role
            };
        }

        private static bool IsCrowdedNonBossRoom(bool bossPresent, int livingEnemyCount)
        {
            return !bossPresent && livingEnemyCount >= M137PerformanceComfortPolicy.M3CrowdedRoomEnemyThreshold;
        }

        private static EnemyTacticalRole DowngradeCrowdNonActiveRole(EnemyRuntimeController enemy, EnemyTacticalRole role)
        {
            if (enemy == null)
            {
                return EnemyTacticalRole.Hold;
            }

            if (enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                enemy.IsEndangeredNow ||
                enemy.DistanceToPlayerMeters <= M137PerformanceComfortPolicy.M3CrowdedRoomProtectResponsivenessDistanceMeters)
            {
                return role;
            }

            return role switch
            {
                EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition or EnemyTacticalRole.Investigate =>
                    enemy.DistanceToPlayerMeters > M137PerformanceComfortPolicy.M3CrowdedRoomCheapCommandDistanceMeters
                        ? EnemyTacticalRole.Waiting
                        : EnemyTacticalRole.Hold,
                _ => role
            };
        }

        private static bool ShouldUseCrowdSupportReservation(
            EnemyRuntimeController enemy,
            EnemyTacticalRole role,
            int budgetUsed)
        {
            if (enemy == null ||
                role is EnemyTacticalRole.ActiveThreat or
                    EnemyTacticalRole.Waiting or
                    EnemyTacticalRole.Hold or
                    EnemyTacticalRole.StationarySentinel or
                    EnemyTacticalRole.None ||
                budgetUsed >= M137PerformanceComfortPolicy.M3CrowdedRoomSupportReservationBudgetPerTick)
            {
                return false;
            }

            return enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                enemy.IsEndangeredNow ||
                enemy.DistanceToPlayerMeters <= M137PerformanceComfortPolicy.M3CrowdedRoomProtectResponsivenessDistanceMeters;
        }

        private static bool TryBuildBossRoomSignature(
            IReadOnlyList<EnemyRuntimeController> enemies,
            PlaceholderPlayerController player,
            out int signature,
            out int livingAddCount)
        {
            signature = 0;
            livingAddCount = 0;
            if (enemies == null || player == null)
            {
                return false;
            }

            var bossPresent = false;
            unchecked
            {
                var hash = 17;
                hash = AppendQuantizedPosition(hash, player.transform.localPosition);
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (enemy == null || !enemy.IsAlive)
                    {
                        continue;
                    }

                    if (enemy.BossDefinition != null || enemy.ArchetypeId == EnemyArchetypeId.Boss)
                    {
                        bossPresent = true;
                        continue;
                    }

                    livingAddCount++;
                    hash = hash * 31 + enemy.GetInstanceID();
                    hash = AppendQuantizedPosition(hash, enemy.transform.localPosition);
                    hash = hash * 31 + (int)enemy.CurrentAiLodTier;
                    hash = hash * 31 + (int)enemy.AwarenessState;
                    hash = hash * 31 + StableStringHash(enemy.AiBlackboard.ChosenActionId);
                }

                signature = hash * 31 + livingAddCount;
            }

            return bossPresent;
        }

        private static bool TryBuildCrowdedRoomSignature(
            IReadOnlyList<EnemyRuntimeController> enemies,
            PlaceholderPlayerController player,
            out int signature,
            out int livingEnemyCount)
        {
            signature = 0;
            livingEnemyCount = 0;
            if (enemies == null || player == null)
            {
                return false;
            }

            unchecked
            {
                var hash = 29;
                hash = AppendQuantizedPosition(hash, player.transform.localPosition);
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (enemy == null || !enemy.IsAlive)
                    {
                        continue;
                    }

                    if (enemy.BossDefinition != null || enemy.ArchetypeId == EnemyArchetypeId.Boss)
                    {
                        return false;
                    }

                    livingEnemyCount++;
                    hash = hash * 31 + enemy.GetInstanceID();
                    hash = AppendQuantizedPosition(hash, enemy.transform.localPosition);
                    hash = hash * 31 + (int)enemy.CurrentAiLodTier;
                    hash = hash * 31 + (int)enemy.ReadabilityState;
                    hash = hash * 31 + (int)enemy.AwarenessState;
                    hash = hash * 31 + StableStringHash(enemy.AiBlackboard.ChosenActionId);
                }

                signature = hash * 31 + livingEnemyCount;
            }

            return livingEnemyCount >= M137PerformanceComfortPolicy.M3CrowdedRoomEnemyThreshold;
        }

        private static int AppendQuantizedPosition(int hash, Vector3 position)
        {
            unchecked
            {
                hash = hash * 31 + Mathf.RoundToInt(position.x);
                hash = hash * 31 + Mathf.RoundToInt(position.z);
                return hash;
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                var hash = 23;
                for (var index = 0; index < value.Length; index++)
                {
                    hash = hash * 31 + value[index];
                }

                return hash;
            }
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
            out Vector3 reserved,
            out EnemyPathStatus reservationPathStatus,
            out int reservationPathCornerCount,
            out float reservationPathLengthMeters,
            out string reservationReason,
            bool respectExistingReservations = true,
            bool recordReservation = true,
            bool compactReservationMode = false)
        {
            reserved = enemy != null ? enemy.transform.localPosition : Vector3.zero;
            reservationPathStatus = EnemyPathStatus.NotRequested;
            reservationPathCornerCount = 0;
            reservationPathLengthMeters = 0f;
            reservationReason = string.Empty;
            if (enemy == null || room == null || player == null || role is EnemyTacticalRole.Hold or EnemyTacticalRole.Waiting or EnemyTacticalRole.StationarySentinel)
            {
                reservationReason = "reservation_not_needed";
                return false;
            }

            M136PerformanceOperationCounters.ReportTacticalReservationAttempt();

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
            var bestScore = float.NegativeInfinity;
            var best = reserved;
            var bestPathStatus = EnemyPathStatus.NotRequested;
            var bestCornerCount = 0;
            var bestPathLength = 0f;
            var bestReason = string.Empty;
            var distanceCandidateCount = compactReservationMode ? 2 : 4;
            var angleOffsets = compactReservationMode ? BossReservationAngleOffsets : ReservationAngleOffsets;
            for (var distanceIndex = 0; distanceIndex < distanceCandidateCount; distanceIndex++)
            {
                var candidateDistance = distanceIndex switch
                {
                    1 => desiredDistance + 0.45f,
                    2 => Mathf.Max(0.35f, desiredDistance - 0.35f),
                    3 => desiredDistance + 0.9f,
                    _ => desiredDistance
                };
                for (var angleIndex = 0; angleIndex < angleOffsets.Length; angleIndex++)
                {
                    M136PerformanceOperationCounters.ReportTacticalReservationCandidateChecked();
                    var angle = primaryAngle + angleOffsets[angleIndex];
                    var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    var candidate = anchor + direction.normalized * candidateDistance;
                    candidate.y = enemy.transform.localPosition.y;
                    if (!TryResolveReachableReservation(
                            room,
                            enemy.transform.localPosition,
                            candidate,
                            enemy.RadiusMeters,
                            out var reachableCandidate,
                            out var pathStatus,
                            out var cornerCount,
                            out var pathLength,
                            out var reachReason,
                            GetReservationPath(),
                            reportTacticalPathSolve: true))
                    {
                        reservationReason = reachReason;
                        continue;
                    }

                    if (respectExistingReservations && IsTooCloseToReserved(reachableCandidate, enemy.RadiusMeters + 0.42f))
                    {
                        reservationReason = "reservation_collision";
                        continue;
                    }

                    if (!HasClearAttackReservation(enemy, room, player, actionId, reachableCandidate, out var attackReachReason))
                    {
                        reservationReason = attackReachReason;
                        continue;
                    }

                    if (compactReservationMode)
                    {
                        reserved = reachableCandidate;
                        reservationPathStatus = pathStatus;
                        reservationPathCornerCount = cornerCount;
                        reservationPathLengthMeters = pathLength;
                        reservationReason = reachReason;
                        if (recordReservation)
                        {
                            reservedPositions.Add(reachableCandidate);
                        }

                        return true;
                    }

                    var score = ClearanceScore(room, reachableCandidate, enemy.RadiusMeters) * 0.35f
                        - Mathf.Abs(candidateDistance - desiredDistance) * 0.75f
                        - Vector3.Distance(Flat(reachableCandidate), Flat(enemy.transform.localPosition)) * 0.04f
                        - pathLength * 0.025f
                        - angleIndex * 0.02f
                        + (pathStatus == EnemyPathStatus.Ready ? 0.5f : 0f);
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    best = reachableCandidate;
                    bestPathStatus = pathStatus;
                    bestCornerCount = cornerCount;
                    bestPathLength = pathLength;
                    bestReason = reachReason;
                }
            }

            if (bestScore <= float.NegativeInfinity)
            {
                reservationReason = string.IsNullOrWhiteSpace(reservationReason) ? "no_reachable_navmesh_candidate" : reservationReason;
                return false;
            }

            reserved = best;
            reservationPathStatus = bestPathStatus;
            reservationPathCornerCount = bestCornerCount;
            reservationPathLengthMeters = bestPathLength;
            reservationReason = bestReason;
            if (recordReservation)
            {
                reservedPositions.Add(best);
            }

            return true;
        }

        private static bool HasClearAttackReservation(
            EnemyRuntimeController enemy,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            string actionId,
            Vector3 candidateLocalPosition,
            out string reason)
        {
            reason = string.Empty;
            if (enemy == null || room == null || player == null || string.IsNullOrWhiteSpace(actionId))
            {
                return true;
            }

            var attack = enemy.ResolveAttackProfileForAi(actionId);
            if (attack == null)
            {
                return true;
            }

            var result = EnemyAttackReachabilityService.Evaluate(
                room,
                candidateLocalPosition,
                player.transform.localPosition,
                enemy.RadiusMeters,
                PlaceholderPlayerController.DefaultRadiusMeters,
                attack,
                attack.RuntimeKind,
                canReposition: false);
            if (result.CanCommit)
            {
                reason = "attack_line_clear";
                return true;
            }

            reason = $"no_clear_attack_line:{result.Reason}";
            return false;
        }

        private NavMeshPath GetReservationPath()
        {
            reservationPath ??= new NavMeshPath();
            return reservationPath;
        }

        public static bool TryResolveReachableReservation(
            RoomRuntimeRoot room,
            Vector3 currentLocalPosition,
            Vector3 candidateLocalPosition,
            float radiusMeters,
            out Vector3 resolvedLocalPosition,
            out EnemyPathStatus pathStatus,
            out int pathCornerCount,
            out float pathLengthMeters,
            out string reason,
            NavMeshPath reusablePath = null,
            bool reportTacticalPathSolve = false)
        {
            resolvedLocalPosition = candidateLocalPosition;
            pathStatus = EnemyPathStatus.NotRequested;
            pathCornerCount = 0;
            pathLengthMeters = 0f;
            reason = string.Empty;

            if (room == null)
            {
                reason = "missing_room";
                pathStatus = EnemyPathStatus.InvalidRequest;
                return false;
            }

            if (!room.HasNavMeshBake)
            {
                reason = string.IsNullOrWhiteSpace(room.NavMeshBakeError) ? "missing_navmesh_bake" : room.NavMeshBakeError;
                pathStatus = EnemyPathStatus.InvalidRequest;
                return false;
            }

            var areaMask = WalkableAreaMask();
            var startWorld = room.transform.TransformPoint(currentLocalPosition);
            if (!NavMesh.SamplePosition(startWorld, out var startHit, ReservationNavMeshSampleRadiusMeters, areaMask))
            {
                reason = "start_not_on_navmesh";
                pathStatus = EnemyPathStatus.InvalidRequest;
                return false;
            }

            var candidateWorld = room.transform.TransformPoint(candidateLocalPosition);
            if (!NavMesh.SamplePosition(candidateWorld, out var candidateHit, ReservationNavMeshSampleRadiusMeters, areaMask))
            {
                reason = "candidate_not_on_navmesh";
                pathStatus = EnemyPathStatus.Unreachable;
                return false;
            }

            resolvedLocalPosition = room.transform.InverseTransformPoint(candidateHit.position);
            resolvedLocalPosition.y = candidateLocalPosition.y;
            if (!RoomLocalCollision.CanOccupy(room, resolvedLocalPosition, radiusMeters))
            {
                reason = "candidate_blocked";
                pathStatus = EnemyPathStatus.Unreachable;
                return false;
            }

            var path = reusablePath ?? new NavMeshPath();
            path.ClearCorners();
            if (reportTacticalPathSolve)
            {
                M136PerformanceOperationCounters.ReportTacticalReservationPathSolve();
            }

            if (!NavMesh.CalculatePath(startHit.position, candidateHit.position, areaMask, path))
            {
                reason = "navmesh_path_failed";
                pathStatus = EnemyPathStatus.Unreachable;
                return false;
            }

            pathStatus = MapReservationPathStatus(path.status);
            pathCornerCount = path.corners != null ? path.corners.Length : 0;
            pathLengthMeters = PathLength(path.corners);
            if (pathStatus != EnemyPathStatus.Ready)
            {
                reason = pathStatus == EnemyPathStatus.Partial ? "navmesh_path_partial" : "navmesh_path_unreachable";
                return false;
            }

            reason = "navmesh_reachable";
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

            for (var index = 0; index < ClearanceSampleDirections.Length; index++)
            {
                var sample = localPosition + ClearanceSampleDirections[index] * Mathf.Max(0.5f, radius + 0.25f);
                if (RoomLocalCollision.CanOccupy(room, sample, radius))
                {
                    score += 1f;
                }
            }

            return score;
        }

        private static EnemyPathStatus MapReservationPathStatus(NavMeshPathStatus status)
        {
            return status switch
            {
                NavMeshPathStatus.PathComplete => EnemyPathStatus.Ready,
                NavMeshPathStatus.PathPartial => EnemyPathStatus.Partial,
                NavMeshPathStatus.PathInvalid => EnemyPathStatus.Unreachable,
                _ => EnemyPathStatus.InvalidRequest
            };
        }

        private static float PathLength(Vector3[] corners)
        {
            if (corners == null || corners.Length < 2)
            {
                return 0f;
            }

            var length = 0f;
            for (var index = 1; index < corners.Length; index++)
            {
                length += Vector3.Distance(Flat(corners[index - 1]), Flat(corners[index]));
            }

            return length;
        }

        private static int WalkableAreaMask()
        {
            var notWalkable = NavMesh.GetAreaFromName("Not Walkable");
            return notWalkable >= 0 ? NavMesh.AllAreas & ~(1 << notWalkable) : NavMesh.AllAreas;
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
