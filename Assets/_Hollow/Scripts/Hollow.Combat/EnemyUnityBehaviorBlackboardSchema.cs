using System;
using System.Collections.Generic;
using Unity.Behavior;

namespace Hollow.Combat
{
    public static class EnemyUnityBehaviorBlackboardSchema
    {
        public const int SchemaVersion = 104;

        public const string InputEnemy = "Enemy";
        public const string InputPlayer = "Player";
        public const string InputDistanceToPlayer = "DistanceToPlayer";
        public const string InputAwareness = "Awareness";
        public const string InputDisposition = "Disposition";
        public const string InputEndangered = "Endangered";
        public const string InputIdle = "IsIdle";
        public const string InputTacticalRole = "TacticalRole";
        public const string InputPathStatus = "PathStatus";
        public const string InputTimeSeconds = "TimeSeconds";
        public const string InputDeltaTime = "DeltaTime";

        public const string OutputCommandKind = "OutputCommandKind";
        public const string OutputActionId = "OutputActionId";
        public const string OutputSpeedMultiplier = "OutputSpeedMultiplier";
        public const string OutputReason = "OutputReason";

        private static readonly string[] RequiredInputs =
        {
            InputDistanceToPlayer,
            InputAwareness,
            InputDisposition,
            InputEndangered,
            InputIdle,
            InputTacticalRole,
            InputPathStatus
        };

        private static readonly string[] OptionalInputs =
        {
            InputEnemy,
            InputPlayer,
            InputTimeSeconds,
            InputDeltaTime
        };

        private static readonly string[] RequiredOutputs =
        {
            OutputCommandKind,
            OutputActionId,
            OutputSpeedMultiplier,
            OutputReason
        };

        public static IReadOnlyList<string> RequiredInputNames => RequiredInputs;

        public static IReadOnlyList<string> OptionalInputNames => OptionalInputs;

        public static IReadOnlyList<string> RequiredOutputNames => RequiredOutputs;

        public static bool TryValidateDefinition(
            EnemyUnityBehaviorPilotGraphDefinition definition,
            out string reason)
        {
            reason = string.Empty;
            if (definition == null)
            {
                reason = "unity_behavior_definition_missing";
                return false;
            }

            if (definition.SchemaVersion < SchemaVersion)
            {
                reason = $"unity_behavior_schema_outdated:{definition.SchemaVersion}";
                return false;
            }

            if (!ContainsAll(definition.RequiredBlackboardInputs, RequiredInputs, out var missingInput))
            {
                reason = $"unity_behavior_schema_missing_input:{missingInput}";
                return false;
            }

            if (!ContainsAll(definition.RequiredBlackboardOutputs, RequiredOutputs, out var missingOutput))
            {
                reason = $"unity_behavior_schema_missing_output:{missingOutput}";
                return false;
            }

            return true;
        }

        public static bool TryValidateOfficialGraph(
            BehaviorGraph graph,
            EnemyUnityBehaviorPilotGraphDefinition definition,
            out string reason)
        {
            if (!TryValidateDefinition(definition, out reason))
            {
                return false;
            }

            if (graph == null)
            {
                reason = "unity_behavior_graph_missing";
                return false;
            }

            var blackboard = graph.BlackboardReference;
            if (blackboard == null)
            {
                reason = "unity_behavior_graph_uncompiled_or_missing_root";
                return false;
            }

            foreach (var requiredInput in RequiredInputs)
            {
                if (!blackboard.GetVariable(requiredInput, out _))
                {
                    reason = $"unity_behavior_graph_missing_input:{requiredInput}";
                    return false;
                }
            }

            foreach (var requiredOutput in RequiredOutputs)
            {
                if (!blackboard.GetVariable(requiredOutput, out _))
                {
                    reason = $"unity_behavior_graph_missing_output:{requiredOutput}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public static string[] CopyRequiredInputs()
        {
            return Copy(RequiredInputs);
        }

        public static string[] CopyRequiredOutputs()
        {
            return Copy(RequiredOutputs);
        }

        private static bool ContainsAll(IReadOnlyList<string> values, IReadOnlyList<string> required, out string missing)
        {
            missing = string.Empty;
            if (values == null)
            {
                missing = required.Count > 0 ? required[0] : string.Empty;
                return false;
            }

            foreach (var requiredValue in required)
            {
                var found = false;
                for (var i = 0; i < values.Count; i++)
                {
                    if (string.Equals(values[i], requiredValue, StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    missing = requiredValue;
                    return false;
                }
            }

            return true;
        }

        private static string[] Copy(string[] values)
        {
            var copy = new string[values.Length];
            Array.Copy(values, copy, values.Length);
            return copy;
        }
    }
}
