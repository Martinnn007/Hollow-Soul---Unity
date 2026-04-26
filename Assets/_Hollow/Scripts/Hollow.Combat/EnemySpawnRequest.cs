using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct EnemySpawnRequest
    {
        public EnemySpawnRequest(RoomRuntimeRoot room, Transform parent, GameObject enemyPrefab, PlaceholderPlayerController player, EnemyCatalog catalog, DifficultyTierDefinition difficultyTier, CombatDiagnosticsModel diagnostics)
        {
            Room = room;
            Parent = parent;
            EnemyPrefab = enemyPrefab;
            Player = player;
            Catalog = catalog;
            DifficultyTier = difficultyTier;
            Diagnostics = diagnostics;
        }

        public RoomRuntimeRoot Room { get; }

        public Transform Parent { get; }

        public GameObject EnemyPrefab { get; }

        public PlaceholderPlayerController Player { get; }

        public EnemyCatalog Catalog { get; }

        public DifficultyTierDefinition DifficultyTier { get; }

        public CombatDiagnosticsModel Diagnostics { get; }
    }
}
