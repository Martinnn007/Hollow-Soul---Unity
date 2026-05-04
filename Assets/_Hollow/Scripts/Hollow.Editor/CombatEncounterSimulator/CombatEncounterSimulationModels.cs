using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Editor.CombatEncounterSimulator
{
    public enum CombatEncounterRoomPreset
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Arena = 3,
        ActiveDesignerRoom = 4,
        Custom = 5
    }

    public enum CombatEncounterDifficulty
    {
        Easy = 0,
        Standard = 1,
        Hard = 2,
        StressTest = 3
    }

    public enum CombatEncounterSpawnPattern
    {
        AroundPlayer = 0,
        DoorAmbush = 1,
        Corners = 2,
        ClusteredGroup = 3,
        SpreadPatrol = 4,
        RangedBackline = 5,
        CustomMarkers = 6
    }

    public enum CombatEncounterPressureLane
    {
        Melee = 0,
        Ranged = 1,
        Area = 2,
        Charge = 3
    }

    [Serializable]
    public sealed class CombatEncounterEnemyGroup
    {
        public string spawnKind = "spawnEnemyNormal";
        public int count = 3;
        public CombatEncounterSpawnPattern spawnPattern = CombatEncounterSpawnPattern.SpreadPatrol;
        public bool aiEnabled = true;
        public string notes = string.Empty;

        public CombatEncounterEnemyGroup Clone()
        {
            return new CombatEncounterEnemyGroup
            {
                spawnKind = spawnKind,
                count = Mathf.Max(0, count),
                spawnPattern = spawnPattern,
                aiEnabled = aiEnabled,
                notes = notes ?? string.Empty
            };
        }
    }

    [Serializable]
    public sealed class CombatEncounterScenario
    {
        public string scenarioName = "New Encounter Simulation";
        public CombatEncounterRoomPreset roomPreset = CombatEncounterRoomPreset.Medium;
        public Vector2 customRoomSizeMeters = new(16f, 12f);
        public string playerLoadoutId = "balanced";
        public CombatEncounterDifficulty difficulty = CombatEncounterDifficulty.Standard;
        public float durationSeconds = 30f;
        public float tickSeconds = 0.25f;
        public int seed = 1842;
        public bool usePathfinding = true;
        public float obstacleDensity;
        public bool includeRuntimePressureBudgets = true;
        public List<CombatEncounterEnemyGroup> enemyGroups = new()
        {
            new CombatEncounterEnemyGroup()
        };

        public Vector2 RoomSizeMeters
        {
            get
            {
                return roomPreset switch
                {
                    CombatEncounterRoomPreset.Small => new Vector2(10f, 8f),
                    CombatEncounterRoomPreset.Medium => new Vector2(16f, 12f),
                    CombatEncounterRoomPreset.Large => new Vector2(24f, 16f),
                    CombatEncounterRoomPreset.Arena => new Vector2(28f, 20f),
                    CombatEncounterRoomPreset.ActiveDesignerRoom or CombatEncounterRoomPreset.Custom => new Vector2(
                        Mathf.Clamp(customRoomSizeMeters.x, 6f, 40f),
                        Mathf.Clamp(customRoomSizeMeters.y, 6f, 40f)),
                    _ => new Vector2(16f, 12f)
                };
            }
        }

        public CombatEncounterScenario Clone()
        {
            return new CombatEncounterScenario
            {
                scenarioName = string.IsNullOrWhiteSpace(scenarioName) ? "Encounter Simulation" : scenarioName,
                roomPreset = roomPreset,
                customRoomSizeMeters = customRoomSizeMeters,
                playerLoadoutId = string.IsNullOrWhiteSpace(playerLoadoutId) ? "balanced" : playerLoadoutId,
                difficulty = difficulty,
                durationSeconds = Mathf.Clamp(durationSeconds, 3f, 240f),
                tickSeconds = Mathf.Clamp(tickSeconds, 0.05f, 1f),
                seed = seed,
                usePathfinding = usePathfinding,
                obstacleDensity = Mathf.Clamp01(obstacleDensity),
                includeRuntimePressureBudgets = includeRuntimePressureBudgets,
                enemyGroups = enemyGroups?
                    .Where(group => group != null && group.count > 0 && !string.IsNullOrWhiteSpace(group.spawnKind))
                    .Select(group => group.Clone())
                    .ToList() ?? new List<CombatEncounterEnemyGroup>()
            };
        }
    }

    public sealed class CombatEncounterEntitySnapshot
    {
        public string displayName = string.Empty;
        public string spawnKind = string.Empty;
        public Vector2 position;
        public bool alive;
        public bool attacking;
        public bool stuck;
        public CombatEncounterPressureLane lane;
    }

    public sealed class CombatEncounterFrame
    {
        public float timeSeconds;
        public float playerHealth;
        public int aliveEnemies;
        public float meleePressure;
        public float rangedPressure;
        public float areaPressure;
        public float chargePressure;
        public int pathRequests;
        public int deferredPathRequests;
        public int stuckEnemies;
        public Vector2 playerPosition;
        public List<CombatEncounterEntitySnapshot> entities = new();
    }

    public sealed class CombatEncounterEnemyTypeMetrics
    {
        public string spawnKind = string.Empty;
        public string displayName = string.Empty;
        public int count;
        public int deaths;
        public int attackStarts;
        public int hits;
        public int damageDealt;
        public float stuckSeconds;
        public int pathRequests;
        public int deferredPathRequests;
        public float estimatedPathSolveMs;

        public float HitRate => attackStarts <= 0 ? 0f : hits / (float)attackStarts;

        public float AttacksPerSecond(float durationSeconds)
        {
            return durationSeconds <= 0f ? 0f : attackStarts / durationSeconds;
        }
    }

    public sealed class CombatEncounterSimulationResult
    {
        public CombatEncounterScenario scenario;
        public int seed;
        public float durationSeconds;
        public bool playerSurvived;
        public bool playerDied;
        public float playerFinalHealth;
        public int totalEnemies;
        public int enemyDeaths;
        public int totalAttackStarts;
        public int totalHits;
        public int totalDamageTaken;
        public float peakPressure;
        public float averagePressure;
        public float rangedPressureShare;
        public float areaPressureShare;
        public float pathRequestsPerSecond;
        public float averagePathSolveMs;
        public float maxPathSolveMs;
        public int totalDeferredPathRequests;
        public float stuckSeconds;
        public List<CombatEncounterEnemyTypeMetrics> enemyMetrics = new();
        public List<CombatEncounterFrame> frames = new();
        public List<string> warnings = new();
        public List<string> recommendations = new();
    }

    public sealed class CombatEncounterBatchResult
    {
        public CombatEncounterScenario scenario;
        public List<CombatEncounterSimulationResult> results = new();
        public List<string> recommendations = new();

        public int Runs => results.Count;

        public float SurvivalRate => Runs == 0 ? 0f : results.Count(result => result.playerSurvived) / (float)Runs;

        public float AverageFinalHealth => Runs == 0 ? 0f : results.Average(result => result.playerFinalHealth);

        public float AverageEnemyDeaths => Runs == 0 ? 0f : results.Average(result => result.enemyDeaths);

        public float AverageAttacksPerSecond => Runs == 0 ? 0f : results.Average(result => result.durationSeconds <= 0f ? 0f : result.totalAttackStarts / result.durationSeconds);

        public float AverageDamageTaken => Runs == 0 ? 0f : results.Average(result => result.totalDamageTaken);

        public float AveragePeakPressure => Runs == 0 ? 0f : results.Average(result => result.peakPressure);

        public float AveragePathRequestsPerSecond => Runs == 0 ? 0f : results.Average(result => result.pathRequestsPerSecond);

        public float AveragePathSolveMs => Runs == 0 ? 0f : results.Average(result => result.averagePathSolveMs);

        public float AverageStuckSeconds => Runs == 0 ? 0f : results.Average(result => result.stuckSeconds);
    }
}
