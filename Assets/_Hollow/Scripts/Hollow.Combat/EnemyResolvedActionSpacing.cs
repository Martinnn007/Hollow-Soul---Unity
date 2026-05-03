using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct EnemyResolvedActionSpacing
    {
        public EnemyResolvedActionSpacing(
            string actionId,
            float desiredStartDistanceMeters,
            float commitRangeMinMeters,
            float commitRangeMaxMeters,
            float closeToleranceMeters,
            float longToleranceMeters,
            float closePressureBias,
            EnemySpacingRecoveryMode recoveryMovementMode,
            float recoveryMovementDistanceMeters,
            float recoverySpeedMultiplier,
            int maxResetCountBeforeCommit,
            bool isFallback)
        {
            ActionId = actionId ?? string.Empty;
            DesiredStartDistanceMeters = Mathf.Max(0f, desiredStartDistanceMeters);
            CommitRangeMinMeters = Mathf.Max(0f, commitRangeMinMeters);
            CommitRangeMaxMeters = Mathf.Max(CommitRangeMinMeters, commitRangeMaxMeters);
            CloseToleranceMeters = Mathf.Max(0f, closeToleranceMeters);
            LongToleranceMeters = Mathf.Max(0f, longToleranceMeters);
            ClosePressureBias = Mathf.Clamp01(closePressureBias);
            RecoveryMovementMode = recoveryMovementMode;
            RecoveryMovementDistanceMeters = Mathf.Max(0f, recoveryMovementDistanceMeters);
            RecoverySpeedMultiplier = Mathf.Max(0f, recoverySpeedMultiplier);
            MaxResetCountBeforeCommit = Mathf.Max(0, maxResetCountBeforeCommit);
            IsFallback = isFallback;
        }

        public string ActionId { get; }
        public float DesiredStartDistanceMeters { get; }
        public float CommitRangeMinMeters { get; }
        public float CommitRangeMaxMeters { get; }
        public float CloseToleranceMeters { get; }
        public float LongToleranceMeters { get; }
        public float ClosePressureBias { get; }
        public EnemySpacingRecoveryMode RecoveryMovementMode { get; }
        public float RecoveryMovementDistanceMeters { get; }
        public float RecoverySpeedMultiplier { get; }
        public int MaxResetCountBeforeCommit { get; }
        public bool IsFallback { get; }

        public bool IsTooClose(float distanceMeters)
        {
            return distanceMeters < Mathf.Max(0f, CommitRangeMinMeters - CloseToleranceMeters);
        }

        public bool IsTooFar(float distanceMeters)
        {
            return distanceMeters > CommitRangeMaxMeters + LongToleranceMeters;
        }

        public bool IsInsideCommitRange(float distanceMeters)
        {
            return distanceMeters >= CommitRangeMinMeters && distanceMeters <= CommitRangeMaxMeters;
        }

        public bool IsInsideEnvelope(float distanceMeters)
        {
            return !IsTooClose(distanceMeters) && !IsTooFar(distanceMeters);
        }

        public float DistanceDelta(float distanceMeters)
        {
            if (IsTooClose(distanceMeters))
            {
                return distanceMeters - CommitRangeMinMeters;
            }

            if (IsTooFar(distanceMeters))
            {
                return distanceMeters - CommitRangeMaxMeters;
            }

            return distanceMeters - DesiredStartDistanceMeters;
        }
    }
}
