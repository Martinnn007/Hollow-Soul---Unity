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
            bool allowLocalDetour = true)
        {
            Room = room;
            CurrentLocalPosition = currentLocalPosition;
            DesiredLocalPosition = desiredLocalPosition;
            RadiusMeters = Mathf.Max(RoomLocalCollision.MinimumRadiusMeters, radiusMeters);
            Mode = mode;
            Intent = intent;
            Intelligence = intelligence;
            AllowLocalDetour = allowLocalDetour;
        }

        public RoomRuntimeRoot Room { get; }

        public Vector3 CurrentLocalPosition { get; }

        public Vector3 DesiredLocalPosition { get; }

        public float RadiusMeters { get; }

        public EnemyNavigationMode Mode { get; }

        public EnemyNavigationIntent Intent { get; }

        public EnemyIntelligenceLevel Intelligence { get; }

        public bool AllowLocalDetour { get; }
    }
}
