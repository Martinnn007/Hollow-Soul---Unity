using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct EnemyBehaviorTreeContext
    {
        public EnemyBehaviorTreeContext(
            EnemyRuntimeController enemy,
            float deltaTime,
            float timeSeconds,
            float distanceToPlayer,
            bool isIdle,
            bool isEndangered,
            bool shouldSentinelEngage,
            bool canStartRangedAttack,
            bool canStartChargeAttack)
        {
            Enemy = enemy;
            DeltaTime = deltaTime;
            TimeSeconds = timeSeconds;
            DistanceToPlayer = distanceToPlayer;
            IsIdle = isIdle;
            IsEndangered = isEndangered;
            ShouldSentinelEngage = shouldSentinelEngage;
            CanStartRangedAttack = canStartRangedAttack;
            CanStartChargeAttack = canStartChargeAttack;
        }

        public EnemyRuntimeController Enemy { get; }

        public float DeltaTime { get; }

        public float TimeSeconds { get; }

        public float DistanceToPlayer { get; }

        public bool IsIdle { get; }

        public bool IsEndangered { get; }

        public bool ShouldSentinelEngage { get; }

        public bool CanStartRangedAttack { get; }

        public bool CanStartChargeAttack { get; }

        public EnemyAwarenessState Awareness => Enemy != null ? Enemy.AwarenessState : EnemyAwarenessState.Unaware;

        public EnemyIntelligenceLevel Intelligence => Enemy != null ? Enemy.Intelligence : EnemyIntelligenceLevel.Instinctive;

        public EnemyInstinctDisposition Disposition => Enemy != null ? Enemy.Disposition : EnemyInstinctDisposition.Predator;

        public EnemyBehaviorId BehaviorId => Enemy != null ? Enemy.BehaviorId : EnemyBehaviorId.Chaser;

        public bool IsTooClose => Enemy != null && DistanceToPlayer < Enemy.PreferredRangeMinMeters;

        public bool IsTooFar => Enemy != null && DistanceToPlayer > Enemy.PreferredRangeMaxMeters;

        public bool IsInPreferredRange => Enemy != null &&
                                          DistanceToPlayer >= Enemy.PreferredRangeMinMeters &&
                                          DistanceToPlayer <= Enemy.PreferredRangeMaxMeters;

        public bool CanStartMeleeAction(string actionId)
        {
            return Enemy != null && Enemy.CanStartBehaviorMeleeAction(actionId, TimeSeconds);
        }

        public bool CanStartAreaAction(string actionId)
        {
            return Enemy != null && Enemy.CanStartBehaviorAreaAction(actionId, TimeSeconds);
        }

        public bool CanStartGuardAction(string actionId)
        {
            return Enemy != null && Enemy.CanStartBehaviorGuardAction(actionId, TimeSeconds);
        }

        public bool CanStartCreatureMoveAction(string actionId)
        {
            return Enemy != null && Enemy.CanStartBehaviorCreatureMoveAction(actionId, TimeSeconds);
        }

        public bool CanStartCreatureSignalAction(string actionId)
        {
            return Enemy != null && Enemy.CanStartBehaviorCreatureSignalAction(actionId, TimeSeconds);
        }

        public bool CanStartRangedAction(string actionId)
        {
            return Enemy != null && Enemy.CanStartBehaviorRangedAction(actionId, TimeSeconds);
        }

        public float Deterministic01(string salt)
        {
            var hash = 17;
            var value = salt ?? string.Empty;
            for (var index = 0; index < value.Length; index++)
            {
                hash = hash * 31 + value[index];
            }

            var spawn = Enemy != null ? Enemy.SpawnIndex + 7 : 7;
            var bucket = Mathf.FloorToInt(TimeSeconds * 1.6f);
            hash = hash * 31 + spawn * 97 + bucket * 53;
            return Mathf.Abs(hash % 1000) / 1000f;
        }
    }
}
