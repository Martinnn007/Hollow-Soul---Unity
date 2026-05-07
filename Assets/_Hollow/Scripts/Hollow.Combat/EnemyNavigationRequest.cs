using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct EnemyNavigationRequest
    {
        public EnemyNavigationRequest(
            RoomRuntimeRoot room,
            Vector3 currentLocalPosition,
            Vector3 desiredLocalPosition,
            float radiusMeters,
            EnemyNavigationMode mode,
            EnemyNavigationIntent intent,
            EnemyIntelligenceLevel intelligence,
            bool allowLocalDetour = true,
            bool allowPathfinding = false,
            Vector3? finalGoalLocalPosition = null,
            float timeSeconds = 0f,
            int pathSeed = 0,
            float maxStepDistanceMeters = 0f,
            float pathAgeSeconds = 0f,
            bool hasActionEnvelopeGoal = false,
            Vector3? actionEnvelopeAnchorLocalPosition = null,
            float actionEnvelopeDesiredDistanceMeters = 0f,
            float actionEnvelopeMinDistanceMeters = 0f,
            float actionEnvelopeMaxDistanceMeters = 0f,
            EnemyNavMeshAgentBridge navMeshAgentBridge = null,
            float desiredSpeedMetersPerSecond = 0f)
        {
            Room = room;
            CurrentLocalPosition = currentLocalPosition;
            DesiredLocalPosition = desiredLocalPosition;
            RadiusMeters = Mathf.Max(RoomLocalCollision.MinimumRadiusMeters, radiusMeters);
            Mode = mode;
            Intent = intent;
            Intelligence = intelligence;
            AllowLocalDetour = allowLocalDetour;
            AllowPathfinding = allowPathfinding;
            FinalGoalLocalPosition = finalGoalLocalPosition ?? desiredLocalPosition;
            TimeSeconds = Mathf.Max(0f, timeSeconds);
            PathSeed = pathSeed;
            MaxStepDistanceMeters = Mathf.Max(0f, maxStepDistanceMeters);
            PathAgeSeconds = Mathf.Max(0f, pathAgeSeconds);
            HasActionEnvelopeGoal = hasActionEnvelopeGoal;
            ActionEnvelopeAnchorLocalPosition = actionEnvelopeAnchorLocalPosition ?? FinalGoalLocalPosition;
            ActionEnvelopeDesiredDistanceMeters = Mathf.Max(0f, actionEnvelopeDesiredDistanceMeters);
            ActionEnvelopeMinDistanceMeters = Mathf.Max(0f, actionEnvelopeMinDistanceMeters);
            ActionEnvelopeMaxDistanceMeters = Mathf.Max(ActionEnvelopeMinDistanceMeters, actionEnvelopeMaxDistanceMeters);
            NavMeshAgentBridge = navMeshAgentBridge;
            DesiredSpeedMetersPerSecond = Mathf.Max(0f, desiredSpeedMetersPerSecond);
        }

        public RoomRuntimeRoot Room { get; }

        public Vector3 CurrentLocalPosition { get; }

        public Vector3 DesiredLocalPosition { get; }

        public float RadiusMeters { get; }

        public EnemyNavigationMode Mode { get; }

        public EnemyNavigationIntent Intent { get; }

        public EnemyIntelligenceLevel Intelligence { get; }

        public bool AllowLocalDetour { get; }

        public bool AllowPathfinding { get; }

        public Vector3 FinalGoalLocalPosition { get; }

        public float TimeSeconds { get; }

        public int PathSeed { get; }

        public float MaxStepDistanceMeters { get; }

        public float PathAgeSeconds { get; }

        public bool HasActionEnvelopeGoal { get; }

        public Vector3 ActionEnvelopeAnchorLocalPosition { get; }

        public float ActionEnvelopeDesiredDistanceMeters { get; }

        public float ActionEnvelopeMinDistanceMeters { get; }

        public float ActionEnvelopeMaxDistanceMeters { get; }

        public EnemyNavMeshAgentBridge NavMeshAgentBridge { get; }

        public float DesiredSpeedMetersPerSecond { get; }
    }
}
