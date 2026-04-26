using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct EnemySpawnRequest
    {
        public EnemySpawnRequest(RoomRuntimeRoot room, Transform parent, GameObject enemyPrefab, PlaceholderPlayerController player, EnemyCatalog catalog, DifficultyTierDefinition difficultyTier, CombatDiagnosticsModel diagnostics)
            : this(room, parent, enemyPrefab, null, player, catalog, difficultyTier, diagnostics, RoomCombatEncounterContext.Empty)
        {
        }

        public EnemySpawnRequest(
            RoomRuntimeRoot room,
            Transform parent,
            GameObject enemyPrefab,
            GameObject enemyProjectilePrefab,
            PlaceholderPlayerController player,
            EnemyCatalog catalog,
            DifficultyTierDefinition difficultyTier,
            CombatDiagnosticsModel diagnostics,
            RoomCombatEncounterContext encounterContext)
        {
            Room = room;
            Parent = parent;
            EnemyPrefab = enemyPrefab;
            EnemyProjectilePrefab = enemyProjectilePrefab;
            Player = player;
            Catalog = catalog;
            DifficultyTier = difficultyTier;
            Diagnostics = diagnostics;
            EncounterContext = encounterContext ?? RoomCombatEncounterContext.Empty;
        }

        public RoomRuntimeRoot Room { get; }

        public Transform Parent { get; }

        public GameObject EnemyPrefab { get; }

        public GameObject EnemyProjectilePrefab { get; }

        public PlaceholderPlayerController Player { get; }

        public EnemyCatalog Catalog { get; }

        public DifficultyTierDefinition DifficultyTier { get; }

        public CombatDiagnosticsModel Diagnostics { get; }

        public RoomCombatEncounterContext EncounterContext { get; }
    }
}
