using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct EnemyUnityBehaviorBlackboard
    {
        public EnemyUnityBehaviorBlackboard(
            EnemyRuntimeController enemy,
            float deltaTime,
            float timeSeconds,
            float distanceToPlayer,
            bool isIdle,
            bool isEndangered,
            EnemyAwarenessState awareness,
            EnemyInstinctDisposition disposition,
            EnemyTacticalRole tacticalRole,
            EnemyPathStatus pathStatus)
        {
            Enemy = enemy;
            Player = enemy != null ? enemy.PlayerGameObject : null;
            DeltaTime = deltaTime;
            TimeSeconds = timeSeconds;
            DistanceToPlayer = distanceToPlayer;
            IsIdle = isIdle;
            IsEndangered = isEndangered;
            Awareness = awareness;
            Disposition = disposition;
            TacticalRole = tacticalRole;
            PathStatus = pathStatus;
        }

        public EnemyRuntimeController Enemy { get; }

        public GameObject Player { get; }

        public float DeltaTime { get; }

        public float TimeSeconds { get; }

        public float DistanceToPlayer { get; }

        public bool IsIdle { get; }

        public bool IsEndangered { get; }

        public EnemyAwarenessState Awareness { get; }

        public EnemyInstinctDisposition Disposition { get; }

        public EnemyTacticalRole TacticalRole { get; }

        public EnemyPathStatus PathStatus { get; }

        public bool IsEngaged => Awareness >= EnemyAwarenessState.Engaged;

        public bool IsAlertedOrBetter => Awareness >= EnemyAwarenessState.Alerted;

        public bool ShouldFlee => IsEndangered || (Disposition == EnemyInstinctDisposition.Prey && DistanceToPlayer <= EnemyRuntimeController.CloseThreatDistanceMeters * 1.45f);

        public static EnemyUnityBehaviorBlackboard FromContext(EnemyBehaviorTreeContext context, EnemyTacticalRole tacticalRole, EnemyPathStatus pathStatus)
        {
            return new EnemyUnityBehaviorBlackboard(
                context.Enemy,
                context.DeltaTime,
                context.TimeSeconds,
                context.DistanceToPlayer,
                context.IsIdle,
                context.IsEndangered,
                context.Awareness,
                context.Disposition,
                tacticalRole,
                pathStatus);
        }
    }
}
