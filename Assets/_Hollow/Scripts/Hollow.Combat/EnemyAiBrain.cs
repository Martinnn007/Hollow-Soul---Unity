using UnityEngine;
using Hollow.Core.Diagnostics;

namespace Hollow.Combat
{
    public sealed class EnemyAiBrain
    {
        private EnemyBehaviorCommand cachedCommand = EnemyBehaviorCommand.None("ai_uninitialized");
        private float nextThinkTime;
        private string topScores = string.Empty;

        public EnemyAiLodTier LodTier { get; private set; } = EnemyAiLodTier.Full;

        public EnemyAiBlackboard Blackboard { get; private set; } = EnemyAiBlackboard.Empty;

        public void Reset()
        {
            cachedCommand = EnemyBehaviorCommand.None("ai_reset");
            nextThinkTime = 0f;
            topScores = string.Empty;
            LodTier = EnemyAiLodTier.Full;
            Blackboard = EnemyAiBlackboard.Empty;
        }

        public bool TryReuseCommand(
            EnemyRuntimeController enemy,
            float timeSeconds,
            float distanceToPlayer,
            out EnemyBehaviorCommand command)
        {
            command = EnemyBehaviorCommand.None("ai_no_cached_command");
            SetLodTier(ResolveLodTier(enemy, distanceToPlayer));
            if (enemy != null)
            {
                EnemyAiDebugOverlay.ReportBrainAgent(enemy.GetInstanceID(), LodTier);
            }

            if (enemy == null ||
                enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                cachedCommand.Kind == EnemyBehaviorCommandKind.None ||
                cachedCommand.StartsCommittedAction)
            {
                return false;
            }

            if (timeSeconds >= nextThinkTime)
            {
                return false;
            }

            command = SimplifyForLod(cachedCommand, enemy);
            UpdateBlackboard(enemy, cachedCommand, command, distanceToPlayer, 0f, "cached_plan");
            EnemyAiDebugOverlay.RecordCommandReuse(enemy.GetInstanceID(), LodTier);
            return true;
        }

        public EnemyBehaviorCommand ChooseCommand(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand treeCommand,
            float timeSeconds,
            float distanceToPlayer,
            RoomThreatDirector threatDirector)
        {
            SetLodTier(ResolveLodTier(enemy, distanceToPlayer));
            var thinkInterval = ThinkInterval(enemy, LodTier);
            if (!EnemyAiThinkBudget.TryAcquireThink(enemy, LodTier))
            {
                var fallback = cachedCommand.Kind == EnemyBehaviorCommandKind.None
                    ? SimplifyForLod(treeCommand, enemy)
                    : SimplifyForLod(cachedCommand, enemy);
                nextThinkTime = timeSeconds + 0.025f + ThinkJitter(enemy) * 0.5f;
                UpdateBlackboard(enemy, treeCommand, fallback, distanceToPlayer, 0f, "ai_think_budget_deferred");
                if (enemy != null)
                {
                    EnemyAiDebugOverlay.RecordCommandReuse(enemy.GetInstanceID(), LodTier);
                }

                return fallback;
            }

            nextThinkTime = timeSeconds + thinkInterval + ThinkJitter(enemy);
            if (enemy != null)
            {
                EnemyAiDebugOverlay.RecordBrainThink(enemy.GetInstanceID(), LodTier, thinkInterval);
                M136PerformanceOperationCounters.ReportAiThink((int)LodTier);
            }

            var chosen = SimplifyForLod(treeCommand, enemy);
            var pressurePenalty = 0f;
            var cooldownReason = "tree";
            topScores = string.Empty;

            if (TryResolveBossRoomNonActiveAddCommand(enemy, chosen, distanceToPlayer, out var bossAddCommand, out var bossAddReason))
            {
                cachedCommand = bossAddCommand;
                M136PerformanceOperationCounters.ReportBossAddScorerSkip();
                M136PerformanceOperationCounters.ReportBossAddCachedCommandReuse();
                if (enemy != null)
                {
                    EnemyAiDebugOverlay.RecordCommandReuse(enemy.GetInstanceID(), LodTier);
                }

                UpdateBlackboard(enemy, treeCommand, bossAddCommand, distanceToPlayer, 0f, bossAddReason);
                return bossAddCommand;
            }

            if (TryResolveCrowdedRoomNonActiveCommand(enemy, chosen, distanceToPlayer, out var crowdCommand, out var crowdReason))
            {
                cachedCommand = crowdCommand;
                M136PerformanceOperationCounters.ReportTacticalCrowdScorerSkip();
                M136PerformanceOperationCounters.ReportTacticalCrowdCachedIntentReuse();
                if (enemy != null)
                {
                    EnemyAiDebugOverlay.RecordCommandReuse(enemy.GetInstanceID(), LodTier);
                }

                UpdateBlackboard(enemy, treeCommand, crowdCommand, distanceToPlayer, 0f, crowdReason);
                return crowdCommand;
            }

            var scorerBudgetAllowed = !chosen.StartsCommittedAction ||
                LodTier == EnemyAiLodTier.Background ||
                EnemyAiScorerBudget.TryAcquireScorer(enemy);

            if (chosen.StartsCommittedAction &&
                LodTier != EnemyAiLodTier.Background &&
                scorerBudgetAllowed &&
                EnemyActionScorer.TryChooseAction(enemy, chosen, timeSeconds, distanceToPlayer, threatDirector, out var scored, out topScores))
            {
                chosen = new EnemyBehaviorCommand(scored.CommandKind, scored.ActionId, Mathf.Max(0.1f, treeCommand.SpeedMultiplier), "ai_scorer");
                pressurePenalty = scored.PressurePenalty;
                cooldownReason = scored.Reason;
                EnemyAiDebugOverlay.RecordPressurePenalty(pressurePenalty);
            }
            else if (chosen.StartsCommittedAction && LodTier == EnemyAiLodTier.Background)
            {
                chosen = new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "ai_background_hold");
                cooldownReason = "background_lod";
            }
            else if (chosen.StartsCommittedAction && !scorerBudgetAllowed)
            {
                if (IsBossRoomAdd(enemy))
                {
                    M136PerformanceOperationCounters.ReportBossAddScorerSkip();
                }

                chosen = enemy != null && enemy.LastTacticalIntent.Role == EnemyTacticalRole.ActiveThreat
                    ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, Mathf.Max(0.45f, treeCommand.SpeedMultiplier), "ai_scorer_budget_reposition")
                    : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "ai_scorer_budget_face");
                cooldownReason = "ai_scorer_budget_deferred";
            }
            else if (chosen.StartsCommittedAction)
            {
                cooldownReason = string.IsNullOrWhiteSpace(topScores) ? "no_scorer_candidate" : topScores;
            }

            cachedCommand = chosen;
            UpdateBlackboard(enemy, treeCommand, chosen, distanceToPlayer, pressurePenalty, cooldownReason);
            return chosen;
        }

        public bool TryResolveBossRoomCachedAddCommand(
            EnemyRuntimeController enemy,
            float timeSeconds,
            float distanceToPlayer,
            out EnemyBehaviorCommand command)
        {
            command = EnemyBehaviorCommand.None("boss_add_cached_command_unavailable");
            SetLodTier(ResolveLodTier(enemy, distanceToPlayer));
            if (!TryResolveBossRoomNonActiveAddCommand(
                    enemy,
                    EnemyBehaviorCommand.None("boss_add_cached_before_graph"),
                    distanceToPlayer,
                    out var bossAddCommand,
                    out var bossAddReason))
            {
                return false;
            }

            cachedCommand = bossAddCommand;
            nextThinkTime = timeSeconds + ThinkInterval(enemy, LodTier) + ThinkJitter(enemy);
            M136PerformanceOperationCounters.ReportBossAddScorerSkip();
            M136PerformanceOperationCounters.ReportBossAddCachedCommandReuse();
            if (enemy != null)
            {
                EnemyAiDebugOverlay.RecordCommandReuse(enemy.GetInstanceID(), LodTier);
            }

            UpdateBlackboard(enemy, EnemyBehaviorCommand.None("boss_add_cached_before_graph"), bossAddCommand, distanceToPlayer, 0f, bossAddReason);
            command = bossAddCommand;
            return true;
        }

        public bool TryResolveCrowdedRoomCachedCommand(
            EnemyRuntimeController enemy,
            float timeSeconds,
            float distanceToPlayer,
            out EnemyBehaviorCommand command)
        {
            command = EnemyBehaviorCommand.None("crowd_cached_command_unavailable");
            SetLodTier(ResolveLodTier(enemy, distanceToPlayer));
            if (!TryResolveCrowdedRoomNonActiveCommand(
                    enemy,
                    EnemyBehaviorCommand.None("crowd_cached_before_graph"),
                    distanceToPlayer,
                    out var crowdCommand,
                    out var crowdReason))
            {
                return false;
            }

            cachedCommand = crowdCommand;
            nextThinkTime = timeSeconds + ThinkInterval(enemy, LodTier) + ThinkJitter(enemy);
            M136PerformanceOperationCounters.ReportTacticalCrowdScorerSkip();
            M136PerformanceOperationCounters.ReportTacticalCrowdCachedIntentReuse();
            if (enemy != null)
            {
                EnemyAiDebugOverlay.RecordCommandReuse(enemy.GetInstanceID(), LodTier);
            }

            UpdateBlackboard(enemy, EnemyBehaviorCommand.None("crowd_cached_before_graph"), crowdCommand, distanceToPlayer, 0f, crowdReason);
            command = crowdCommand;
            return true;
        }

        public static EnemyAiLodTier ResolveLodTier(EnemyRuntimeController enemy, float distanceToPlayer)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return EnemyAiLodTier.Background;
            }

            if (enemy.BossDefinition != null)
            {
                return EnemyAiLodTier.Full;
            }

            if (enemy.RoomHasActiveBoss)
            {
                if (enemy.LastTacticalIntent.Role == EnemyTacticalRole.ActiveThreat)
                {
                    return distanceToPlayer <= 5.75f && enemy.IsVisibleToCamera
                        ? EnemyAiLodTier.Full
                        : EnemyAiLodTier.Reduced;
                }

                if (enemy.ReadabilityState != EnemyReadabilityState.Idle || enemy.IsEndangeredNow)
                {
                    return distanceToPlayer <= 5.25f && enemy.IsVisibleToCamera
                        ? EnemyAiLodTier.Full
                        : EnemyAiLodTier.Reduced;
                }

                if (enemy.LastTacticalIntent.Role is EnemyTacticalRole.Waiting or EnemyTacticalRole.Hold or EnemyTacticalRole.StationarySentinel or EnemyTacticalRole.None)
                {
                    return distanceToPlayer <= 4.25f && enemy.IsVisibleToCamera
                        ? EnemyAiLodTier.Reduced
                        : EnemyAiLodTier.Background;
                }

                return distanceToPlayer <= 10f || enemy.IsVisibleToCamera
                    ? EnemyAiLodTier.Reduced
                    : EnemyAiLodTier.Background;
            }

            if (IsCrowdedRoomNonActiveEnemy(enemy, distanceToPlayer))
            {
                if (enemy.LastTacticalIntent.Role is EnemyTacticalRole.Waiting or
                    EnemyTacticalRole.Hold or
                    EnemyTacticalRole.StationarySentinel or
                    EnemyTacticalRole.None)
                {
                    return distanceToPlayer <= M137PerformanceComfortPolicy.M3CrowdedRoomProtectResponsivenessDistanceMeters && enemy.IsVisibleToCamera
                        ? EnemyAiLodTier.Reduced
                        : EnemyAiLodTier.Background;
                }

                return distanceToPlayer <= M137PerformanceComfortPolicy.M3CrowdedRoomBackgroundDistanceMeters && enemy.IsVisibleToCamera
                    ? EnemyAiLodTier.Reduced
                    : EnemyAiLodTier.Background;
            }

            if (enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                enemy.IsEndangeredNow ||
                enemy.LastTacticalIntent.Role == EnemyTacticalRole.ActiveThreat ||
                distanceToPlayer <= 4.75f)
            {
                return EnemyAiLodTier.Full;
            }

            if (!enemy.IsVisibleToCamera &&
                distanceToPlayer > 8f &&
                enemy.LastTacticalIntent.Role is EnemyTacticalRole.Waiting or EnemyTacticalRole.Hold or EnemyTacticalRole.StationarySentinel or EnemyTacticalRole.None)
            {
                return EnemyAiLodTier.Background;
            }

            if (enemy.AwarenessState == EnemyAwarenessState.Engaged ||
                enemy.AwarenessState is EnemyAwarenessState.Alerted or EnemyAwarenessState.Suspicious ||
                enemy.LastTacticalIntent.Role is EnemyTacticalRole.SupportPressure or EnemyTacticalRole.Reposition or EnemyTacticalRole.Investigate ||
                distanceToPlayer <= 11f)
            {
                return EnemyAiLodTier.Reduced;
            }

            return EnemyAiLodTier.Background;
        }

        private static EnemyBehaviorCommand SimplifyForLod(EnemyBehaviorCommand command, EnemyRuntimeController enemy)
        {
            if (enemy == null)
            {
                return command;
            }

            var tier = ResolveLodTier(enemy, enemy.DistanceToPlayerMeters);
            if (tier != EnemyAiLodTier.Background)
            {
                return command;
            }

            return command.Kind switch
            {
                EnemyBehaviorCommandKind.StartMeleeAction or
                    EnemyBehaviorCommandKind.StartRangedAction or
                    EnemyBehaviorCommandKind.StartChargeAction or
                    EnemyBehaviorCommandKind.StartAreaAction or
                    EnemyBehaviorCommandKind.StartGuardAction or
                    EnemyBehaviorCommandKind.StartCreatureMoveAction or
                    EnemyBehaviorCommandKind.StartCreatureSignalAction => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "ai_background_no_commit"),
                EnemyBehaviorCommandKind.MoveToPlayer or EnemyBehaviorCommandKind.MovePreferredRange => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "ai_background_face"),
                _ => command
            };
        }

        public static bool ShouldSkipBossAddScorerForDiagnostics(
            bool roomHasActiveBoss,
            bool isBoss,
            EnemyTacticalRole tacticalRole)
        {
            return roomHasActiveBoss && !isBoss && tacticalRole != EnemyTacticalRole.ActiveThreat;
        }

        public static bool ShouldUseCrowdedRoomCheapCommandForDiagnostics(
            int activeEnemyCount,
            bool roomHasActiveBoss,
            bool isBoss,
            EnemyTacticalRole tacticalRole,
            EnemyReadabilityState readabilityState,
            bool isEndangered,
            float distanceToPlayer)
        {
            return activeEnemyCount >= M137PerformanceComfortPolicy.M3CrowdedRoomEnemyThreshold &&
                !roomHasActiveBoss &&
                !isBoss &&
                tacticalRole != EnemyTacticalRole.ActiveThreat &&
                readabilityState == EnemyReadabilityState.Idle &&
                !isEndangered &&
                distanceToPlayer > M137PerformanceComfortPolicy.M3CrowdedRoomNonActiveCloseProtectionDistanceMeters;
        }

        private static bool ShouldSkipCrowdedRoomScorerBudget(EnemyRuntimeController enemy)
        {
            return enemy != null &&
                ResolveCrowdEnemyCount(enemy) >= M137PerformanceComfortPolicy.M3CrowdedRoomEnemyThreshold &&
                !enemy.RoomHasActiveBoss &&
                !IsBossEnemy(enemy) &&
                enemy.LastTacticalIntent.Role != EnemyTacticalRole.ActiveThreat &&
                enemy.ReadabilityState == EnemyReadabilityState.Idle &&
                !enemy.IsEndangeredNow;
        }

        private static bool TryResolveBossRoomNonActiveAddCommand(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand chosen,
            float distanceToPlayer,
            out EnemyBehaviorCommand command,
            out string reason)
        {
            command = chosen;
            reason = string.Empty;
            if (enemy == null ||
                !ShouldSkipBossAddScorerForDiagnostics(enemy.RoomHasActiveBoss, IsBossEnemy(enemy), enemy.LastTacticalIntent.Role))
            {
                return false;
            }

            var role = enemy.LastTacticalIntent.Role;
            reason = $"boss_add_{role.ToString().ToLowerInvariant()}_no_scorer";
            command = role switch
            {
                EnemyTacticalRole.Flee => new EnemyBehaviorCommand(
                    EnemyBehaviorCommandKind.Flee,
                    string.Empty,
                    Mathf.Max(0.65f, chosen.SpeedMultiplier),
                    reason),
                EnemyTacticalRole.Waiting or
                    EnemyTacticalRole.Hold or
                    EnemyTacticalRole.StationarySentinel or
                    EnemyTacticalRole.None => distanceToPlayer <= 9f
                        ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, reason)
                        : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, reason),
                _ => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, reason)
            };
            return true;
        }

        private static bool TryResolveCrowdedRoomNonActiveCommand(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand chosen,
            float distanceToPlayer,
            out EnemyBehaviorCommand command,
            out string reason)
        {
            command = chosen;
            reason = string.Empty;
            if (enemy == null ||
                !ShouldUseCrowdedRoomCheapCommandForDiagnostics(
                    ResolveCrowdEnemyCount(enemy),
                    enemy.RoomHasActiveBoss,
                    IsBossEnemy(enemy),
                    enemy.LastTacticalIntent.Role,
                    enemy.ReadabilityState,
                    enemy.IsEndangeredNow,
                    distanceToPlayer))
            {
                return false;
            }

            var role = enemy.LastTacticalIntent.Role;
            reason = $"crowd_{role.ToString().ToLowerInvariant()}_cached_no_scorer";
            command = role switch
            {
                EnemyTacticalRole.Flee => new EnemyBehaviorCommand(
                    EnemyBehaviorCommandKind.Flee,
                    string.Empty,
                    Mathf.Max(0.65f, chosen.SpeedMultiplier),
                    reason),
                EnemyTacticalRole.SupportPressure or
                    EnemyTacticalRole.Reposition or
                    EnemyTacticalRole.Investigate => distanceToPlayer <= M137PerformanceComfortPolicy.M3CrowdedRoomCheapCommandDistanceMeters
                        ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, reason)
                        : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, reason),
                EnemyTacticalRole.Waiting or
                    EnemyTacticalRole.Hold or
                    EnemyTacticalRole.StationarySentinel or
                    EnemyTacticalRole.None => distanceToPlayer <= M137PerformanceComfortPolicy.M3CrowdedRoomCheapCommandDistanceMeters
                        ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, reason)
                        : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, reason),
                _ => new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, reason)
            };
            return true;
        }

        private static bool IsCrowdedRoomNonActiveEnemy(EnemyRuntimeController enemy, float distanceToPlayer)
        {
            return enemy != null &&
                ShouldUseCrowdedRoomCheapCommandForDiagnostics(
                    ResolveCrowdEnemyCount(enemy),
                    enemy.RoomHasActiveBoss,
                    IsBossEnemy(enemy),
                    enemy.LastTacticalIntent.Role,
                    enemy.ReadabilityState,
                    enemy.IsEndangeredNow,
                    distanceToPlayer);
        }

        public static float ResolveAdaptiveThinkIntervalForDiagnostics(
            EnemyIntelligenceLevel intelligence,
            EnemyAiLodTier tier,
            int activeEnemyCount,
            int pendingPathCount,
            bool protectResponsiveness)
        {
            var baseInterval = BaseThinkInterval(intelligence, tier);
            if (protectResponsiveness)
            {
                return baseInterval;
            }

            var swarmLoad = Mathf.Clamp01((Mathf.Max(0, activeEnemyCount) - 18) / 22f);
            var pathLoad = Mathf.Clamp01(Mathf.Max(0, pendingPathCount) / 16f);
            var load = Mathf.Max(swarmLoad, pathLoad * 0.75f);
            var multiplier = tier switch
            {
                EnemyAiLodTier.Full => 1f + load * 0.22f,
                EnemyAiLodTier.Reduced => 1f + load * 0.55f,
                _ => 1f + load * 0.85f
            };
            return Mathf.Min(baseInterval * multiplier, tier switch
            {
                EnemyAiLodTier.Full => 0.24f,
                EnemyAiLodTier.Reduced => 0.82f,
                _ => 1.65f
            });
        }

        private static float ThinkInterval(EnemyRuntimeController enemy, EnemyAiLodTier tier)
        {
            var intelligence = enemy != null ? enemy.Intelligence : EnemyIntelligenceLevel.Simple;
            var navStats = EnemyNavigationDebugOverlay.Stats;
            var protectResponsiveness = enemy != null &&
                (!IsBossRoomAdd(enemy) &&
                    (enemy.ReadabilityState != EnemyReadabilityState.Idle ||
                        enemy.IsEndangeredNow ||
                        enemy.DistanceToPlayerMeters <= 4.25f));
            var interval = ResolveAdaptiveThinkIntervalForDiagnostics(
                intelligence,
                tier,
                ResolveCrowdEnemyCount(enemy),
                navStats.PendingPathUsers,
                protectResponsiveness);
            if (!IsBossRoomAdd(enemy))
            {
                return interval;
            }

            var bossAddFloor = tier switch
            {
                EnemyAiLodTier.Full => 0.18f,
                EnemyAiLodTier.Reduced => 0.55f,
                _ => 1.15f
            };
            return Mathf.Max(interval, bossAddFloor);
        }

        private static bool IsBossRoomAdd(EnemyRuntimeController enemy)
        {
            return enemy != null &&
                enemy.RoomHasActiveBoss &&
                !IsBossEnemy(enemy);
        }

        private static int ResolveCrowdEnemyCount(EnemyRuntimeController enemy)
        {
            return Mathf.Max(
                enemy != null ? enemy.RoomNonBossEnemyCountEstimate : 0,
                EnemyAiDebugOverlay.EstimatedActiveAiAgents);
        }

        private static bool IsBossEnemy(EnemyRuntimeController enemy)
        {
            return enemy != null &&
                (enemy.BossDefinition != null ||
                    enemy.ArchetypeId == EnemyArchetypeId.Boss ||
                    enemy.BehaviorId == EnemyBehaviorId.BossWarden);
        }

        private static float BaseThinkInterval(EnemyIntelligenceLevel intelligence, EnemyAiLodTier tier)
        {
            return tier switch
            {
                EnemyAiLodTier.Full => intelligence is EnemyIntelligenceLevel.Tactical or EnemyIntelligenceLevel.Cunning
                    ? M137PerformanceComfortPolicy.M3FullThreatMinThinkIntervalSeconds
                    : M137PerformanceComfortPolicy.M3FullThreatMaxThinkIntervalSeconds,
                EnemyAiLodTier.Reduced => intelligence is EnemyIntelligenceLevel.Tactical or EnemyIntelligenceLevel.Cunning
                    ? M137PerformanceComfortPolicy.M3ReducedThreatMinThinkIntervalSeconds
                    : M137PerformanceComfortPolicy.M3ReducedThreatMaxThinkIntervalSeconds,
                _ => M137PerformanceComfortPolicy.M3BackgroundMaxThinkIntervalSeconds
            };
        }

        private static float ThinkJitter(EnemyRuntimeController enemy)
        {
            var seed = Mathf.Abs(enemy != null ? enemy.SpawnIndex : 0);
            return (seed % 11) * 0.011f;
        }

        private void SetLodTier(EnemyAiLodTier nextTier)
        {
            if (LodTier != nextTier)
            {
                M136PerformanceOperationCounters.ReportAiLodTransition();
            }

            LodTier = nextTier;
        }

        private static class EnemyAiThinkBudget
        {
            private static int frame = -1;
            private static int thinksUsed;
            private static int bossAddThinksUsed;

            public static bool TryAcquireThink(EnemyRuntimeController enemy, EnemyAiLodTier tier)
            {
                if (enemy != null && enemy.BossDefinition != null)
                {
                    return true;
                }

                var currentFrame = Time.frameCount;
                if (frame != currentFrame)
                {
                    frame = currentFrame;
                    thinksUsed = 0;
                    bossAddThinksUsed = 0;
                }

                if (EnemyAiBrain.IsBossRoomAdd(enemy))
                {
                    var bossAddBudget = Mathf.Max(1, M137PerformanceComfortPolicy.M3BossRoomAddThinkBudgetPerFrame);
                    if (bossAddThinksUsed >= bossAddBudget)
                    {
                        return false;
                    }

                    bossAddThinksUsed++;
                    return true;
                }

                var budget = Mathf.Max(1, M137PerformanceComfortPolicy.M3AiThinkBudgetPerFrame);
                if (thinksUsed >= budget)
                {
                    return false;
                }

                thinksUsed++;
                return true;
            }
        }

        private static class EnemyAiScorerBudget
        {
            private static int frame = -1;
            private static int generalScorersUsed;
            private static int bossAddScorersUsed;

            public static bool TryAcquireScorer(EnemyRuntimeController enemy)
            {
                if (enemy == null || enemy.BossDefinition != null)
                {
                    return true;
                }

                var currentFrame = Time.frameCount;
                if (frame != currentFrame)
                {
                    frame = currentFrame;
                    generalScorersUsed = 0;
                    bossAddScorersUsed = 0;
                }

                var budget = Mathf.Max(1, M137PerformanceComfortPolicy.M3AiThinkBudgetPerFrame);
                if (enemy.RoomHasActiveBoss)
                {
                    if (enemy.LastTacticalIntent.Role != EnemyTacticalRole.ActiveThreat)
                    {
                        return false;
                    }

                    var bossAddBudget = Mathf.Max(1, M137PerformanceComfortPolicy.M3BossRoomAddScorerBudgetPerFrame);
                    if (bossAddScorersUsed >= bossAddBudget)
                    {
                        return false;
                    }

                    bossAddScorersUsed++;
                    return true;
                }

                if (ShouldSkipCrowdedRoomScorerBudget(enemy))
                {
                    M136PerformanceOperationCounters.ReportTacticalCrowdScorerSkip();
                    return false;
                }

                if (generalScorersUsed >= budget)
                {
                    return false;
                }

                generalScorersUsed++;
                return true;
            }
        }

        private void UpdateBlackboard(
            EnemyRuntimeController enemy,
            EnemyBehaviorCommand treeCommand,
            EnemyBehaviorCommand chosenCommand,
            float distanceToPlayer,
            float pressurePenalty,
            string cooldownReason)
        {
            Blackboard = new EnemyAiBlackboard(
                LodTier,
                treeCommand.Kind,
                chosenCommand.Kind,
                chosenCommand.ActionId,
                ExtractTopScore(topScores),
                pressurePenalty,
                distanceToPlayer,
                enemy != null ? enemy.LastNavigationPathStatus : EnemyPathStatus.NotRequested,
                cooldownReason,
                topScores);
        }

        private static float ExtractTopScore(string scores)
        {
            if (string.IsNullOrWhiteSpace(scores))
            {
                return 0f;
            }

            var colon = scores.IndexOf(':');
            if (colon < 0 || colon + 1 >= scores.Length)
            {
                return 0f;
            }

            var comma = scores.IndexOf(',', colon + 1);
            var value = comma >= 0
                ? scores.Substring(colon + 1, comma - colon - 1)
                : scores.Substring(colon + 1);
            return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0f;
        }
    }
}
