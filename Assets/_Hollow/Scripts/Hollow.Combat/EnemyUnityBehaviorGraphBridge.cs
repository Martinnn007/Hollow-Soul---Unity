using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

namespace Hollow.Combat
{
    [DisallowMultipleComponent]
    public sealed class EnemyUnityBehaviorGraphBridge : MonoBehaviour
    {
        public const string InputDistanceToPlayer = EnemyUnityBehaviorBlackboardSchema.InputDistanceToPlayer;
        public const string InputAwareness = EnemyUnityBehaviorBlackboardSchema.InputAwareness;
        public const string InputDisposition = EnemyUnityBehaviorBlackboardSchema.InputDisposition;
        public const string InputEndangered = EnemyUnityBehaviorBlackboardSchema.InputEndangered;
        public const string InputIdle = EnemyUnityBehaviorBlackboardSchema.InputIdle;
        public const string InputTacticalRole = EnemyUnityBehaviorBlackboardSchema.InputTacticalRole;
        public const string InputPathStatus = EnemyUnityBehaviorBlackboardSchema.InputPathStatus;
        public const string OutputCommandKind = EnemyUnityBehaviorBlackboardSchema.OutputCommandKind;
        public const string OutputActionId = EnemyUnityBehaviorBlackboardSchema.OutputActionId;
        public const string OutputSpeedMultiplier = EnemyUnityBehaviorBlackboardSchema.OutputSpeedMultiplier;
        public const string OutputReason = EnemyUnityBehaviorBlackboardSchema.OutputReason;

        private const int MaxTraceEntries = 24;

        [SerializeField] private EnemyUnityBehaviorPilotGraphDefinition pilotGraphDefinition;
        [SerializeField] private BehaviorGraph behaviorGraph;

        private BehaviorGraphAgent graphAgent;
        private EnemyRuntimeController enemy;
        private bool graphStarted;
        private bool graphOutputDirty;
        private readonly List<EnemyUnityBehaviorTraceEntry> traceHistory = new();

        public EnemyUnityBehaviorBlackboard CurrentBlackboard { get; private set; }

        public EnemyBehaviorCommand LastCommand { get; private set; } = EnemyBehaviorCommand.None("unity_behavior_uninitialized");

        public string LastEvaluationReason { get; private set; } = "unity_behavior_uninitialized";

        public string LastOfficialGraphFailureReason { get; private set; } = "unity_behavior_uninitialized";

        public bool UsedEmergencyFallbackLastEvaluation { get; private set; }

        public bool HasOfficialGraph => behaviorGraph != null;

        public bool OfficialGraphReady => graphAgent != null &&
            EnemyUnityBehaviorBlackboardSchema.TryValidateOfficialGraph(behaviorGraph, pilotGraphDefinition, out _);

        public EnemyUnityBehaviorPilotGraphDefinition PilotGraphDefinition => pilotGraphDefinition;

        public IReadOnlyList<EnemyUnityBehaviorTraceEntry> TraceHistory => traceHistory;

        public string LastTraceSummary => traceHistory.Count == 0 ? string.Empty : traceHistory[traceHistory.Count - 1].Summary;

        public void Configure(EnemyRuntimeController nextEnemy, EnemyUnityBehaviorPilotGraphDefinition nextPilotGraphDefinition)
        {
            enemy = nextEnemy;
            pilotGraphDefinition = nextPilotGraphDefinition;
            behaviorGraph = pilotGraphDefinition != null ? pilotGraphDefinition.BehaviorGraph : null;
            graphStarted = false;
            graphOutputDirty = false;
            UsedEmergencyFallbackLastEvaluation = false;
            traceHistory.Clear();

            if (EnemyUnityBehaviorBlackboardSchema.TryValidateOfficialGraph(behaviorGraph, pilotGraphDefinition, out var graphFailureReason))
            {
                graphAgent = GetComponent<BehaviorGraphAgent>() ?? gameObject.AddComponent<BehaviorGraphAgent>();
                graphAgent.enabled = false;
                graphAgent.Graph = behaviorGraph;
                graphAgent.Init();
                LastOfficialGraphFailureReason = string.Empty;
            }
            else
            {
                LastOfficialGraphFailureReason = graphFailureReason;
                if (graphAgent != null)
                {
                    graphAgent.enabled = false;
                }
            }
        }

        public void DisableBridge()
        {
            if (graphAgent != null)
            {
                graphAgent.enabled = false;
            }

            enemy = null;
            pilotGraphDefinition = null;
            behaviorGraph = null;
            graphStarted = false;
            graphOutputDirty = false;
            LastCommand = EnemyBehaviorCommand.None("unity_behavior_disabled");
            LastEvaluationReason = "unity_behavior_disabled";
            LastOfficialGraphFailureReason = "unity_behavior_disabled";
            UsedEmergencyFallbackLastEvaluation = false;
            traceHistory.Clear();
        }

        public bool TryEvaluate(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command)
        {
            CurrentBlackboard = EnemyUnityBehaviorBlackboard.FromContext(
                context,
                context.Enemy != null ? context.Enemy.LastTacticalIntent.Role : EnemyTacticalRole.None,
                context.Enemy != null ? context.Enemy.LastNavigationPathStatus : EnemyPathStatus.NotRequested);

            LastEvaluationReason = "unity_behavior_no_output";
            LastOfficialGraphFailureReason = string.Empty;
            UsedEmergencyFallbackLastEvaluation = false;

            if (enemy == null || !context.IsIdle)
            {
                command = EnemyBehaviorCommand.None("unity_behavior_not_idle");
                LastCommand = command;
                LastEvaluationReason = command.Reason;
                AppendTrace("idle_gate", command, command.Reason);
                EnemyAiDebugOverlay.RecordBehaviorGraphTick(usedEmergencyFallback: false);
                return true;
            }

            var graphProducedCommand = TryRunOfficialGraph(context, out command, out var graphFailureReason);
            if (!graphProducedCommand)
            {
                LastOfficialGraphFailureReason = graphFailureReason;
                if (!AllowsEmergencyFallback())
                {
                    command = EnemyBehaviorCommand.None($"unity_behavior_unavailable:{graphFailureReason}");
                    LastCommand = command;
                    LastEvaluationReason = command.Reason;
                    AppendTrace("unavailable", command, command.Reason);
                    EnemyAiDebugOverlay.RecordBehaviorGraphTick(usedEmergencyFallback: false);
                    return false;
                }

                command = EnemyUnityBehaviorPilotEvaluator.Evaluate(pilotGraphDefinition, context);
                UsedEmergencyFallbackLastEvaluation = true;
                EnemyAiDebugOverlay.RecordBehaviorGraphTick(usedEmergencyFallback: true);
                LastEvaluationReason = string.IsNullOrWhiteSpace(command.Reason)
                    ? $"unity_behavior_emergency_fallback:{graphFailureReason}"
                    : $"unity_behavior_emergency_fallback:{graphFailureReason}:{command.Reason}";
                AppendTrace("emergency_fallback", command, LastEvaluationReason);
            }
            else
            {
                AppendTrace("official_graph", command, LastEvaluationReason);
                EnemyAiDebugOverlay.RecordBehaviorGraphTick(usedEmergencyFallback: false);
            }

            LastCommand = command;
            return true;
        }

        public void SetOutputCommand(EnemyBehaviorCommandKind commandKind, string actionId, float speedMultiplier, string reason)
        {
            LastCommand = new EnemyBehaviorCommand(
                commandKind,
                actionId,
                Mathf.Max(0f, speedMultiplier),
                string.IsNullOrWhiteSpace(reason) ? "unity_behavior_node_output" : reason);
            LastEvaluationReason = LastCommand.Reason;
            graphOutputDirty = true;
        }

        public bool CanStartCommand(EnemyBehaviorCommandKind commandKind, string actionId)
        {
            return enemy != null && enemy.CanStartBehaviorCommand(commandKind, actionId, CurrentBlackboard.TimeSeconds);
        }

        public bool IsInActionRange(string actionId)
        {
            if (enemy == null || string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            var spacing = enemy.ResolveActionSpacingForTacticalIntent(actionId);
            return spacing.IsInsideEnvelope(CurrentBlackboard.DistanceToPlayer);
        }

        private bool TryRunOfficialGraph(EnemyBehaviorTreeContext context, out EnemyBehaviorCommand command, out string failureReason)
        {
            command = EnemyBehaviorCommand.None("unity_behavior_no_graph");
            failureReason = string.Empty;
            if (!EnemyUnityBehaviorBlackboardSchema.TryValidateOfficialGraph(behaviorGraph, pilotGraphDefinition, out failureReason) ||
                graphAgent == null)
            {
                if (string.IsNullOrWhiteSpace(failureReason))
                {
                    failureReason = "unity_behavior_agent_missing";
                }

                return false;
            }

            try
            {
                FeedBlackboard(context);
                var wasEnabled = graphAgent.enabled;
                graphAgent.enabled = true;
                if (!graphStarted)
                {
                    graphAgent.Restart();
                    graphStarted = true;
                }

                graphAgent.Update();
                graphAgent.enabled = wasEnabled;

                if (graphOutputDirty && LastCommand.Kind != EnemyBehaviorCommandKind.None)
                {
                    command = LastCommand;
                    graphOutputDirty = false;
                    LastEvaluationReason = string.IsNullOrWhiteSpace(command.Reason)
                        ? "unity_behavior_node_output"
                        : command.Reason;
                    return true;
                }

                var readOutput = TryReadOutputVariables(out command);
                if (!readOutput)
                {
                    failureReason = command.Reason;
                }

                return readOutput;
            }
            catch (Exception exception)
            {
                command = EnemyBehaviorCommand.None($"unity_behavior_graph_error:{exception.GetType().Name}");
                LastEvaluationReason = command.Reason;
                failureReason = command.Reason;
                if (graphAgent != null)
                {
                    graphAgent.enabled = false;
                }

                return false;
            }
        }

        private void FeedBlackboard(EnemyBehaviorTreeContext context)
        {
            graphOutputDirty = false;
            graphAgent.SetVariableValue(EnemyUnityBehaviorBlackboardSchema.InputEnemy, enemy);
            graphAgent.SetVariableValue(EnemyUnityBehaviorBlackboardSchema.InputPlayer, CurrentBlackboard.Player);
            graphAgent.SetVariableValue(InputDistanceToPlayer, context.DistanceToPlayer);
            graphAgent.SetVariableValue(InputAwareness, (int)context.Awareness);
            graphAgent.SetVariableValue(InputDisposition, (int)context.Disposition);
            graphAgent.SetVariableValue(InputEndangered, context.IsEndangered);
            graphAgent.SetVariableValue(InputIdle, context.IsIdle);
            graphAgent.SetVariableValue(InputTacticalRole, (int)CurrentBlackboard.TacticalRole);
            graphAgent.SetVariableValue(InputPathStatus, (int)CurrentBlackboard.PathStatus);
            graphAgent.SetVariableValue(EnemyUnityBehaviorBlackboardSchema.InputTimeSeconds, context.TimeSeconds);
            graphAgent.SetVariableValue(EnemyUnityBehaviorBlackboardSchema.InputDeltaTime, context.DeltaTime);
            graphAgent.SetVariableValue(OutputCommandKind, (int)EnemyBehaviorCommandKind.None);
            graphAgent.SetVariableValue(OutputActionId, string.Empty);
            graphAgent.SetVariableValue(OutputSpeedMultiplier, 1f);
            graphAgent.SetVariableValue(OutputReason, "unity_behavior_no_output");
        }

        private bool TryReadOutputVariables(out EnemyBehaviorCommand command)
        {
            command = EnemyBehaviorCommand.None("unity_behavior_missing_output_vars");
            if (!graphAgent.GetVariable<int>(OutputCommandKind, out var kindVariable))
            {
                return false;
            }

            var kind = (EnemyBehaviorCommandKind)Mathf.Clamp(
                kindVariable.Value,
                (int)EnemyBehaviorCommandKind.None,
                (int)EnemyBehaviorCommandKind.StartCreatureSignalAction);
            if (kind == EnemyBehaviorCommandKind.None)
            {
                return false;
            }

            var actionId = graphAgent.GetVariable<string>(OutputActionId, out var actionVariable)
                ? actionVariable.Value
                : string.Empty;
            var speed = graphAgent.GetVariable<float>(OutputSpeedMultiplier, out var speedVariable)
                ? Mathf.Max(0f, speedVariable.Value)
                : 1f;
            var reason = graphAgent.GetVariable<string>(OutputReason, out var reasonVariable)
                ? reasonVariable.Value
                : "unity_behavior_blackboard_output";
            command = new EnemyBehaviorCommand(kind, actionId, speed, reason);
            LastEvaluationReason = reason;
            return true;
        }

        private bool AllowsEmergencyFallback()
        {
            return pilotGraphDefinition == null ||
                pilotGraphDefinition.FallbackPolicy == EnemyUnityBehaviorFallbackPolicy.EmergencyOnly;
        }

        private void AppendTrace(string source, EnemyBehaviorCommand command, string reason)
        {
            var graphId = pilotGraphDefinition != null ? pilotGraphDefinition.GraphId : string.Empty;
            var pilotKind = pilotGraphDefinition != null ? pilotGraphDefinition.PilotKind : EnemyUnityBehaviorPilotKind.None;
            traceHistory.Add(new EnemyUnityBehaviorTraceEntry(
                CurrentBlackboard.TimeSeconds,
                graphId,
                pilotKind,
                source,
                command.Kind,
                command.ActionId,
                reason,
                CurrentBlackboard.DistanceToPlayer,
                CurrentBlackboard.Awareness,
                CurrentBlackboard.PathStatus));

            while (traceHistory.Count > MaxTraceEntries)
            {
                traceHistory.RemoveAt(0);
            }
        }
    }
}
