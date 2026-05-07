namespace Hollow.Combat
{
    public readonly struct EnemyUnityBehaviorTraceEntry
    {
        public EnemyUnityBehaviorTraceEntry(
            float timeSeconds,
            string graphId,
            EnemyUnityBehaviorPilotKind pilotKind,
            string source,
            EnemyBehaviorCommandKind commandKind,
            string actionId,
            string reason,
            float distanceToPlayer,
            EnemyAwarenessState awareness,
            EnemyPathStatus pathStatus)
        {
            TimeSeconds = timeSeconds;
            GraphId = graphId ?? string.Empty;
            PilotKind = pilotKind;
            Source = source ?? string.Empty;
            CommandKind = commandKind;
            ActionId = actionId ?? string.Empty;
            Reason = reason ?? string.Empty;
            DistanceToPlayer = distanceToPlayer;
            Awareness = awareness;
            PathStatus = pathStatus;
        }

        public float TimeSeconds { get; }

        public string GraphId { get; }

        public EnemyUnityBehaviorPilotKind PilotKind { get; }

        public string Source { get; }

        public EnemyBehaviorCommandKind CommandKind { get; }

        public string ActionId { get; }

        public string Reason { get; }

        public float DistanceToPlayer { get; }

        public EnemyAwarenessState Awareness { get; }

        public EnemyPathStatus PathStatus { get; }

        public string Summary => $"{Source}:{CommandKind}:{ActionId}:{Reason}";
    }
}
