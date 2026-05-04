using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct EnemyTacticalIntent
    {
        public EnemyTacticalIntent(
            EnemyTacticalRole role,
            EnemyTacticalCommitPolicy commitPolicy,
            string actionId,
            Vector3 reservedLocalPosition,
            bool hasReservedPosition,
            int activeSlotIndex,
            float score,
            string pressureLane,
            string backend,
            string reason)
        {
            Role = role;
            CommitPolicy = commitPolicy;
            ActionId = actionId ?? string.Empty;
            ReservedLocalPosition = reservedLocalPosition;
            HasReservedPosition = hasReservedPosition;
            ActiveSlotIndex = activeSlotIndex;
            Score = score;
            PressureLane = pressureLane ?? string.Empty;
            Backend = backend ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public EnemyTacticalRole Role { get; }

        public EnemyTacticalCommitPolicy CommitPolicy { get; }

        public string ActionId { get; }

        public Vector3 ReservedLocalPosition { get; }

        public bool HasReservedPosition { get; }

        public int ActiveSlotIndex { get; }

        public float Score { get; }

        public string PressureLane { get; }

        public string Backend { get; }

        public string Reason { get; }

        public bool IsActiveThreat => Role == EnemyTacticalRole.ActiveThreat;

        public string Summary => HasReservedPosition
            ? $"{Role}/{CommitPolicy} slot {ActiveSlotIndex} {ActionId} -> {ReservedLocalPosition.x:0.0},{ReservedLocalPosition.z:0.0} {Reason}"
            : $"{Role}/{CommitPolicy} slot {ActiveSlotIndex} {ActionId} {Reason}";

        public EnemyTacticalIntent WithAction(string actionId)
        {
            return new EnemyTacticalIntent(
                Role,
                CommitPolicy,
                actionId,
                ReservedLocalPosition,
                HasReservedPosition,
                ActiveSlotIndex,
                Score,
                PressureLane,
                Backend,
                Reason);
        }

        public static EnemyTacticalIntent Empty { get; } = new(
            EnemyTacticalRole.None,
            EnemyTacticalCommitPolicy.None,
            string.Empty,
            Vector3.zero,
            false,
            -1,
            0f,
            string.Empty,
            string.Empty,
            "no_tactical_intent");
    }
}
