using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class ChaserEnemyController : EnemyRuntimeController
    {
        public const int DefaultHealth = 3;
        public const float DefaultSpeedMetersPerSecond = 1.5f;
        public const int DefaultContactDamage = 1;
        public const float DefaultContactCooldownSeconds = 1f;

        public void Configure(RoomRuntimeRoot room, Hollow.Entities.PlaceholderPlayerController player)
        {
            base.Configure(room, player, EnemyDefinition.CreateRuntimeNormal(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());
        }
    }
}
