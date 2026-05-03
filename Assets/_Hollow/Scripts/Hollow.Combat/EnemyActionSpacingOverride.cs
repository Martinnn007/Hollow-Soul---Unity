using System;
using UnityEngine;

namespace Hollow.Combat
{
    [Serializable]
    public sealed class EnemyActionSpacingOverride
    {
        [SerializeField] private string actionId = string.Empty;
        [SerializeField] private float desiredStartDistanceMeters = 1.25f;
        [SerializeField] private float commitRangeMinMeters = 0.25f;
        [SerializeField] private float commitRangeMaxMeters = 1.5f;
        [SerializeField] private float closeToleranceMeters = 0.2f;
        [SerializeField] private float longToleranceMeters = 0.25f;
        [SerializeField] private EnemySpacingRecoveryMode recoveryMovementMode = EnemySpacingRecoveryMode.Planted;
        [SerializeField] private float recoveryMovementDistanceMeters = 0f;
        [SerializeField] private float recoverySpeedMultiplier = 0.6f;
        [SerializeField] private int maxResetCountBeforeCommit = 1;

        public string ActionId => actionId ?? string.Empty;
        public float DesiredStartDistanceMeters => Mathf.Max(0f, desiredStartDistanceMeters);
        public float CommitRangeMinMeters => Mathf.Max(0f, commitRangeMinMeters);
        public float CommitRangeMaxMeters => Mathf.Max(CommitRangeMinMeters, commitRangeMaxMeters);
        public float CloseToleranceMeters => Mathf.Max(0f, closeToleranceMeters);
        public float LongToleranceMeters => Mathf.Max(0f, longToleranceMeters);
        public EnemySpacingRecoveryMode RecoveryMovementMode => recoveryMovementMode;
        public float RecoveryMovementDistanceMeters => Mathf.Max(0f, recoveryMovementDistanceMeters);
        public float RecoverySpeedMultiplier => Mathf.Max(0f, recoverySpeedMultiplier);
        public int MaxResetCountBeforeCommit => Mathf.Max(0, maxResetCountBeforeCommit);

        public void Configure(
            string nextActionId,
            float nextDesiredStartDistanceMeters,
            float nextCommitRangeMinMeters,
            float nextCommitRangeMaxMeters,
            float nextCloseToleranceMeters,
            float nextLongToleranceMeters,
            EnemySpacingRecoveryMode nextRecoveryMovementMode,
            float nextRecoveryMovementDistanceMeters,
            float nextRecoverySpeedMultiplier,
            int nextMaxResetCountBeforeCommit)
        {
            actionId = nextActionId ?? string.Empty;
            desiredStartDistanceMeters = Mathf.Max(0f, nextDesiredStartDistanceMeters);
            commitRangeMinMeters = Mathf.Max(0f, nextCommitRangeMinMeters);
            commitRangeMaxMeters = Mathf.Max(commitRangeMinMeters, nextCommitRangeMaxMeters);
            closeToleranceMeters = Mathf.Max(0f, nextCloseToleranceMeters);
            longToleranceMeters = Mathf.Max(0f, nextLongToleranceMeters);
            recoveryMovementMode = nextRecoveryMovementMode;
            recoveryMovementDistanceMeters = Mathf.Max(0f, nextRecoveryMovementDistanceMeters);
            recoverySpeedMultiplier = Mathf.Max(0f, nextRecoverySpeedMultiplier);
            maxResetCountBeforeCommit = Mathf.Max(0, nextMaxResetCountBeforeCommit);
        }
    }
}
